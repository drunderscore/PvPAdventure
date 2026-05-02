using Microsoft.Xna.Framework;
using PvPAdventure.Common.Spectator.SpectatorMode;
using PvPAdventure.Common.TeammateSpectator;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spectator;

[Autoload(Side = ModSide.Client)]
public class SpectatorTargetSystem : ModSystem
{
    private const int FullSyncIntervalTicks = 15;
    private const float GhostFollowSpeed = 5f;
    private const float FollowTargetDistance = 16f;
    private const float FollowVerticalOffset = 0f;
    private const float FollowTargetLerp = 0.45f;
    private const float CameraFollowLerp = 0.28f;
    private const float CameraSnapDistance = 2200f;

    private static int target = -1;
    private static int npcTarget = -1;
    private static int previewTarget = -1;
    private static int cameraTarget = -1;
    private static bool hasCameraCenter;
    private static Vector2 smoothedCameraCenter;
    private static int followTargetKey = -1;
    private static int netSyncTicks;
    private static bool hasSmoothedFollowCenter;
    private static Vector2 smoothedFollowCenter;
    #region Targeting
    private static bool CanTarget(int playerId)
    {
        return playerId >= 0 &&
            playerId < Main.maxPlayers &&
            playerId != Main.myPlayer &&
            Main.player[playerId].active &&
            (SpectatorModeSystem.IsInPlayerMode(Main.player[playerId]) || SpectatorModeSystem.IsInSpectateMode(Main.player[playerId]) || Main.player[playerId].ghost);
    }

    private static bool CanTargetNPC(int npcId)
    {
        return npcId >= 0 &&
            npcId < Main.maxNPCs &&
            Main.npc[npcId]?.active == true;
    }

    public static void SetPlayerTarget(int slot, bool preserveAutoDirector = false)
    {
        if (!preserveAutoDirector)
            AutoDirectorSystem.Enabled = false;

        int next = CanTarget(slot) ? slot : -1;

        target = next;
        npcTarget = -1;
        ResetFollowState();

        if (CanTarget(target))
            SnapLocalPlayerNear(Main.player[target]);
    }

    public static void SetNPCTarget(int slot, bool preserveAutoDirector = false)
    {
        if (!preserveAutoDirector)
            AutoDirectorSystem.Enabled = false;

        int next = CanTargetNPC(slot) ? slot : -1;

        npcTarget = next;
        target = -1;
        previewTarget = -1;
        ResetFollowState();

        if (CanTargetNPC(npcTarget))
            SnapLocalPlayerNear(Main.npc[npcTarget]);
    }

    public static void TogglePlayerTarget(int slot)
    {
        if (target == slot)
        {
            ClearTarget();
            return;
        }

        SetPlayerTarget(slot);
    }

    public static void ToggleNPCTarget(int slot)
    {
        if (npcTarget == slot)
        {
            ClearTarget();
            return;
        }

        SetNPCTarget(slot);
    }

    public static void SetPreviewTarget(int slot)
    {
        previewTarget = CanTarget(slot) ? slot : -1;
    }

    public static void ClearPreviewTarget()
    {
        previewTarget = -1;
    }

    public static List<int> GetTargets(int exclude = -1)
    {
        List<int> targets = [];

        for (int i = 0; i < Main.maxPlayers; i++)
            if (CanTarget(i) && i != exclude)
                targets.Add(i);

        return targets;
    }

    public static void ClearTarget(bool preserveAutoDirector = false, bool moveCameraToLocal = true)
    {
        if (!preserveAutoDirector)
            AutoDirectorSystem.Enabled = false;

        if (target == -1 && npcTarget == -1)
            return;

        bool previewStillOwnsCamera = CanTarget(previewTarget);

        if (!previewStillOwnsCamera && moveCameraToLocal)
        {
            Player local = Main.LocalPlayer;
            if (local?.active == true)
            {
                Vector2 screenPosition = local.Center - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
                SpectateCameraFade.SetScreenPosition(screenPosition, allowFade: true);
            }
        }

        if (!previewStillOwnsCamera)
            cameraTarget = -1;

        target = -1;
        npcTarget = -1;
        ResetFollowState();
    }

    public static bool IsTargeting(Player player) => player?.active == true && GetPlayerTarget()?.whoAmI == player.whoAmI;
    public static bool IsLockedTargeting(Player player) => player?.active == true && CanTarget(target) && target == player.whoAmI;
    public static bool IsLockedTargeting(NPC npc) => npc?.active == true && CanTargetNPC(npcTarget) && npcTarget == npc.whoAmI;

