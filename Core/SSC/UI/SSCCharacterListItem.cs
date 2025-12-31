using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace PvPAdventure.Core.SSC.UI;

public class SSCCharacterListItem : UIPanel
{
    private readonly string characterName;
    private readonly long playTimeTicks;

    private readonly UICharacter playerPanel;
    private readonly UIText nameText;

    private readonly Player drawPlayer;

    private readonly Asset<Texture2D> dividerTexture;
    private readonly Asset<Texture2D> innerPanelTexture;

    private readonly string difficultyText;
    private readonly string playtimeTextValue;

    private readonly UIText buttonLabel;
    private readonly UIText deleteButtonLabel;

    private readonly UIImageButton playButton;
    private readonly UIImageButton deleteButton;

    private readonly Action<string> playAction;
    private readonly Action<string> deleteAction;

    public SSCCharacterListItem(
        Player player,
        string name,
        long playTimeTicks,
        int snapPointIndex,
        Action<string> playAction,
        Action<string> deleteAction)
    {
        this.characterName = name;
        this.playTimeTicks = playTimeTicks;
        this.playAction = playAction;
        this.deleteAction = deleteAction;

        BorderColor = new Color(89, 116, 213) * 0.7f;
        BackgroundColor = new Color(63, 82, 151) * 0.7f;

        Height.Set(96f, 0f);
        Width.Set(0f, 1f);
        SetPadding(6f);

        playerPanel = new UICharacter(player, animated: false, hasBackPanel: true, 1f, useAClone: true);
        playerPanel.Left.Set(4f, 0f);
        playerPanel.OnLeftDoubleClick += PlayGame;
        OnLeftDoubleClick += PlayGame;
        Append(playerPanel);

        float contentLeft = 100f;

        nameText = new UIText(name, 0.9f, large: false);
        nameText.Left.Set(contentLeft, 0f);
        nameText.Top.Set(6f, 0f);
        Append(nameText);

        Asset<Texture2D> playTex = Main.Assets.Request<Texture2D>("Images/UI/ButtonPlay");
        Asset<Texture2D> delTex = Main.Assets.Request<Texture2D>("Images/UI/ButtonDelete");

        // Initialize more stuff
        drawPlayer = player;

        dividerTexture = Main.Assets.Request<Texture2D>("Images/UI/Divider");
        innerPanelTexture = Main.Assets.Request<Texture2D>("Images/UI/InnerPanelBackground");

        difficultyText = GetDifficultyLabel(player.difficulty);
        playtimeTextValue = new TimeSpan(playTimeTicks).ToString(@"dd\:hh\:mm\:ss");

        playButton = new UIImageButton(playTex)
        {
            VAlign = 1f
        };
        playButton.Left.Set(4, 0f);
        playButton.OnLeftClick += PlayGame;
        playButton.OnMouseOver += PlayMouseOver;
        playButton.OnMouseOut += ButtonMouseOut;
        playButton.SetSnapPoint("Play", snapPointIndex);
        Append(playButton);

        deleteButton = new UIImageButton(delTex)
        {
            VAlign = 1f,
            HAlign = 1f
        };
        deleteButton.OnLeftClick += DeleteGame;
        deleteButton.OnMouseOver += DeleteMouseOver;
        deleteButton.OnMouseOut += DeleteMouseOut;
        deleteButton.SetSnapPoint("Delete", snapPointIndex);
        Append(deleteButton);

        buttonLabel = new UIText("")
        {
            VAlign = 1f
        };
        buttonLabel.Left.Set(28f, 0f);
        buttonLabel.Top.Set(-3f, 0f);
        Append(buttonLabel);

        deleteButtonLabel = new UIText("")
        {
            VAlign = 1f,
            HAlign = 1f
        };
        deleteButtonLabel.Left.Set(-30f, 0f);
        deleteButtonLabel.Top.Set(-3f, 0f);
        Append(deleteButtonLabel);

       
    }

    public override void MouseOver(UIMouseEvent evt)
    {
        base.MouseOver(evt);
        BackgroundColor = new Color(73, 94, 171);
        BorderColor = new Color(89, 116, 213);
        playerPanel.SetAnimated(true);
    }

    public override void MouseOut(UIMouseEvent evt)
    {
        base.MouseOut(evt);
        BackgroundColor = new Color(63, 82, 151) * 0.7f;
        BorderColor = new Color(89, 116, 213) * 0.7f;
        playerPanel.SetAnimated(false);
    }

    private void PlayMouseOver(UIMouseEvent evt, UIElement listeningElement)
    {
        buttonLabel.SetText(Language.GetTextValue("UI.Play"));
    }

    private void DeleteMouseOver(UIMouseEvent evt, UIElement listeningElement)
    {
        deleteButtonLabel.SetText(Language.GetTextValue("UI.Delete"));
    }

    private void DeleteMouseOut(UIMouseEvent evt, UIElement listeningElement)
    {
        deleteButtonLabel.SetText("");
    }

    private void ButtonMouseOut(UIMouseEvent evt, UIElement listeningElement)
    {
        buttonLabel.SetText("");
    }

    private void PlayGame(UIMouseEvent evt, UIElement listeningElement)
    {
        if (listeningElement == evt.Target)
        {
            playAction?.Invoke(characterName);
        }
    }

    private void DeleteGame(UIMouseEvent evt, UIElement listeningElement)
    {
        deleteAction?.Invoke(characterName);
    }

