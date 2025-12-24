
using DragonLens.Core.Systems.ThemeSystem;
using DragonLens.Core.Systems.ToolSystem;
using DragonLens.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.System;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Integrations.DragonLens.Tools;

[JITWhenModsEnabled("DragonLens")]
[ExtendsFromMod("DragonLens")]
public class DLPauseTool : Tool
{
    public override string IconKey => DLIntegration.PauseKey;

    public override string DisplayName =>
        Language.GetTextValue($"Mods.PvPAdventure.Tools.DLPauseTool.DisplayName.{ModContent.GetInstance<PauseManager>().IsPaused}");

    public override string Description =>
    string.Format(
        Language.GetTextValue("Mods.PvPAdventure.Tools.DLPauseTool.Description"),
        Language.GetTextValue($"Mods.PvPAdventure.Tools.DLPauseTool.DisplayName.{ModContent.GetInstance<PauseManager>().IsPaused}").ToLower()
    );

    public override void OnActivate()
    {
        var pm = ModContent.GetInstance<PauseManager>();

        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            pm.PauseGame();
        }
        else if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            var packet = Mod.GetPacket();
            packet.Write((byte)AdventurePacketIdentifier.PauseGame);
            packet.Send();
        }
    }

    public override void DrawIcon(SpriteBatch spriteBatch, Rectangle position)
    {
        base.DrawIcon(spriteBatch, position);

        var pm = ModContent.GetInstance<PauseManager>();

        if (pm.IsPaused)
        {
            GUIHelper.DrawOutline(spriteBatch, new Rectangle(position.X - 4, position.Y - 4, 46, 46), ThemeHandler.ButtonColor.InvertColor());

            Texture2D tex = DLIntegration.GlowAlpha.Value;
            if (tex == null) return;

            Color color = new(255, 215, 150);
            color.A = 0;
            var target = new Rectangle(position.X, position.Y, 38, 38);

            spriteBatch.Draw(tex, target, color);
        }
    }
}