using Microsoft.Xna.Framework;
using PvPAdventure.Content.Items;
using PvPAdventure.System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Core.SpawnAndSpectate;

/// <summary>
/// Mnaging spawn region detection and spawn point syncing for players.
/// </summary>
public class SpawnPointPlayer : ModPlayer
{
    // Spawn point last sent to server, updated when changed
    private Point _lastSpawn = new(-1, -1);

    private bool _cachedInSpawnRegion;
    private int _spawnRegionCooldown;
    private Point _lastRegionTile = new(int.MinValue, int.MinValue);

    private Point _ownBedTileCached = new(-1, -1);
    private bool _ownBedValidCached;
    private int _ownBedValidCooldown;

    private Point _rawSpawnCached = new(-1, -1);
    private bool _rawSpawnValidCached;
    private int _rawSpawnValidCooldown;

    public override void OnHurt(Player.HurtInfo info)
    {
        base.OnHurt(info);

        // Only care if the player is currently using the AdventureMirror
        if (Player.itemTime > 0 &&
            Player.HeldItem?.type == ModContent.ItemType<AdventureMirror>())
        {
            if (Player.HeldItem.ModItem is AdventureMirror mirror)
            {
                mirror.CancelItemUse(Player);
            }

            // Show hurt popup to indicate cancellation
            //if (Player.whoAmI == Main.myPlayer)
            //{
            //    PopupText.NewText(new AdvancedPopupRequest
            //    {
            //        Color = Color.Crimson,
            //        Text = Language.GetTextValue("Mods.PvPAdventure.AdventureMirror.Cancelled"),
            //        Velocity = new(0f, -4),
            //        DurationInFrames = 120
            //    }, Player.Top + new Vector2(0, -4));
            //}
        }
    }

    public override void PostUpdate()
    {
        if (Main.dedServ || Player.whoAmI != Main.myPlayer)
            return;

        if (Main.netMode == NetmodeID.MultiplayerClient)
            UpdatePlayerSpawnpoint();

        bool inSpawnRegion = IsPlayerInSpawnRegionCached();

        if (ModContent.GetInstance<GameManager>().CurrentPhase == GameManager.Phase.Playing && inSpawnRegion)
            SpawnAndSpectateSystem.SetEnabled(true);
        else
            SpawnAndSpectateSystem.SetEnabled(false);
    }
    public bool IsPlayerInSpawnRegion() => IsPlayerInSpawnRegionCached();

    private bool IsPlayerInSpawnRegionCached()
    {
        Point tilePos = Player.Center.ToTileCoordinates();

        if (_spawnRegionCooldown-- > 0 && tilePos == _lastRegionTile)
            return _cachedInSpawnRegion;

        _spawnRegionCooldown = 10; // re-check ~6 times/sec
        _lastRegionTile = tilePos;
        _cachedInSpawnRegion = ComputeIsPlayerInSpawnRegion(tilePos);
        return _cachedInSpawnRegion;
    }

    private bool ComputeIsPlayerInSpawnRegion(Point tilePos)
    {
        // World spawn region via RegionManager
        var regionManager = ModContent.GetInstance<RegionManager>();
        if (regionManager.GetRegionContaining(tilePos) != null)
            return true;

        const float radiusWorld = 25f * 16f;
        const float radiusSq = radiusWorld * radiusWorld;

        // Own bed: distance first, validate only occasionally
        if (Player.SpawnX >= 0 && Player.SpawnY >= 0)
        {
            Vector2 bedWorld = new(Player.SpawnX * 16f, Player.SpawnY * 16f);
            if (Vector2.DistanceSquared(bedWorld, Player.Center) <= radiusSq)
            {
                Point bedTile = new(Player.SpawnX, Player.SpawnY);

                if (bedTile != _ownBedTileCached)
                {
                    _ownBedTileCached = bedTile;
                    _ownBedValidCooldown = 0; // force recheck
                }

                if (_ownBedValidCooldown-- <= 0)
                {
                    _ownBedValidCached = Player.CheckSpawn(bedTile.X, bedTile.Y);
                    _ownBedValidCooldown = 30; // recheck twice/sec while near bed
                }

                if (_ownBedValidCached)
                    return true;
            }
        }

        // Teammates: distance first, only then expensive validation
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player other = Main.player[i];
            if (other == null || !other.active || other.whoAmI == Player.whoAmI)
                continue;

            if (other.team == 0 || other.team != Player.team)
                continue;

            if (other.SpawnX < 0 || other.SpawnY < 0)
                continue;

            Vector2 bedWorld = new(other.SpawnX * 16f, other.SpawnY * 16f);
            if (Vector2.DistanceSquared(bedWorld, Player.Center) > radiusSq)
                continue;

            if (!Player.CheckSpawn(other.SpawnX, other.SpawnY))
                continue;

            return true;
        }

        return false;
    }

    private static bool HasValidBedSpawn(Player player)
    {
        // No bed set at all
        if (player.SpawnX < 0 || player.SpawnY < 0)
            return false;

        if (!Player.CheckSpawn(player.SpawnX, player.SpawnY))
        {
            return false;
        }

        return true;
    }

    // Update the player's bed spawnpoint to the server
    private void UpdatePlayerSpawnpoint()
    {
        Point raw = new(Player.SpawnX, Player.SpawnY);

        if (raw != _rawSpawnCached)
        {
            _rawSpawnCached = raw;
            _rawSpawnValidCooldown = 0; // force recheck on change
        }

        if (_rawSpawnValidCooldown-- <= 0)
        {
            _rawSpawnValidCached = raw.X >= 0 && raw.Y >= 0 && Player.CheckSpawn(raw.X, raw.Y);
            _rawSpawnValidCooldown = 60; // once/sec
        }

        Point current = _rawSpawnValidCached ? raw : new Point(-1, -1);

        if (current == _lastSpawn)
            return;

        _lastSpawn = current;

        var packet = Mod.GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.PlayerBed);
        packet.Write((byte)Player.whoAmI);
        packet.Write(current.X);
        packet.Write(current.Y);
        packet.Send();
    }
}
