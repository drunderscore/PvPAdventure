using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.World.ShopItems;

internal class ShopItemTooltips : GlobalItem
{
    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        if (item.type != ItemID.SniperRifle) return;
        var name = tooltips.FirstOrDefault(t => t.Mod == "Terraria" && t.Name == "ItemName");
        name?.Text = "Pink Sniper Rifle";
    }

    public override bool PreDrawInInventory(Item item, SpriteBatch sb, Vector2 pos, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        if (item.type != ItemID.SniperRifle) return true;
        var tex = ModContent.Request<Texture2D>(".../PinkSniper").Value;
        sb.Draw(tex, pos, null, drawColor, 0f, origin, scale, SpriteEffects.None, 0f);
        return false;
    }

    public override bool PreDrawInWorld(Item item, SpriteBatch sb, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        if (item.type != ItemID.SniperRifle) return true;
        var tex = ModContent.Request<Texture2D>(".../PinkSniper").Value;
        sb.Draw(tex, item.position - Main.screenPosition, null, lightColor, rotation, Vector2.Zero, scale, SpriteEffects.None, 0f);
        return false;
    }
}
