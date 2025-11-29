using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Features.Matchmaking
{
    public sealed class MatchmakingServerSystem : ModSystem
    {
        private int _lastOnline;

        public override void PostUpdateEverything()
        {
            if (Main.netMode != NetmodeID.Server) return;

            var mod = ModContent.GetInstance<PvPAdventure>();

            // prune any queued entries that are no longer active
            mod.PruneQueueInactive();

            // recompute online and broadcast if changed
            int online = 0;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                var p = Main.player[i];
                if (p != null && p.active) online++;
            }

            if (online != _lastOnline)
            {
                _lastOnline = online;
                mod.BroadcastQueueCounts();
            }
        }
    }
}
