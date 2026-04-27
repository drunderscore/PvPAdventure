using Microsoft.Xna.Framework;
using PvPAdventure.Common.GameTimer;
using PvPAdventure.Common.Spawnbox;
using PvPAdventure.Common.SpawnSelector.Net;
using PvPAdventure.Content.Items;
using PvPAdventure.Core.Config;
using PvPAdventure.Core.Net;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.SpawnSelector;

public class SpawnPlayer : ModPlayer
{
    private Point lastSpawn = new(-1, -1);

    private bool isInSpawnRegion;
    private Point spawnRegionTile = new(int.MinValue, int.MinValue);
    private ulong nextSpawnRegionCheckTick;
    private ulong nextBedSyncTick;

    public SpawnType SelectedType { get; private set; } = SpawnType.None;
    public int SelectedPlayerIndex { get; private set; } = -1;

    private SpawnType lastSelectedType = SpawnType.None;
    private int lastSelectedPlayerIndex = -1;

    public bool ExecuteRequested { get; private set; }

    public bool SpawnedPortalThisUse;
    public bool AdventureMirrorHadCountdownThisUse;
    public bool AdventureMirrorCountdownStartedThisUse;
    public int AdventureMirrorTicksLeft;
    public int AdventureMirrorElapsedTicks;
    public Vector2 AdventureMirrorPortalWorld;
    public int TeleportCooldownTicks { get; private set; }
    public bool IsTeleportOnCooldown => TeleportCooldownTicks > 0;
    public int TeleportCooldownSecondsLeft => (TeleportCooldownTicks + 59) / 60;

    #region Portal
    public static bool HasPortal(Player player) => PortalSystem.HasPortal(player);

    public static bool TryGetPortalWorldPos(Player player, out Vector2 worldPos)
    {
        return TryGetPortal(player, out worldPos, out _);
    }

    public static bool TryGetPortal(Player player, out Vector2 worldPos, out int health)
    {
        return TryGetPortal(player, out worldPos, out health, out _);
    }

    public static bool TryGetPortal(Player player, out Vector2 worldPos, out int health, out int createTicksRemaining)
    {
        return TryGetPortal(player, out worldPos, out health, out createTicksRemaining, out _);
    }

    public static bool TryGetPortal(Player player, out Vector2 worldPos, out int health, out int createTicksRemaining, out int maxHealth)
    {
        return PortalSystem.TryGetPortal(player, out worldPos, out health, out createTicksRemaining, out maxHealth);
    }
    #endregion

    public void RequestExecute() => ExecuteRequested = true;

    public void ClearExecuteRequest() => ExecuteRequested = false;

    public bool CanTeleportNow() => TeleportCooldownTicks <= 0;

    public void StartTeleportCooldown() =>
        TeleportCooldownTicks = ModContent.GetInstance<ServerConfig>().SpawnTeleportCooldownSeconds * 60;

    internal void StartAdventureMirrorUse()
    {
        SpawnedPortalThisUse = false;
        AdventureMirrorHadCountdownThisUse = true;
        AdventureMirrorCountdownStartedThisUse = true;
        AdventureMirrorTicksLeft = AdventureMirror.GetRecallFrames();
        AdventureMirrorElapsedTicks = 0;
        AdventureMirrorPortalWorld = Player.Bottom;
        Log.Chat($"[Mirror] Start adventure mirror use: player={Player.name}, recallTicks={AdventureMirrorTicksLeft}, pos={AdventureMirrorPortalWorld}, netMode={Main.netMode}");
    }

    internal void ResetAdventureMirrorUse()
    {
        SpawnedPortalThisUse = false;
        AdventureMirrorHadCountdownThisUse = false;
        AdventureMirrorCountdownStartedThisUse = false;
        AdventureMirrorTicksLeft = 0;
        AdventureMirrorElapsedTicks = 0;
        AdventureMirrorPortalWorld = Vector2.Zero;
    }

