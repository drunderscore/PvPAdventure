using Microsoft.Xna.Framework;
using PvPAdventure.Common.Spectator.SpectatorMode;
using PvPAdventure.Core.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI;

internal sealed class SpectatorSettingsPanelUIState : UIState
{
    private SpectatorSettingsPanel settingsPanel;

    public override void OnActivate()
    {
        RemoveAllChildren();
        EnsurePanel();
    }

    public void Rebuild()
    {
        settingsPanel?.Rebuild();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        Player local = Main.LocalPlayer;
        if (local?.active == true && SpectatorModeSystem.IsInSpectateMode(local))
            EnsurePanel();
        else
            settingsPanel?.Remove();
    }

    private void EnsurePanel()
    {
        if (settingsPanel?.Parent is not null)
            return;

        settingsPanel ??= new SpectatorSettingsPanel();
        Append(settingsPanel);
    }
}

internal sealed class SpectatorSettingsEyeUIState : UIState
{
    private SpectatorSettingsEyeOnlyButton eyeButton;

    public override void OnActivate()
    {
        RemoveAllChildren();
        EnsureEyeButton();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        Player local = Main.LocalPlayer;
        if (local?.active == true && SpectatorModeSystem.IsInSpectateMode(local))
            EnsureEyeButton();
        else
            eyeButton?.Remove();
    }

    private void EnsureEyeButton()
    {
        if (eyeButton?.Parent is not null)
            return;

        eyeButton ??= new SpectatorSettingsEyeOnlyButton();
        Append(eyeButton);
    }

    private sealed class SpectatorSettingsEyeOnlyButton : UIPanel
    {
        public SpectatorSettingsEyeOnlyButton()
        {
            HAlign = 1f;
            Left.Set(-SpectatorSettingsPanel.RightOffset, 0f);
            Top.Set(SpectatorSettingsPanel.TopOffset, 0f);
            Width.Set(SpectatorSettingsPanel.HeaderHeight, 0f);
            Height.Set(SpectatorSettingsPanel.HeaderHeight, 0f);
            SetPadding(0f);
            BackgroundColor = new Color(63, 82, 151);
            BorderColor = Color.Black;

            OnLeftClick += (_, _) =>
            {
                SoundEngine.PlaySound(SoundID.MenuOpen);
                ModContent.GetInstance<SpectatorUISystem>().ToggleSpectatorSettingsPanel();
            };

            Append(new UIImage(Ass.Icon_Eye.Value)
            {
                HAlign = 0.5f,
                VAlign = 0.5f
            });
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            BorderColor = IsMouseHovering ? Color.Yellow : Color.Black;

            if (IsMouseHovering)
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.instance.MouseText("Show spectator info");
            }
        }
    }
}
