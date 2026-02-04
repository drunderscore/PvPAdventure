using Microsoft.Xna.Framework;
using PvPAdventure.Core.Debug;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using static PvPAdventure.Common.SSC.SSC;

namespace PvPAdventure.Common.SSC;

[Autoload(Side = ModSide.Both)]
public sealed class GhostClampSystem : ModSystem
{
    public override void PostUpdatePlayers()
    {
        if (!SSC.IsEnabled || Main.netMode != NetmodeID.Server)
            return;

        Vector2 spawnPos = new(Main.spawnTileX * 16f, Main.spawnTileY * 16f - 48f);

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player p = Main.player[i];
            if (p == null || !p.active)
                continue;

            if (!p.ghost)
                continue;

            p.position = spawnPos;
            p.velocity = Vector2.Zero;
            //p.direction = 1;

            // Push authoritative state to clients
            NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, p.whoAmI);
        }
    }
}

// UNUSED currently but keep for future use if we want to make the ghost invisible instead of just clamping them to spawn.
//public class DrawPlayerGhostInvisible : ModPlayer
//{
//    public static bool ForceFullBrightOnce;
//    public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
//    {
//        if (!ForceFullBrightOnce)
//            return;

//        drawInfo.shadow = 0f;
//        drawInfo.stealth = 1f;

//        var p = drawInfo.drawPlayer;
//        p.socialIgnoreLight = true;

//        drawInfo.colorEyeWhites = Color.White;
//        drawInfo.colorEyes = p.eyeColor;
//        drawInfo.colorHair = p.GetHairColor(useLighting: false);
//        drawInfo.colorHead = p.skinColor;
//        drawInfo.colorBodySkin = p.skinColor;
//        drawInfo.colorLegs = p.skinColor;

//        drawInfo.colorShirt = p.shirtColor;
//        drawInfo.colorUnderShirt = p.underShirtColor;
//        drawInfo.colorPants = p.pantsColor;
//        drawInfo.colorShoes = p.shoeColor;

//        drawInfo.colorArmorHead = Color.White;
//        drawInfo.colorArmorBody = Color.White;
//        drawInfo.colorArmorLegs = Color.White;
//        drawInfo.colorMount = Color.White;

//        drawInfo.colorDisplayDollSkin = PlayerDrawHelper.DISPLAY_DOLL_DEFAULT_SKIN_COLOR;

//        drawInfo.headGlowColor = new Color(drawInfo.headGlowColor.R, drawInfo.headGlowColor.G, drawInfo.headGlowColor.B, 0);
//        drawInfo.bodyGlowColor = new Color(drawInfo.bodyGlowColor.R, drawInfo.bodyGlowColor.G, drawInfo.bodyGlowColor.B, 0);
//        drawInfo.armGlowColor = new Color(drawInfo.armGlowColor.R, drawInfo.armGlowColor.G, drawInfo.armGlowColor.B, 0);
//        drawInfo.legsGlowColor = new Color(drawInfo.legsGlowColor.R, drawInfo.legsGlowColor.G, drawInfo.legsGlowColor.B, 0);
//    }
//}
