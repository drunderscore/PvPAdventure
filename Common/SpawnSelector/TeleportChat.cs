using Microsoft.Xna.Framework;
using PvPAdventure.Common.Teams;
using PvPAdventure.Core.Config;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.SpawnSelector;

public static class TeleportChat
{
    private static readonly Color MessageColor = Color.Yellow;

    public static void Announce(Player player, SpawnType type, int targetIdx = -1)
    {
        var clientConfig = ModContent.GetInstance<ClientConfig>();
        if (!clientConfig.ShowTeleportPlayerMessages)
            return;

        if (player == null || !player.active)
            return;

        string destination = GetDestination(player, type, targetIdx);
        if (destination == "")
            return;

        TeamChatManager.SendSystemTeamMessage(
            player,
            $"{player.name} has teleported to {destination}",
            MessageColor
        );
    }

    private static string GetDestination(Player player, SpawnType type, int targetIdx)
    {
        return type switch
        {
            SpawnType.World => "world spawn",
            SpawnType.MyBed => "their own bed",
            SpawnType.MyPortal => "their own portal",
            SpawnType.Random => "a random location",
            SpawnType.TeammateBed => GetOwnedDestination(player, targetIdx, "bed"),
            SpawnType.TeammatePortal => GetOwnedDestination(player, targetIdx, "portal"),
            _ => ""
        };
    }

    private static string GetOwnedDestination(Player player, int targetIdx, string place)
    {
        if (targetIdx == player.whoAmI)
            return $"their own {place}";

        if (targetIdx < 0 || targetIdx >= Main.maxPlayers)
            return "";

        Player target = Main.player[targetIdx];
        if (target == null || !target.active)
            return "";

        return $"{target.name}'s {place}";
    }
}
