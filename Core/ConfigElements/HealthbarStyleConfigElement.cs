using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.HealthBars;
using PvPAdventure.Core.Helpers;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Config.UI;
using Terraria.UI;

namespace PvPAdventure.Core.ConfigElements;

public class HealthbarStyleConfigElement : StringOptionElement
{
    public override void OnBind()
    {
        base.OnBind();

        var oldSetValue = setValue;

        setValue = index =>
        {
            oldSetValue(index);
            if (Item is AdventureClientConfig cfg)
            {
                var hb = ModContent.GetInstance<HealthbarSystem>();
                 hb.Style = cfg.Theme;
            }
        };
    }

    public override void Draw(SpriteBatch sb)
    {
        base.Draw(sb);

        string selectedTheme = getValue();

        Texture2D previewTexture = selectedTheme switch
        {
            "Vanilla" => Ass.HPVanillaConfigItem.Value,
            "Fancy" => Ass.HP_Fancy.Value,
            "Golden" => Ass.HP_Golden.Value,
            "Leaf" => Ass.HP_Leaf.Value,
            "Retro" => Ass.HP_Retro.Value,
            "Sticks" => Ass.HP_Sticks.Value,
            "StoneGold" => Ass.HP_StoneGold.Value,
            "Tribute" => Ass.HP_Tribute.Value,
            "TwigLeaf" => Ass.HP_TwigLeaf.Value,
            "Valkyrie" => Ass.HP_Valkyrie.Value,
            _ => null
        };

        CalculatedStyle dims = GetDimensions();

        if (previewTexture != null)
        {
            // Position
            int w = previewTexture.Width;
            int h = previewTexture.Height;
            Rectangle rect = new((int)dims.X + 170, (int)dims.Y, w, h);

            // Custom offset for special texture
            if (selectedTheme == "Vanilla") rect.Y += 8;

            // Draw
            sb.Draw(previewTexture, rect, Color.White);

            List<string> supported = [
                "Vanilla", "Fancy", "Golden", "Leaf"
            ];

            if (!supported.Contains(selectedTheme))
            {
                Utils.DrawBorderString(sb, "not supported :(", new Vector2(rect.X+10, rect.Y+6), Color.White, 0.9f);
            }
        }
    }
}

