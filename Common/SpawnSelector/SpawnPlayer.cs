using Microsoft.Xna.Framework;
using PvPAdventure.Common.GameTimer;
using PvPAdventure.Common.Spawnbox;
using PvPAdventure.Common.SpawnSelector.Net;
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

    private bool cachedInSpawnRegion;
    private int spawnRegionCooldown;
    private Point lastRegionTile = new(int.MinValue, int.MinValue);

    private Point ownBedTileCached = new(-1, -1);
    private bool ownBedValidCached;
    private int ownBedValidCooldown;

    private Point rawSpawnCached = new(-1, -1);
    private bool rawSpawnValidCached;
    private int rawSpawnValidCooldown;

    public SpawnType SelectedType { get; private set; } = SpawnType.None;
    public int SelectedPlayerIndex { get; private set; } = -1;

    private SpawnType lastSelectedType = SpawnType.None;
    private int lastSelectedPlayerIndex = -1;

    public bool ExecuteRequested { get; private set; }

    public bool SpawnedPortalThisUse;
    public bool AdventureMirrorHadCountdownThisUse;

    #region Portal
    private bool hasPortal;
    private Vector2 portalWorldPos;
    private int portalHealth;

    public void SetPortal(Vector2 worldPos, bool sync = true)
    {
        bool replacing = hasPortal;
        Vector2 oldPos = portalWorldPos;
        int oldHealth = portalHealth;

        hasPortal = true;
        portalWorldPos = worldPos;
        portalHealth = PortalSystem.PortalMaxHealth;

        Log.Debug($"[Portal] set {Player.name} replace={replacing} hp={oldHealth}->{portalHealth} pos={oldPos}->{worldPos}");

        if (sync)
            SyncPortal();
    }

    public void ClearPortal(bool sync = true)
    {
        if (!hasPortal && portalWorldPos == default)
            return;

        Log.Debug($"[Portal] clear {Player.name} hp={portalHealth}");

        hasPortal = false;
        portalWorldPos = default;
        portalHealth = 0;

        if (SelectedType == SpawnType.MyPortal || SelectedType == SpawnType.TeammatePortal)
            ClearSelection();

        if (sync)
            SyncPortal();
    }

    internal void ApplyPortalFromNet(bool hasPortal, Vector2 worldPos, int health)
    {
        Log.Debug($"[Portal] net {Player.name} has={hasPortal} hp={health}");

        this.hasPortal = hasPortal;
        portalWorldPos = hasPortal ? worldPos : default;
        portalHealth = hasPortal ? Utils.Clamp(health, 1, PortalSystem.PortalMaxHealth) : 0;

        if (Main.netMode == NetmodeID.Server || hasPortal)
            return;

        SpawnPlayer local = Main.LocalPlayer?.GetModPlayer<SpawnPlayer>();
        if (local != null &&
            local.SelectedType == SpawnType.TeammatePortal &&
            local.SelectedPlayerIndex == Player.whoAmI)
        {
            local.ClearSelection();
        }
    }

    internal bool DamagePortal(Player attacker, int damage, string source)
    {
        if (!hasPortal)
            return false;

        damage = Utils.Clamp(damage, 1, PortalSystem.PortalMaxHealth);
        int oldHealth = portalHealth;
        portalHealth -= damage;

        string attackerName = attacker?.name ?? "<unknown>";
        Log.Debug($"[Portal] hit {Player.name} by {attackerName} {oldHealth}->{portalHealth} ({source})");

        if (portalHealth <= 0)
        {
            Log.Chat($"{Player.name}'s portal died to {attackerName}");
            PortalFxNetHandler.Send(portalWorldPos, killed: true, damage);
            ClearPortal();
            return true;
        }

        PortalFxNetHandler.Send(portalWorldPos, killed: false, damage);
        SyncPortal();
        return true;
    }

    public static bool HasPortal(Player player)
    {
        return player != null && player.active && player.GetModPlayer<SpawnPlayer>().hasPortal;
    }

    public static bool TryGetPortalWorldPos(Player player, out Vector2 worldPos)
    {
        worldPos = default;
        if (player == null || !player.active)
            return false;

        SpawnPlayer sp = player.GetModPlayer<SpawnPlayer>();
        if (!sp.hasPortal)
            return false;

        worldPos = sp.portalWorldPos;
        return true;
    }

    public static bool TryGetPortal(Player player, out Vector2 worldPos, out int health)
    {
        worldPos = default;
        health = 0;

        if (player == null || !player.active)
            return false;

        SpawnPlayer sp = player.GetModPlayer<SpawnPlayer>();
        if (!sp.hasPortal)
            return false;

        worldPos = sp.portalWorldPos;
        health = sp.portalHealth;
        return true;
    }
    #endregion

    public void RequestExecute()
    {
        ExecuteRequested = true;
    }

    public void ClearExecuteRequest()
    {
        ExecuteRequested = false;
    }

    public bool IsPlayerInSpawnRegion()
    {
        Point tilePos = Player.Center.ToTileCoordinates();

        if (spawnRegionCooldown-- > 0 && tilePos == lastRegionTile)
            return cachedInSpawnRegion;

        spawnRegionCooldown = 10;
        lastRegionTile = tilePos;
        cachedInSpawnRegion = ComputeIsPlayerInSpawnRegion(tilePos);
        return cachedInSpawnRegion;
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

        if (Player.dead && Player.respawnTimer == 2)
            Player.respawnTimer = 1;

        SendSelectionIfNeeded();
    }

    internal void ApplySelectionFromNet(SpawnType type, int idx)
    {
        NormalizeSelection(type, idx, out SpawnType newType, out int newIdx);
        SetSelection(newType, newIdx);

        if (Player.dead && Player.respawnTimer == 2)
            Player.respawnTimer = 1;
    }

    public override void PostUpdate()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            UpdatePlayerSpawnpoint();
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

        if (SelectedType == SpawnType.None)
        {
            Player.respawnTimer = 2;

            if (Player.whoAmI == Main.myPlayer)
                SpawnSystem.SetCanTeleport(true);

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

        if (Player.whoAmI == Main.myPlayer)
        {
            PortalSystem.ClearPortal(Player);
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
            bool ok = Player.SpawnX >= 0 && Player.SpawnY >= 0;
            if (ok)
                ok = Player.CheckSpawn(Player.SpawnX, Player.SpawnY);

            if (!ok)
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
        if (requester == null || !requester.active)
            return false;

        if (idx < 0 || idx >= Main.maxPlayers)
            return false;

        Player bedOwner = Main.player[idx];
        if (bedOwner == null || !bedOwner.active)
            return false;

        if (bedOwner.SpawnX < 0 || bedOwner.SpawnY < 0)
            return false;

        if (!Player.CheckSpawn(bedOwner.SpawnX, bedOwner.SpawnY))
            return false;

        if (idx == requester.whoAmI)
            return true;

        if (requester.team == 0 || bedOwner.team != requester.team)
            return false;

        return true;
    }

    internal static bool IsValidTeammatePortalIndex(Player requester, int idx)
    {
        if (requester == null || !requester.active)
            return false;

        if (idx < 0 || idx >= Main.maxPlayers)
            return false;

        Player portalOwner = Main.player[idx];
        if (portalOwner == null || !portalOwner.active)
            return false;

        if (!HasPortal(portalOwner))
            return false;

        if (idx == requester.whoAmI)
            return true;

        if (requester.team == 0 || portalOwner.team != requester.team)
            return false;

        return true;
    }

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

    private void SyncPortal()
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
            return;

        if (Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI != Main.myPlayer)
            return;

        PlayerPortalNetHandler.Send(Player.whoAmI, hasPortal, portalWorldPos, portalHealth);
    }

    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
    {
        if (!hasPortal)
            return;

        PlayerPortalNetHandler.Send(Player.whoAmI, hasPortal, portalWorldPos, portalHealth, toWho, fromWho);
    }

    private void UpdatePlayerSpawnpoint()
    {
        Point raw = new(Player.SpawnX, Player.SpawnY);

        if (raw != rawSpawnCached)
        {
            rawSpawnCached = raw;
            rawSpawnValidCooldown = 0;
        }

        if (rawSpawnValidCooldown-- <= 0)
        {
            rawSpawnValidCached = raw.X >= 0 && raw.Y >= 0 && Player.CheckSpawn(raw.X, raw.Y);
            rawSpawnValidCooldown = 60;
        }

        Point current = rawSpawnValidCached ? raw : new Point(-1, -1);
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

        // Check if player is within my own bed tile pos
        if (Player.SpawnX >= 0 && Player.SpawnY >= 0)
        {
            Vector2 bedWorld = new Vector2(Player.SpawnX * 16f+1, Player.SpawnY * 16f);
            if (Vector2.DistanceSquared(bedWorld, Player.Center) <= radiusSq)
            {
                Point bedTile = new Point(Player.SpawnX, Player.SpawnY);

                if (bedTile != ownBedTileCached)
                {
                    ownBedTileCached = bedTile;
                    ownBedValidCooldown = 0;
                }

                if (ownBedValidCooldown-- <= 0)
                {
                    ownBedValidCached = Player.CheckSpawn(bedTile.X, bedTile.Y);
                    ownBedValidCooldown = 30;
                }

                if (ownBedValidCached)
                    return true;
            }
        }

        // Check if player is within a teammate bed tile pos
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player other = Main.player[i];
            if (other == null || !other.active || other.whoAmI == Player.whoAmI)
                continue;

            if (other.team == 0 || other.team != Player.team)
                continue;

            if (other.SpawnX < 0 || other.SpawnY < 0)
                continue;

            Vector2 bedWorld = new Vector2(other.SpawnX * 16f, other.SpawnY * 16f);
            if (Vector2.DistanceSquared(bedWorld, Player.Center) > radiusSq)
                continue;

            if (Player.CheckSpawn(other.SpawnX, other.SpawnY))
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

    private void ClearLastSelection()
    {
        lastSelectedType = SpawnType.None;
        lastSelectedPlayerIndex = -1;
    }

    #region Helpers
    private static string FormatSpawn(SpawnType type, int idx)
    {
        if (type == SpawnType.TeammateBed)
            return $"Bed ({GetPlayerNameSafe(idx)})";

        if (type == SpawnType.TeammatePortal)
            return $"Portal ({GetPlayerNameSafe(idx)})";

        return type.ToString();
    }

    private static string GetPlayerNameSafe(int idx)
    {
        if (idx < 0 || idx >= Main.maxPlayers)
            return "<unknown>";

        Player p = Main.player[idx];
        return p?.name ?? "<unknown>";
    }
    #endregion
}
