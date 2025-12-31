
using DragonLens.Core.Systems.ThemeSystem;
using DragonLens.Core.Systems.ToolSystem;
using DragonLens.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.AdminTools.EndGameTool;
using PvPAdventure.System;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Integrations.DragonLens.Tools;

[JITWhenModsEnabled("DragonLens")]
[ExtendsFromMod("DragonLens")]
public class DLEndGameTool : Tool
{
    public override string IconKey => DLIntegration.EndGameKey;
    public override string DisplayName => Language.GetTextValue("Mods.PvPAdventure.Tools.DLEndGameTool.DisplayName");
    public override string Description => Language.GetTextValue("Mods.PvPAdventure.Tools.DLEndGameTool.Description");
    public override bool HasRightClick => true;
    public override void OnRightClick()
    {
        var gm = ModContent.GetInstance<GameManager>();

        if (gm.CurrentPhase == GameManager.Phase.Playing)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                gm.EndGame();
            }
            else if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                var packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
                packet.Write((byte)AdventurePacketIdentifier.EndGame);
                packet.Send();
            }
        }
        else if (gm._startGameCountdown.HasValue)
        {
            Main.NewText(Language.GetTextValue("Mods.PvPAdventure.Tools.DLEndGameTool.CountdownInProgress"), Color.Red);
        }
        else if (gm.CurrentPhase == GameManager.Phase.Waiting)
        {
            Main.NewText(Language.GetTextValue("Mods.PvPAdventure.Tools.DLEndGameTool.GameNotStartedYet"), Color.Red);
        }
    }

    public override void OnActivate()
    {
        var endGameSystem = ModContent.GetInstance<EndGameSystem>();
        if (endGameSystem == null)
        {
            Main.NewText("Failed to open EndGameSystem: System not found.", Color.Red);
            return;
        }

        var gm = ModContent.GetInstance<GameManager>();
        if (gm.CurrentPhase == GameManager.Phase.Playing)
        {
            endGameSystem.ToggleActive();
        }
        else if (gm._startGameCountdown.HasValue)
        {
            Main.NewText(Language.GetTextValue("Mods.PvPAdventure.Tools.DLEndGameTool.CountdownInProgress"), Color.Red);
        }
        else if (gm.CurrentPhase == GameManager.Phase.Waiting)
        {
            Main.NewText(Language.GetTextValue("Mods.PvPAdventure.Tools.DLEndGameTool.GameNotStartedYet"), Color.Red);
        }
    }

    public override void DrawIcon(SpriteBatch spriteBatch, Rectangle position)
    {
        base.DrawIcon(spriteBatch, position);

        var gms = ModContent.GetInstance<EndGameSystem>();

        if (gms.IsActive())
        {
            GUIHelper.DrawOutline(spriteBatch, new Rectangle(position.X - 4, position.Y - 4, 46, 46), ThemeHandler.ButtonColor.InvertColor());

            Texture2D tex = DLIntegration.GlowAlpha.Value;
            if (tex == null) return;

            Color color = new(255, 215, 150, 0);
            color.A = 0;
            var target = new Rectangle(position.X, position.Y, 38, 38);

            spriteBatch.Draw(tex, target, color);
        }
    }
}