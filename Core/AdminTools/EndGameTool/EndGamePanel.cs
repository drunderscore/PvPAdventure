using Microsoft.Xna.Framework;
using PvPAdventure.Core.AdminTools.UI;
using PvPAdventure.System;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Core.AdminTools.EndGameTool;

internal class EndGamePanel : DraggablePanel
{
    private readonly UITextPanel<string> yesButton;
    private readonly UITextPanel<string> noButton;

    protected override float MinResizeW => 260f;
    protected override float MinResizeH => 100f;
    protected override void OnClosePanelLeftClick()
    {
        ModContent.GetInstance<EndGameSystem>().ToggleActive();
    }

    public EndGamePanel() : base(Language.GetTextValue("Mods.PvPAdventure.Tools.DLEndGameTool.ReallyEndGame"))
    {
        Width.Set(400, 0);
        Height.Set(100, 0);
        HAlign = 0.5f;
        VAlign = 0.7f;
        ContentPanel.SetPadding(12f);

        // No
        noButton = new UITextPanel<string>(Language.GetTextValue("Mods.PvPAdventure.Tools.DLEndGameTool.No"))
        {
            Width = { Pixels = 120f },
            Height = { Pixels = 40f },
            Left = { Pixels = 10f },
            VAlign = 0.5f
        };
        noButton.OnMouseOver += (_, _) =>
        {
            noButton.BorderColor = Color.Yellow;
            //Main.instance.MouseText("Click to cancel prompt");
        };
        noButton.OnMouseOut += (_, _) => noButton.BorderColor = Color.Black;
        noButton.OnLeftClick += (_, _) => ModContent.GetInstance<EndGameSystem>().ToggleActive();

        // Yes
        yesButton = new UITextPanel<string>(Language.GetTextValue("Mods.PvPAdventure.Tools.DLEndGameTool.Yes"))
        {
            Width = { Pixels = 120f },
            Height = { Pixels = 40f },
            Left = { Percent = 1f, Pixels = -10f - 120f },
            VAlign = 0.5f
        };
        yesButton.OnMouseOver += (_, _) =>
        {
            yesButton.BorderColor = Color.Yellow;
            //Main.instance.MouseText("Click to end game");
        };
        yesButton.OnMouseOut += (_, _) => yesButton.BorderColor = Color.Black;
        yesButton.OnLeftClick += (_, _) =>
        {
            var gm = ModContent.GetInstance<GameManager>();

            if (Main.netMode == NetmodeID.SinglePlayer)
                gm.EndGame();
            else if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                var packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
                packet.Write((byte)AdventurePacketIdentifier.EndGame);
                packet.Send();
            }

            ModContent.GetInstance<EndGameSystem>().ToggleActive();
        };

        ContentPanel.Append(noButton);
        ContentPanel.Append(yesButton);
    }

}