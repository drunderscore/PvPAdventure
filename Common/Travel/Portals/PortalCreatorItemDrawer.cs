using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Content.Portals;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Travel.Portals;

/// <summary>
/// Draws the held item for the portal creator item with the team-specific texture.
/// </summary>
[Autoload(Side = ModSide.Client)]
internal sealed class PortalCreatorItemDrawer : GlobalItem
{
    public override bool AppliesToEntity(Item entity, bool lateInstantiation)
    {
        return true;
    }

    public override void Load()
    {
        On_PlayerDrawLayers.DrawPlayer_27_HeldItem += DrawPortalCreatorItemWithTeamColor;
    }
    public override void Unload() 
    {
        On_PlayerDrawLayers.DrawPlayer_27_HeldItem -= DrawPortalCreatorItemWithTeamColor;
    }

    #region Held item
    private static void DrawPortalCreatorItemWithTeamColor(On_PlayerDrawLayers.orig_DrawPlayer_27_HeldItem orig, ref PlayerDrawSet drawInfo)
    {
        Item item = drawInfo.drawPlayer.HeldItem;

        // Check if it's a portal creator item
        bool isPortalCreator = item?.ModItem is PortalCreatorItem;

        if (!isPortalCreator)
        {
            orig(ref drawInfo);
            return;
        }

        Main.instance.LoadItem(item.type);
        Texture2D vanilla = TextureAssets.Item[item.type].Value;

        // Resolve which texture we actually want to show
        Texture2D replacement;

        // Use the team-specific texture
        replacement = PortalAssets.GetCreatorTexture(drawInfo.drawPlayer.team);

        // Standard detour pattern: Capture the cache before and after the original call
        int start = drawInfo.DrawDataCache.Count;
        orig(ref drawInfo);
        int end = drawInfo.DrawDataCache.Count;

        for (int i = start; i < end; i++)
        {
            DrawData data = drawInfo.DrawDataCache[i];

            // If the original draw call used the vanilla texture, replace it with the team-specific texture
            if (data.texture == vanilla)
            {
                drawInfo.DrawDataCache[i] = ScaleDrawData(data, vanilla, replacement);
            }
        }
    }
    #endregion

    #region Helpers
    private static DrawData ScaleDrawData(DrawData data, Texture2D from, Texture2D to)
    {
        const float itemScale = 1f;

        float sx = to.Width / (float)from.Width;
        float sy = to.Height / (float)from.Height;

        data.texture = to;
        data.scale *= itemScale;

        if (data.sourceRect.HasValue)
        {
            Rectangle r = data.sourceRect.Value;
            data.sourceRect = new Rectangle((int)(r.X * sx), (int)(r.Y * sy), (int)(r.Width * sx), (int)(r.Height * sy));
        }

        data.origin *= new Vector2(sx, sy);
        return data;
    }
    #endregion

}