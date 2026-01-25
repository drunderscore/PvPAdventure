using Microsoft.Xna.Framework;
using PvPAdventure.Common.GameTimer;
using PvPAdventure.Common.Spawnbox;
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
            if (newType == SpawnType.Teammate || newType == SpawnType.TeammateBed)
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
    public void RestoreLastSelection()
    {
        if (lastSelectedType == SpawnType.None)
            return;

        ToggleSelection(lastSelectedType, lastSelectedPlayerIndex);
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
            TryAutoSelectLatestSelection();
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

        if (type == SpawnType.Teammate || type == SpawnType.TeammateBed)
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

        if (normalizedType == SpawnType.Teammate)
        {
            if (!SpawnSystem.IsValidTeammateIndex(Player, normalizedIdx))
            {
                normalizedType = SpawnType.None;
                normalizedIdx = -1;
            }

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

    private static string FormatSpawn(SpawnType type, int idx)
    {
        if (type == SpawnType.Teammate)
            return $"Player ({GetPlayerNameSafe(idx)})";

        if (type == SpawnType.TeammateBed)
            return $"Bed ({GetPlayerNameSafe(idx)})";

        return type.ToString();
    }

    private static string GetPlayerNameSafe(int idx)
    {
        if (idx < 0 || idx >= Main.maxPlayers)
            return "<unknown>";

        Player p = Main.player[idx];
        return p?.name ?? "<unknown>";
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

        lastSpawn = current;

        var packet = Mod.GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.PlayerBed);
        packet.Write((byte)Player.whoAmI);
        packet.Write(current.X);
        packet.Write(current.Y);
        packet.Send();
    }

    private bool ComputeIsPlayerInSpawnRegion(Point tilePos)
    {
        var regionManager = ModContent.GetInstance<RegionManager>();
        if (regionManager.GetRegionContaining(tilePos) != null)
            return true;

        const float radiusWorld = 10f * 16f;
        const float radiusSq = radiusWorld * radiusWorld;

        if (Player.SpawnX >= 0 && Player.SpawnY >= 0)
        {
            Vector2 bedWorld = new Vector2(Player.SpawnX * 16f, Player.SpawnY * 16f);
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

        return false;
    }

    private void ClearLastSelection()
    {
        lastSelectedType = SpawnType.None;
        lastSelectedPlayerIndex = -1;
    }
}