    public static Player GetPlayerTarget()
    {
        if (!SpectatorModeSystem.IsInSpectateMode(Main.LocalPlayer))
            return null;

        if (CanTarget(previewTarget))
            return Main.player[previewTarget];

        if (CanTarget(target))
            return Main.player[target];

        return null;
    }

    public static Player GetLockedPlayerTarget()
    {
        if (!SpectatorModeSystem.IsInSpectateMode(Main.LocalPlayer) || !CanTarget(target))
            return null;

        return Main.player[target];
    }

    public static NPC GetLockedNPCTarget()
    {
        if (!SpectatorModeSystem.IsInSpectateMode(Main.LocalPlayer) || !CanTargetNPC(npcTarget))
            return null;

        return Main.npc[npcTarget];
    }

    public static string GetLockedTargetStatusText()
    {
        if (GetLockedNPCTarget() is NPC npc)
            return $"Spectating \"{npc.FullName}\"";

        if (GetLockedPlayerTarget() is Player player)
            return $"Spectating {player.name}";

        return null;
    }
    #endregion

    #region Hooks
    public override void ModifyScreenPosition()
    {
        if (TryGetCameraTarget(out Vector2 targetCenter, out int cameraId))
        {
            bool targetChanged = cameraTarget != cameraId;
            Vector2 cameraCenter = GetSmoothedCameraCenter(targetCenter, targetChanged);
            Vector2 screenPosition = ClampScreenPosition(cameraCenter - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f);

            SpectateCameraFade.SetScreenPosition(screenPosition, targetChanged);
            cameraTarget = cameraId;
            return;
        }

        cameraTarget = -1;
        hasCameraCenter = false;
    }

    public override void PostUpdatePlayers()
    {
        Player targetPlayer = GetLockedPlayerTarget();
        if (targetPlayer?.active == true)
        {
            FollowLockedTarget(targetPlayer);
            return;
        }

        NPC targetNPC = GetLockedNPCTarget();
        if (targetNPC?.active == true)
        {
            FollowLockedTarget(targetNPC);
            return;
        }

        ResetFollowState();
    }

    private static void FollowLockedTarget(Player targetPlayer)
    {
        Player local = Main.LocalPlayer;
        if (local?.active != true || targetPlayer?.active != true || targetPlayer.whoAmI == local.whoAmI)
            return;

        FollowCenter(local, targetPlayer.Center, targetPlayer.velocity, targetPlayer.direction, targetPlayer.whoAmI);
    }

    private static void SnapLocalPlayerNear(Player targetPlayer)
    {
        Player local = Main.LocalPlayer;
        if (local?.active != true || targetPlayer?.active != true || targetPlayer.whoAmI == local.whoAmI)
            return;

        SnapLocalPlayerNear(targetPlayer.Center, targetPlayer.velocity, targetPlayer.direction, targetPlayer.whoAmI);
    }

    private static void FollowLockedTarget(NPC targetNPC)
    {
        Player local = Main.LocalPlayer;
        if (local?.active != true || targetNPC?.active != true)
            return;

        FollowCenter(local, targetNPC.Center, targetNPC.velocity, GetNPCDirection(targetNPC), 1000 + targetNPC.whoAmI);
    }

    private static void SnapLocalPlayerNear(NPC targetNPC)
    {
        if (targetNPC?.active != true)
            return;

        SnapLocalPlayerNear(targetNPC.Center, targetNPC.velocity, GetNPCDirection(targetNPC), 1000 + targetNPC.whoAmI);
    }

    private static void FollowCenter(Player local, Vector2 targetCenter, Vector2 targetVelocity, int direction, int targetKey)
    {
        Vector2 desiredCenter = GetFollowCenter(targetCenter, targetVelocity, direction);
        bool targetChanged = followTargetKey != targetKey;
        bool shouldSnap = targetChanged || !hasSmoothedFollowCenter;

        followTargetKey = targetKey;
        ApplyFollowDirection(local, direction);

        if (shouldSnap)
        {
            smoothedFollowCenter = desiredCenter;
            hasSmoothedFollowCenter = true;
            local.Center = desiredCenter;
        }
        else
        {
            smoothedFollowCenter = Vector2.Lerp(smoothedFollowCenter, desiredCenter, FollowTargetLerp);
            MoveLocalPlayerToward(local, smoothedFollowCenter);
        }

        local.fallStart = (int)(local.position.Y / 16f);

        SyncLocalPlayerPosition(forceFullSync: false);
    }

