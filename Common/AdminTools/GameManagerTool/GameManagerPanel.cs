using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Game;
using PvPAdventure.Core.Net;
using PvPAdventure.Core.Utilities;
using PvPAdventure.UI;
using System;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.AdminTools.GameManagerTool;

internal class GameManagerPanel : UIDraggablePanel
{
    private const int FramesPerSecond = 60;
    private const int FramesPerMinute = FramesPerSecond * 60;
    private const int MinGameMinutes = 1;
    private const int MaxGameMinutes = 195;
    private const int MaxCountdownSeconds = 300;
    private const int CountdownStepSeconds = 10;
    private const int GameTimeStepMinutes = 15;

    private UIStatusTextRow _status;
    private readonly UIGameManagerSlider _countdown;
    private readonly UIGameManagerSlider _time;

    private int _countdownTimeInSeconds = 10;
    private int _gameTimeInFrames = MaxGameMinutes * FramesPerMinute;
    private int _configuredGameTimeInFrames = MaxGameMinutes * FramesPerMinute;
    private bool _wasActive;

    protected override float MinResizeH => 380f;
    protected override float MinResizeW => 365f;

    public GameManagerPanel()
        : base(Language.GetTextValue("Mods.PvPAdventure.Tools.DLStartGameTool.AdventureGameTimer"))
    {
        Width.Set(365, 0);
        Height.Set(380, 0);
        HAlign = 0.5f;
        VAlign = 0.7f;
        ContentPanel.SetPadding(0);

        float top = 0f;
        _status = AddStatus(ref top);
        UpdateStatus(ModContent.GetInstance<GameManager>());

        _countdown = AddSliderSection(ref top, "Countdown", Ass.IconTime.Value,
            0f,
            MaxCountdownSeconds,
            _countdownTimeInSeconds,
            1f,
            value => _countdownTimeInSeconds = (int)Math.Round(value),
            value => SetCountdownSeconds((int)Math.Round(value), syncActiveCountdown: true),
            FormatSeconds,
            CountdownStepSeconds,
            iconScale: 0.8f);

        _time = AddSliderSection(ref top, "Time", Ass.Stopwatch.Value,
            MinGameMinutes,
            MaxGameMinutes,
            MaxGameMinutes,
            1f,
            value => _gameTimeInFrames = MinutesToFrames((int)Math.Round(value)),
            value => ApplyGameTimeSliderValue((int)Math.Round(value)),
            FormatMinutes,
            GameTimeStepMinutes);

        // Mirror the slider rows: an icon + label header, then its control below.
        AddIconLabel(ref top, "Start", Ass.IconStartGame.Value, 0.9f);
        AddActionRow(ref top, "Start!", StartSelectedGame,
            rightClick: null,
            tooltip: "Start the countdown with the selected parameters",
            enabled: CanStartGame,
            disabledTooltip: StartDisabledReason);

        AddIconLabel(ref top, "End", Ass.IconEndGame.Value, 0.9f);
        AddActionRow(ref top, "End Game", OpenEndGameConfirmation,
            rightClick: InstantEndGame,
            tooltip: "Left click to open confirmation\nRight click to instantly end game",
            enabled: CanEndGame,
            disabledTooltip: EndDisabledReason);
    }

    public override void Update(GameTime gameTime)
    {
        GameManager gm = ModContent.GetInstance<GameManager>();
        bool isPlaying = gm.CurrentPhase == GameManager.Phase.Playing;
        bool hasCountdown = gm._startGameCountdown.HasValue;
        bool isActive = isPlaying || hasCountdown;

        UpdateStatus(gm);
        _countdown.Enabled = !isPlaying;

        base.Update(gameTime);

        if (hasCountdown && !_countdown.IsHeld)
        {
            float secondsLeft = gm._startGameCountdown.Value / (float)FramesPerSecond;
            _countdownTimeInSeconds = (int)Math.Round(secondsLeft);
            _countdown.SetValue(secondsLeft);
        }

        if (isActive && !_time.IsHeld)
        {
            _gameTimeInFrames = gm.TimeRemaining;
            _time.SetValue(gm.TimeRemaining / (float)FramesPerMinute);
        }
        else if (!isActive && _wasActive && !_time.IsHeld)
        {
            // TimeRemaining reaches zero when a match ends. Keep that display value from becoming
            // the duration of the next match opened from this long-lived panel.
            _gameTimeInFrames = _configuredGameTimeInFrames;
            _time.SetValue(_gameTimeInFrames / (float)FramesPerMinute);
        }

        _wasActive = isActive;
    }

