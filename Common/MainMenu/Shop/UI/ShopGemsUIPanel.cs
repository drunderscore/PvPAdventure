using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.MainMenu.Gems;
using PvPAdventure.Core.Utilities;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI;
using Terraria.UI;
using Terraria.UI.Chat;

public class ShopGemsUIPanel : UIPanel
{
    public int Gems { get; set; }

    public ShopGemsUIPanel()
    {
        // Load gems
        //Gems = GemStorage.Read();
        //Log.Info($"Loaded {Gems} gems for display in shop UI");

        Height.Set(42f, 0f);

        //BackgroundColor = new Color(26, 40, 89) * 0.8f; // dark blue
        BackgroundColor = new Color(63, 82, 151) * 0.7f; // light blue
        //BorderColor = new Color(13, 20, 44) * 0.8f; // dark blue
        BorderColor = new Color(15, 15, 15);

        PaddingLeft = 10f;
        PaddingTop = 8f;
    }

    public override void Draw(SpriteBatch sb)
    {
        base.Draw(sb);

        int gems = GemStorage.GemCount;

        if (IsMouseHovering)
            UICommon.TooltipMouseText("Gems are rewarded for high placement in TPVPA matches.");

        CalculatedStyle inner = GetInnerDimensions();
        Vector2 pos = new(inner.X - 2, inner.Y - 2);

        sb.Draw(Ass.Icon_Gem.Value, pos, null, Color.White, 0f, Vector2.Zero, 1.3f, SpriteEffects.None, 0f);

        Vector2 textPos = pos + new Vector2(50f, 4f);
        ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.MouseText.Value, $"{gems} Gems", textPos, Color.LightGray, 0f, Vector2.Zero, Vector2.One);
    }
}
