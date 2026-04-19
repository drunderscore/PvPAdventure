using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Common.Spectator.UI.State;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using System;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.ModLoader.UI.ModBrowser;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace PvPAdventure.Common.MainMenu.State;

public abstract class MainMenuPageUIState : UIState
{
    // Screen metrics tracking for responsive UI
    private int lastScreenWidth;
    private int lastScreenHeight;
    private float lastUiScale;

    // Flag to rebuild UI with F5 in debug mode
    protected bool pendingRebuild;

    // Content
    private UIPanel mainPanel;
    private UIAutoScaleTextTextPanel<string> refreshButton;
    private UIAutoScaleTextTextPanel<string> backButton;
    private UIBrowserStatus statusBadge;
    private string statusText = "Completed";
    private MainMenuLoaderImage loaderImage;

    // Dimensions
    protected virtual float MainPanelMinWidth => 650f;

    protected virtual string HeaderLocalizationKey => string.Empty;

    /// <summary>
    /// Override this method to populate the main content panel: <see cref="mainPanel"/>
    /// </summary>
    protected virtual void Populate(UIPanel panel) { }

    /// <summary>
    /// Override this method to refresh the content when the refresh button is clicked.
    /// </summary>
    protected virtual void RefreshContent() { }

    public override void OnInitialize()
    {
        Rebuild();
    }

    public override void OnActivate()
    {
        RefreshContent();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (KeyboardHelper.JustPressed(Keys.Escape))
            GoBack();

        UpdateScreenMetrics();
        UILinkPointNavigator.Shortcuts.BackButtonCommand = 7;

#if DEBUG
        if (KeyboardHelper.JustPressed(Keys.F5))
            pendingRebuild = true;

        if (pendingRebuild)
        {
            Log.Debug("F5 pressed, rebuilding state...");
            pendingRebuild = false;
            Rebuild();
            RefreshContent();
            return;
        }
#endif
    }

    private void UpdateScreenMetrics()
    {
        if (lastScreenWidth != Main.screenWidth || lastScreenHeight != Main.screenHeight || lastUiScale != Main.UIScale)
        {
            //Log.Debug("Recalculating page due to screen size change.");
            lastScreenWidth = Main.screenWidth;
            lastScreenHeight = Main.screenHeight;
            lastUiScale = Main.UIScale;
            Recalculate();
        }
    }

    protected void SetCurrentAsyncState(AsyncProviderState state, string? text = null)
    {
        bool loading = state == AsyncProviderState.Loading;

        refreshButton?.SetText(loading ? "Loading" : "Refresh");
        statusBadge?.SetCurrentState(state);

        statusText = text ?? state.ToString();

        if (loading)
        {
            if (loaderImage != null && loaderImage.Parent == null)
                mainPanel.Append(loaderImage);
        }
        else if (loaderImage?.Parent != null)
        {
            loaderImage.Parent.RemoveChild(loaderImage);
        }
    }

    private void GoBack()
    {
        SoundEngine.PlaySound(SoundID.MenuClose);

        MainMenuTPVPABrowserUIState previous = new();
        previous.Activate();
        ModContent.GetInstance<MainMenuSystem>().ui?.SetState(previous);
    }

    private void Rebuild()
    {
        // Dimensions
        const float mainPanelTop = 220f;
        const float mainPanelBottomReserve = 110f;
        const float widthPercent = 0.8f;
        float minWidth = MainPanelMinWidth;
        const float maxWidth = 650f;

        const float buttonHeight = 40f;
        const float buttonGap = 6f;
        const float footerGap = 6f;

        RemoveAllChildren();

        // Add root
        UIElement root = new() { HAlign = 0.5f };
        root.Width.Set(0f, widthPercent);
        root.MinWidth.Set(minWidth, 0f);
        root.MaxWidth.Set(maxWidth, 0f);
        root.Top.Set(mainPanelTop, 0f);
        root.Height.Set(-mainPanelTop, 1f);
        Append(root);

        // Add main panel
        mainPanel = new UIPanel
        {
            BackgroundColor = new Color(33, 43, 79) * 0.8f,
            BorderColor = Color.Black
        };
        mainPanel.Width.Set(0f, 1f);
        mainPanel.Height.Set(-mainPanelBottomReserve, 1f);
        root.Append(mainPanel);

        // Populate the main panel with content
        Populate(mainPanel);

        // Add loader image
        loaderImage = new MainMenuLoaderImage(0.5f, 0.5f, 1f)
        {
            WithBackground = false
        };

        // Add header panel
        if (!string.IsNullOrWhiteSpace(HeaderLocalizationKey))
        {
            UITextPanel<LocalizedText> header = new(Language.GetText(HeaderLocalizationKey), 0.8f, true)
            {
                HAlign = 0.5f,
                BackgroundColor = new Color(73, 94, 171)
            };
            header.Top.Set(mainPanelTop - 46f, 0f);
            header.SetPadding(15f);
            Append(header);
        }

        // Add footer element
        UIElement footer = new() { HAlign = 0.5f };
        footer.Width.Set(0f, 1f);
        footer.Height.Set(buttonHeight * 2f + buttonGap, 0f);
        footer.Top.Set(-mainPanelBottomReserve + footerGap, 1f);
        footer.SetPadding(0f);
        root.Append(footer);

        // Add refresh button
        refreshButton = CreateActionButton("Refresh", RefreshContent);
        footer.Append(refreshButton);

        // Add back button
        backButton = CreateActionButton("Back", GoBack);
        backButton.Top.Set(buttonHeight + buttonGap, 0f);
        footer.Append(backButton);

        // Add status badge
        statusBadge = new UIBrowserStatus
        {
            HAlign = 1f,
            Top = { Pixels = 0f }
        };
        statusBadge.OnUpdate += _ =>
        {
            if (statusBadge.IsMouseHovering)
            {
                //UICommon.TooltipMouseText(statusText);
                Main.instance.MouseText(statusText);
            }
        };
        footer.Append(statusBadge);

        SetCurrentAsyncState(AsyncProviderState.Completed);
        UpdateScreenMetrics();
    }
    
