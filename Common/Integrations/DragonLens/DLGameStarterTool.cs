
using DragonLens.Core.Systems.ThemeSystem;
using DragonLens.Core.Systems.ToolSystem;
using DragonLens.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Integrations.GameStarter;
using PvPAdventure.Core.Helpers;
using PvPAdventure.System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Integrations.DragonLens;

[JITWhenModsEnabled("DragonLens")]
[ExtendsFromMod("DragonLens")]
public class DLGameStarterTool : Tool
{
    public override string IconKey => DLIntegration.GameStarterKey;
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
            return "Countdown";
        }
        else
        {
            return "Game Starter";
        }
    }
    public override string Description => GetDescription();
    private string GetDescription()
    {
        var gm = ModContent.GetInstance<GameManager>();
        if (gm.CurrentPhase == GameManager.Phase.Playing)
        {
            return "Right click to end game instantly";
        }
        else if (gm._startGameCountdown.HasValue && Main.netMode == NetmodeID.SinglePlayer)
        {
            return "Countdown in progress...";
            //return "Cancel the countdown";
        }
        else
        {
            return "Right click to start game instantly";
        }
    }
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
        //else if (gm._startGameCountdown.HasValue && Main.netMode == NetmodeID.SinglePlayer)
        //{
        //    return "Options for starting or ending the game\nRight click to start instantly";
        //}
        else if (gm.CurrentPhase == GameManager.Phase.Waiting)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                gm.StartGame(time: 216000, countdownTimeInSeconds: 0); // 60 minutes
            }
            else if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                var packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
                packet.Write((byte)AdventurePacketIdentifier.StartGame);
                packet.Write(216000);
                packet.Write(0);
                packet.Send();
            }
        }
    }

    public override void OnActivate()
    {
        var sys = ModContent.GetInstance<GameStarterSystem>();
        if (sys == null)
        {
            Main.NewText("Failed to open StartGameSystem: System not found.", Color.Red);
            return;
        }

        var gm = ModContent.GetInstance<GameManager>();
        if (gm.CurrentPhase == GameManager.Phase.Playing)
        {
            ModContent.GetInstance<GameStarterSystem>().ShowEndDialog();
        }
        else if (gm._startGameCountdown.HasValue)
        {
            //gm._startGameCountdown = null;
            //gm.TimeRemaining = 0;
            //gm.CurrentPhase = GameManager.Phase.Waiting;
            //ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral("Cancelled countdown."), Color.Red);
        }
        else
        {
            var gss = ModContent.GetInstance<GameStarterSystem>();

            if (gss.IsActive())
            {
                gss.Hide();
            }
            else
            {
                gss.ShowStartDialog();
            }
        }
    }

    public override void DrawIcon(SpriteBatch spriteBatch, Rectangle position)
    {
        base.DrawIcon(spriteBatch, position);

        var sys = ModContent.GetInstance<GameStarterSystem>();

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