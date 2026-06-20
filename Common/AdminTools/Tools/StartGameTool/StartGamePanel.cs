using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.AdminTools.Tools.EndGameTool;
using PvPAdventure.Common.Game;
using PvPAdventure.Core.Net;
using PvPAdventure.UI;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.AdminTools.Tools.StartGameTool;

internal class StartGamePanel : UIDraggablePanel
{
    private const int FramesPerSecond = 60;
    private const int FramesPerMinute = FramesPerSecond * 60;
    private const int MaxGameMinutes = 195;
    private const int MaxCountdownSeconds = 300;
    private const int CountdownStepSeconds = 10;

    private readonly UITextPanel<string> _startButton;
    private readonly UITextActionPanel _endGameButton;
    private readonly UISliderElement _gameTimeSlider;
    private readonly UISliderElement _countdownSlider;
    private readonly UITextPanel<string>[] _countdownButtons;

    private int _countdownTimeInSeconds = 10;
    private int _gameTimeInFrames = MaxGameMinutes * FramesPerMinute;

    private bool _countdownSliderBeingDragged;
    private bool _gameTimeSliderBeingDragged;

    protected override float MinResizeH => 240f;
    protected override float MinResizeW => 320f;

    public StartGamePanel()
        : base(Language.GetTextValue("Mods.PvPAdventure.Tools.DLStartGameTool.AdventureGameTimer"))
    {
        Width.Set(380, 0);
        Height.Set(260, 0);
        HAlign = 0.5f;
        VAlign = 0.7f;
        ContentPanel.SetPadding(12);

        _countdownSlider = new UISliderElement(
            label: Language.GetTextValue("Mods.PvPAdventure.Tools.DLStartGameTool.Countdown"),
            min: 0f,
            max: MaxCountdownSeconds,
            defaultValue: _countdownTimeInSeconds,
            step: 1f,
            onValueChanged: value =>
            {
                _countdownTimeInSeconds = (int)Math.Round(value);
                _countdownSliderBeingDragged = true;
            })
        {
            Width = { Percent = 1f, Pixels = -76f },
            Top = { Pixels = 0f },
            ShowDisabledLockIcon = true,
            DisabledTooltip = Language.GetTextValue("Mods.PvPAdventure.Tools.DLStartGameTool.CountdownLocked")
        };
        _countdownSlider.OnRelease = releasedValue =>
        {
            _countdownSliderBeingDragged = false;
            SetCountdownSeconds((int)Math.Round(releasedValue), syncActiveCountdown: true);
        };
        ContentPanel.Append(_countdownSlider);

        UITextPanel<string> countdownMinus = CreateTextButton(
            "-",
            Language.GetTextValue("Mods.PvPAdventure.Tools.DLStartGameTool.CountdownMinusTooltip"),
            () => SnapCountdown(direction: -1),
            30f,
            24f);
        countdownMinus.Left.Set(-68f, 1f);
        countdownMinus.Top.Set(0f, 0f);
        ContentPanel.Append(countdownMinus);

        UITextPanel<string> countdownPlus = CreateTextButton(
            "+",
            Language.GetTextValue("Mods.PvPAdventure.Tools.DLStartGameTool.CountdownPlusTooltip"),
            () => SnapCountdown(direction: 1),
            30f,
            24f);
        countdownPlus.Left.Set(-32f, 1f);
        countdownPlus.Top.Set(0f, 0f);
        ContentPanel.Append(countdownPlus);

        _countdownButtons = new[] { countdownMinus, countdownPlus };

        _gameTimeSlider = new UISliderElement(
            label: Language.GetTextValue("Mods.PvPAdventure.Tools.DLStartGameTool.Time"),
            min: 0f,
            max: MaxGameMinutes,
            defaultValue: MaxGameMinutes,
            step: 1f,
            onValueChanged: value =>
            {
                _gameTimeInFrames = MinutesToFrames((int)Math.Round(value));
                _gameTimeSliderBeingDragged = true;
            })
        {
            Top = { Pixels = 48f }
        };
        _gameTimeSlider.OnRelease = releasedValue =>
        {
            _gameTimeSliderBeingDragged = false;
            ApplyGameTimeSliderValue((int)Math.Round(releasedValue));
        };
        ContentPanel.Append(_gameTimeSlider);

        UIElement gameTimeButtonRow = new()
        {
            Width = { Pixels = 190f },
            Height = { Pixels = 28f },
            Top = { Pixels = 78f },
            HAlign = 0.5f
        };
        ContentPanel.Append(gameTimeButtonRow);

        AppendGameTimeButton(gameTimeButtonRow, "--", 0, -10, "SubtractTenMinutesTooltip");
        AppendGameTimeButton(gameTimeButtonRow, "-", 1, -1, "SubtractOneMinuteTooltip");
        AppendGameTimeButton(gameTimeButtonRow, "+", 2, 1, "AddOneMinuteTooltip");
        AppendGameTimeButton(gameTimeButtonRow, "++", 3, 10, "AddTenMinutesTooltip");

        _endGameButton = new UITextActionPanel(
            text: "",
            leftClickAction: OpenEndGameConfirmation,
            height: 42f,
            icon: Ass.IconEndGame.Value)
        {
            Width = { Pixels = 48f },
            Height = { Pixels = 42f },
            Top = { Pixels = 116f },
            HAlign = 0.5f
        };
        _endGameButton.OnMouseOver += (_, _) =>
            Main.instance.MouseText(Language.GetTextValue("Mods.PvPAdventure.Tools.DLEndGameTool.DisplayName"));
        ContentPanel.Append(_endGameButton);

        _startButton = new UITextPanel<string>(
            Language.GetTextValue("Mods.PvPAdventure.Tools.DLStartGameTool.StartExclamation"))
        {
            Width = { Pixels = 120f },
            Height = { Pixels = 40f },
            HAlign = 0.5f,
            VAlign = 1f
        };
        _startButton.OnMouseOver += (_, _) => _startButton.BorderColor = Color.Yellow;
        _startButton.OnMouseOut += (_, _) => _startButton.BorderColor = Color.Black;
        _startButton.OnLeftClick += (_, _) => StartSelectedGame();
        ContentPanel.Append(_startButton);
    }

