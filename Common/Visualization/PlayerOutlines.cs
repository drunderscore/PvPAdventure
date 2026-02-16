using System.Reflection;
using Microsoft.Xna.Framework;
using PvPAdventure.Core.Config;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.Graphics.Renderers;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Visualization;

[Autoload(Side = ModSide.Client)]
public class PlayerOutlines : ModSystem
{
    private delegate void CreateOutlinesDelegate(float alpha, float scale, Color borderColor);

    private CreateOutlinesDelegate _createOutlines;

    //private int _outlineCallsThisSecond;
    private int _secCounter;

    public override void Load()
    {
        _createOutlines =
            typeof(LegacyPlayerRenderer).GetMethod("CreateOutlines", BindingFlags.NonPublic | BindingFlags.Instance)
                .CreateDelegate<CreateOutlinesDelegate>(Main.PlayerRenderer);
        On_PlayerDrawLayers.DrawPlayer_RenderAllLayers += OnPlayerDrawLayersDrawPlayer_RenderAllLayers;
    }

    public override void PostUpdateEverything()
    {
        if (++_secCounter < 60)
            return;

        _secCounter = 0;
        //Log.Chat($"[Perf] PlayerOutlines calls/s={_outlineCallsThisSecond}");
        //_outlineCallsThisSecond = 0;
    }

    private void OnPlayerDrawLayersDrawPlayer_RenderAllLayers(
    On_PlayerDrawLayers.orig_DrawPlayer_RenderAllLayers orig,
    ref PlayerDrawSet drawinfo)
    {
        try
        {
            if (drawinfo.shadow != 0.0f || drawinfo.headOnlyRender)
                return;

            var config = ModContent.GetInstance<ClientConfig>();
            if (!config.PlayerOutlines)
                return; // <-- disables ALL outline drawing on THIS client

            Player p = drawinfo.drawPlayer;

            if (p.dead)
                return;

            Team team = (Team)p.team;
            if (team == Team.None)
                return;

            var screenBounds = new Rectangle(
                (int)Main.screenPosition.X,
                (int)Main.screenPosition.Y,
                Main.screenWidth,
                Main.screenHeight);

            if (!p.getRect().Intersects(screenBounds))
                return;

            _createOutlines(
                p.stealth,
                1.0f,
                Main.teamColor[(int)team].MultiplyRGBA(Lighting.GetColor(drawinfo.Center.ToTileCoordinates())));
        }
        finally
        {
            orig(ref drawinfo);
        }
    }
}
