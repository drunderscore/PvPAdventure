using Microsoft.Xna.Framework;
using PvPAdventure.Common.Spectator;
using PvPAdventure.Common.Spectator.SpectatorMode;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Config.UI;
using Terraria.UI;

namespace PvPAdventure.Common.SpawnSelector;

// Allows spectating teammates and their beds, as well as world spawn and own bed, when dead or spawn selector is open.
[Autoload(Side = ModSide.Client)]
public class SpectateSystem : ModSystem
{
    public static bool MapRestore;
    public static int? HoveredPlayerIndex;
    public static SpawnType HoveringType = SpawnType.None;
    public static bool HoveringTeammatePlayer { get; private set; }

    private static bool wasHovering;
    private static bool hasRestorePos;
    private static Vector2 restoreScreenPos;

    public static void ClearHover()
    {
        HoveringType = SpawnType.None;
        HoveredPlayerIndex = null;
        HoveringTeammatePlayer = false;
    }

    public static void TrySetHover(SpawnType type, int idx)
    {
        HoveringTeammatePlayer = false;

        if (type == SpawnType.MyBed || type == SpawnType.MyPortal)
        {
            HoveringType = type;
            HoveredPlayerIndex = idx;
            return;
        }

        if (type == SpawnType.TeammateBed || type == SpawnType.TeammatePortal)
        {
            HoveringType = type;
            HoveredPlayerIndex = idx;
            return;
        }

        HoveringType = type;
        HoveredPlayerIndex = idx;
    }

    public static void TrySetTeammatePlayerHover(int idx)
    {
        if (!IsValidTeammatePlayer(idx))
        {
            ClearTeammatePlayerHoverIfMatch(idx);
            return;
        }

        HoveringType = SpawnType.None;
        HoveredPlayerIndex = idx;
        HoveringTeammatePlayer = true;
    }

    public static void ClearHoverIfMatch(SpawnType type, int idx)
    {
        if (HoveringType == type && HoveredPlayerIndex == idx)
            ClearHover();
    }

    public static void ClearTeammatePlayerHoverIfMatch(int idx)
    {
        if (HoveringTeammatePlayer && HoveredPlayerIndex == idx)
            ClearHover();
    }

    private static bool IsValidBed(int idx)
    {
        if (idx < 0 || idx >= Main.maxPlayers)
            return false;

        Player p = Main.player[idx];
        if (p == null || !p.active)
            return false;

        return p.SpawnX >= 0 &&
               p.SpawnY >= 0 &&
               Player.CheckSpawn(p.SpawnX, p.SpawnY);
    }

    private static bool IsValidTeammatePlayer(int idx)
    {
        if (idx < 0 || idx >= Main.maxPlayers)
            return false;

        Player local = Main.LocalPlayer;
        Player p = Main.player[idx];

        if (local == null || !local.active || p == null || !p.active)
            return false;

        if (idx == local.whoAmI)
            return true;

        return local.team != 0 && p.team == local.team;
    }

    public override void ModifyScreenPosition()
    {
        Player local = Main.LocalPlayer;
        if (local == null || !local.active)
            return;

        if (SpectatorModeSystem.IsInSpectateMode(local))
        {
            Restore();
            return;
        }

        if (IsAnyConfigUIOpen())
        {
            Restore();
            return;
        }

        if (local.GetModPlayer<SpawnPlayer>().IsTeleportOnCooldown)
        {
            Restore();
            return;
        }


        if (!local.dead && !SpawnSystem.Enabled)
        {
            Restore();
            return;
        }

        ValidateHover();

        bool hovering = HoveringTeammatePlayer || HoveringType != SpawnType.None;

        HandleHoverTransitions(local, hovering);

        if (!hovering)
            return;

        ApplyCamera();
    }

    private static bool IsAnyConfigUIOpen()
    {
        UIState s = Main.InGameUI?._currentState;
        return s is UIModConfig || s is UIModConfigList || Main.ingameOptionsWindow;
    }

    private static void ValidateHover()
    {
        if (HoveringTeammatePlayer && !IsValidTeammatePlayer(HoveredPlayerIndex ?? -1))
            ClearHover();

        if (HoveringType == SpawnType.TeammateBed && !IsValidBed(HoveredPlayerIndex ?? -1))
            ClearHover();

        if (HoveringType == SpawnType.MyPortal && !PortalSystem.HasPortal(Main.LocalPlayer))
            ClearHover();

        if (HoveringType == SpawnType.TeammatePortal && !SpawnPlayer.IsValidTeammatePortalIndex(Main.LocalPlayer, HoveredPlayerIndex ?? -1))
            ClearHover();
    }

    private static void HandleHoverTransitions(Player local, bool hovering)
    {
        if (hovering && !wasHovering)
        {
            restoreScreenPos =
                local.Center -
                new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;

            hasRestorePos = true;

            if (Main.mapFullscreen)
            {
                Main.mapFullscreen = false;
                MapRestore = true;
            }
        }

        if (!hovering && wasHovering)
            Restore();

        wasHovering = hovering;
    }

    private static void Restore()
    {
        if (MapRestore)
        {
            Main.mapFullscreen = true;
            MapRestore = false;
        }

        if (hasRestorePos)
        {
            SpectateCameraFade.SetScreenPosition(restoreScreenPos);
            hasRestorePos = false;
        }

        ClearHover();
        wasHovering = false;
    }

    private static void ApplyCamera()
    {
        // Spectate teammate player.
        if (HoveringTeammatePlayer && HoveredPlayerIndex is int teammateIdx)
        {
            Player p = Main.player[teammateIdx];
            if (p == null || !p.active)
                return;

            SetCameraTo(p.Center);
            return;
        }

        // Always spectate world spawn when hovering world spawn.
        if (HoveringType == SpawnType.World)
        {
            Vector2 pos = new Vector2(Main.spawnTileX, Main.spawnTileY - 3).ToWorldCoordinates();
            SetCameraTo(pos);
            return;
        }

        // Spectate my bed.
        if (HoveringType == SpawnType.MyBed)
        {
            Player me = Main.LocalPlayer;
            Vector2 myBedPos = new Vector2(me.SpawnX, me.SpawnY - 3).ToWorldCoordinates();
            SetCameraTo(myBedPos);
            return;
        }

        // Spectate my portal.
        if (HoveringType == SpawnType.MyPortal)
        {
            if (!PortalSystem.TryGetPortalWorldPos(Main.LocalPlayer, out Vector2 portalPos))
                return;

            SetCameraTo(portalPos);
            return;
        }

        // Spectate teammate portal.
        if (HoveringType == SpawnType.TeammatePortal && HoveredPlayerIndex is int idx)
        {
            Player p = Main.player[idx];
            if (p == null || !p.active)
                return;

            if (!SpawnPlayer.TryGetPortalWorldPos(p, out Vector2 portalPos))
                return;

            SetCameraTo(portalPos);
            return;
        }

        // Spectate teammate bed.
        if (HoveredPlayerIndex is int playerIdx)
        {
            Player p = Main.player[playerIdx];
            if (p == null || !p.active)
                return;

            if (HoveringType == SpawnType.TeammateBed)
            {
                Vector2 teammateBedPos = new Vector2(p.SpawnX, p.SpawnY - 3).ToWorldCoordinates();
                SetCameraTo(teammateBedPos);
                return;
            }
        }
    }

    private static void SetCameraTo(Vector2 worldPosition)
    {
        SpectateCameraFade.SetScreenPosition(worldPosition - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f);
    }
}
