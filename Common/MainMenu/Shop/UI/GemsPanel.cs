using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.MainMenu.Profile;
using PvPAdventure.Core.Utilities;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI;
using Terraria.UI;
using Terraria.UI.Chat;

namespace PvPAdventure.Common.MainMenu.Shop.UI;

public class GemsPanel : UIPanel
{
    public GemsPanel()
    {
        Height.Set(42f, 0f);
        BackgroundColor = new Color(63, 82, 151) * 0.7f;
        BorderColor = new Color(15, 15, 15);
        PaddingTop = 8;
        PaddingLeft = 6;
    }

    public override void Draw(SpriteBatch sb)
    {
        base.Draw(sb);

        int gems = ProfileStorage.Gems;

        if (IsMouseHovering)
            UICommon.TooltipMouseText("Gems are awarded for achievements and high placement in TPVPA matches.");

        CalculatedStyle inner = GetInnerDimensions();
        Vector2 pos = new(inner.X - 2, inner.Y - 2);
        sb.Draw(Ass.Icon_Gem.Value, pos, null, Color.White, 0f, Vector2.Zero, 1.3f, SpriteEffects.None, 0f);

        string text = $"{gems} Gems";
        float textAreaLeft = pos.X + 50f;
        float textAreaRight = inner.X + inner.Width - 10f;
        float textAreaWidth = System.Math.Max(0f, textAreaRight - textAreaLeft);

        Vector2 textSize = ChatManager.GetStringSize(FontAssets.MouseText.Value, text, Vector2.One);
        float textX = textAreaLeft + (textAreaWidth - textSize.X) * 0.5f;

        ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.MouseText.Value, text, new Vector2(textX, pos.Y + 4f), Color.WhiteSmoke, 0f, Vector2.Zero, Vector2.One);
    }
}
