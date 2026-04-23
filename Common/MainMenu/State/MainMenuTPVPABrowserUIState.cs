using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Common.MainMenu.Achievements.UI;
using PvPAdventure.Common.MainMenu.Leaderboards;
using PvPAdventure.Common.MainMenu.MatchHistory.UI;
using PvPAdventure.Common.MainMenu.PlayerStats;
using PvPAdventure.Common.MainMenu.Profile;
using PvPAdventure.Common.MainMenu.ServerList;
using PvPAdventure.Common.MainMenu.Shop.UI;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.MainMenu.State;

internal sealed class MainMenuTPVPABrowserUIState : UIState
{
    private UIText descriptionText = null!;

    private int lastScreenWidth;
    private int lastScreenHeight;
    private float lastUiScale;
    private GemsPanel gemsPanel = null!;

    private bool pendingRebuild;

    public override void OnInitialize()
    {
        Rebuild();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (KeyboardHelper.JustPressed(Keys.Escape))
            GoBackToMainMenu();
#if DEBUG
        if (KeyboardHelper.JustPressed(Keys.F5))
            pendingRebuild = true;

        if (pendingRebuild)
        {
            pendingRebuild = false;
            Rebuild();
            return;
        }
#endif

        UpdateScreenMetrics();
    }

    private void UpdateScreenMetrics()
    {
        if (lastScreenWidth != Main.screenWidth || lastScreenHeight != Main.screenHeight || lastUiScale != Main.UIScale)
            {
            //Log.Debug("Recalculating TPVPA browser due to screen size change.");
            lastScreenWidth = Main.screenWidth;
            lastScreenHeight = Main.screenHeight;
            lastUiScale = Main.UIScale;
            Recalculate();
        }
    }

