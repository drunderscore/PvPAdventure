using PvPAdventure.System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using static PvPAdventure.Core.SpawnAndSpectate.SpawnSystem;

namespace PvPAdventure.Core.SpawnAndSpectate;

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

    public bool HasSelection => SelectedType != SpawnType.None;

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

        if (Player.whoAmI == Main.myPlayer)
            SpawnSystem.SetCanTeleport(false);
    }

    public void ToggleSelection(SpawnType type, int playerIndex = -1)
    {
        SpawnType prevType = SelectedType;
        int prevIdx = SelectedPlayerIndex;

        bool same =
            SelectedType == type &&
            ((type != SpawnType.TeammateBed && type != SpawnType.TeammateBed) ||
             SelectedPlayerIndex == playerIndex);

        if (same)
        {
            if (Player.whoAmI == Main.myPlayer)
                Log.Chat("Cancel spawn: " + FormatSpawn(prevType, prevIdx));

            ClearSelection();
            SendSelectionIfNeeded();
            return;
        }

        if (type == SpawnType.Teammate &&
            !SpawnSystem.IsValidTeammateIndex(playerIndex))
        {
            type = SpawnType.None;
            playerIndex = -1;
        }

        if (type == SpawnType.TeammateBed)
        {
            bool ok = playerIndex == Player.whoAmI;

            if (!ok)
            {
                if (Player.team == 0 || playerIndex < 0 || playerIndex >= Main.maxPlayers)
                {
                    ok = false;
                }
                else
                {
                    Player bedOwner = Main.player[playerIndex];
                    ok = bedOwner != null && bedOwner.active && bedOwner.team == Player.team;
                }
            }

            if (!ok)
            {
                type = SpawnType.None;
                playerIndex = -1;
            }
        }

        SelectedType = type;

        SelectedPlayerIndex =
            (type == SpawnType.Teammate || type == SpawnType.TeammateBed)
                ? playerIndex
                : -1;

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

    private static string FormatSpawn(SpawnType type, int idx)
    {
        return type switch
        {
            SpawnType.Teammate => $"Player ({Main.player[idx].name})",
            SpawnType.TeammateBed => $"Bed ({Main.player[idx].name})",
            _ => type.ToString()
        };
    }

    private void SendSelectionIfNeeded()
    {
        if (Main.netMode != Terraria.ID.NetmodeID.MultiplayerClient)
            return;

        if (Player.whoAmI != Main.myPlayer)
            return;

        var packet = Mod.GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.SpawnSelection);
        packet.Write((byte)SelectedType);
        packet.Write((short)SelectedPlayerIndex); // -1 for none
        packet.Send();
    }

    internal void ApplySelectionFromNet(SpawnType type, int idx)
    {
        if (type == SpawnType.Teammate && !SpawnSystem.IsValidTeammateIndex(idx))
            type = SpawnType.None;

        if (type == SpawnType.TeammateBed)
        {
            bool ok = idx == Player.whoAmI;

            if (!ok)
            {
                if (Player.team == 0 || idx < 0 || idx >= Main.maxPlayers)
                {
                    ok = false;
                }
                else
                {
                    Player bedOwner = Main.player[idx];
                    ok = bedOwner != null && bedOwner.active && bedOwner.team == Player.team;
                }
            }

            if (!ok)
                type = SpawnType.None;
        }

        SelectedType = type;
        SelectedPlayerIndex =
            (type == SpawnType.Teammate || type == SpawnType.TeammateBed)
                ? idx
                : -1;

        if (Player.dead && Player.respawnTimer == 2)
            Player.respawnTimer = 1;
    }

    public bool IsPlayerInWorldSpawnRegion(Point tilePos)
    {
        var regionManager = ModContent.GetInstance<RegionManager>();
        if (regionManager.GetRegionContaining(tilePos) != null)
            return true;
        return false;
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

    public override void PostUpdate()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            UpdatePlayerSpawnpoint();
    }

    // Keeps the respawn timer at 1 to allow for selection
    public override void UpdateDead()
    {
        if (Player.respawnTimer > 2)
        {
            base.UpdateDead();
            return;
        }

        bool hasSelection = SelectedType != SpawnType.None;

        if (!hasSelection)
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

    public override void OnHurt(Player.HurtInfo info)
    {
        base.OnHurt(info);

        if (Player.whoAmI != Main.LocalPlayer.whoAmI)
            return;

        var cfg = ModContent.GetInstance<AdventureClientConfig>();
        if (cfg.CloseMapOnHurt && Main.mapFullscreen)
            Main.mapFullscreen = false;
    }

    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        base.Kill(damage, hitDirection, pvp, damageSource);

        if (Player.whoAmI != Main.myPlayer)
            return;

        var cfg = ModContent.GetInstance<AdventureClientConfig>();
        if (!cfg.AutoSelectWorldSpawnOnDeath)
            return;

        if (SelectedType == SpawnType.None)
            ToggleSelection(SpawnType.World);
    }
}