    protected override void OnClosePanelLeftClick()
    {
        ModContent.GetInstance<GameManagerUISystem>().Hide();
    }

    private UIStatusTextRow AddStatus(ref float top)
    {
        UIStatusTextRow row = new();
        AddElement(row, ref top);
        return row;
    }

    private UIGameManagerSlider AddSliderSection(ref float top, string title, Texture2D icon, float min, float max, float value, float step, Action<float> onChange, Action<float> onRelease, Func<float, string> format, float buttonStep = 0f, float iconScale = 1f)
    {
        UIGameManagerSliderRow row = new(title, icon, min, max, value, step, onChange, onRelease, format, buttonStep, iconScale);
        AddElement(row, ref top);
        return row.Slider;
    }

    private void AddIconLabel(ref float top, string text, Texture2D icon, float iconScale = 1f)
    {
        AddElement(new UIIconLabelRow(text, icon, iconScale: iconScale), ref top);
    }

    private void AddActionRow(ref float top, string text, Action onClick, Action rightClick, string tooltip, Func<bool> enabled = null, Func<string> disabledTooltip = null)
    {
        UITextActionPanel button = new(text, onClick, width: 0f, height: 34f, rightAction: rightClick, tooltip: tooltip)
        {
            Left = { Pixels = 16f },
            Top = { Pixels = top },
            Width = { Pixels = -32f, Percent = 1f }, // full content width, 16px margin each side
            IsEnabled = enabled,
            DisabledTooltip = disabledTooltip
        };

        ContentPanel.Append(button);
        top += 40f;
    }

    private void AddElement(UIElement element, ref float top)
    {
        element.Top.Set(top, 0f);
        ContentPanel.Append(element);
        top += element.Height.Pixels;
    }

    private void UpdateStatus(GameManager gm)
    {
        if (_status == null || gm == null)
            return;

        if (gm._startGameCountdown.HasValue)
        {
            int secondsLeft = Math.Max(0, (int)Math.Ceiling(gm._startGameCountdown.Value / (float)FramesPerSecond));
            _status.SetStatus($"Countdown ({secondsLeft}s)", Color.Yellow);
            return;
        }

        if (gm.CurrentPhase == GameManager.Phase.Playing)
        {
            _status.SetStatus("Playing", Color.LimeGreen);
            return;
        }

        _status.SetStatus("Waiting", RedTeamColor());
    }

    private static Color RedTeamColor() => Main.teamColor[(int)Terraria.Enums.Team.Red];

