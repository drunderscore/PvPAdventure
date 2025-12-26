using Microsoft.Xna.Framework;
using PvPAdventure.Content.Items;
using PvPAdventure.System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static PvPAdventure.Core.SpawnAndSpectate.SpawnAndSpectateSystem;

namespace PvPAdventure.Core.SpawnAndSpectate;

/// <summary>
/// Player-specific logic for managing spawn region detection.
/// Also manages spawn selection features in the mod.
/// </summary>
public class SpawnAndSpectatePlayer : ModPlayer
{
    // Spawn point last sent to server
    private Point _lastSpawn = new(-1, -1);

    internal enum CommitRespawnType : byte
    {
        Random,
        Teammate
    }

    private bool _respawnCommitted;
    private CommitRespawnType _commitType;
    private int _commitTargetIndex = -1;

    private bool TryApplyTeammateSpawn(int targetIndex)
    {
        if (targetIndex < 0 || targetIndex >= Main.maxPlayers)
            return false;

        Player target = Main.player[targetIndex];
        if (target == null || !target.active || target.dead)
            return false;

        Vector2 pos = target.Center + new Vector2(48f, 0f);
        Player.SpawnX = (int)(pos.X / 16f);
        Player.SpawnY = (int)(pos.Y / 16f);
        return true;
    }

    private void CommitRespawn(CommitRespawnType type, int targetIndex)
    {
        _respawnCommitted = true;
        _commitType = type;
        _commitTargetIndex = targetIndex;

        if (type == CommitRespawnType.Teammate)
            TryApplyTeammateSpawn(targetIndex);

        Player.respawnTimer = 1;

        if (Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer)
        {
            ModPacket p = Mod.GetPacket();
            p.Write((byte)AdventurePacketIdentifier.SpawnAndSpectateCommitRespawn);
            p.Write((byte)type);
            p.Write(targetIndex);
            p.Send();
        }
    }

    internal void ReceiveCommitRespawnFromNet(CommitRespawnType type, int targetIndex)
    {
        _respawnCommitted = true;
        _commitType = type;
        _commitTargetIndex = targetIndex;

        if (type == CommitRespawnType.Teammate)
            TryApplyTeammateSpawn(targetIndex);

        Player.respawnTimer = 1;
    }

    public void CommitRandomRespawn()
    {
        CommitRespawn(CommitRespawnType.Random, -1);
    }

    public void CommitTeammateRespawn(int targetIndex)
    {
        CommitRespawn(CommitRespawnType.Teammate, targetIndex);
    }

    public override void UpdateDead()
    {
        if (_respawnCommitted)
        {
            return;
        }

        if (Player.respawnTimer <= 2)
        {
            if (SpawnAndSpectateSystem.SelectedSpawnPlayerIndex is int idx)
            {
                CommitRespawn(CommitRespawnType.Teammate, idx);
                return;
            }

            Player.respawnTimer = 2;

            if (!Main.dedServ && Player.whoAmI == Main.myPlayer)
            {
                SpawnAndSpectateSystem.SetMode(SpawnSpectateMode.SpawnSelect);
            }
        }
    }

    public override void OnRespawn()
    {
        if (!Main.dedServ && Player.whoAmI == Main.myPlayer)
        {
            SpawnAndSpectateSystem.ClearSpectate();
            SpawnAndSpectateSystem.HoveredPlayerIndex = null;
            SpawnAndSpectateSystem.SelectedSpawnPlayerIndex = null;
            SpawnAndSpectateSystem.SetMode(SpawnSpectateMode.None);
        }

        if (!_respawnCommitted)
            return;

        if (_commitType == CommitRespawnType.Random)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient) // server or SP
                Player.TeleportationPotion();
        }

        _respawnCommitted = false;
        _commitTargetIndex = -1;
    }

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

    public bool IsPlayerInSpawnRegion()
    {
        // Is player in the world spawn region?
        var regionManager = ModContent.GetInstance<RegionManager>();
        Point tilePos = Player.Center.ToTileCoordinates();
        var region = regionManager.GetRegionContaining(tilePos);
        if (region != null)
        {
            return true;
        }

        // Is player in their own bed spawn region?
        if (HasValidBedSpawn(Player))
        {
            Vector2 bedSpawnPoint = new(Player.SpawnX, Player.SpawnY);
            float distanceToBedSpawn = Vector2.Distance(bedSpawnPoint * 16f, Player.Center);
            if (distanceToBedSpawn <= 25 * 16f)
            {
                return true;
            }
        }

        // Is player in any bed spawn region?
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player other = Main.player[i];

            if (other == null || !other.active || other.whoAmI == Player.whoAmI)
                continue;

            // Only care about same team and non-zero team
            if (other.team == 0 || other.team != Player.team)
                continue;

            // No bed set for this teammate
            if (other.SpawnX == -1 || other.SpawnY == -1)
                continue;

            // Ensure bed is valid
            if (!HasValidBedSpawn(other))
                continue;

            Vector2 teammateBedTile = new Vector2(other.SpawnX, other.SpawnY);
            float distanceToTeammateBed = Vector2.Distance(teammateBedTile * 16f, Player.Center);

            if (distanceToTeammateBed <= 25*16f)
            {
                return true;
            }
        }
        return false;
    }

    public override void PostUpdate()
    {
        if (Main.dedServ || Player.whoAmI != Main.myPlayer)
            return;

        if (_respawnCommitted)
        {
            SpawnAndSpectateSystem.SetMode(SpawnSpectateMode.None);
            return;
        }

        bool playing = ModContent.GetInstance<GameManager>().CurrentPhase == GameManager.Phase.Playing;

        if (playing)
        {
            if (Player.dead)
            {
                if (Player.respawnTimer <= 1)
                    SpawnAndSpectateSystem.SetMode(SpawnSpectateMode.SpawnSelect);
                else
                    SpawnAndSpectateSystem.SetMode(SpawnSpectateMode.Spectate);
            }
            else if (IsPlayerInSpawnRegion())
            {
                SpawnAndSpectateSystem.SetMode(SpawnSpectateMode.SpawnSelect);
            }
            else
            {
                SpawnAndSpectateSystem.SetMode(SpawnSpectateMode.None);
            }
        }

        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            UpdatePlayerSpawnpoint();
        }
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
        Point current;

        // No bed set at all
        if (Player.SpawnX == -1 || Player.SpawnY == -1)
        {
            current = new Point(-1, -1);
        }
        // Bed set, but housing / tiles now invalid -> treat as no bed for the mod
        else if (!Player.CheckSpawn(Player.SpawnX, Player.SpawnY))
        {
            current = new Point(-1, -1);
        }
        else
        {
            current = new Point(Player.SpawnX, Player.SpawnY);
        }

        if (current == _lastSpawn)
            return;

        _lastSpawn = current;

        var packet = Mod.GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.PlayerBed);
        packet.Write((byte)Player.whoAmI);
        packet.Write(current.X);
        packet.Write(current.Y);
        packet.Send();

#if DEBUG
        if (Player != null && Player.name != string.Empty)
            Main.NewText($"[DEBUG/MODPLAYER] Sync spawn for {Player.name}: ({current.X}, {current.Y})");
#endif
    }
}
