using Microsoft.Xna.Framework;
using PvPAdventure.Common.Integrations.SharedUI;
using PvPAdventure.System;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Integrations.GameManagerIntegration;

/// <summary>
/// The main UI element for starting a game (draggable title + content panel)
/// </summary>
internal class GameManagerElement : DraggablePanel
{
    private readonly UITextPanel<string> _startButton;
    private readonly SliderElement _gameTimeSlider;
    private readonly SliderElement _countdownSlider;

    private int _countdownTimeInSeconds = 10;
    private int _gameTimeInFrames = 195 * 60 * 60;

    protected override float MinResizeH => 155f;
    protected override float MinResizeW => 220f;

    public GameManagerElement() : base(Language.GetTextValue("Mods.PvPAdventure.Tools.DLGameManagerTool.StartGame"))
    {
        Width.Set(360, 0);
        Height.Set(180, 0);
        HAlign = 0.5f;
        VAlign = 0.7f;
        ContentPanel.SetPadding(12);

        _gameTimeSlider = new SliderElement(
            label: Language.GetTextValue("Mods.PvPAdventure.Tools.DLGameManagerTool.Time"),
            min: 0f,
            max: 195f,
            defaultValue: 195f,
            step: 1f,
            onValueChanged: value =>
            {
                int totalMinutes = (int)value;
                _gameTimeInFrames = totalMinutes * 60 * 60;
            }
        );
        ContentPanel.Append(_gameTimeSlider);

        _countdownSlider = new SliderElement(
            label: Language.GetTextValue("Mods.PvPAdventure.Tools.DLGameManagerTool.Countdown"),
            min: 0f,
            max: 10f,
            defaultValue: 10f,
            step: 1f,
            onValueChanged: value =>
            {
                _countdownTimeInSeconds = (int)value;
            }
        )
        {
            Top = { Pixels = 26f }
        };
        ContentPanel.Append(_countdownSlider);

        _startButton = new UITextPanel<string>(Language.GetTextValue("Mods.PvPAdventure.Tools.DLGameManagerTool.StartExclamation"))
        {
            Width = { Pixels = 120f },
            Height = { Pixels = 40f },
            HAlign = 0.5f,
            VAlign = 1f
        };
        _startButton.OnMouseOver += (_, _) => _startButton.BorderColor = Color.Yellow;
        _startButton.OnMouseOut += (_, _) => _startButton.BorderColor = Color.Black;
        _startButton.OnLeftClick += (_, _) =>
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                var gm = ModContent.GetInstance<GameManager>();
                gm.StartGame(time: _gameTimeInFrames, countdownTimeInSeconds: _countdownTimeInSeconds);
            }
            else if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                var packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
                packet.Write((byte)AdventurePacketIdentifier.StartGame);
                packet.Write(_gameTimeInFrames);
                packet.Write(_countdownTimeInSeconds);
                packet.Send();
            }

            ModContent.GetInstance<GameManagerSystem>().Hide();
        };
        ContentPanel.Append(_startButton);
    }

    protected override void OnClosePanelLeftClick()
    {
        ModContent.GetInstance<GameManagerSystem>().Hide();
    }
}