    internal void InvalidateSpawnRegionCache() => nextSpawnRegionCheckTick = 0;

    internal static void InvalidateSpawnRegionCaches()
    {
        for (int i = 0; i < Main.maxPlayers; i++)
            if (Main.player[i] is { active: true } player)
                player.GetModPlayer<SpawnPlayer>().InvalidateSpawnRegionCache();
    }

    public bool IsPlayerInSpawnRegion()
    {
        Point tilePos = Player.Center.ToTileCoordinates();

        if (tilePos == spawnRegionTile && Main.GameUpdateCount < nextSpawnRegionCheckTick)
            return isInSpawnRegion;

        spawnRegionTile = tilePos;
        nextSpawnRegionCheckTick = Main.GameUpdateCount + 10;
        return isInSpawnRegion = ComputeIsPlayerInSpawnRegion(tilePos);
    }

    public void ClearSelection()
    {
        SelectedType = SpawnType.None;
        SelectedPlayerIndex = -1;

        ClearExecuteRequest();

        if (Player.whoAmI == Main.myPlayer)
            SpawnSystem.SetCanTeleport(false);
    }

    public void ToggleSelection(SpawnType type, int playerIndex = -1)
    {
        SpawnType prevType = SelectedType;
        int prevIdx = SelectedPlayerIndex;

        NormalizeSelection(type, playerIndex, out SpawnType newType, out int newIdx);

        bool same = prevType == newType;
        if (same)
        {
            if (newType == SpawnType.TeammateBed || newType == SpawnType.TeammatePortal)
                same = prevIdx == newIdx;
        }

        if (same)
        {
            if (Player.whoAmI == Main.myPlayer)
                Log.Chat("Cancel spawn: " + FormatSpawn(prevType, prevIdx));

            ClearSelection();
            ClearLastSelection();
            SendSelectionIfNeeded();
            return;
        }

        if (newType == SpawnType.None && prevType != SpawnType.None)
        {
            if (Player.whoAmI == Main.myPlayer)
                Log.Chat("Cancel spawn: " + FormatSpawn(prevType, prevIdx));

            ClearSelection();
            ClearLastSelection();
            SendSelectionIfNeeded();
            return;
        }

        SetSelection(newType, newIdx);

        if (Player.whoAmI == Main.myPlayer)
        {
            if (SelectedType == SpawnType.None)
            {
                if (prevType != SpawnType.None)
                    Log.Chat("Cancel spawn: " + FormatSpawn(prevType, prevIdx));
            }
            else
            {
                Log.Chat("Selected spawn: " + FormatSpawn(SelectedType, SelectedPlayerIndex));
            }
        }

        if (Player.dead && Player.respawnTimer == 2 && CanTeleportNow())
            Player.respawnTimer = 1;

        SendSelectionIfNeeded();
    }

    internal void ApplySelectionFromNet(SpawnType type, int idx)
    {
        NormalizeSelection(type, idx, out SpawnType newType, out int newIdx);
        SetSelection(newType, newIdx);

        if (Player.dead && Player.respawnTimer == 2 && CanTeleportNow())
            Player.respawnTimer = 1;
    }

    public override void PostUpdate()
    {
        if (TeleportCooldownTicks > 0)
            TeleportCooldownTicks--;

        UpdateAdventureMirrorUse();

        if (Main.netMode == NetmodeID.MultiplayerClient)
            UpdatePlayerSpawnpoint();
    }

