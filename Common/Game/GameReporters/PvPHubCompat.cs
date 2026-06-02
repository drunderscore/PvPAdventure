using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Game.GameReporters;

[JITWhenModsEnabled("PvPHub")]
[ExtendsFromMod("PvPHub")]
public class PvPHubCompat
{
    public static bool IsPvPHubLoaded => ModLoader.TryGetMod("PvPHub", out _);

    public static bool TryGetSteamId(Player player, out ulong steamId)
    {
        ulong? id = player.GetModPlayer<PvPHub.Common.Authentication.AuthenticatedPlayer>().SteamId;

        if (id.HasValue && id.Value != 0 && id.Value <= (ulong)long.MaxValue)
        {
            steamId = id.Value;
            return true;
        }

        steamId = 0;
        return false;
    }
}
