using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Shop;

internal sealed class HeldItemSkinHookSystem : ModSystem
{
    public override void Load()
    {
        if (Main.dedServ)
            return;

        On_PlayerDrawLayers.DrawPlayer_27_HeldItem += SwapHeldItemTexture;
    }

    public override void Unload()
    {
        On_PlayerDrawLayers.DrawPlayer_27_HeldItem -= SwapHeldItemTexture;
    }

    private static void SwapHeldItemTexture(On_PlayerDrawLayers.orig_DrawPlayer_27_HeldItem orig, ref PlayerDrawSet drawInfo)
    {
        Player player = drawInfo.drawPlayer;
        Item item = player.HeldItem;

        if (!ItemSkinRegistry.TryGetSkin(item, out Texture2D skinTex, out _))
        {
            orig(ref drawInfo);
            return;
        }

        Texture2D vanillaTex = TextureAssets.Item[item.type].Value;
        int before = drawInfo.DrawDataCache.Count;

        orig(ref drawInfo);

        float scaleRatio = ItemSkinsGlobalItem.GetScaleRatio(item, skinTex);
        float ox = vanillaTex.Width <= 0 ? 1f : skinTex.Width / (float)vanillaTex.Width;
        float oy = vanillaTex.Height <= 0 ? 1f : skinTex.Height / (float)vanillaTex.Height;

        for (int i = before; i < drawInfo.DrawDataCache.Count; i++)
        {
            DrawData dd = drawInfo.DrawDataCache[i];
            if (dd.texture != vanillaTex)
                continue;

            dd.texture = skinTex;

            if (dd.sourceRect.HasValue)
            {
                Rectangle r = dd.sourceRect.Value;
                if (r.Width == vanillaTex.Width && r.Height == vanillaTex.Height)
                    dd.sourceRect = skinTex.Bounds;
            }
            else
            {
                dd.sourceRect = skinTex.Bounds;
            }

            dd.origin = new Vector2(dd.origin.X * ox, dd.origin.Y * oy);
            dd.scale *= scaleRatio;

            drawInfo.DrawDataCache[i] = dd;
        }
    }
}