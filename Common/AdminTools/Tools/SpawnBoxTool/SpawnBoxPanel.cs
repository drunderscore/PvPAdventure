using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Game;
using PvPAdventure.Common.Spawnbox;
using PvPAdventure.Core.Utilities;
using PvPAdventure.UI;
using System;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Common.AdminTools.Tools.SpawnBoxTool;

internal sealed class SpawnBoxPanel : UIDraggablePanel
{
    private UIStatusTextRow _status;
    private UIGameManagerSlider _widthSlider;
    private UIGameManagerSlider _heightSlider;
    private UIGameManagerSlider _thicknessSlider;
    private UIGameManagerSlider _xOffsetSlider;
    private UIGameManagerSlider _yOffsetSlider;
    private int _width;
    private int _height;
    private int _thickness;
    private int _xOffset;
    private int _yOffset;

    protected override float MinResizeW => 365f;
    protected override float MinResizeH => 436f;
    protected override bool ShowRefreshButton => false;

    public SpawnBoxPanel()
        : base(Language.GetTextValue("Mods.PvPAdventure.Tools.SpawnBoxTool.DisplayName"))
    {
        Width.Set(365f, 0f);
        Height.Set(436f, 0f);
        HAlign = 0.5f;
        VAlign = 0.7f;
        ContentPanel.SetPadding(0f);

        SyncFromSystem(force: true);
        float top = 0f;
        _status = AddStatus(ref top);
        Texture2D resize = Ass.IconResize.Value;
        Texture2D origin = Ass.ConfigMapWorldSpawn.Value;
        _widthSlider = AddSlider(ref top, "Width", resize, _width, v => _width = Round(v));
        _heightSlider = AddSlider(ref top, "Height", resize, _height, v => _height = Round(v));
        _thicknessSlider = AddSlider(ref top, "Thickness", resize, _thickness, v => _thickness = Round(v), min: SpawnBoxSettings.MinThickness, max: SpawnBoxSettings.MaxThickness);
        _xOffsetSlider = AddSlider(ref top, "X Offset", origin, _xOffset, v => _xOffset = Round(v), min: SpawnBoxSettings.MinOffset, max: SpawnBoxSettings.MaxOffset);
        _yOffsetSlider = AddSlider(ref top, "Y Offset", origin, _yOffset, v => _yOffset = Round(v), min: SpawnBoxSettings.MinOffset, max: SpawnBoxSettings.MaxOffset);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (_widthSlider?.IsHeld != true && _heightSlider?.IsHeld != true && _thicknessSlider?.IsHeld != true && _xOffsetSlider?.IsHeld != true && _yOffsetSlider?.IsHeld != true)
            SyncFromSystem(force: false);

        UpdateStatus();
    }

    protected override void OnClosePanelLeftClick() => ModContent.GetInstance<SpawnBoxToolSystem>().ToggleActive();

    private UIStatusTextRow AddStatus(ref float top)
    {
        UIStatusTextRow row = new();
        row.Top.Set(top, 0f);
        ContentPanel.Append(row);
        top += row.Height.Pixels;
        UpdateStatus(row);
        return row;
    }

    private UIGameManagerSlider AddSlider(ref float top, string title, Texture2D icon, int value, Action<float> onChange, int min = SpawnBoxSettings.MinSize, int max = SpawnBoxSettings.MaxSize)
    {
        UIGameManagerSliderRow row = new(title, icon, min, max, value, 1f, onChange, _ => Commit(), v => $"{Round(v)} tiles", buttonStep: 1f, iconScale: 0.85f);
        row.Top.Set(top, 0f);
        ContentPanel.Append(row);
        top += row.Height.Pixels;
        return row.Slider;
    }

    private void Commit() => SpawnBoxNetHandler.SendSet(new SpawnBoxSettings(_width, _height, _xOffset, _yOffset, _thickness));

    private void SyncFromSystem(bool force)
    {
        SpawnBoxSettings settings = ModContent.GetInstance<SpawnBoxSystem>().Settings;
        if (!force && settings == new SpawnBoxSettings(_width, _height, _xOffset, _yOffset, _thickness))
            return;

        _width = settings.Width;
        _height = settings.Height;
        _thickness = settings.Thickness;
        _xOffset = settings.XOffset;
        _yOffset = settings.YOffset;
        _widthSlider?.SetValue(_width);
        _heightSlider?.SetValue(_height);
        _thicknessSlider?.SetValue(_thickness);
        _xOffsetSlider?.SetValue(_xOffset);
        _yOffsetSlider?.SetValue(_yOffset);
    }

    private void UpdateStatus() => UpdateStatus(_status);

    private static void UpdateStatus(UIStatusTextRow row)
    {
        if (row == null)
            return;

        SpawnBoxSystem box = ModContent.GetInstance<SpawnBoxSystem>();
        GameManager gm = ModContent.GetInstance<GameManager>();
        bool isPlaying = gm.CurrentPhase == GameManager.Phase.Playing;
        bool isInside = Main.LocalPlayer?.active == true && box.TouchesWorldHitbox(Main.LocalPlayer.Hitbox);
        bool canPass = isInside && box.CanExit;

        row.SetStatus(
            ($"{(isPlaying ? "Playing" : "Waiting")} (", Color.White),
            (isInside ? "Inside, " : "Outside, ", Color.White),
            (canPass ? "can pass" : "cannot pass", canPass ? Color.LimeGreen : RedTeamColor()),
            (")", Color.White));
    }

    private static Color RedTeamColor() => Main.teamColor[(int)Terraria.Enums.Team.Red];

    private static int Round(float value) => (int)Math.Round(value);
}
