using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Skins;

[Autoload(Side = ModSide.Client)]
internal sealed class SkinDrawGlobalItem : GlobalItem
{
    public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        => SkinRegistry.IsSkinnableItemType(entity.type);

    public override void Load() => On_PlayerDrawLayers.DrawPlayer_27_HeldItem += DrawHeldItem;
    public override void Unload() => On_PlayerDrawLayers.DrawPlayer_27_HeldItem -= DrawHeldItem;

    // --- Held item ---

    private static void DrawHeldItem(On_PlayerDrawLayers.orig_DrawPlayer_27_HeldItem orig, ref PlayerDrawSet drawInfo)
    {
        Item item = drawInfo.drawPlayer.HeldItem;

        if (item is null || item.IsAir || !SkinRegistry.TryGetSkin(item, out SkinDefinition skin))
        {
            orig(ref drawInfo);
            return;
        }

        Main.instance.LoadItem(item.type);
        Texture2D vanilla = TextureAssets.Item[item.type].Value;
        Texture2D replacement = SkinRegistry.ResolveTexture(skin, vanilla, out _);

        int start = drawInfo.DrawDataCache.Count;
        orig(ref drawInfo);
        int end = drawInfo.DrawDataCache.Count;

        for (int i = start; i < end; i++)
        {
            DrawData data = drawInfo.DrawDataCache[i];
            if (data.texture == vanilla)
                drawInfo.DrawDataCache[i] = ScaleDrawData(data, vanilla, replacement);
        }
    }

    private static DrawData ScaleDrawData(DrawData data, Texture2D from, Texture2D to)
    {
        float sx = to.Width / (float)from.Width;
        float sy = to.Height / (float)from.Height;
        data.texture = to;
        if (data.sourceRect.HasValue)
        {
            Rectangle r = data.sourceRect.Value;
            data.sourceRect = new Rectangle((int)(r.X * sx), (int)(r.Y * sy), (int)(r.Width * sx), (int)(r.Height * sy));
        }
        data.origin *= new Vector2(sx, sy);
        return data;
    }

    // --- Inventory ---

    public override bool PreDrawInInventory(Item item, SpriteBatch sb, Vector2 position, Rectangle frame,
        Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        if (!SkinRegistry.TryGetSkin(item, out SkinDefinition skin))
            return true;

        Texture2D vanilla = TextureAssets.Item[item.type].Value;
        Texture2D tex = SkinRegistry.ResolveTexture(skin, vanilla, out _);
        (Rectangle src, Vector2 org) = ScaleFrameAndOrigin(frame, origin, vanilla, tex);

        sb.Draw(tex, position, src, drawColor, 0f, org, scale, SpriteEffects.None, 0f);
        if (item.color != Color.Transparent)
            sb.Draw(tex, position, src, itemColor, 0f, org, scale, SpriteEffects.None, 0f);

        return false;
    }

    // --- World ---

    public override bool PreDrawInWorld(Item item, SpriteBatch sb, Color lightColor, Color alphaColor,
        ref float rotation, ref float scale, int whoAmI)
    {
        if (!SkinRegistry.TryGetSkin(item, out SkinDefinition skin))
            return true;

        Texture2D vanilla = TextureAssets.Item[item.type].Value;
        Texture2D tex = SkinRegistry.ResolveTexture(skin, vanilla, out _);

        Rectangle vanillaFrame = Main.itemAnimations[item.type]?.GetFrame(vanilla) ?? vanilla.Frame();
        (Rectangle src, _) = ScaleFrameAndOrigin(vanillaFrame, Vector2.Zero, vanilla, tex);

        Vector2 origin = src.Size() * 0.5f;
        Vector2 drawPos = item.position - Main.screenPosition + new Vector2(item.width * 0.5f, item.height - src.Height * 0.5f);
        Color color = item.GetAlpha(lightColor);

        sb.Draw(tex, drawPos, src, color, rotation, origin, scale, SpriteEffects.None, 0f);
        if (item.color != Color.Transparent)
            sb.Draw(tex, drawPos, src, item.GetColor(color), rotation, origin, scale, SpriteEffects.None, 0f);

        return false;
    }

    // --- Tooltips ---

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        if (!SkinRegistry.TryGetSkin(item, out SkinDefinition skin))
            return;

        for (int i = 0; i < tooltips.Count; i++)
        {
            if (tooltips[i].Mod == "Terraria" && tooltips[i].Name == "ItemName")
            {
                tooltips[i].Text = $"{skin.Name} ({Lang.GetItemNameValue(item.type)})";
                tooltips.Insert(i + 1, new TooltipLine(Mod, "SkinDescription", skin.Description)
                {
                    OverrideColor = new Color(255, 150, 60)
                });
                break;
            }
        }
    }

    // --- Shared helper ---

    private static (Rectangle src, Vector2 origin) ScaleFrameAndOrigin(Rectangle frame, Vector2 origin, Texture2D from, Texture2D to)
    {
        float sx = to.Width / (float)from.Width;
        float sy = to.Height / (float)from.Height;
        return (
            new Rectangle((int)(frame.X * sx), (int)(frame.Y * sy), (int)(frame.Width * sx), (int)(frame.Height * sy)),
            origin * new Vector2(sx, sy)
        );
    }
}