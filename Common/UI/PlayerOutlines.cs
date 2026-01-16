using System.Reflection;
using Microsoft.Xna.Framework;
using PvPAdventure.Core.Config;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.Graphics.Renderers;
using Terraria.ModLoader;

namespace PvPAdventure.Common.UI;

[Autoload(Side = ModSide.Client)]
public class PlayerOutlines : ModSystem
{
    private delegate void CreateOutlinesDelegate(float alpha, float scale, Color borderColor);

    private CreateOutlinesDelegate _createOutlines;

    public override void Load()
    {
        _createOutlines =
            typeof(LegacyPlayerRenderer).GetMethod("CreateOutlines", BindingFlags.NonPublic | BindingFlags.Instance)
                .CreateDelegate<CreateOutlinesDelegate>(Main.PlayerRenderer);
        On_PlayerDrawLayers.DrawPlayer_RenderAllLayers += OnPlayerDrawLayersDrawPlayer_RenderAllLayers;
    }

    private void OnPlayerDrawLayersDrawPlayer_RenderAllLayers(On_PlayerDrawLayers.orig_DrawPlayer_RenderAllLayers orig,
        ref PlayerDrawSet drawinfo)
    {
        try
        {
            if (drawinfo.shadow != 0.0f)
                return;

            if (drawinfo.headOnlyRender)
                return;

            var team = (Team)drawinfo.drawPlayer.team;
            if (team == Team.None)
                return;

            if (drawinfo.drawPlayer.dead)
                return;

            var screenBounds = new Rectangle((int)Main.screenPosition.X, (int)Main.screenPosition.Y, Main.screenWidth,
                Main.screenHeight);
            var playerBounds = drawinfo.drawPlayer.getRect();

            if (!playerBounds.Intersects(screenBounds))
                return;

            var adventureClientConfig = ModContent.GetInstance<ClientConfig>();

            if (!adventureClientConfig.PlayerOutline.Self && drawinfo.drawPlayer.whoAmI == Main.myPlayer)
                return;

            // Don't show outlines for teammates, but if you want self outlines, still show it.
            if (!adventureClientConfig.PlayerOutline.Team && team == (Team)Main.LocalPlayer.team &&
                (!adventureClientConfig.PlayerOutline.Self || drawinfo.drawPlayer.whoAmI != Main.myPlayer))
                return;

            _createOutlines(drawinfo.drawPlayer.stealth, 1.0f,
                Main.teamColor[(int)team].MultiplyRGBA(Lighting.GetColor(drawinfo.Center.ToTileCoordinates())));
        }
        finally
        {
            orig(ref drawinfo);
        }
    }
}