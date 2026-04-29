using Microsoft.Xna.Framework;
using PvPAdventure.Common.Spectator.Drawers;
using PvPAdventure.Common.Teams;
using PvPAdventure.Common.Travel;
using PvPAdventure.Core.Config;
using System;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Chat;

/// <summary>
/// Helper class for sending chat messages when players teleport.
/// Mostly used for spawn selector.
/// </summary>
public static class TeleportChat
{
    public static void Announce(Player player, TravelType type, int targetIdx = -1)
    {
        var clientConfig = ModContent.GetInstance<ClientConfig>();

        if (!clientConfig.ShowTeleportPlayerMessages)
            return;

        if (player?.active != true)
            return;

        string destination = GetDestination(player, type, targetIdx);

        if (destination == "")
            return;

        SendSystemTeamMessage(player, $"{player.name} has teleported to {destination}", Color.Yellow);
    }

    public static void AnnouncePortalOpened(Player player)
    {
        var clientConfig = ModContent.GetInstance<ClientConfig>();

        if (!clientConfig.ShowTeleportPlayerMessages)
            return;

        if (player?.active != true)
            return;

        string biome = BiomeHelper.GetBiomeDisplayName(player);

        SendSystemTeamMessage(player, $"{player.name} has opened a portal in {biome}", Color.Yellow);
    }

    public static void AnnouncePortalDestroyed(Player owner, string fallbackOwnerName)
    {
        var clientConfig = ModContent.GetInstance<ClientConfig>();

        if (!clientConfig.ShowTeleportPlayerMessages)
            return;

        if (owner?.active != true)
            return;

        //string biome = BiomeHelper.GetBiomeDisplayName(owner);

        string name = !string.IsNullOrWhiteSpace(fallbackOwnerName) ? fallbackOwnerName : owner.name;

        SendSystemTeamMessage(owner, $"{name}'s portal has been destroyed!", Color.Yellow);
    }

    private static string GetDestination(Player player, TravelType type, int targetIdx)
    {
        return type switch
        {
            TravelType.World => "world spawn",
            TravelType.Bed => GetOwnedDestination(player, targetIdx, "bed"),
            TravelType.Portal => GetOwnedDestination(player, targetIdx, "portal"),
            TravelType.Random => "a random location",
            _ => ""
        };
    }

    private static string GetOwnedDestination(Player player, int targetIdx, string place)
    {
        if (player?.active != true)
            return "";

        if (targetIdx == player.whoAmI)
            return $"their own {place}";

        if (targetIdx < 0 || targetIdx >= Main.maxPlayers)
            return "";

        return Main.player[targetIdx] is { active: true } target ? $"{target.name}'s {place}" : "";
    }

    private static string GetOwnedDestination(int targetIdx, string place)
    {
        if (targetIdx < 0 || targetIdx >= Main.maxPlayers)
            return "";

        return Main.player[targetIdx] is { active: true } target ? $"{target.name}'s {place}" : "";
    }

    public static void SendSystemTeamMessage(Player player, string text, Color color, string selfText = null)
    {
        if (player == null || !player.active)
            return;

        selfText ??= text;

        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            Main.NewText(selfText, color);
            return;
        }

        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            if (player.whoAmI == Main.myPlayer)
                Main.NewText(selfText, color);

            return;
        }

        if (player.team == 0)
        {
            ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral(selfText), color, player.whoAmI);
            return;
        }

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player teammate = Main.player[i];

            if (teammate == null || !teammate.active || teammate.team != player.team)
                continue;

            string targetText = i == player.whoAmI ? selfText : text;
            //NetworkText message = NetworkText.FromLiteral(ChatPrefixFormatter.TeamChannelMarker + targetText);
            NetworkText message = NetworkText.FromLiteral(targetText);

            ChatHelper.SendChatMessageToClient(message, color, i);
        }
    }
}
