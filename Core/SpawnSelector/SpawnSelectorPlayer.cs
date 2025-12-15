using Microsoft.Xna.Framework;
using PvPAdventure.Content.Items;
using PvPAdventure.System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Core.SpawnSelector;

public class SpawnSelectorPlayer : ModPlayer
{
    // Variables
    private Point _lastSpawn = new(-1, -1);

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

            // Show text only for the local player
            if (Player.whoAmI == Main.myPlayer)
            {
                //PopupTextHelper.NewText("Mirror interrupted!", Player, Color.Crimson);
            }
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
        if (Main.dedServ)
            return;

        if (Player.whoAmI != Main.myPlayer)
            return;

        if (ModContent.GetInstance<GameManager>().CurrentPhase == GameManager.Phase.Playing &&
            Main.mapFullscreen && IsPlayerInSpawnRegion())
        {
            SpawnSelectorSystem.SetEnabled(true);
        }
        else
        {
            SpawnSelectorSystem.SetEnabled(false);
        }

        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        UpdatePlayerSpawnpoint();
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
        Main.NewText($"[DEBUG/MODPLAYER] Sync spawn for {Player.name}: ({current.X}, {current.Y})");
#endif
    }
}
