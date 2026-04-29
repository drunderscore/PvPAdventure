using Microsoft.Xna.Framework;
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

        Color teamColor = Main.teamColor[Math.Clamp(player.team, 0, Main.teamColor.Length - 1)];
        SendSystemTeamMessage(player, $"{player.name} has teleported to {destination}", teamColor);
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
            NetworkText message = NetworkText.FromLiteral(ChatPrefixFormatter.TeamChannelMarker + targetText);

            ChatHelper.SendChatMessageToClient(message, color, i);
        }
    }

    private static string GetDestination(Player player, TravelType type, int targetIdx)
    {
        return type switch
        {
            TravelType.World => "world spawn",
            TravelType.Bed => "a bed",
            TravelType.Portal => "a portal",
            TravelType.Random => "a random location",
            _ => ""
        };
    }

    private static string GetOwnedDestination(Player player, int targetIdx, string place)
    {
        if (targetIdx == player.whoAmI)
            return $"their own {place}";

        if (targetIdx < 0 || targetIdx >= Main.maxPlayers)
            return "";

        return Main.player[targetIdx] is { active: true } target ? $"{target.name}'s {place}" : "";
    }
}