    public override void Update(GameTime gameTime)
    {
        GameManager gm = ModContent.GetInstance<GameManager>();
        bool isPlaying = gm.CurrentPhase == GameManager.Phase.Playing;
        bool hasCountdown = gm._startGameCountdown.HasValue;

        _countdownSlider.Enabled = !isPlaying;
        foreach (UITextPanel<string> button in _countdownButtons)
            ApplyButtonEnabledVisuals(button, !isPlaying);

        base.Update(gameTime);

        if (hasCountdown && !_countdownSliderBeingDragged)
        {
            float secondsLeft = gm._startGameCountdown.Value / (float)FramesPerSecond;
            _countdownTimeInSeconds = (int)Math.Round(secondsLeft);
            _countdownSlider.SetValue(secondsLeft);
        }

        if ((isPlaying || hasCountdown) && !_gameTimeSliderBeingDragged)
        {
            _gameTimeInFrames = gm.TimeRemaining;
            _gameTimeSlider.SetValue(gm.TimeRemaining / (float)FramesPerMinute);
        }

        UpdateStartButton(isPlaying, hasCountdown);
    }

    protected override void OnClosePanelLeftClick()
    {
        ModContent.GetInstance<StartGameSystem>().Hide();
    }

    private void SnapCountdown(int direction)
    {
        if (ModContent.GetInstance<GameManager>().CurrentPhase == GameManager.Phase.Playing)
            return;

        int current = Math.Clamp(_countdownTimeInSeconds, 0, MaxCountdownSeconds);
        int next;

        if (direction > 0)
        {
            next = current % CountdownStepSeconds == 0
                ? current + CountdownStepSeconds
                : ((current + CountdownStepSeconds - 1) / CountdownStepSeconds) * CountdownStepSeconds;
        }
        else
        {
            next = current % CountdownStepSeconds == 0
                ? current - CountdownStepSeconds
                : (current / CountdownStepSeconds) * CountdownStepSeconds;
        }

        SetCountdownSeconds(Math.Clamp(next, 0, MaxCountdownSeconds), syncActiveCountdown: true);
    }

