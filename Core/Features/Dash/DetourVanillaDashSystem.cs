using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Features.Dash
{
    internal class DetourVanillaDashSystem : ModSystem
    {
        public override void Load()
        {
            On_Player.DoCommonDashHandle += VanillaDashDetour;
        }
        public override void Unload()
        {
            On_Player.DoCommonDashHandle -= VanillaDashDetour;
        }

        private static void VanillaDashDetour(On_Player.orig_DoCommonDashHandle orig, Player self, out int dir, out bool dashing, Player.DashStartAction dashStartAction)
        {
            // Get client config
            var clientConfig = ModContent.GetInstance<AdventureClientConfig>();

            // Execute vanilla dash only if enabled in config
            if (clientConfig.IsVanillaDashEnabled)
            {
                orig.Invoke(self, out dir, out dashing, dashStartAction);
                return;
            }
            else
            {
                // Must set out parameters even if not dashing
                dir = 0;
                dashing = false;
            }
            
            //if (self.whoAmI != Main.myPlayer)
            //orig.Invoke(self, out dir, out dashing, dashStartAction);
        }
    }
}
