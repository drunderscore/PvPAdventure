
using DragonLens.Core.Systems.ThemeSystem;
using DragonLens.Core.Systems.ToolSystem;
using DragonLens.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Integrations.StartGame;
using PvPAdventure.Common.Integrations.TeamAssigner;
using PvPAdventure.Core.Helpers;
using PvPAdventure.System;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Integrations.DragonLens;

[JITWhenModsEnabled("DragonLens")]
[ExtendsFromMod("DragonLens")]
public class DLStartGameTool : Tool
{
    public override string IconKey => DLIntegration.StartGameKey;
    public override string DisplayName => GetDisplayName();

    private string GetDisplayName()
    {
        var gm = ModContent.GetInstance<GameManager>();
        if (gm.CurrentPhase == GameManager.Phase.Playing)
        {
            return "End Game";
        }
        else if (gm._startGameCountdown.HasValue && Main.netMode == NetmodeID.SinglePlayer)
        {
            return "Cancel Countdown";
        }
        else
        {
            return "Start Game";
        }
    }
    public override string Description => GetDescription();
    private string GetDescription()
    {
        var gm = ModContent.GetInstance<GameManager>();
        if (gm.CurrentPhase == GameManager.Phase.Playing)
        {
            return "Click to open confirmation window\nRight click to end game instantly";
        }
        else if (gm._startGameCountdown.HasValue && Main.netMode == NetmodeID.SinglePlayer)
        {
            return "Click to cancel the countdown";
        }
        else
        {
            return "Left click to open start game options\nRight click to start instantly";
        }
    }
    public override bool HasRightClick => true;
    public override void OnRightClick()
    {
        var gm = ModContent.GetInstance<GameManager>();

        if (gm.CurrentPhase == GameManager.Phase.Playing)
        {
            gm.EndGame();
        }
        //else if (gm._startGameCountdown.HasValue && Main.netMode == NetmodeID.SinglePlayer)
        //{
        //    return "Options for starting or ending the game\nRight click to start instantly";
        //}
        else
        {
            gm.StartGame(216000, countdownTimeInSeconds: -1); // 60 minutes
        }
    }

    public override void OnActivate()
    {
        Log.Info("DLStartGameTool activated");
        var sys = ModContent.GetInstance<StartGameSystem>();
        if (sys == null)
        {
            Main.NewText("Failed to open StartGameSystem: System not found.", Color.Red);
            return;
        }

        var gm = ModContent.GetInstance<GameManager>();
        if (gm.CurrentPhase == GameManager.Phase.Playing)
        {
            ModContent.GetInstance<StartGameSystem>().ShowEndDialog();
        }
        else if (gm._startGameCountdown.HasValue && Main.netMode == NetmodeID.SinglePlayer)
        {
            gm._startGameCountdown = null;
            gm.TimeRemaining = 0;
            gm.CurrentPhase = GameManager.Phase.Waiting;
            ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral("Cancelled countdown."), Color.Red);
        }
        else
        {
            var sgs = ModContent.GetInstance<StartGameSystem>();

            if (sgs.IsActive())
            {
                sgs.Hide();
            }
            else
            {
                sgs.ShowStartDialog();
            }
        }
    }

    public override void DrawIcon(SpriteBatch spriteBatch, Rectangle position)
    {
        base.DrawIcon(spriteBatch, position);

        var sys = ModContent.GetInstance<StartGameSystem>();

        if (sys.IsActive())
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