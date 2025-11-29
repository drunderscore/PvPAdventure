using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.Core.Features.MainMenu.UI
{
    public static class UiFactory
    {
        public static HoverButton Button(string text, float topPx, UIState parent, float hAlign = 0.5f)
        {
            var b = new HoverButton(text);
            b.HAlign = hAlign;
            b.Top.Set(topPx, 0f);
            parent.Append(b);
            return b;
        }

        public static UIText Title(string text, float topPx, UIState parent)
        {
            var t = new UIText(text, 1.1f, true) { HAlign = 0.5f };
            t.Top.Set(topPx, 0f);
            parent.Append(t);
            return t;
        }
    }
}
