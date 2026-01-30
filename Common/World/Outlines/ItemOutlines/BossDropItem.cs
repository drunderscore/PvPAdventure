using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Config;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Enums;
using Terraria.ModLoader;

namespace PvPAdventure.Common.World.Outlines.ItemOutlines;

// An item dropped by a boss that is team-restricted
public sealed class BossDropItem : GlobalItem
{
    public override bool InstancePerEntity => true;
    public Team? _team;

    public override void NetSend(Item item, BinaryWriter writer)
    {
        bool has = _team.HasValue;
        writer.Write(has);

        if (has)
            writer.Write7BitEncodedInt((int)_team!.Value);
    }

    public override void NetReceive(Item item, BinaryReader reader)
    {
        _team = reader.ReadBoolean() ? (Team)reader.Read7BitEncodedInt() : null;
    }

    public override bool CanPickup(Item item, Player player)
    {
        Team? team = _team;
        return !team.HasValue || team.Value == (Team)player.team;
    }

    public override bool OnPickup(Item item, Player player)
    {
        _team = null;
        return true;
    }

    public override bool PreDrawInWorld(Item item, SpriteBatch sb, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        // Disable: world item outline drawing
        var config = ModContent.GetInstance<ClientConfig>();
        if (!config.LootOutlines)
            return true;

        Team? team = _team;

        // debug: draw ONLY team items, HIDE all other items. do not delete.
        //if (!team.HasValue)
        //return false;

        // draw regular items normally
        if (!team.HasValue || team.Value == Team.None)
            return true;

        // debug: draw ALL items with outlines. performance/stress-test.
        //Color border = team.HasValue && team.Value != Team.None
        //? Main.teamColor[(int)team.Value]
        //: Color.White;

        // Draw with a bright color
        Color border = Main.teamColor[(int)team.Value].MultiplyRGBA(lightColor);
        //Color border = Main.teamColor[(int)team.Value];
        border.A = 255;

        DrawWorldItemOutline(item, sb, lightColor, alphaColor, rotation, scale, border);

        return base.PreDrawInWorld(item, sb, lightColor, alphaColor, ref rotation, ref scale, whoAmI);
    }

    public override void Update(Item item, ref float gravity, ref float maxFallSpeed)
    {
        //base.Update(item, ref gravity, ref maxFallSpeed);
    }

    private void DrawWorldItemOutline(Item item, SpriteBatch sb, Color lightColor, Color alphaColor, float rotation, float scale, Color borderColor)
    {
        Rectangle hitbox = Item.GetDrawHitbox(item.type, null);
        Vector2 worldCenter = new(item.Bottom.X, item.Bottom.Y - hitbox.Height * 0.5f);
        Vector2 screenCenter = worldCenter - Main.screenPosition;

        //Vector2 off = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
        //Vector2 screenCenter = worldCenter - Main.screenPosition + off;

        // debug: draw item hitbox. do not delete.
        //int sx = (int)(item.Bottom.X - hitbox.Width * 0.5f - Main.screenPosition.X);
        //int sy = (int)(item.Bottom.Y - hitbox.Height - Main.screenPosition.Y);
        //sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(sx, sy, hitbox.Width, hitbox.Height), Color.Black);

        var outlineSys = ModContent.GetInstance<ItemOutlineSystem>();

        if (outlineSys.TryGet(item.type, hitbox.Width, hitbox.Height, borderColor, out RenderTarget2D rt, out Vector2 origin))
        {
            sb.Draw(rt, screenCenter, null, Color.White, rotation, origin, scale, SpriteEffects.None, 0f);
        }
    }

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(item, tooltips);
    }

    public override void PostDrawInWorld(Item item, SpriteBatch sb, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
    {
        base.PostDrawInWorld(item, sb, lightColor, alphaColor, rotation, scale, whoAmI);
    }

}