    private void UpdateAdventureMirrorUse()
    {
        if (!AdventureMirrorCountdownStartedThisUse)
            return;

        if (Player.dead || Player.ghost || Player.HeldItem?.ModItem is not AdventureMirror)
        {
            Log.Chat($"[Mirror] Failed to create portal: cancelled before finish, player={Player.name}, dead={Player.dead}, ghost={Player.ghost}, held={Player.HeldItem?.Name ?? "<null>"}, elapsedTicks={AdventureMirrorElapsedTicks}, ticksLeft={AdventureMirrorTicksLeft}");
            AdventureMirror.ResetUseState(Player);
            return;
        }

        if (Player.velocity.LengthSquared() > 0f)
        {
            if (Player.whoAmI == Main.myPlayer)
                AdventureMirror.Warning(Player, "Mods.PvPAdventure.AdventureMirror.Cancelled");

            Log.Chat($"[Mirror] Failed to create portal: cancelled by movement, player={Player.name}, velocity={Player.velocity}, elapsedTicks={AdventureMirrorElapsedTicks}, ticksLeft={AdventureMirrorTicksLeft}");
            AdventureMirror.ResetUseState(Player);
            return;
        }

        if (AdventureMirrorTicksLeft > 0)
        {
            AdventureMirrorTicksLeft--;
            AdventureMirrorElapsedTicks++;
            return;
        }

        if (SpawnedPortalThisUse)
        {
            Log.Chat($"[Mirror] End ignored: already spawned this use, player={Player.name}, elapsedTicks={AdventureMirrorElapsedTicks}, ticksLeft={AdventureMirrorTicksLeft}");
            return;
        }

        SpawnedPortalThisUse = true;
        Log.Chat($"[Mirror] End adventure mirror use: player={Player.name}, elapsedTicks={AdventureMirrorElapsedTicks}, ticksLeft={AdventureMirrorTicksLeft}, selectedType={SelectedType}, pos={AdventureMirrorPortalWorld}, netMode={Main.netMode}");

        if (SelectedType != SpawnType.None)
        {
            if (Player.whoAmI == Main.myPlayer)
            {
                RequestExecute();
                SpawnSystem.TryExecuteSelection(Player, this);
            }

            AdventureMirror.ResetUseState(Player);
            return;
        }

        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            PlayerPortalNetHandler.SendFinishMirrorUse(Player, AdventureMirrorPortalWorld, AdventureMirrorElapsedTicks, AdventureMirrorTicksLeft);
            Log.Chat($"[Mirror] Portal create request sent: player={Player.name}, elapsedTicks={AdventureMirrorElapsedTicks}, ticksLeft={AdventureMirrorTicksLeft}, pos={AdventureMirrorPortalWorld}");
        }
        else
        {
            bool success = PortalSystem.TryCreatePortalAtPosition(Player, AdventureMirrorPortalWorld, out string reason);
            Log.Chat(success
                ? $"Successfully created portal: player={Player.name}, elapsedTicks={AdventureMirrorElapsedTicks}, ticksLeft={AdventureMirrorTicksLeft}, reason={reason}"
                : $"Failed to create portal: player={Player.name}, elapsedTicks={AdventureMirrorElapsedTicks}, ticksLeft={AdventureMirrorTicksLeft}, reason={reason}");
        }