    private void Rebuild()
    {
        const int marginPx = 20;
        const int topPx = 250;
        const int buttonsHeightPx = 284;
        const int backHeightPx = 50;

        RemoveAllChildren();

        int screenH = Main.minScreenH;
        int panelHeightPx = screenH - topPx - (backHeightPx + marginPx * 2);
        float separatorY = buttonsHeightPx + 12f;
        float descHeight = panelHeightPx - buttonsHeightPx - 32f;

        UIElement root = new() { HAlign = 0.5f };
        root.Width.Set(600f, 0f);
        root.Top.Set(topPx, 0f);
        root.Height.Set(screenH - topPx, 0f);

        UITextPanel<LocalizedText> header = new(Language.GetText("Mods.PvPAdventure.MainMenu.TerrariaPvPAdventure"), 0.8f, true)
        {
            HAlign = 0.5f,
            BackgroundColor = new Color(73, 94, 171)
        };
        header.Top.Set(-48f, 0f);
        header.SetPadding(15f);

        gemsPanel = new GemsPanel
        {
            HAlign = 0f,
        };
        gemsPanel.Width.Set(90, 0f);
        gemsPanel.Height.Set(42f, 0f);
        gemsPanel.Left.Set(0f, 0f);
        gemsPanel.Top.Set(-46f, 0f);
        gemsPanel.SetContent(MainMenuProfileState.Instance.Gems, MainMenuProfileState.Instance.HasSyncedFromBackend);
        gemsPanel.OnMouseOver += (_, _) =>
        {
            descriptionText.SetText("Gems are awarded for achievements and high placement in TPVPA matches.");
        };

        gemsPanel.OnMouseOut += (_, _) =>
        {
            descriptionText.SetText(Language.GetText("Workshop.HubDescriptionDefault"));
        };

        UIPanel panel = new() { BackgroundColor = new Color(33, 43, 79) * 0.8f };
        panel.Width.Set(0f, 1f);
        panel.Height.Set(panelHeightPx, 0f);

        UITextPanel<LocalizedText> back = new(Language.GetText("UI.Back"), 0.7f, true) { VAlign = 1f };
        back.Width.Set(0f, 1f);
        back.Height.Set(backHeightPx, 0f);
        back.Top.Set(-marginPx, 0f);
        back.SetSnapPoint("Back", 0);
        back.OnMouseOver += (evt, _) =>
        {
            SoundEngine.PlaySound(12);
            SetPanelColors(evt.Target, new Color(73, 94, 171), Colors.FancyUIFatButtonMouseOver);
        };
        back.OnMouseOut += (evt, _) => SetPanelColors(evt.Target, new Color(63, 82, 151) * 0.8f, Color.Black);
        back.OnLeftClick += (_, _) => GoBackToMainMenu();
        root.Append(back);

        UIElement buttons = new();
        buttons.Width.Set(0f, 1f);
        buttons.Height.Set(buttonsHeightPx, 0f);
        buttons.SetPadding(0f);

        buttons.Append(CreateButton(
            Ass.Icon_PlayMenu,
            "Mods.PvPAdventure.MainMenu.ServerList",
            "Mods.PvPAdventure.MainMenu.ServerListDescription",
            () => OpenState(() => new ServerListUIState()),
            0f,
            0f
        ));

        buttons.Append(CreateButton(
            Ass.Icon_MatchHistory,
            "Mods.PvPAdventure.MainMenu.MatchHistory",
            "Mods.PvPAdventure.MainMenu.MatchHistoryDescription",
            () => OpenState(() => new MatchHistoryUIState()),
            1f,
            0f
        ));

        buttons.Append(CreateButton(
            Ass.Icon_Stats,
            "Mods.PvPAdventure.MainMenu.Stats",
            "Mods.PvPAdventure.MainMenu.StatsDescription",
            () => OpenState(() => new PlayerStatsUIState()),
            0f,
            0.5f
        ));

        buttons.Append(CreateButton(
            Ass.Icon_Achievements,
            "Mods.PvPAdventure.MainMenu.Achievements",
            "Mods.PvPAdventure.MainMenu.AchievementsDescription",
            () => OpenState(() => new AchievementsUIState()),
            1f,
            0.5f
        ));

        buttons.Append(CreateButton(
            Ass.Icon_Shop,
            "Mods.PvPAdventure.MainMenu.Shop",
            "Mods.PvPAdventure.MainMenu.ShopDescription",
            () => OpenState(() => new ShopUIState()),
            0f,
            1f
        ));

        buttons.Append(CreateButton(
            Ass.Icon_Leaderboards,
            "Mods.PvPAdventure.MainMenu.Leaderboards",
            "Mods.PvPAdventure.MainMenu.LeaderboardsDescription",
            () => OpenState(() => new LeaderboardsUIState()),
            1f,
            1f
        ));

        UIWorkshopHub.AddHorizontalSeparator(panel, separatorY);
        panel.Append(buttons);
        panel.Append(CreateDescriptionPanel(descHeight));

        root.Append(panel);
        root.Append(gemsPanel);
        root.Append(header);
        Append(root);

        Recalculate();

        UIPanel CreateButton(Asset<Texture2D> icon, string textKey, string descKey, Action onClick, float hAlign, float vAlign)
        {
            UIPanel button = new()
            {
                HAlign = hAlign,
                VAlign = vAlign,
                BackgroundColor = new Color(63, 82, 151) * 0.7f,
                BorderColor = new Color(89, 116, 213) * 0.7f
            };
            button.Width.Set(-3f, 0.5f);
            button.Height.Set(-3f, 0.33f);
            button.SetPadding(6f);
            button.SetSnapPoint("Button", 0);

            button.OnMouseOver += (evt, _) => SetPanelColors(evt.Target, new Color(73, 94, 171), new Color(89, 116, 213));
            button.OnMouseOut += (evt, _) => SetPanelColors(evt.Target, new Color(63, 82, 151) * 0.7f, new Color(89, 116, 213) * 0.7f);

            button.OnMouseOver += (_, _) => descriptionText.SetText(Language.GetText(descKey));
            button.OnMouseOut += (_, _) => descriptionText.SetText(Language.GetText("Workshop.HubDescriptionDefault"));
            button.OnLeftClick += (_, _) => onClick();

            UIElement iconBox = new()
            {
                IgnoresMouseInteraction = true,
                VAlign = 0.5f
            };
            iconBox.Width.Set(64f, 0f);
            iconBox.Height.Set(64f, 0f);
            iconBox.Left.Set(8f, 0f);
            iconBox.Top.Set(-2f, 0f);
            button.Append(iconBox);

            UIImage bgImage = new(Ass.MenuIconBackground)
            {
                IgnoresMouseInteraction = true,
                HAlign = 0.5f,
                VAlign = 0.5f,
                ImageScale = 1f
            };
            iconBox.Append(bgImage);

            UIImage image = new(icon)
            {
                IgnoresMouseInteraction = true,
                HAlign = 0.5f,
                VAlign = 0.5f,
                ImageScale = 1.5f
            };
            iconBox.Append(image);

            UIText text = new(Language.GetText(textKey), 0.45f, true)
            {
                IgnoresMouseInteraction = true,
                HAlign = 0f,
                VAlign = 0.5f,
                TextOriginX = 0f,
                TextOriginY = 0f,
                IsWrapped = true,
                PaddingRight = 20f,
                PaddingTop = 10f
            };
            text.Width.Set(-80f, 1f);
            text.Height.Set(0f, 1f);
            text.Top.Set(5f, 0f);
            text.Left.Set(80f, 0f);
            button.Append(text);

            return button;
        }

        UIElement CreateDescriptionPanel(float height)
        {
            UISlicedImage box = new(Main.Assets.Request<Texture2D>("Images/UI/CharCreation/CategoryPanelHighlight"))
            {
                HAlign = 0.5f,
                VAlign = 1f,
                Color = Color.LightGray * 0.7f
            };
            box.Width.Set(0f, 1f);
            box.Height.Set(height, 0f);
            box.Top.Set(2f, 0f);
            box.SetSliceDepths(10);

            descriptionText = new UIText(Language.GetText("Workshop.HubDescriptionDefault"))
            {
                IsWrapped = true,
                PaddingLeft = 20f,
                PaddingRight = 20f,
                PaddingTop = 6f
            };
            descriptionText.Width.Set(0f, 1f);
            descriptionText.Height.Set(0f, 1f);
            descriptionText.Top.Set(5f, 0f);

            box.Append(descriptionText);
            return box;
        }

        static void SetPanelColors(UIElement target, Color background, Color border)
        {
            if (target is UIPanel panel)
            {
                panel.BackgroundColor = background;
                panel.BorderColor = border;
            }
        }
    }

    private void GoBackToMainMenu()
    {
        SoundEngine.PlaySound(11);
        Main.menuMode = 0;
    }

    public static void OpenState(Func<UIState> create, bool playSound = true)
    {
        if (playSound)
            SoundEngine.PlaySound(SoundID.MenuOpen);

        MainMenuSystem menu = ModContent.GetInstance<MainMenuSystem>();
        UIState state = create();
        menu.ui?.SetState(state);
        state.Recalculate();
    }
}