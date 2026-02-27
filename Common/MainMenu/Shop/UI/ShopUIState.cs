using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Common.MainMenu.Gems;
using PvPAdventure.Common.MainMenu.MatchHistory.UI;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace PvPAdventure.Common.MainMenu.Shop.UI;

public sealed class ShopUIState : ResizableUIState
{
    private readonly UIState? previous;
    private ShopUIPanel shopPanel = null!;

    public ShopUIState(UIState? previous) => this.previous = previous;

    public override void OnInitialize()
    {
        var root = new UIElement
        {
            Width = new StyleDimension(0f, 0.7f),
            Height = new StyleDimension(0f, 0.9f),
            Top = new StyleDimension(210, 0f),
            HAlign = 0.5f,
            MinWidth = new StyleDimension(700f, 0f),
            MaxWidth = new StyleDimension(900f, 0f)
        };
        Append(root);

        var @base = new UIElement
        {
            Width = StyleDimension.Fill,
            Height = new StyleDimension(-160f * 1.75f, 1f),
            //BackgroundColor = new Color(33, 43, 79) * 0.8f
        };
        root.Append(@base);

        shopPanel = new ShopUIPanel
        {
            Width = StyleDimension.Fill,
            Height = StyleDimension.Fill,
            Top = new StyleDimension(6f, 0f),
            //BackgroundColor = UICommon.DefaultUIBlueMouseOver
        };
        @base.Append(shopPanel);

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
        base.OnActivate();
        GemStorage.Read();
        UnlockedStorage.Load();
        shopPanel.Refresh();
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