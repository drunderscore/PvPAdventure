//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using Terraria;
//using Terraria.GameContent;
//using Terraria.ModLoader;

//namespace PvPAdventure.Common.Skins;

///// <summary>
///// Handles skin texture drawing for items in inventory and in-world,
///// plus tooltip overrides to display the skin's name.
///// </summary>
//internal sealed class SkinGlobalItem : GlobalItem
//{
//    /// <summary>
//    /// Draws the skin texture instead of the vanilla texture when viewed in the local player's inventory.
//    /// </summary>
//    public override bool PreDrawInInventory(
//        Item item,
//        SpriteBatch sb,
//        Vector2 pos,
//        Rectangle frame,
//        Color drawColor,
//        Color itemColor,
//        Vector2 origin,
//        float scale)
//    {
//        if (!TryGetLocalPlayerSkin(item.type, out SkinDefinition def))
//            return true;

//        Texture2D tex = def.Texture.Value;

//        if (tex == null)
//            return true;

//        float s = scale * GetScaleRatio(item, tex);
//        sb.Draw(tex, pos, tex.Bounds, drawColor, 0f, tex.Bounds.Size() * 0.5f, s, SpriteEffects.None, 0f);
//        return false;
//    }

//    /// <summary>
//    /// Draws the skin texture when the item is dropped in the world.
//    /// Uses the local player's skin selection for visual consistency.
//    /// </summary>
//    public override bool PreDrawInWorld(
//        Item item,
//        SpriteBatch sb,
//        Color lightColor,
//        Color alphaColor,
//        ref float rotation,
//        ref float scale,
//        int whoAmI)
//    {
//        if (!TryGetLocalPlayerSkin(item.type, out SkinDefinition def))
//            return true;

//        Texture2D tex = def.Texture.Value;

//        if (tex == null)
//            return true;

//        float s = scale * GetScaleRatio(item, tex);
//        sb.Draw(tex, item.Center - Main.screenPosition, null, lightColor, rotation, tex.Size() * 0.5f, s, SpriteEffects.None, 0f);
//        return false;
//    }

//    /// <summary>
//    /// Replaces the item name tooltip with the skin's display name.
//    /// </summary>
//    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
//    {
//        if (!TryGetLocalPlayerSkin(item.type, out SkinDefinition def))
//            return;

//        TooltipLine nameLine = tooltips.FirstOrDefault(t => t.Mod == "Terraria" && t.Name == "ItemName");

//        if (nameLine != null)
//            nameLine.Text = def.Name;
//    }

//    /// <summary>
//    /// Tries to get the local player's selected skin for the given item type.
//    /// </summary>
//    private static bool TryGetLocalPlayerSkin(int itemType, out SkinDefinition def)
//    {
//        def = default;

//        if (Main.dedServ)
//            return false;

//        Player localPlayer = Main.LocalPlayer;

//        if (localPlayer == null || !localPlayer.active)
//            return false;

//        return localPlayer.GetModPlayer<SkinPlayer>().TryGetSkin(itemType, out def);
//    }

//    /// <summary>
//    /// Computes the uniform scale ratio so the skin texture fits the same visual footprint
//    /// as the vanilla item texture.
//    /// </summary>
//    internal static float GetScaleRatio(Item item, Texture2D skinTex)
//    {
//        Texture2D vanilla = TextureAssets.Item[item.type].Value;
//        int v = Math.Max(vanilla.Width, vanilla.Height);
//        int s = Math.Max(skinTex.Width, skinTex.Height);
//        return s <= 0 ? 1f : v / (float)s;
//    }
//}
