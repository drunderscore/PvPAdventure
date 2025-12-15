using Microsoft.Xna.Framework;
using PvPAdventure.Common.Integrations.SharedUI;
using PvPAdventure.System;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.Integrations.GameManagerIntegration;

internal class EndGameElement : DraggablePanel
{
    private readonly UITextPanel<string> yesButton;
    private readonly UITextPanel<string> noButton;

    protected override float MinResizeW => 260f;
    protected override float MinResizeH => 100f;
    protected override void OnClosePanelLeftClick()
    {
        ModContent.GetInstance<GameManagerSystem>().Hide();
    }

    public EndGameElement() : base(Language.GetTextValue("Mods.PvPAdventure.Tools.DLGameManagerTool.ReallyEndGame"))
    {
        Width.Set(400, 0);
        Height.Set(100, 0);
        HAlign = 0.5f;
        VAlign = 0.7f;
        ContentPanel.SetPadding(12f);

        // No
        noButton = new UITextPanel<string>(Language.GetTextValue("Mods.PvPAdventure.Tools.DLGameManagerTool.No"))
        {
            Width = { Pixels = 120f },
            Height = { Pixels = 40f },
            Left = { Pixels = 10f },
            VAlign = 0.5f
        };
        noButton.OnMouseOver += (_, _) => noButton.BorderColor = Color.Yellow;
        noButton.OnMouseOut += (_, _) => noButton.BorderColor = Color.Black;
        noButton.OnLeftClick += (_, _) => ModContent.GetInstance<GameManagerSystem>().Hide();

        // Yes
        yesButton = new UITextPanel<string>(Language.GetTextValue("Mods.PvPAdventure.Tools.DLGameManagerTool.Yes"))
        {
            Width = { Pixels = 120f },
            Height = { Pixels = 40f },
            Left = { Percent = 1f, Pixels = -10f - 120f },
            VAlign = 0.5f
        };
        yesButton.OnMouseOver += (_, _) => yesButton.BorderColor = Color.Yellow;
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

            ModContent.GetInstance<GameManagerSystem>().Hide();
        };

        ContentPanel.Append(noButton);
        ContentPanel.Append(yesButton);
    }

}