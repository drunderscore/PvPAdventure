using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Common.MainMenu.MatchHistory;
using PvPAdventure.Common.MainMenu.MatchHistory.UI;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace PvPAdventure.Common.MainMenu.Shop.UI;

public sealed class ShopUIState : ResizableUIState
{
    private readonly UIState? previous;
    private ShopUIPanel shopPanel = null!;
    private readonly List<MatchResult> matches = [];

    public ShopUIState(UIState? previous) => this.previous = previous;

    public override void OnInitialize()
    {
        var root = new UIElement
        {
            Width = new StyleDimension(0f, 0.55f),
            Height = new StyleDimension(0f, 0.95f),
            Top = new StyleDimension(190, 0f),
            HAlign = 0.5f,
            MinWidth = new StyleDimension(650f, 0f),
            //MaxWidth = new StyleDimension(900f, 0f)
        };
        Append(root);

        var baseElement = new UIElement
        {
            Width = StyleDimension.Fill,
            Height = new StyleDimension(-160f * 1.75f, 1f),
            //BackgroundColor = new Color(33, 43, 79) * 0.8f
        };
        root.Append(baseElement);

        shopPanel = new ShopUIPanel
        {
            Width = StyleDimension.Fill,
            Height = StyleDimension.Fill,
            Top = new StyleDimension(6f, 0f),
            //BackgroundColor = UICommon.DefaultUIBlueMouseOver
        };
        shopPanel.SetPadding(12);
        shopPanel.PaddingLeft = 4;
        baseElement.Append(shopPanel);

        root.Append(new UIBackButton<LocalizedText>(Language.GetText("UI.Back"), GoBack)
        {
            Top = new StyleDimension(-(160f + 50f), 0f),
            VAlign = 1f,
            HAlign = 0f,
            Left = new StyleDimension(0f, 0f),
            Width = StyleDimension.Fill
        });
    }

    public override void OnActivate()
    {
        //// Load matches
        //matches.Clear();
        //matches.AddRange(MatchStorage.LoadMatchesFromFolder(MatchStorage.GetFolderPath()));

        //// Load profile gems
        //ProfileStorage.Load();
        //ProfileStorage.RebuildGems(matches);

        //// Refresh panel
        //shopPanel.Refresh();
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