using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Features.Matchmaking
{
    public sealed class MatchmakingClientPlayer : ModPlayer
    {
        public override void OnEnterWorld()
        {
            MatchmakingClient.RequestCounts();
        }
    }
}