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
///// Global item overrides for the skin system.
///// Handles inventory drawing, world drawing, and tooltip name replacement.
///// Works for ALL players — uses <see cref="SkinPlayer.TryGetSkin"/> which works
///// for both local player (from ProfileStorage) and remote players (from network).
///// </summary>
//internal sealed class SkinGlobalItem : GlobalItem
//{
//    /// <summary>
//    /// Only applies to item types that have at least one skin defined.
//    /// </summary>
//    public override bool AppliesToEntity(Item entity, bool lateInstantiation)
//    {
//        for (int i = 0; i < SkinCatalog.All.Length; i++)
//        {
//            if (SkinCatalog.All[i].ItemType == entity.type)
//                return true;
//        }

//        return false;
//    }

//    /// <summary>
//    /// Override inventory drawing to use the skin texture.
//    /// Applies for the local player's items.
//    /// </summary>
//    public override bool PreDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position,
//        Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
//    {
//        if (!TryGetLocalPlayerSkin(item.type, out SkinDefinition def))
//            return true;

//        Texture2D tex = def.Texture?.Value;

//        if (tex == null)
//        {
//            Log.Debug($"[SkinGlobalItem] PreDrawInInventory skin texture null for '{def.Name}' id={def.Id}");
//            return true;
//        }

//        float s = scale * GetScaleRatio(item, tex);
//        Vector2 drawOrigin = tex.Bounds.Size() * 0.5f;

//        Log.Debug($"[SkinGlobalItem] PreDrawInInventory drawing '{def.Name}' for itemType={item.type} scale={s:F2}");

//        spriteBatch.Draw(
//            tex,
//            position,
//            tex.Bounds,
//            drawColor,
//            0f,
//            drawOrigin,
//            s,
//            SpriteEffects.None,
//            0f);

//        return false; // Skip vanilla draw
//    }

//    /// <summary>
//    /// Override world drawing (dropped items on ground) to use the skin texture.
//    /// Only draws the skin for the local player's owned skins.
//    /// </summary>
//    public override bool PreDrawInWorld(Item item, SpriteBatch spriteBatch, Color lightColor,
//        Color alphaColor, ref float rotation, ref float scale, int whoAmI)
//    {
//        if (!TryGetLocalPlayerSkin(item.type, out SkinDefinition def))
//            return true;

//        Texture2D tex = def.Texture?.Value;

//        if (tex == null)
//        {
//            Log.Debug($"[SkinGlobalItem] PreDrawInWorld skin texture null for '{def.Name}' id={def.Id}");
//            return true;
//        }

//        float s = scale * GetScaleRatio(item, tex);
//        Vector2 drawOrigin = tex.Size() * 0.5f;
//        Vector2 drawPos = item.Center - Main.screenPosition;

//        Log.Debug($"[SkinGlobalItem] PreDrawInWorld drawing '{def.Name}' for itemType={item.type} at pos={drawPos}");

//        spriteBatch.Draw(
//            tex,
//            drawPos,
//            null,
//            lightColor,
//            rotation,
//            drawOrigin,
//            s,
//            SpriteEffects.None,
//            0f);

//        return false; // Skip vanilla draw
//    }

//    /// <summary>
//    /// Replace the item name in tooltips with the skin name.
//    /// </summary>
//    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
//    {
//        if (!TryGetLocalPlayerSkin(item.type, out SkinDefinition def))
//            return;

//        TooltipLine nameLine = tooltips.FirstOrDefault(t => t.Mod == "Terraria" && t.Name == "ItemName");

//        if (nameLine != null)
//        {
//            Log.Debug($"[SkinGlobalItem] ModifyTooltips replacing '{nameLine.Text}' → '{def.Name}' for itemType={item.type}");
//            nameLine.Text = def.Name;
//        }
//    }

//    /// <summary>
//    /// Attempts to get the skin for the local player for a given item type.
//    /// Uses <see cref="SkinPlayer"/>.
//    /// </summary>
//    private static bool TryGetLocalPlayerSkin(int itemType, out SkinDefinition def)
//    {
//        def = default;

//        Player localPlayer = Main.LocalPlayer;

//        if (localPlayer == null || !localPlayer.active)
//            return false;

//        return localPlayer.GetModPlayer<SkinPlayer>().TryGetSkin(itemType, out def);
//    }

//    /// <summary>
//    /// Calculates a uniform scale ratio so the skin texture matches the visual footprint
//    /// of the vanilla item texture. Uses the larger dimension ratio.
//    /// </summary>
//    internal static float GetScaleRatio(Item item, Texture2D skinTex)
//    {
//        Main.instance.LoadItem(item.type);
//        Texture2D vanillaTex = TextureAssets.Item[item.type].Value;

//        if (vanillaTex == null || skinTex == null)
//            return 1f;

//        float ratioW = (float)vanillaTex.Width / skinTex.Width;
//        float ratioH = (float)vanillaTex.Height / skinTex.Height;

//        // Use the smaller ratio so the skin doesn't exceed the vanilla footprint
//        return Math.Min(ratioW, ratioH);
//    }
//}
