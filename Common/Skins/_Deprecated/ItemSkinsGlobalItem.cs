//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;
//using System.Collections.Generic;
//using System.Linq;
//using Terraria;
//using Terraria.GameContent;
//using Terraria.ModLoader;
//using Terraria.ModLoader.IO;

//namespace PvPAdventure.Common.Skins;

//internal sealed class ItemSkinsGlobalItem : GlobalItem
//{
//    public override bool InstancePerEntity => true;

//    public byte SkinIndex = ItemSkinDefinition.NoSkin;

//    public override bool AppliesToEntity(Item entity, bool lateInstantiation) => ItemSkinDefinition.HasAnySkins(entity.type);

//    public override GlobalItem Clone(Item from, Item to)
//    {
//        ItemSkinsGlobalItem clone = (ItemSkinsGlobalItem)base.Clone(from, to);
//        clone.SkinIndex = SkinIndex;
//        return clone;
//    }

//    public override void SaveData(Item item, TagCompound tag)
//    {
//        if (SkinIndex != ItemSkinDefinition.NoSkin)
//            tag["Skin"] = (int)SkinIndex;
//    }

//    public override void LoadData(Item item, TagCompound tag)
//    {
//        if (tag.ContainsKey("Skin"))
//            SkinIndex = (byte)tag.GetInt("Skin");
//        else
//            SkinIndex = ItemSkinDefinition.NoSkin;
//    }

//    public override void UpdateInventory(Item item, Player player) => ApplyName(item);

//    public override void Update(Item item, ref float gravity, ref float maxFallSpeed) => ApplyName(item);

//    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
//    {
//        if (!ItemSkinDefinition.TryGetSkin(item.type, GetActiveSkinIndex(item), out _, out _))
//            return;

//        TooltipLine line = tooltips.FirstOrDefault(t => t.Mod == "Terraria" && t.Name == "ItemName");
//        if (line != null)
//            line.Text = item.AffixName();
//    }

//    public override bool PreDrawInInventory(Item item, SpriteBatch sb, Vector2 pos, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
//    {
//        //return false;

//        if (!ItemSkinDefinition.TryGetSkin(item.type, GetActiveSkinIndex(item), out Texture2D tex, out _))
//            return true;

//        float s = scale * GetScaleRatio(item, tex);
//        sb.Draw(tex, pos, tex.Bounds, drawColor, 0f, tex.Bounds.Size() * 0.5f, s, SpriteEffects.None, 0f);
//        return false;
//    }

//    public override bool PreDrawInWorld(Item item, SpriteBatch sb, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
//    {
//        //return false;

//        if (!ItemSkinDefinition.TryGetSkin(item.type, GetActiveSkinIndex(item), out Texture2D tex, out _))
//            return true;

//        float s = scale * GetScaleRatio(item, tex);
//        sb.Draw(tex, item.Center - Main.screenPosition, null, lightColor, rotation, tex.Size() * 0.5f, s, SpriteEffects.None, 0f);
//        return false;
//    }

//    private void ApplyName(Item item)
//    {
//        if (ItemSkinDefinition.TryGetSkin(item.type, GetActiveSkinIndex(item), out _, out string name))
//            item.SetNameOverride(name);
//        else
//            item.ClearNameOverride();
//    }

//    // For local player: resolve from preference. For remote players/world items: use synced SkinIndex.
//    internal byte GetActiveSkinIndex(Item item)
//    {
//        //if (Main.netMode != Terraria.ID.NetmodeID.SinglePlayer)
//        //    return SkinIndex;

//        //if (SkinPreference.TryGetSelected(item.type, out string skinId) &&
//        //    ProfileStorage.IsUnlocked(skinId) &&
//        //    ItemSkins.TryGetSkinIndex(item.type, skinId, out byte idx))
//        //    return idx;

//        return ItemSkinDefinition.NoSkin;
//    }

//    internal static float GetScaleRatio(Item item, Texture2D skinTex)
//    {
//        Texture2D vanilla = TextureAssets.Item[item.type].Value;
//        int v = vanilla.Width > vanilla.Height ? vanilla.Width : vanilla.Height;
//        int s = skinTex.Width > skinTex.Height ? skinTex.Width : skinTex.Height;
//        return s <= 0 ? 1f : v / (float)s;
//    }
//}