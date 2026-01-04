//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;
//using Microsoft.Xna.Framework.Input;
//using ReLogic.Content;
//using ReLogic.Graphics;
//using System;
//using Terraria;
//using Terraria.GameContent;
//using Terraria.GameContent.UI.Elements;
//using Terraria.ID;
//using Terraria.Localization;
//using Terraria.UI;

//namespace PvPAdventure.Core.SSC.UI;

//public class SSCCharacterListItem : UIPanel
//{
//    private readonly string characterName;
//    private readonly Player drawPlayer;

//    private readonly UICharacter playerPanel;
//    private readonly UIText nameText;

//    private readonly Asset<Texture2D> dividerTexture;
//    private readonly Asset<Texture2D> innerPanelTexture;

//    private readonly string difficultyText;
//    private readonly string playtimeTextValue;

//    private readonly UIText buttonLabel;
//    private readonly UIText deleteButtonLabel;

//    public SSCCharacterListItem(
//        Player player,
//        string name,
//        long playTimeTicks,
//        int snapPointIndex,
//        Action<string> playAction,
//        Action<string> deleteAction)
//    {
//        characterName = name;
//        drawPlayer = player;

//        dividerTexture = Main.Assets.Request<Texture2D>("Images/UI/Divider");
//        innerPanelTexture = Main.Assets.Request<Texture2D>("Images/UI/InnerPanelBackground");

//        difficultyText = GetDifficultyLabel(player.difficulty);
//        playtimeTextValue = new TimeSpan(playTimeTicks).ToString(@"dd\:hh\:mm\:ss");

//        BorderColor = new Color(89, 116, 213) * 0.7f;
//        BackgroundColor = new Color(63, 82, 151) * 0.7f;

//        Height.Set(96f, 0f);
//        Width.Set(0f, 1f);
//        SetPadding(6f);

//        playerPanel = new UICharacter(player, animated: false, hasBackPanel: true, 1f, useAClone: true)
//        {
//            Left = { Pixels = 4f }
//        };
//        playerPanel.OnLeftDoubleClick += (_, _) => playAction?.Invoke(characterName);
//        OnLeftDoubleClick += (_, _) => playAction?.Invoke(characterName);
//        Append(playerPanel);

//        nameText = new UIText(name, 0.9f, large: false)
//        {
//            Top = { Pixels = 6f }
//        };
//        Append(nameText);

//        var playButton = new UIImageButton(Main.Assets.Request<Texture2D>("Images/UI/ButtonPlay"))
//        {
//            VAlign = 1f,
//            Left = { Pixels = 4f }
//        };
//        playButton.OnLeftClick += (_, _) => playAction?.Invoke(characterName);
//        playButton.OnMouseOver += (_, _) => buttonLabel.SetText(Language.GetTextValue("UI.Play"));
//        playButton.OnMouseOut += (_, _) => buttonLabel.SetText("");
//        playButton.SetSnapPoint("Play", snapPointIndex);
//        Append(playButton);

//        var deleteButton = new UIImageButton(Main.Assets.Request<Texture2D>("Images/UI/ButtonDelete"))
//        {
//            VAlign = 1f,
//            HAlign = 1f
//        };
//        deleteButton.OnLeftClick += (_, _) =>
//        {
//            if (Main.keyState.IsKeyDown(Keys.LeftShift))
//                deleteAction?.Invoke(characterName);
//        };
//        deleteButton.OnMouseOver += (_, _) => deleteButtonLabel.SetText("Shift+click to delete");
//        deleteButton.OnMouseOut += (_, _) => deleteButtonLabel.SetText("");
//        deleteButton.SetSnapPoint("Delete", snapPointIndex);
//        Append(deleteButton);

//        buttonLabel = new UIText("")
//        {
//            VAlign = 1f,
//            Left = { Pixels = 28f },
//            Top = { Pixels = -3f }
//        };
//        Append(buttonLabel);

//        deleteButtonLabel = new UIText("")
//        {
//            VAlign = 1f,
//            HAlign = 1f,
//            Left = { Pixels = -30f },
//            Top = { Pixels = -3f }
//        };
//        Append(deleteButtonLabel);
//    }

//    public override void MouseOver(UIMouseEvent evt)
//    {
//        base.MouseOver(evt);
//        BackgroundColor = new Color(73, 94, 171);
//        BorderColor = new Color(89, 116, 213);
//        playerPanel.SetAnimated(true);
//    }

//    public override void MouseOut(UIMouseEvent evt)
//    {
//        base.MouseOut(evt);
//        BackgroundColor = new Color(63, 82, 151) * 0.7f;
//        BorderColor = new Color(89, 116, 213) * 0.7f;
//        playerPanel.SetAnimated(false);
//    }

//    private void DrawPanel(SpriteBatch sb, Vector2 pos, float width)
//    {
//        var tex = innerPanelTexture.Value;
//        int h = tex.Height;

