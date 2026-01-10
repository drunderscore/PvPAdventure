using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Visuals;

public sealed class MapHoldingPlayer : ModPlayer
{
    private static Texture2D _mapTex;

    public override void Load()
    {
    }

    public override void Unload()
    {
    }

    public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
    {
        // Only when map is open.
        if (!Main.mapFullscreen)
            return;

        Player p = drawInfo.drawPlayer;

        // Slight downward angle, like holding an object
        float rotation =
            p.direction == 1
                ? -1.9f
                : 1.9f;

        // Put arms out in front.
        p.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);
        p.SetCompositeArmBack( true, Player.CompositeArmStretchAmount.Full, rotation);

        // Prevent held-item animation fighting the pose
        p.itemAnimation = 0;
        p.itemTime = 0;
    }
}

// Draw item adjacent to player.
public sealed class AdjacentItemLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition()
    {
        return new AfterParent(PlayerDrawLayers.HeldItem);
    }

    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
    {
        // Only when map is open.
        if (!Main.mapFullscreen)
            return false;

        return !drawInfo.drawPlayer.dead;
    }

    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        Player player = drawInfo.drawPlayer;

        // Replace with your own condition / item type selection.
        int itemType = ItemID.TrifoldMap;

        Texture2D texture = TextureAssets.Item[itemType].Value;

        Rectangle frame = texture.Bounds;
        if (Main.itemAnimations[itemType] != null)
        {
            frame = Main.itemAnimations[itemType].GetFrame(texture);
        }

        Vector2 origin = frame.Size() * 0.5f;

        // World anchor that behaves well with mounts and most movement.
        Vector2 anchorWorld = player.MountedCenter;
        anchorWorld.Y += player.gfxOffY;

        // Offset in pixels relative to player. Multiply by direction to stay "next to" the facing side.
        Vector2 offset = new Vector2(18f * player.direction, -6f * player.gravDir);

        // If you want it to follow fullRotation (minecarts, some mounts), rotate the offset.
        offset = offset.RotatedBy(player.fullRotation);

        Vector2 drawPos = anchorWorld + offset;
        drawPos -= Main.screenPosition;

        SpriteEffects effects = SpriteEffects.None;
        if (player.direction == -1)
        {
            effects = SpriteEffects.FlipHorizontally;
        }

        // Optional: sample lighting so it matches the world.
        Color color = Lighting.GetColor((int)(drawPos.X + Main.screenPosition.X) / 16, (int)(drawPos.Y + Main.screenPosition.Y) / 16);

        DrawData data = new DrawData(
            texture,
            drawPos,
            frame,
            color,
            0f,
            origin,
            1f,
            effects,
            0
        );

        drawInfo.DrawDataCache.Add(data);
    }
}
