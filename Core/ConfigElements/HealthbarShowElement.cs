using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.HealthBars;
using PvPAdventure.Core.Helpers;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Core.ConfigElements;

public class HealthbarShowElement : BaseBoolConfigElement
{
    public override void OnBind()
    {
        base.OnBind();
    }

    protected override void OnToggled(bool newValue)
    {
        var cfg = ModContent.GetInstance<AdventureClientConfig>();
        cfg.ShowHealthBars = newValue;

        var hbSystem = ModContent.GetInstance<HealthbarSystem>();
        hbSystem.IsActive = newValue;
    }

    protected override void DrawPreview(SpriteBatch sb)
    {
        if (!Value) return;

        // Position
        var dims = GetDimensions();
        Vector2 pos = new(dims.X + 175 + 3, dims.Y + 11);
        Rectangle rect = new((int)pos.X - 8, (int)pos.Y - 6, 22, 22);

        // Preview asset to draw
        // TODO
    }
}