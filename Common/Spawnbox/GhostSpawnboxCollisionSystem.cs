using Microsoft.Xna.Framework;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spawnbox;

[Autoload(Side = ModSide.Both)]
internal class GhostSpawnboxCollisionSystem : ModSystem
{
    public override void Load()
    {
        On_Player.Ghost += OnPlayerGhost;
    }

    public override void Unload()
    {
        On_Player.Ghost -= OnPlayerGhost;
    }

    private void OnPlayerGhost(On_Player.orig_Ghost orig, Player self)
    {
        orig(self);

        if (!self.ghost)
            return;

        var regionManager = ModContent.GetInstance<RegionManager>();
        var spawnRegion = regionManager.Regions.FirstOrDefault(r => r.Order == 10);

        if (spawnRegion == null)
            return;

        // Region.Area is in tiles. Convert to world pixels.
        var bounds = new Rectangle(
            spawnRegion.Area.X * 16,
            spawnRegion.Area.Y * 16,
            spawnRegion.Area.Width * 16,
            spawnRegion.Area.Height * 16);

        // Keep the full player hitbox inside the box.
        float minX = bounds.Left;
        float maxX = bounds.Right - self.width;
        float minY = bounds.Top;
        float maxY = bounds.Bottom - self.height;

        var pos = self.position;

        float clampedX = MathHelper.Clamp(pos.X, minX, maxX);
        float clampedY = MathHelper.Clamp(pos.Y, minY, maxY);

        bool changed = false;

        if (clampedX != pos.X)
        {
            pos.X = clampedX;
            self.velocity.X = 0f;
            changed = true;
        }

        if (clampedY != pos.Y)
        {
            pos.Y = clampedY;
            self.velocity.Y = 0f;
            changed = true;
        }

        if (!changed)
            return;

        self.position = pos;

        if (Main.netMode == NetmodeID.Server)
            NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, self.whoAmI);
    }
}
