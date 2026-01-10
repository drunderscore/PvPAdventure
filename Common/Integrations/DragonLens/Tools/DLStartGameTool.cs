using DragonLens.Core.Systems.ThemeSystem;
using DragonLens.Core.Systems.ToolSystem;
using DragonLens.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.AdminTools.StartGameTool;
using PvPAdventure.System;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Integrations.DragonLens.Tools;

[JITWhenModsEnabled("DragonLens")]
[ExtendsFromMod("DragonLens")]
public class DLStartGameTool : Tool
{
    public override string IconKey => DLIntegration.StartGameKey;

    public override string DisplayName => GetDisplayName();

    private string GetDisplayName()
    {
        var gm = ModContent.GetInstance<GameManager>();

        string localizationKey = "Mods.PvPAdventure.Tools.DLStartGameTool.";
        localizationKey += gm.CurrentPhase == GameManager.Phase.Playing ? "AdjustGameTime" : "DisplayName";

        return Language.GetTextValue(localizationKey);
    }

    public override string Description => GetDescription();

    private string GetDescription()
    {
        var gm = ModContent.GetInstance<GameManager>();

        string localizationKey = "Mods.PvPAdventure.Tools.DLStartGameTool.Description.";

        if (gm.CurrentPhase == GameManager.Phase.Playing)
        {
            localizationKey += "AdjustGameTimeDescription";
        }
        else if (gm._startGameCountdown.HasValue)
        {
            localizationKey += "CountdownInProgress";
        }
        else
        {
            localizationKey += "StartGameDescription";
        }

        return Language.GetTextValue(localizationKey);
    }

    public override bool HasRightClick => true;

    public override void OnRightClick()
    {
        var gm = ModContent.GetInstance<GameManager>();
        var gms = ModContent.GetInstance<StartGameSystem>();

        if (gm.CurrentPhase == GameManager.Phase.Playing)
        {
            // While playing, right click just opens adjust-time (no end-game here).
            if (gms != null)
            {
                if (gms.IsActive())
                    gms.Hide();
                else
                    gms.ShowExtendGameDialog();
            }

            return;
        }

        if (gm._startGameCountdown.HasValue)
        {
            Main.NewText(Language.GetTextValue("Mods.PvPAdventure.Tools.DLStartGameTool.CannotStart"), Color.Red);
            return;
        }

        int rightClickTimeInFrames = 60 * 60 * 60; // 60 minutes

        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            gm.StartGame(time: rightClickTimeInFrames, countdownTimeInSeconds: 0);
        }
        else if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            var packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
            packet.Write((byte)AdventurePacketIdentifier.StartGame);
            packet.Write(rightClickTimeInFrames);
            packet.Write(0);
            packet.Send();
        }
    }

    public override void OnActivate()
    {
        var gms = ModContent.GetInstance<StartGameSystem>();
        if (gms == null)
        {
            Main.NewText("Failed to open StartGameSystem: System not found.", Color.Red);
            return;
        }

        // Toggle behavior first.
        if (gms.IsActive())
        {
            gms.Hide();
            return;
        }

        var gm = ModContent.GetInstance<GameManager>();

        if (gm.CurrentPhase == GameManager.Phase.Playing)
        {
            gms.ShowExtendGameDialog();
            return;
        }

        if (gm._startGameCountdown.HasValue)
        {
            Main.NewText(Language.GetTextValue("Mods.PvPAdventure.Tools.DLStartGameTool.CannotStart"), Color.Red);
            return;
        }

        gms.ShowStartDialog();
    }

    public override void DrawIcon(SpriteBatch spriteBatch, Rectangle position)
    {
        base.DrawIcon(spriteBatch, position);

        var gms = ModContent.GetInstance<StartGameSystem>();
        if (gms == null)
        {
            return;
        }

        if (gms.IsActive())
        {
            GUIHelper.DrawOutline(
                spriteBatch,
                new Rectangle(position.X - 4, position.Y - 4, 46, 46),
                ThemeHandler.ButtonColor.InvertColor());

            Texture2D tex = DLIntegration.GlowAlpha.Value;
            if (tex == null)
            {
                return;
            }

            Color color = new(255, 215, 150, 0) { A = 0 };
            var target = new Rectangle(position.X, position.Y, 38, 38);

            spriteBatch.Draw(tex, target, color);
        }
    }
}
