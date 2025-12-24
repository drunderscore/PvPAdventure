
using DragonLens.Core.Systems.ThemeSystem;
using DragonLens.Core.Systems.ToolSystem;
using DragonLens.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.AdminTools.GameManagerIntegration;
using PvPAdventure.System;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Integrations.DragonLens.Tools;

[JITWhenModsEnabled("DragonLens")]
[ExtendsFromMod("DragonLens")]
public class DLGameManagerTool : Tool
{
    public override string IconKey => DLIntegration.GameManagerKey;
    public override string DisplayName => Language.GetTextValue("Mods.PvPAdventure.Tools.DLGameManagerTool.DisplayName");
    public override string Description => GetDescription();
    private string GetDescription()
    {
        var gm = ModContent.GetInstance<GameManager>();
        string localizationKey = "Mods.PvPAdventure.Tools.DLGameManagerTool.Description.";
        if (gm.CurrentPhase == GameManager.Phase.Playing)
        {
            localizationKey += "RightClickToEndGame";
        }
        else if (gm._startGameCountdown.HasValue && Main.netMode == NetmodeID.SinglePlayer)
        {
            localizationKey += "CountdownInProgress";
        }
        else if (gm.CurrentPhase == GameManager.Phase.Waiting)
        {
            localizationKey += "RightClickToStartGame";
        }
        else
        {
            localizationKey += "UnknownState"; // should never happen
        }
        return Language.GetTextValue(localizationKey);
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
        else if (gm._startGameCountdown.HasValue)
        {
            Main.NewText(Language.GetTextValue("Mods.PvPAdventure.Tools.DLGameManagerTool.CannotStart"), Color.Red);
        }
        else if (gm.CurrentPhase == GameManager.Phase.Waiting)
        {
            // Start game with default time of 60 minutes
            // Note: This option is usually for debugging purposes.
            int rightClickTimeInFrames = 216000;

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
    }

    public override void OnActivate()
    {
        var gms = ModContent.GetInstance<GameManagerSystem>();
        if (gms == null)
        {
            Main.NewText("Failed to open StartGameSystem: System not found.", Color.Red);
            return;
        }

        var gm = ModContent.GetInstance<GameManager>();
        if (gm.CurrentPhase == GameManager.Phase.Playing)
        {
            gms.ShowEndDialog();
        }
        else if (gm._startGameCountdown.HasValue)
        {
            Main.NewText(Language.GetTextValue("Mods.PvPAdventure.Tools.DLGameManagerTool.CannotStart"), Color.Red);
            //gm._startGameCountdown = null;
            //gm.TimeRemaining = 0;
            //gm.CurrentPhase = GameManager.Phase.Waiting;
            //ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral("Cancelled countdown."), Color.Red);
        }
        else
        {
            if (gms.IsActive())
            {
                gms.Hide();
            }
            else
            {
                gms.ShowStartDialog();
            }
        }
    }

    public override void DrawIcon(SpriteBatch spriteBatch, Rectangle position)
    {
        base.DrawIcon(spriteBatch, position);

        var gms = ModContent.GetInstance<GameManagerSystem>();

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