using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.System;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.Integrations.HerosMod.StartGame;

internal class StartGameElement : UIElement
{
    private UIPanel titlePanel;
    private UIPanel contentPanel;
    private UIText titleText;
    private UITextPanel<string> startButton;
    private SliderElement gameTimeSlider;
    private SliderElement countdownSlider;
    private int countdownTimeInSeconds = 10;
    private int gameTimeInFrames = 195 * 60 * 60;
    private bool dragging;
    private Vector2 dragOffset;

    public override void OnInitialize()
    {
        Width.Set(350, 0);
        Height.Set(180, 0);
        HAlign = 0.5f;
        VAlign = 0.7f;

        // Title panel
        titlePanel = new UIPanel
        {
            Width = new StyleDimension(0, 1),
            Height = new StyleDimension(40, 0),
            BackgroundColor = new Color(63, 82, 151) * 0.7f,
            BorderColor = Color.Black
        };
        titlePanel.SetPadding(0);

        titleText = new UIText("Start Game", 0.7f, true)
        {
            HAlign = 0.5f,
            VAlign = 0.5f
        };

        titlePanel.Append(titleText);
        Append(titlePanel);

        // Close panel
        UIPanel closePanel = new()
        {
            Height = new StyleDimension(0, precent: 1),
            Width = new StyleDimension(pixels: 40, 0),
            HAlign = 1f,
            VAlign = 0.5f
        };
        closePanel.OnLeftClick += (_, _) => ModContent.GetInstance<StartGameSystem>().Hide();
        closePanel.OnMouseOver += (_, _) => closePanel.BorderColor = Color.Yellow;
        closePanel.OnMouseOut += (_, _) => closePanel.BorderColor = Color.Black;
        UIText closeText = new("X", large: true, textScale: 0.55f)
        {
            HAlign = 0.5f,
            VAlign = 0.5f
        };
        closePanel.Append(closeText);
        titlePanel.Append(closePanel);

        // Content panel
        contentPanel = new UIPanel
        {
            Top = new StyleDimension(40, 0),
            Width = new StyleDimension(0, 1),
            Height = new StyleDimension(-40, 1),
            BackgroundColor = new Color(20, 20, 60) * 0.7f,
            BorderColor = Color.Black
        };
        contentPanel.SetPadding(12);

        gameTimeSlider = new SliderElement(
            label: "Time",
            min: 0f,
            max: 195f,
            defaultValue: 195f,
            step: 1f,
            onValueChanged: value =>
            {
                int totalMinutes = (int)value;
                gameTimeInFrames = totalMinutes * 60 * 60;
            }
        );

        contentPanel.Append(gameTimeSlider);

        countdownSlider = new SliderElement(
            label: "Countdown",
            min: 0f,
            max: 10f,
            defaultValue: 10f,
            step: 1f,
            onValueChanged: value =>
            {
                countdownTimeInSeconds = (int)value;
            }
        );
        countdownSlider.Top.Set(26, 0);

        contentPanel.Append(countdownSlider);

        startButton = new UITextPanel<string>("Start!")
        {
            Width = new StyleDimension(120, 0),
            Height = new StyleDimension(40, 0),
            HAlign = 0.5f,
            VAlign = 1f,
            Top = new StyleDimension(0, 0),
        };
        startButton.OnMouseOver += (_, _) => startButton.BorderColor = Color.Yellow;
        startButton.OnMouseOut += (_, _) => startButton.BorderColor = Color.Black;
        startButton.OnLeftClick += (_, _) =>
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                var gm = ModContent.GetInstance<GameManager>();
                gm.StartGame(time: gameTimeInFrames, countdownTimeInSeconds: countdownTimeInSeconds-1);
            }
            else if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                var packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
                packet.Write((byte)AdventurePacketIdentifier.StartGame);
                packet.Write(gameTimeInFrames);
                packet.Write(countdownTimeInSeconds);
                packet.Send();
            }

            ModContent.GetInstance<StartGameSystem>().Hide();
        };

        contentPanel.Append(startButton);
        Append(contentPanel);
    }

    public override void Update(GameTime gameTime)
    {
        if (IsMouseHovering)
        {
            Main.LocalPlayer.mouseInterface = true;
        }

        base.Update(gameTime);
        UpdateDragging();
    }

    private void UpdateDragging()
    {
        if (dragging)
        {
            float x = Main.mouseX - dragOffset.X;
            float y = Main.mouseY - dragOffset.Y;
            Left.Pixels = x;
            Top.Pixels = y;
            Recalculate();
        }

        if (Parent is null)
            return;

        Rectangle parentSpace = Parent.GetDimensions().ToRectangle();
        if (!GetDimensions().ToRectangle().Intersects(parentSpace))
        {
            Left.Pixels = Utils.Clamp(Left.Pixels, 0, parentSpace.Right - Width.Pixels);
            Top.Pixels = Utils.Clamp(Top.Pixels, 0, parentSpace.Bottom - Height.Pixels);
            Recalculate();
        }
    }

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        if (titlePanel != null && titlePanel.ContainsPoint(evt.MousePosition))
        {
            dragging = true;
            dragOffset = evt.MousePosition - new Vector2(Left.Pixels, Top.Pixels);
        }
    }

    public override void LeftMouseUp(UIMouseEvent evt)
    {
        dragging = false;
        Recalculate();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
    }
}
