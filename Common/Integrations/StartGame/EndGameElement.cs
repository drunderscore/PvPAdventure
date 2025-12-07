using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.System;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.Integrations.StartGame;

internal class EndGameElement : UIElement
{
    private UIPanel titlePanel;
    private UIPanel contentPanel;
    private UIText titleText;
    private UITextPanel<string> yesButton;
    private UITextPanel<string> noButton;

    // Dragging
    private bool dragging;
    private Vector2 dragOffset;

    public override void OnInitialize()
    {
        Width.Set(400, 0);
        Height.Set(100, 0);
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

        titleText = new UIText("Really end game?", 0.7f, true)
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

        // Buttons
        noButton = new UITextPanel<string>("No")
        {
            Width = new StyleDimension(120, 0),
            Height = new StyleDimension(40, 0),
            HAlign = 0.25f,
            VAlign = 0.5f
        };
        noButton.OnMouseOver += (_, _) => noButton.BorderColor = Color.Yellow;
        noButton.OnMouseOut += (_, _) => noButton.BorderColor = Color.Black;
        noButton.OnLeftClick += (_, _) => ModContent.GetInstance<StartGameSystem>().Hide();

        yesButton = new UITextPanel<string>("Yes")
        {
            Width = new StyleDimension(120, 0),
            Height = new StyleDimension(40, 0),
            HAlign = 0.75f,
            VAlign = 0.5f
        };
        yesButton.OnMouseOver += (_, _) => yesButton.BorderColor = Color.Yellow;
        yesButton.OnMouseOut += (_, _) => yesButton.BorderColor = Color.Black;
        yesButton.OnLeftClick += (_, _) =>
        {
            var gm = ModContent.GetInstance<GameManager>();

            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                gm.EndGame();
            }
            else if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                var packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
                packet.Write((byte)AdventurePacketIdentifier.EndGame);
                packet.Send();
            }

            ModContent.GetInstance<StartGameSystem>().Hide();
        };

        contentPanel.Append(noButton);
        contentPanel.Append(yesButton);

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

            var d = GetDimensions();
            float w = d.Width;
            float h = d.Height;

            //if (x < 0) x = 0;
            //else if (x + w > Main.screenWidth) x = Main.screenWidth - w;

            //if (y < 0) y = 0;
            //else if (y + h > Main.screenHeight) y = Main.screenHeight - h;

            Left.Pixels = x;
            Top.Pixels = y;
            Recalculate();
        }

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
        dragging = true;
        dragOffset = evt.MousePosition - new Vector2(Left.Pixels, Top.Pixels);
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