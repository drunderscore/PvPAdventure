using Microsoft.Xna.Framework;
using PvPAdventure.Content.Items;
using PvPAdventure.Core.SpawnSelector.Systems;
using PvPAdventure.System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Core.SpawnSelector.Players;

public class AdventureMirrorPlayer : ModPlayer
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
        // Is player in resin's spawn region?
        var regionManager = ModContent.GetInstance<RegionManager>();
        Point tilePos = Player.Center.ToTileCoordinates();
        var region = regionManager.GetRegionContaining(tilePos);
        if (region != null)
        {
            return true;
        }

        // Is player in bed spawn region?
        Vector2 bedSpawnPoint = new(Player.SpawnX, Player.SpawnY);
        float distanceToBedSpawn = Vector2.Distance(bedSpawnPoint * 16f, Player.Center);
        if (distanceToBedSpawn <= 25 * 16f)
        {
            return true;
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

            Vector2 teammateBedTile = new Vector2(other.SpawnX, other.SpawnY);
            float distanceToTeammateBed = Vector2.Distance(teammateBedTile * 16f, Player.Center);

            if (distanceToTeammateBed <= 25*16f)
            {
                return true;
            }
        }
        return false;
    }

#if DEBUG
    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        Player.respawnTimer = 0;
        base.Kill(damage, hitDirection, pvp, damageSource);
    }
#endif

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

    // Update the player's bed spawnpoint to the server
    private void UpdatePlayerSpawnpoint()
    {
        Point current = new(Player.SpawnX, Player.SpawnY);

        if (current == _lastSpawn)
            return; // nothing changed

        // Spawn changed (bed set, removed, bed mined, room invalid, etc.)
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

    #region Fullbright
    public static bool ForceFullBrightOnce;
    public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
    {
        if (drawInfo.headOnlyRender)
            return;

        if (!ForceFullBrightOnce)
            return;

        drawInfo.shadow = 0f;
        drawInfo.stealth = 1f;

        var p = drawInfo.drawPlayer;
        p.socialIgnoreLight = true;

        drawInfo.colorEyeWhites = Color.White;
        drawInfo.colorEyes = p.eyeColor;
        drawInfo.colorHair = p.GetHairColor(useLighting: false);
        drawInfo.colorHead = p.skinColor;
        drawInfo.colorBodySkin = p.skinColor;
        drawInfo.colorLegs = p.skinColor;

        drawInfo.colorShirt = p.shirtColor;
        drawInfo.colorUnderShirt = p.underShirtColor;
        drawInfo.colorPants = p.pantsColor;
        drawInfo.colorShoes = p.shoeColor;

        drawInfo.colorArmorHead = Color.White;
        drawInfo.colorArmorBody = Color.White;
        drawInfo.colorArmorLegs = Color.White;
        drawInfo.colorMount = Color.White;

        drawInfo.colorDisplayDollSkin = PlayerDrawHelper.DISPLAY_DOLL_DEFAULT_SKIN_COLOR;

        drawInfo.headGlowColor = new Color(drawInfo.headGlowColor.R, drawInfo.headGlowColor.G, drawInfo.headGlowColor.B, 0);
        drawInfo.bodyGlowColor = new Color(drawInfo.bodyGlowColor.R, drawInfo.bodyGlowColor.G, drawInfo.bodyGlowColor.B, 0);
        drawInfo.armGlowColor = new Color(drawInfo.armGlowColor.R, drawInfo.armGlowColor.G, drawInfo.armGlowColor.B, 0);
        drawInfo.legsGlowColor = new Color(drawInfo.legsGlowColor.R, drawInfo.legsGlowColor.G, drawInfo.legsGlowColor.B, 0);
    }
    #endregion
}