        AdventureMirror.ResetUseState(Player);
    }

    public override void UpdateDead()
    {
        if (ModContent.GetInstance<GameManager>().CurrentPhase == GameManager.Phase.Waiting)
        {
            base.UpdateDead();
            return;
        }

        if (Player.respawnTimer > 2)
        {
            base.UpdateDead();
            return;
        }

        bool canTeleportNow = CanTeleportNow();
        if (SelectedType == SpawnType.None || !canTeleportNow)
        {
            Player.respawnTimer = 2;

            if (Player.whoAmI == Main.myPlayer)
                SpawnSystem.SetCanTeleport(SelectedType == SpawnType.None && canTeleportNow);

            return;
        }

        if (Player.respawnTimer == 2)
            Player.respawnTimer = 1;

        if (Player.whoAmI == Main.myPlayer)
            SpawnSystem.SetCanTeleport(false);

        base.UpdateDead();
    }

    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        base.Kill(damage, hitDirection, pvp, damageSource);

        if (Main.netMode != NetmodeID.MultiplayerClient || Player.whoAmI == Main.myPlayer)
            PortalSystem.ClearPortal(Player);

        if (Player.whoAmI == Main.myPlayer)
        {
            TryAutoSelectLatestSelection();
        }
    }

    public bool TryAutoSelectLatestSelection()
    {
        if (Player.whoAmI != Main.myPlayer)
            return false;

        if (!Player.dead && IsPlayerInSpawnRegion()) // Do NOT auto-select latest while in a spawn region (prevents instant execution).
            return false;

        if (SelectedType != SpawnType.None)
            return false;

        var config = ModContent.GetInstance<ClientConfig>();
        if (!config.AutoSelectLatestSpawnOption)
            return false;

        NormalizeSelection(lastSelectedType, lastSelectedPlayerIndex, out SpawnType type, out int idx);
        if (type == SpawnType.None)
            return false;

        SetSelection(type, idx);

        Log.Chat($"Auto-selected latest option: {FormatSpawn(SelectedType, SelectedPlayerIndex)} for player: {Player.name}");

        SendSelectionIfNeeded();
        return true;
    }

    private void SetSelection(SpawnType type, int idx)
    {
        SelectedType = type;

        if (type == SpawnType.TeammateBed || type == SpawnType.TeammatePortal)
            SelectedPlayerIndex = idx;
        else
            SelectedPlayerIndex = -1;

        if (SelectedType != SpawnType.None)
        {
            lastSelectedType = SelectedType;
            lastSelectedPlayerIndex = SelectedPlayerIndex;
        }
    }

    private void NormalizeSelection(SpawnType requestedType, int requestedIdx, out SpawnType normalizedType, out int normalizedIdx)
    {
        normalizedType = requestedType;
        normalizedIdx = requestedIdx;

        if (normalizedType == SpawnType.None)
        {
            normalizedIdx = -1;
            return;
        }

        if (normalizedType == SpawnType.World || normalizedType == SpawnType.Random)
        {
            normalizedIdx = -1;
            return;
        }

        if (normalizedType == SpawnType.MyBed)
        {
            if (!IsOwnSpawnValid(Player))
                normalizedType = SpawnType.None;

            normalizedIdx = -1;
            return;
        }

        if (normalizedType == SpawnType.MyPortal)
        {
            bool ok = PortalSystem.HasPortal(Player);
            if (!ok)
                normalizedType = SpawnType.None;

            normalizedIdx = -1;
            return;
        }

        if (normalizedType == SpawnType.TeammateBed)
        {
            if (!IsValidTeammateBedIndex(Player, normalizedIdx))
            {
                normalizedType = SpawnType.None;
                normalizedIdx = -1;
            }

            return;
        }

        if (normalizedType == SpawnType.TeammatePortal)
        {
            if (!IsValidTeammatePortalIndex(Player, normalizedIdx))
            {
                normalizedType = SpawnType.None;
                normalizedIdx = -1;
            }

            return;
        }

        normalizedType = SpawnType.None;
        normalizedIdx = -1;
    }

    private static bool IsValidTeammateBedIndex(Player requester, int idx)
    {
        return requester?.active == true &&
               TryGetActivePlayer(idx, out Player bedOwner) &&
               IsOwnSpawnValid(bedOwner) &&
               IsSelfOrTeammate(requester, bedOwner);
    }

    internal static bool IsValidTeammatePortalIndex(Player requester, int idx)
    {
        return requester?.active == true &&
               TryGetActivePlayer(idx, out Player portalOwner) &&
               HasPortal(portalOwner) &&
               IsSelfOrTeammate(requester, portalOwner);
    }

    private static bool TryGetActivePlayer(int idx, out Player player)
    {
        player = idx >= 0 && idx < Main.maxPlayers ? Main.player[idx] : null;
        return player?.active == true;
    }

    private static bool IsOwnSpawnValid(Player player) =>
        player.SpawnX >= 0 && player.SpawnY >= 0 && Player.CheckSpawn(player.SpawnX, player.SpawnY);

    private static bool IsSelfOrTeammate(Player requester, Player owner) =>
        requester.whoAmI == owner.whoAmI || requester.team != 0 && owner.team == requester.team;

    private void SendSelectionIfNeeded()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        if (Player.whoAmI != Main.myPlayer)
            return;

        var packet = Mod.GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.SpawnSelection);
        packet.Write((byte)SelectedType);
        packet.Write((short)SelectedPlayerIndex);
        packet.Send();
    }

    private void UpdatePlayerSpawnpoint()
    {
        if (Main.GameUpdateCount < nextBedSyncTick)
            return;

        nextBedSyncTick = Main.GameUpdateCount + 60;

        Point raw = new(Player.SpawnX, Player.SpawnY);
        Point current = IsOwnSpawnValid(Player) ? raw : new Point(-1, -1);
        if (current == lastSpawn)
            return;

        Point prev = lastSpawn; // for logging
        lastSpawn = current;

        var packet = Mod.GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.PlayerBed);
        packet.Write((byte)Player.whoAmI);
        packet.Write(current.X);
        packet.Write(current.Y);
        packet.Send();

        if (Player.whoAmI == Main.myPlayer)
            Log.Chat($"Bed changed: ({prev.X},{prev.Y}) -> ({current.X},{current.Y})");
    }

    private bool ComputeIsPlayerInSpawnRegion(Point tilePos)
    {
        // Check if player is in spawnbox
        var regionManager = ModContent.GetInstance<RegionManager>();
        if (regionManager.GetRegionContaining(tilePos) != null)
            return true;

        const float radiusWorld = 8f * 16f;
        const float radiusSq = radiusWorld * radiusWorld;

        if (IsBedInRange(Player, radiusSq))
            return true;

        // Check if player is within a teammate bed tile pos
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player other = Main.player[i];
            if (other == null || !other.active || other.whoAmI == Player.whoAmI)
                continue;

            if (other.team == 0 || other.team != Player.team)
                continue;

            if (IsBedInRange(other, radiusSq))
                return true;
        }

        // Check if player is within my portal world pos
        if (PortalSystem.TryGetPortalWorldPos(Player, out Vector2 portalWorld) && Vector2.DistanceSquared(portalWorld, Player.Center) <= radiusSq)
            return true;

        // Check if player is within a teammate's portal world pos
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player other = Main.player[i];
            if (other == null || !other.active || other.whoAmI == Player.whoAmI)
                continue;

            if (other.team == 0 || other.team != Player.team)
                continue;

            if (!PortalSystem.TryGetPortalWorldPos(other, out Vector2 teammatePortalWorld))
                continue;

            if (Vector2.DistanceSquared(teammatePortalWorld, Player.Center) <= radiusSq)
                return true;
        }

        return false;
    }

    private void ClearLastSelection() => (lastSelectedType, lastSelectedPlayerIndex) = (SpawnType.None, -1);

    #region Helpers
    private bool IsBedInRange(Player owner, float radiusSq) =>
        IsOwnSpawnValid(owner) &&
        Vector2.DistanceSquared(new Vector2(owner.SpawnX * 16f, owner.SpawnY * 16f), Player.Center) <= radiusSq;

    private static string FormatSpawn(SpawnType type, int idx) => type switch
    {
        SpawnType.TeammateBed => $"Bed ({GetPlayerNameSafe(idx)})",
        SpawnType.TeammatePortal => $"Portal ({GetPlayerNameSafe(idx)})",
        _ => type.ToString()
    };

    private static string GetPlayerNameSafe(int idx) =>
        idx >= 0 && idx < Main.maxPlayers ? Main.player[idx]?.name ?? "<unknown>" : "<unknown>";
    #endregion
}