    private void SetCountdownSeconds(int seconds, bool syncActiveCountdown)
    {
        GameManager gm = ModContent.GetInstance<GameManager>();
        if (gm.CurrentPhase == GameManager.Phase.Playing)
            return;

        seconds = Math.Clamp(seconds, 0, MaxCountdownSeconds);
        _countdownTimeInSeconds = seconds;
        _countdown.SetValue(seconds);

        if (!syncActiveCountdown || !gm._startGameCountdown.HasValue)
            return;

        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            gm.SetCountdown(seconds);
        }
        else if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
            packet.Write((byte)AdventurePacketIdentifier.GameManager);
            packet.Write((byte)GameManagerNetHandler.GameManagerPacketType.UpdateCountdown);
            packet.Write(seconds);
            packet.Send();
        }
    }

    private void ApplyGameTimeSliderValue(int minutes)
    {
        GameManager gm = ModContent.GetInstance<GameManager>();
        int desiredFrames = MinutesToFrames(Math.Clamp(minutes, 0, MaxGameMinutes));

        if (gm.CurrentPhase == GameManager.Phase.Playing || gm._startGameCountdown.HasValue)
        {
            AdjustActiveGameTime(desiredFrames - gm.TimeRemaining);
            return;
        }

        SetIdleGameTime(desiredFrames);
    }

    private static void AdjustActiveGameTime(int deltaFrames)
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
            packet.Write((byte)AdventurePacketIdentifier.GameManager);
            packet.Write((byte)GameManagerNetHandler.GameManagerPacketType.AdjustGameTime);
            packet.Write(deltaFrames);
            packet.Send();
        }
    }

    private void SetIdleGameTime(int frames)
    {
        int maxFrames = MinutesToFrames(MaxGameMinutes);
        int minFrames = MinutesToFrames(MinGameMinutes);
        _gameTimeInFrames = Math.Clamp(frames, minFrames, maxFrames);
        _configuredGameTimeInFrames = _gameTimeInFrames;
        _time.SetValue(_gameTimeInFrames / (float)FramesPerMinute);
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

        _configuredGameTimeInFrames = _gameTimeInFrames;

        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            gm.StartGame(time: _gameTimeInFrames, countdownTimeInSeconds: _countdownTimeInSeconds);
        }
        else if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
            packet.Write((byte)AdventurePacketIdentifier.GameManager);
            packet.Write((byte)GameManagerNetHandler.GameManagerPacketType.StartGame);
            packet.Write(_gameTimeInFrames);
            packet.Write(_countdownTimeInSeconds);
            packet.Send();
        }
    }

    private static void OpenEndGameConfirmation()
    {
        GameManager gm = ModContent.GetInstance<GameManager>();

        if (gm.CurrentPhase == GameManager.Phase.Playing || gm._startGameCountdown.HasValue)
        {
            ModContent.GetInstance<ConfirmEndGameUISystem>().ToggleActive();
        }
        else
        {
            Main.NewText(Language.GetTextValue("Mods.PvPAdventure.Tools.DLEndGameTool.GameNotStartedYet"), Color.Red);
        }
    }

    private static void InstantEndGame()
    {
        GameManager gm = ModContent.GetInstance<GameManager>();

        if (gm.CurrentPhase != GameManager.Phase.Playing && !gm._startGameCountdown.HasValue)
        {
            Main.NewText(Language.GetTextValue("Mods.PvPAdventure.Tools.DLEndGameTool.GameNotStartedYet"), Color.Red);
            return;
        }

        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            gm.EndGame();
        }
        else if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
            packet.Write((byte)AdventurePacketIdentifier.GameManager);
            packet.Write((byte)GameManagerNetHandler.GameManagerPacketType.EndGame);
            packet.Send();
        }
    }

    private static bool CanStartGame()
    {
        GameManager gm = ModContent.GetInstance<GameManager>();
        return gm.CurrentPhase != GameManager.Phase.Playing && !gm._startGameCountdown.HasValue;
    }

    private static string StartDisabledReason()
    {
        GameManager gm = ModContent.GetInstance<GameManager>();
        if (gm.CurrentPhase == GameManager.Phase.Playing)
            return Language.GetTextValue("Mods.PvPAdventure.Tools.DLStartGameTool.AlreadyInProgress");
        if (gm._startGameCountdown.HasValue)
            return Language.GetTextValue("Mods.PvPAdventure.Tools.DLStartGameTool.CannotStart");
        return null;
    }

    private static bool CanEndGame()
    {
        GameManager gm = ModContent.GetInstance<GameManager>();
        return gm.CurrentPhase == GameManager.Phase.Playing || gm._startGameCountdown.HasValue;
    }

    private static string EndDisabledReason()
    {
        GameManager gm = ModContent.GetInstance<GameManager>();
        return Language.GetTextValue("Mods.PvPAdventure.Tools.DLEndGameTool.GameNotStartedYet");
    }

    private static int MinutesToFrames(int minutes) => minutes * FramesPerMinute;

    private static string FormatSeconds(float value) => $"{Math.Max(0, (int)Math.Round(value))} seconds";

    private static string FormatMinutes(float value)
    {
        int minutes = Math.Max(0, (int)Math.Round(value));
        int hours = minutes / 60;
        int remainder = minutes % 60;

        if (hours > 0 && remainder > 0)
            return $"{hours} hrs {remainder} mins";

        return hours > 0 ? $"{hours} hrs" : $"{minutes} mins";
    }
}
