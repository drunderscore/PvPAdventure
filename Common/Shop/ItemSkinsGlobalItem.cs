using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Shop;

internal sealed class ItemSkinsGlobalItem : GlobalItem
{
    public override bool AppliesToEntity(Item entity, bool lateInstantiation) => ItemSkinRegistry.HasAnySkins(entity.type);

    public override void UpdateInventory(Item item, Player player) => ApplyName(item);

    public override void Update(Item item, ref float gravity, ref float maxFallSpeed) => ApplyName(item);

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        if (!ItemSkinRegistry.TryGetSkin(item, out _, out _))
            return;

        TooltipLine line = tooltips.FirstOrDefault(t => t.Mod == "Terraria" && t.Name == "ItemName");
        if (line != null)
            line.Text = item.AffixName();
    }

    public override bool PreDrawInInventory(Item item, SpriteBatch sb, Vector2 pos, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        if (!ItemSkinRegistry.TryGetSkin(item, out Texture2D tex, out _))
            return true;

        float s = scale * GetScaleRatio(item, tex);
        sb.Draw(tex, pos, tex.Bounds, drawColor, 0f, tex.Bounds.Size() * 0.5f, s, SpriteEffects.None, 0f);
        return false;
    }

    public override bool PreDrawInWorld(Item item, SpriteBatch sb, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        if (!ItemSkinRegistry.TryGetSkin(item, out Texture2D tex, out _))
            return true;

        float s = scale * GetScaleRatio(item, tex);
        sb.Draw(tex, item.Center - Main.screenPosition, null, lightColor, rotation, tex.Size() * 0.5f, s, SpriteEffects.None, 0f);
        return false;
    }

    private static void ApplyName(Item item)
    {
        if (ItemSkinRegistry.TryGetSkin(item, out _, out string name))
            item.SetNameOverride(name);
        else
            item.ClearNameOverride();
    }

    internal static float GetScaleRatio(Item item, Texture2D skinTex)
    {
        Texture2D vanilla = TextureAssets.Item[item.type].Value;
        int v = vanilla.Width > vanilla.Height ? vanilla.Width : vanilla.Height;
        int s = skinTex.Width > skinTex.Height ? skinTex.Width : skinTex.Height;
        return s <= 0 ? 1f : v / (float)s;
    }
}