using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Common.MainMenu.MatchHistory;
using PvPAdventure.Common.MainMenu.MatchHistory.UI;
using PvPAdventure.Common.MainMenu.State;
using PvPAdventure.Common.MainMenu.UI;
using Steamworks;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace PvPAdventure.Common.MainMenu.PlayerStats;

public sealed class PlayerStatsUIState : ResizableUIState
{
    private readonly UIState? previous;
    private PlayerStatsUIPanel statsPanel = null!;
    private readonly List<MatchResult> matches = [];

    public PlayerStatsUIState(UIState? previous) => this.previous = previous;

    public override void OnInitialize()
    {
        var root = new UIElement
        {
            Width = new StyleDimension(0f, 0.5f),
            Height = new StyleDimension(0f, 0.8f),
            Top = new StyleDimension(210f, 0f),
            HAlign = 0.5f,
            MinWidth = new StyleDimension(700f, 0f),
            MaxWidth = new StyleDimension(600f, 0f)
        };
        Append(root);

        var backPanel = new UIElement
        {
            Width = StyleDimension.Fill,
            Height = new StyleDimension(-160f * 1.75f, 1f),
            //BackgroundColor = new Color(33, 43, 79) * 0.8f
        };
        root.Append(backPanel);

        backPanel.Append(new UITextPanel<LocalizedText>(Language.GetText("Mods.PvPAdventure.MainMenu.Stats"), 0.9f, true)
        {
            Top = new StyleDimension(-52f, 0f),
            HAlign = 0.5f,
            BackgroundColor = UICommon.DefaultUIBlue
        });

        statsPanel = new PlayerStatsUIPanel
        {
            Width = StyleDimension.Fill,
            Height = StyleDimension.Fill,
            Top = new StyleDimension(6f, 0f),
            //BackgroundColor = UICommon.DefaultUIBlueMouseOver
        };
        backPanel.Append(statsPanel);

        root.Append(new UIBackButton<LocalizedText>(Language.GetText("UI.Back"), GoBack)
        {
            Top = new StyleDimension(-(160f + 50f), 0f),
            VAlign = 1f,
            HAlign = 0.5f,
            Width = StyleDimension.Fill,
            Left = new StyleDimension(0f, 0f)
        });
    }

    public override void OnActivate()
    {
        base.OnActivate();

        //matches.Clear();
        //matches.AddRange(MatchStorage.LoadMatchesFromFolder(MatchStorage.GetFolderPath()));

        //ulong steamId;
        //try { steamId = SteamUser.GetSteamID().m_SteamID; }
        //catch { steamId = 0; }

        //statsPanel.Update(matches, steamId);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        UILinkPointNavigator.Shortcuts.BackButtonCommand = 7;

        if (Main.keyState.IsKeyDown(Keys.Escape) && !Main.oldKeyState.IsKeyDown(Keys.Escape))
            GoBack();
    }

    private void GoBack()
    {
        SoundEngine.PlaySound(SoundID.MenuClose);
        var menu = ModContent.GetInstance<MainMenuSystem>();
        menu.ui?.SetState(previous);
    }
}