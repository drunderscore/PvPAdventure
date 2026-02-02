using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Visualization.HoldingMap;

[Autoload(Side = ModSide.Client)]
public sealed class MapItemDrawItemLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition()
    {
        // Draw the map on top of the held item layer
        return new AfterParent(PlayerDrawLayers.HeldItem);
    }

    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
    {
        Player player = drawInfo.drawPlayer;

        if (player.dead)
            return false;

        return player.GetModPlayer<MapHoldingPlayer>().HoldingMap;
    }

    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        Player player = drawInfo.drawPlayer;

        // Get the trifold map texture and frame
        int itemType = ItemID.TrifoldMap;
        Texture2D texture = TextureAssets.Item[itemType].Value;

        Rectangle frame = texture.Bounds;
        if (Main.itemAnimations[itemType] != null)
            frame = Main.itemAnimations[itemType].GetFrame(texture);

        // Calculate drawing position
        Vector2 origin = frame.Size() * 0.5f;
        Vector2 anchorWorld = player.MountedCenter;
        anchorWorld.Y += player.gfxOffY;

        // Offset for holding the map
        Vector2 offset = new(18f * player.direction, -6f * player.gravDir);
        offset = offset.RotatedBy(player.fullRotation);

        Vector2 drawPos = anchorWorld + offset;
        drawPos -= Main.screenPosition;

        // Flip the sprite based on player direction
        SpriteEffects effects = SpriteEffects.None;
        if (player.direction == -1)
            effects = SpriteEffects.FlipHorizontally;

        // Get lighting color at the anchor position
        Color color = Lighting.GetColor(
            (int)(anchorWorld.X / 16f),
            (int)(anchorWorld.Y / 16f)
        );

        // Add the new draw data
        DrawData data = new( texture,drawPos, frame,color,0f,origin,1f,effects, 0);

        drawInfo.DrawDataCache.Add(data);
    }
}