    private void SetCountdownSeconds(int seconds, bool syncActiveCountdown)
    {
        GameManager gm = ModContent.GetInstance<GameManager>();
        if (gm.CurrentPhase == GameManager.Phase.Playing)
            return;

        seconds = Math.Clamp(seconds, 0, MaxCountdownSeconds);
        _countdownTimeInSeconds = seconds;
        _countdownSlider.SetValue(seconds);

        if (!syncActiveCountdown || !gm._startGameCountdown.HasValue)
            return;

        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            gm.SetCountdown(seconds);
        }
        else if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
            packet.Write((byte)AdventurePacketIdentifier.GameTimer);
            packet.Write((byte)GameTimerNetHandler.GameTimerPacketType.UpdateCountdown);
            packet.Write(seconds);
            packet.Send();
        }
    }

    private void ApplyGameTimeSliderValue(int minutes)
    {
        GameManager gm = ModContent.GetInstance<GameManager>();

        if (gm.CurrentPhase == GameManager.Phase.Playing || gm._startGameCountdown.HasValue)
        {
            int desiredFrames = MinutesToFrames(Math.Clamp(minutes, 0, MaxGameMinutes));
            AdjustActiveGameTime(desiredFrames - gm.TimeRemaining);
            return;
        }

        SetIdleGameTime(MinutesToFrames(minutes));
    }

    private void AdjustGameTimeByMinutes(int minutes)
    {
        GameManager gm = ModContent.GetInstance<GameManager>();
        int deltaFrames = MinutesToFrames(minutes);

        if (gm.CurrentPhase == GameManager.Phase.Playing || gm._startGameCountdown.HasValue)
        {
            AdjustActiveGameTime(deltaFrames);
            return;
        }

        SetIdleGameTime(_gameTimeInFrames + deltaFrames);
    }

    private void AdjustActiveGameTime(int deltaFrames)
    {
        if (deltaFrames == 0)
            return;

        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            ModContent.GetInstance<GameManager>().AdjustTimeRemaining(deltaFrames);
        }
        else if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
            packet.Write((byte)AdventurePacketIdentifier.GameTimer);
            packet.Write((byte)GameTimerNetHandler.GameTimerPacketType.AdjustGameTime);
            packet.Write(deltaFrames);
            packet.Send();
        }
    }

    private void SetIdleGameTime(int frames)
    {
        int maxFrames = MinutesToFrames(MaxGameMinutes);
        _gameTimeInFrames = Math.Clamp(frames, 0, maxFrames);
        _gameTimeSlider.SetValue(_gameTimeInFrames / (float)FramesPerMinute);
    }

    private void StartSelectedGame()
    {
        GameManager gm = ModContent.GetInstance<GameManager>();

        if (gm.CurrentPhase == GameManager.Phase.Playing)
            return;

        if (gm._startGameCountdown.HasValue)
        {
            Main.NewText(Language.GetTextValue("Mods.PvPAdventure.Tools.DLStartGameTool.CannotStart"), Color.Red);
            return;
        }

        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            gm.StartGame(time: _gameTimeInFrames, countdownTimeInSeconds: _countdownTimeInSeconds);
        }
        else if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
            packet.Write((byte)AdventurePacketIdentifier.GameTimer);
            packet.Write((byte)GameTimerNetHandler.GameTimerPacketType.StartGame);
            packet.Write(_gameTimeInFrames);
            packet.Write(_countdownTimeInSeconds);
            packet.Send();
        }

        ModContent.GetInstance<StartGameSystem>().Hide();
    }

    private static void OpenEndGameConfirmation()
    {
        GameManager gm = ModContent.GetInstance<GameManager>();

        if (gm.CurrentPhase == GameManager.Phase.Playing)
        {
            ModContent.GetInstance<EndGameSystem>().ToggleActive();
        }
        else if (gm._startGameCountdown.HasValue)
        {
            Main.NewText(Language.GetTextValue("Mods.PvPAdventure.Tools.DLEndGameTool.CountdownInProgress"), Color.Red);
        }
        else
        {
            Main.NewText(Language.GetTextValue("Mods.PvPAdventure.Tools.DLEndGameTool.GameNotStartedYet"), Color.Red);
        }
    }

    private void UpdateStartButton(bool isPlaying, bool hasCountdown)
    {
        if (isPlaying)
        {
            _startButton.SetText(Language.GetTextValue("Mods.PvPAdventure.Tools.DLStartGameTool.Running"));
            _startButton.BackgroundColor = new Color(50, 50, 55) * 0.8f;
            return;
        }

        if (hasCountdown)
        {
            _startButton.SetText(Language.GetTextValue("Mods.PvPAdventure.Tools.DLStartGameTool.Countdown"));
            _startButton.BackgroundColor = new Color(80, 70, 35) * 0.85f;
            return;
        }

        _startButton.SetText(Language.GetTextValue("Mods.PvPAdventure.Tools.DLStartGameTool.StartExclamation"));
        _startButton.BackgroundColor = new Color(73, 94, 171) * 0.8f;
    }

    private static UITextPanel<string> CreateTextButton(string text, string tooltip, Action action, float width, float height)
    {
        UITextPanel<string> button = new(text, 0.85f)
        {
            Width = { Pixels = width },
            Height = { Pixels = height }
        };
        button.SetPadding(0f);
        button.OnMouseOver += (_, _) =>
        {
            button.BorderColor = Color.Yellow;
            if (!string.IsNullOrWhiteSpace(tooltip))
                Main.instance.MouseText(tooltip);
        };
        button.OnMouseOut += (_, _) => button.BorderColor = Color.Black;
        button.OnLeftClick += (_, _) =>
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
            action?.Invoke();
        };
        return button;
    }

    private void AppendGameTimeButton(UIElement row, string text, int index, int minutes, string tooltipKey)
    {
        UITextPanel<string> button = CreateTextButton(
            text,
            Language.GetTextValue($"Mods.PvPAdventure.Tools.DLStartGameTool.{tooltipKey}"),
            () => AdjustGameTimeByMinutes(minutes),
            42f,
            28f);
        button.Left.Set(index * 49f, 0f);
        row.Append(button);
    }

    private static void ApplyButtonEnabledVisuals(UITextPanel<string> button, bool enabled)
    {
        button.BackgroundColor = enabled
            ? new Color(73, 94, 171) * 0.8f
            : new Color(45, 45, 50) * 0.75f;
        button.BorderColor = enabled ? Color.Black : Color.DimGray;
    }

    private static int MinutesToFrames(int minutes)
    {
        return minutes * FramesPerMinute;
    }
}
