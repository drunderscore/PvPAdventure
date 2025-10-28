using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Features.Dash
{
    public class DashKeybind : ModSystem
    {
        public override void Load()
        {
            On_Player.DashMovement += CustomDashHandle;
        }

        public override void Unload()
        {
            On_Player.DashMovement -= CustomDashHandle;
        }

        private static void CustomDashHandle(On_Player.orig_DashMovement orig, Player self)
        {
            if (self.whoAmI == Main.myPlayer && KeybindSystem.DashKB?.JustPressed == true)
            {
                ModContent.GetInstance<PvPAdventure>().Logger.Info($"Dash key pressed on player {self.whoAmI} (dashDelay={self.dashDelay}).");
            }

            orig(self);
        }
    }
}