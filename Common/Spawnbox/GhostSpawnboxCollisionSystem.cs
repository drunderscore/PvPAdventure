using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Common.GameTimer;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static PvPAdventure.Common.Spawnbox.RegionManager;

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
        Vector2 oldPosition = self.position;
        orig(self);

        if (!self.ghost)
            return;

        bool fastGhost = self.whoAmI == Main.myPlayer && (Main.keyState.IsKeyDown(Keys.LeftShift) || Main.keyState.IsKeyDown(Keys.RightShift));
        if (fastGhost)
        {
            Vector2 delta = self.position - oldPosition;
            self.position += delta * 3f;
        }

        GameManager gameManager = ModContent.GetInstance<GameManager>();
        bool shouldClampToSpawnbox = gameManager.CurrentPhase != GameManager.Phase.Playing;

        if (!shouldClampToSpawnbox)
            return;

        RegionManager regionManager = ModContent.GetInstance<RegionManager>();
        Region spawnRegion = regionManager.Regions.FirstOrDefault(r => r.Order == 10);

        if (spawnRegion == null)
            return;

        Rectangle bounds = new(
            spawnRegion.Area.X * 16,
            spawnRegion.Area.Y * 16,
            spawnRegion.Area.Width * 16,
            spawnRegion.Area.Height * 16);

        float minX = bounds.Left;
        float maxX = bounds.Right - self.width;
        float minY = bounds.Top;
        float maxY = bounds.Bottom - self.height;

        Vector2 pos = self.position;
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