//        sb.Draw(tex, pos, new Rectangle(0, 0, 8, h), Color.White);
//        sb.Draw(tex, new Vector2(pos.X + 8f, pos.Y), new Rectangle(8, 0, 8, h), Color.White, 0f, Vector2.Zero, new Vector2((width - 16f) / 8f, 1f), SpriteEffects.None, 0f);
//        sb.Draw(tex, new Vector2(pos.X + width - 8f, pos.Y), new Rectangle(16, 0, 8, h), Color.White);
//    }

//    protected override void DrawSelf(SpriteBatch sb)
//    {
//        base.DrawSelf(sb);

//        CalculatedStyle inner = GetInnerDimensions();
//        CalculatedStyle playerOuter = playerPanel.GetOuterDimensions();

//        float rightAreaLeft = playerOuter.X + playerOuter.Width + 6f;
//        float rightAreaRight = inner.X + inner.Width;
//        float rightAreaWidth = rightAreaRight - rightAreaLeft;

//        if (rightAreaWidth <= 0f)
//        {
//            return;
//        }

//        nameText.Left.Pixels = rightAreaLeft - inner.X;

//        DynamicSpriteFont font = GetMouseTextFont();
//        if (font == null)
//        {
//            return;
//        }

//        Texture2D div = dividerTexture.Value;
//        float dividerY = inner.Y + 20f;
//        sb.Draw(div, new Vector2(rightAreaLeft, dividerY), new Rectangle(0, 0, div.Width, div.Height), Color.White, 0f, Vector2.Zero, new Vector2(rightAreaWidth / div.Width, 1f), SpriteEffects.None, 0f);

//        float panelY = inner.Y + 26f;
//        float panelGap = 6f;

//        float p1W = rightAreaWidth * 0.46f;
//        float p2W = rightAreaWidth * 0.22f;
//        float p3W = rightAreaWidth - p1W - p2W - panelGap * 2f;

//        if (p3W < 60f)
//        {
//            p3W = 60f;
//            p2W = Math.Max(60f, rightAreaWidth - p1W - p3W - panelGap * 2f);
//        }

//        Vector2 p1Pos = new(rightAreaLeft, panelY);
//        Vector2 p2Pos = new(rightAreaLeft + p1W + panelGap, panelY);
//        Vector2 p3Pos = new(rightAreaLeft + p1W + panelGap + p2W + panelGap, panelY);

//        DrawPanel(sb, p1Pos, p1W);
//        DrawPanel(sb, p2Pos, p2W);
//        DrawPanel(sb, p3Pos, p3W);

//        float statScale = 0.88f;
//        float statGap = 8f * statScale; // slightly more space between HP and MP

//        string hpText = $"{drawPlayer.statLife} HP";
//        string mpText = $"{drawPlayer.statMana} MP";

//        Vector2 hpSize = font.MeasureString(hpText) * statScale;
//        Vector2 mpSize = font.MeasureString(mpText) * statScale;

//        Asset<Texture2D> heart = TextureAssets.Heart;
//        Asset<Texture2D> mana = TextureAssets.Mana;

//        float heartW = heart.Width() * statScale;
//        float manaW = mana.Width() * statScale;

//        float hpBlockW = heartW + hpSize.X;
//        float mpBlockW = manaW + mpSize.X;
//        float totalWidth = hpBlockW + statGap + mpBlockW;

//        float startX = p1Pos.X + (p1W - totalWidth) * 0.5f;
//        float y = p1Pos.Y + 4f;

//        sb.Draw(heart.Value, new Vector2(startX, y), null, Color.White, 0f, Vector2.Zero, statScale, SpriteEffects.None, 0f);
//        startX += heartW;
//        Utils.DrawBorderString(sb, hpText, new Vector2(startX + 1f, y + 1f), Color.White, statScale);
//        startX += hpSize.X + statGap;

//        sb.Draw(mana.Value, new Vector2(startX, y - 2f), null, Color.White, 0f, Vector2.Zero, statScale, SpriteEffects.None, 0f);
//        startX += manaW;
//        Utils.DrawBorderString(sb, mpText, new Vector2(startX + 1f, y + 1f), Color.White, statScale);

//        DrawCenteredPanelText(sb, font, p2Pos, p2W, difficultyText, statScale);
//        DrawCenteredPanelText(sb, font, p3Pos, p3W, playtimeTextValue, statScale);
//    }

//    private static void DrawCenteredPanelText(SpriteBatch sb, DynamicSpriteFont font, Vector2 panelPos, float panelW, string text, float scale)
//    {
//        Vector2 size = font.MeasureString(text) * scale;
//        float x = panelPos.X + (panelW - size.X) * 0.5f;
//        Utils.DrawBorderString(sb, text, new Vector2(x + 1f, panelPos.Y + 5f), Color.White, scale);
//    }

//    private static DynamicSpriteFont GetMouseTextFont()
//    {
//        var asset = FontAssets.MouseText;

//        if (asset != null && asset.IsLoaded && asset.Value != null)
//            return asset.Value;
//        return null;
//    }

//    private static string GetDifficultyLabel(int difficulty) => difficulty switch
//    {
//        PlayerDifficultyID.MediumCore => "Mediumcore",
//        PlayerDifficultyID.Hardcore => "Hardcore",
//        PlayerDifficultyID.Creative => "Journey",
//        _ => "Classic",
//    };
//}