    private void DrawPanel(SpriteBatch spriteBatch, Vector2 position, float width)
    {
        spriteBatch.Draw(
            innerPanelTexture.Value,
            position,
            new Rectangle(0, 0, 8, innerPanelTexture.Value.Height),
            Color.White
        );

        spriteBatch.Draw(
            innerPanelTexture.Value,
            new Vector2(position.X + 8f, position.Y),
            new Rectangle(8, 0, 8, innerPanelTexture.Value.Height),
            Color.White,
            0f,
            Vector2.Zero,
            new Vector2((width - 16f) / 8f, 1f),
            SpriteEffects.None,
            0f
        );

        spriteBatch.Draw(
            innerPanelTexture.Value,
            new Vector2(position.X + width - 8f, position.Y),
            new Rectangle(16, 0, 8, innerPanelTexture.Value.Height),
            Color.White
        );
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        CalculatedStyle inner = GetInnerDimensions();

        float rightAreaLeft = inner.X + 100f;
        float rightAreaWidth = inner.Width - 100f;

        if (rightAreaWidth <= 0f)
            return;

        // Divider: under name, above the panels
        Texture2D div = dividerTexture.Value;
        float dividerY = inner.Y + 26f;
        spriteBatch.Draw(
            div,
            new Vector2(rightAreaLeft, dividerY),
            new Rectangle(0, 0, div.Width, div.Height),
            Color.White,
            0f,
            Vector2.Zero,
            new Vector2(rightAreaWidth / div.Width, 1f),
            SpriteEffects.None,
            0f
        );

        // 3 panels (left-to-right) below the divider
        float panelY = inner.Y + 29f;
        float panelGap = 6f;

        float p1W = rightAreaWidth * 0.46f;
        float p2W = rightAreaWidth * 0.22f;
        float p3W = rightAreaWidth - p1W - p2W - panelGap * 2f;

        if (p3W < 60f)
        {
            p3W = 60f;
            p2W = Math.Max(60f, rightAreaWidth - p1W - p3W - panelGap * 2f);
        }

        Vector2 p1Pos = new Vector2(rightAreaLeft, panelY);
        Vector2 p2Pos = new Vector2(rightAreaLeft + p1W + panelGap, panelY);
        Vector2 p3Pos = new Vector2(rightAreaLeft + p1W + panelGap + p2W + panelGap, panelY);

        DrawPanel(spriteBatch, p1Pos, p1W);
        DrawPanel(spriteBatch, p2Pos, p2W);
        DrawPanel(spriteBatch, p3Pos, p3W);

        // Panel 1: HP + MP with heart/mana icons
        float statScale = 0.88f;
        float statGap = 5f * statScale;

        string hpText = $"{drawPlayer.statLife} HP";
        string mpText = $"{drawPlayer.statMana} MP";

        Vector2 hpSize = FontAssets.MouseText.Value.MeasureString(hpText) * statScale;
        Vector2 mpSize = FontAssets.MouseText.Value.MeasureString(mpText) * statScale;

        Asset<Texture2D> heart = TextureAssets.Heart;
        Asset<Texture2D> mana = TextureAssets.Mana;

        float heartW = heart.Width() * statScale;
        float manaW = mana.Width() * statScale;

        float hpBlockW = heartW + hpSize.X;
        float mpBlockW = manaW + mpSize.X;
        float totalWidth = hpBlockW + statGap + mpBlockW;

        float startX = p1Pos.X + (p1W - totalWidth) * 0.5f;
        float y = p1Pos.Y + 4f;

        spriteBatch.Draw(heart.Value, new Vector2(startX, y), null, Color.White, 0f, Vector2.Zero, statScale, SpriteEffects.None, 0f);
        startX += heartW;

        Utils.DrawBorderString(spriteBatch, hpText, new Vector2(startX + 1f, y + 1f), Color.White, statScale);
        startX += hpSize.X;
        startX += statGap;

        spriteBatch.Draw(mana.Value, new Vector2(startX, y - 2f), null, Color.White, 0f, Vector2.Zero, statScale, SpriteEffects.None, 0f);
        startX += manaW;

        Utils.DrawBorderString(spriteBatch, mpText, new Vector2(startX + 1f, y + 1f), Color.White, statScale);

        // Panel 2: difficulty (Classic/Mediumcore/Hardcore/Journey)
        DrawCenteredPanelText(spriteBatch, p2Pos, p2W, difficultyText, statScale);

        // Panel 3: playtime
        DrawCenteredPanelText(spriteBatch, p3Pos, p3W, playtimeTextValue, statScale);
    }


    private static void DrawCenteredPanelText(SpriteBatch spriteBatch, Vector2 panelPos, float panelW, string text, float scale)
    {
        var font = FontAssets.MouseText.Value;
        if (font == null) return;

        Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * scale;

        float x = panelPos.X + (panelW - size.X) * 0.5f;
        float y = panelPos.Y + 4f;

        Utils.DrawBorderString(spriteBatch, text, new Vector2(x + 1f, y + 1f), Color.White, scale);
    }

    private static string GetDifficultyLabel(int difficulty)
    {
        if (difficulty == PlayerDifficultyID.MediumCore)
        {
            return "Mediumcore";
        }

        if (difficulty == PlayerDifficultyID.Hardcore)
        {
            return "Hardcore";
        }

        if (difficulty == PlayerDifficultyID.Creative)
        {
            return "Journey";
        }

        return "Classic";
    }

}