    private static void SnapLocalPlayerNear(Vector2 targetCenter, Vector2 targetVelocity, int direction, int targetKey)
    {
        Player local = Main.LocalPlayer;
        if (local?.active != true)
            return;

        Vector2 desiredCenter = GetFollowCenter(targetCenter, targetVelocity, direction);

        followTargetKey = targetKey;
        smoothedFollowCenter = desiredCenter;
        hasSmoothedFollowCenter = true;
        ApplyFollowDirection(local, direction);
        local.Center = desiredCenter;
        local.velocity = Vector2.Zero;
        local.fallStart = (int)(local.position.Y / 16f);

        SyncLocalPlayerPosition(forceFullSync: true);
    }

    private static void ResetFollowState()
    {
        followTargetKey = -1;
        hasSmoothedFollowCenter = false;
        netSyncTicks = 0;
    }

    private static void SyncLocalPlayerPosition(bool forceFullSync)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        Player local = Main.LocalPlayer;

        NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, local.whoAmI);

        if (forceFullSync || ++netSyncTicks >= FullSyncIntervalTicks)
        {
            netSyncTicks = 0;
            NetMessage.SendData(MessageID.SyncPlayer, -1, -1, null, local.whoAmI);
        }
    }

    private static void MoveLocalPlayerToward(Player local, Vector2 destination)
    {
        Vector2 oldCenter = local.Center;
        Vector2 delta = destination - oldCenter;

        if (delta.LengthSquared() <= GhostFollowSpeed * GhostFollowSpeed)
            local.Center = destination;
        else
            local.Center = oldCenter + Vector2.Normalize(delta) * GhostFollowSpeed;

        local.velocity = Vector2.Zero;
    }

    private static void ApplyFollowDirection(Player local, int direction)
    {
        int normalizedDirection = direction < 0 ? -1 : 1;

        local.direction = normalizedDirection;
        local.ghostDir = normalizedDirection;
    }

    private static int GetNPCDirection(NPC npc)
    {
        if (npc.spriteDirection != 0)
            return npc.spriteDirection;

        return npc.direction;
    }

    private static Vector2 GetFollowCenter(Vector2 center, Vector2 velocity, int direction)
    {
        float horizontalDirection = MathHelper.Distance(velocity.X, 0f) > 0.1f
            ? velocity.X < 0f ? -1f : 1f
            : direction == 0 ? 1f : direction;
        Vector2 offset = new(-horizontalDirection * FollowTargetDistance, FollowVerticalOffset);

        return center + offset;
    }

    private static bool TryGetCameraTarget(out Vector2 targetCenter, out int cameraId)
    {
        Player lockedPlayer = GetLockedPlayerTarget();
        if (lockedPlayer?.active == true)
        {
            targetCenter = lockedPlayer.Center;
            cameraId = lockedPlayer.whoAmI;
            return true;
        }

        NPC lockedNPC = GetLockedNPCTarget();
        if (lockedNPC?.active == true)
        {
            targetCenter = lockedNPC.Center;
            cameraId = 1000 + lockedNPC.whoAmI;
            return true;
        }

        if (CanTarget(previewTarget))
        {
            Player previewPlayer = Main.player[previewTarget];
            targetCenter = previewPlayer.Center;
            cameraId = 2000 + previewPlayer.whoAmI;
            return true;
        }

        targetCenter = Vector2.Zero;
        cameraId = -1;
        return false;
    }

    private static Vector2 GetSmoothedCameraCenter(Vector2 targetCenter, bool targetChanged)
    {
        bool shouldSnap = targetChanged ||
            !hasCameraCenter ||
            Vector2.DistanceSquared(smoothedCameraCenter, targetCenter) > CameraSnapDistance * CameraSnapDistance;

        if (shouldSnap)
        {
            smoothedCameraCenter = targetCenter;
            hasCameraCenter = true;
            return smoothedCameraCenter;
        }

        smoothedCameraCenter = Vector2.Lerp(smoothedCameraCenter, targetCenter, CameraFollowLerp);
        return smoothedCameraCenter;
    }

    private static Vector2 ClampScreenPosition(Vector2 screenPosition)
    {
        float maxX = System.Math.Max(0f, Main.maxTilesX * 16f - Main.screenWidth);
        float maxY = System.Math.Max(0f, Main.maxTilesY * 16f - Main.screenHeight);

        screenPosition.X = MathHelper.Clamp(screenPosition.X, 0f, maxX);
        screenPosition.Y = MathHelper.Clamp(screenPosition.Y, 0f, maxY);

        return screenPosition;
    }

    #endregion
}
