using Microsoft.Xna.Framework;
using PvPAdventure.Common.GameTimer;
using PvPAdventure.Core.Net;
using Steamworks;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Common.AdminTools.Tools.StartGameTool;

public class ExtendGamePanel : UI.DraggablePanel
{
    private readonly UITextPanel<string> applyButton;
    private readonly UI.SliderElement timeAdjustSlider;

    private int timeAdjustInFrames;
    protected override float MinResizeH => 130f;
    protected override float MinResizeW => 220f;

    public ExtendGamePanel()
        : base(Language.GetTextValue("Mods.PvPAdventure.Tools.DLStartGameTool.AdjustGameTime"))
    {
        Width.Set(360, 0);
        Height.Set(140, 0);
        HAlign = 0.5f;
        VAlign = 0.7f;
        ContentPanel.SetPadding(12);

        timeAdjustSlider = new UI.SliderElement(
            label: Language.GetTextValue("Mods.PvPAdventure.Tools.DLStartGameTool.Time"),
            min: -60f,
            max: 60f,
            defaultValue: 0f,
            step: 1f,
            onValueChanged: value =>
            {
                int deltaMinutes = (int)value;
                timeAdjustInFrames = deltaMinutes * 60 * 60;
            }
        );
        ContentPanel.Append(timeAdjustSlider);

        applyButton = new UITextPanel<string>(Language.GetTextValue("Mods.PvPAdventure.Tools.DLStartGameTool.Apply"))
        {
            Width = { Pixels = 120f },
            Height = { Pixels = 40f },
            HAlign = 0.5f,
            VAlign = 1f
        };

        applyButton.OnMouseOver += (_, _) => applyButton.BorderColor = Color.Yellow;
        applyButton.OnMouseOut += (_, _) => applyButton.BorderColor = Color.Black;

        applyButton.OnLeftClick += (_, _) =>
        {
            if (timeAdjustInFrames == 0)
            {
                ModContent.GetInstance<StartGameSystem>().Hide();
                return;
            }

            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                GameManager gm = ModContent.GetInstance<GameManager>();
                gm.AdjustTimeRemaining(timeAdjustInFrames);
            }
            else if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                var packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
                packet.Write((byte)AdventurePacketIdentifier.GameTimer);
                packet.Write((byte)GameTimerNetHandler.GameTimerPacketType.AdjustGameTime);
                packet.Write(timeAdjustInFrames);
                packet.Send();
            }

            ModContent.GetInstance<StartGameSystem>().Hide();
        };

        ContentPanel.Append(applyButton);
    }

    protected override void OnClosePanelLeftClick()
    {
        ModContent.GetInstance<StartGameSystem>().Hide();
    }
}
