using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Common.MainMenu.MatchHistory;
using PvPAdventure.Common.MainMenu.MatchHistory.UI;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace PvPAdventure.Common.MainMenu.Achievements.UI;

public sealed class AchievementsUIState : ResizableUIState
{
    private readonly UIState? previous;
    private AchievementsUIPanel achievementsPanel = null!;
    private readonly List<MatchResult> matches = [];

    public AchievementsUIState(UIState? previous) => this.previous = previous;

    public override void OnInitialize()
    {
        var root = new UIElement
        {
            Width = new StyleDimension(0f, 0.7f),
            Height = new StyleDimension(0f, 1f),
            Top = new StyleDimension(160f, 0f),
            HAlign = 0.5f,
            MinWidth = new StyleDimension(700f, 0f),
            MaxWidth = new StyleDimension(900f, 0f)
        };
        Append(root);

        var panel = new UIElement
        {
            Width = StyleDimension.Fill,
            Height = new StyleDimension(-160f * 1.75f, 1f),
        };
        root.Append(panel);

        achievementsPanel = new AchievementsUIPanel
        {
            Width = StyleDimension.Fill,
            Height = StyleDimension.Fill,
            Top = new StyleDimension(6f, 0f)
        };
        panel.Append(achievementsPanel);

        float btnH = 50f;
        float btnTop = -(160f + btnH);

        var btnRow = new UIElement
        {
            Top = new StyleDimension(btnTop, 0f),
            VAlign = 1f,
            Width = StyleDimension.Fill,
            Height = new StyleDimension(btnH, 0f)
        };
        btnRow.SetPadding(0f);
        root.Append(btnRow);

        float gap = 8f;
        float resetW = 110f;

        btnRow.Append(new UIBackButton<LocalizedText>(Language.GetText("UI.Back"), GoBack)
        {
            Width = StyleDimension.FromPixelsAndPercent(-(resetW + gap), 1f),
            Height = StyleDimension.Fill
        });

        var reset = new UITextPanel<string>("Reset", 0.85f, true)
        {
            Width = new StyleDimension(resetW, 0f),
            Height = StyleDimension.Fill,
            HAlign = 1f,
            BackgroundColor = new Color(63, 82, 151) * 0.8f,
            BorderColor = Color.Black
        };

        reset.OnMouseOver += (_, _) =>
        {
            SoundEngine.PlaySound(12);
            reset.BackgroundColor = new Color(73, 94, 171);
            reset.TextColor = Color.White;
            reset.BorderColor = Color.Yellow;
        };

        reset.OnMouseOut += (_, _) =>
        {
            reset.BackgroundColor = new Color(63, 82, 151) * 0.8f;
            reset.TextColor = Color.LightGray;
            reset.BorderColor = Color.Black;
        };

        reset.OnLeftClick += (_, _) => ResetAchievements();

        btnRow.Append(reset);
    }

    public override void OnActivate()
    {
        base.OnActivate();

        //matches.Clear();
        //matches.AddRange(MatchStorage.LoadMatchesFromFolder(MatchStorage.GetFolderPath()));

        //ProfileStorage.Load();
        //ProfileStorage.RebuildAchievements(matches);

        //achievementsPanel.Refresh();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        UILinkPointNavigator.Shortcuts.BackButtonCommand = 7;

        if (Main.keyState.IsKeyDown(Keys.Escape) && !Main.oldKeyState.IsKeyDown(Keys.Escape))
            GoBack();
    }

    private void ResetAchievements()
    {
        SoundEngine.PlaySound(SoundID.Grab);

        //ProfileStorage.EnsureLoaded();

        //ProfileStorage.Achievements = new AchievementProgress();
        //ProfileStorage.Save();

        //ProfileStorage.RebuildAchievements(matches);
        //ProfileStorage.RebuildGems(matches);

        //achievementsPanel.Refresh();
    }

    private void GoBack()
    {
        SoundEngine.PlaySound(SoundID.MenuClose);

        var menu = ModContent.GetInstance<MainMenuSystem>();
        menu.ui?.SetState(previous);
    }
}