    #region Load state messaging

    public static string FormatLoadingMessage(string subject)
    {
        return $"Loading {subject}...";
    }

    public static string FormatErrorMessage(string subject, string? error)
    {
        string resolvedError = string.IsNullOrWhiteSpace(error) ? "Unknown error" : error.Trim();
        return $"Failed to load {subject}.\nError: {resolvedError}";
    }

    public static UIElement CreateWrappedMessageElement(string message, float textScale = 1f, float minHeight = 120f)
    {
        UIElement container = new();
        container.Width.Set(0f, 1f);
        container.Height.Set(minHeight, 0f);
        container.SetPadding(0f);
        container.Append(CreateWrappedMessage(message, textScale));
        return container;
    }

    public static UIText CreateWrappedMessage(string message, float textScale = 1f)
    {
        UIText text = new(message, textScale)
        {
            IsWrapped = true,
            Left = new StyleDimension(16f, 0f),
            Top = new StyleDimension(16f, 0f),
            TextOriginX = 0f,
            TextOriginY = 0f
        };
        text.Width.Set(-32f, 1f);
        return text;
    }

    public static void ShowWrappedMessage(UIElement container, string message, float textScale = 1f)
    {
        container.RemoveAllChildren();
        container.Append(CreateWrappedMessage(message, textScale));
        container.Recalculate();
    }

    #endregion

    #region UI
    private static UIAutoScaleTextTextPanel<TText> CreateActionButton<TText>(TText text, Action onClick)
    {
        UIAutoScaleTextTextPanel<TText> button = new(text);
        button.Width.Set(-10f, 0.5f);
        button.Height.Set(40f, 0f);
        button.PaddingLeft = 10f;
        button.PaddingRight = 10f;
        button.PaddingTop = 6f;
        button.PaddingBottom = 6f;
        button.BackgroundColor = UICommon.DefaultUIBlueMouseOver;
        button.BorderColor = Color.Black;

        bool playedTick = false;
        button.OnMouseOver += (_, _) =>
        {
            button.BackgroundColor = UICommon.DefaultUIBlue;
            button.BorderColor = Colors.FancyUIFatButtonMouseOver;
            if (playedTick)
                return;

            SoundEngine.PlaySound(SoundID.MenuTick);
            playedTick = true;
        };
        button.OnMouseOut += (_, _) =>
        {
            button.BackgroundColor = UICommon.DefaultUIBlueMouseOver;
            button.BorderColor = Color.Black;
            playedTick = false;
        };
        button.OnLeftClick += (_, _) => onClick();
        return button;
    }


    private sealed class MainMenuLoaderImage : UIElement
    {
        public bool WithBackground;
        public int FrameTick;
        public int Frame;

        private readonly float scale;
        private readonly Asset<Texture2D> backgroundTexture;
        private readonly Asset<Texture2D> loaderTexture;

        public MainMenuLoaderImage(float hAlign, float vAlign, float scale = 1f)
        {
            this.scale = scale;
            backgroundTexture = UICommon.LoaderBgTexture;
            loaderTexture = UICommon.LoaderTexture;

            Width.Set(200f * scale, 0f);
            Height.Set(200f * scale, 0f);
            HAlign = hAlign;
            VAlign = vAlign;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (loaderTexture?.Value == null)
                return;

            if (++FrameTick >= 5)
            {
                FrameTick = 0;
                if (++Frame >= 16)
                    Frame = 0;
            }

            CalculatedStyle dimensions = GetDimensions();
            Vector2 position = new((int)dimensions.X, (int)dimensions.Y);

            if (WithBackground && backgroundTexture?.Value != null)
            {
                spriteBatch.Draw(backgroundTexture.Value, position, new Rectangle(0, 0, 200, 200), Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }

            Rectangle frame = new(200 * (Frame / 8), 200 * (Frame % 8), 200, 200);
            spriteBatch.Draw(loaderTexture.Value, position, frame, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
    #endregion
}
