using Microsoft.Xna.Framework;
using PvPAdventure.Core.Config;
using System;
using Terraria;
using Terraria.GameContent.UI.Chat;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Chat;

internal sealed class ChatPrefixSystem : ModSystem
{
    public override void Load()
    {
        if (!Main.dedServ)
            On_RemadeChatMonitor.AddNewMessage += AddPrefix;
    }

    public override void Unload()
    {
        On_RemadeChatMonitor.AddNewMessage -= AddPrefix;
    }

    private void AddPrefix(On_RemadeChatMonitor.orig_AddNewMessage orig, RemadeChatMonitor self, string text, Color color, int widthLimitInPixels)
    {
        try
        {
            if (!ModContent.GetInstance<ClientConfig>().ShowChatChannelPrefixes)
            {
                orig(self, text, color, widthLimitInPixels);
                return;
            }

            orig(self, ChatPrefixFormatter.Apply(text, color), color, widthLimitInPixels);
        }
        catch (Exception e)
        {
            Mod.Logger.Warn($"Chat prefix failed; fell back to vanilla text. {e}");
            orig(self, text, color, widthLimitInPixels);
        }
    }
}

internal enum ChatPrefixKind
{
    None,
    All,
    Team,
    System,
}

internal static class ChatPrefixFormatter
{
    private const string AllPrefixText = "(ALL)";
    private const string TeamPrefixText = "(TEAM)";
    private const string SystemPrefixText = "(SYSTEM)";
    internal const string AllChannelMarker = "\uE000PVPA_ALL\uE000";
    internal const string TeamChannelMarker = "\uE000PVPA_TEAM\uE000";

    //private static readonly Color AllPrefixColor = new(189, 199, 213);
    private static readonly Color AllPrefixColor = new(220, 228, 240);
    //private static readonly Color SystemPrefixColor = new(255, 209, 102);

    public static string Apply(string text, Color color)
    {
        bool forceAll = text.Contains(AllChannelMarker, StringComparison.Ordinal);
        bool forceTeam = text.Contains(TeamChannelMarker, StringComparison.Ordinal);

        text = text.Replace(AllChannelMarker, "");
        text = text.Replace(TeamChannelMarker, "");

        if (HasOwnPrefix(text))
            return text;

        ChatPrefixKind kind = forceTeam ? ChatPrefixKind.Team : forceAll ? ChatPrefixKind.All : GetKind(text, color);

        return kind switch
        {
            ChatPrefixKind.Team => MakePrefix(TeamPrefixText, color) + text,
            ChatPrefixKind.All => MakePrefix(AllPrefixText, AllPrefixColor) + text,
            _ => text,
        };
    }

    private static ChatPrefixKind GetKind(string text, Color color)
    {
        // Player messages can be inferred from sender + team color.
        if (TryGetSenderName(text, out string senderName))
            return IsTeamMessage(senderName, color) ? ChatPrefixKind.Team : ChatPrefixKind.All;

        // System/local/private messages should not pretend to be all-chat.
        // They need an explicit marker if they should show (ALL) or (TEAM).
        return ChatPrefixKind.None;
    }

    private static bool TryGetSenderName(string text, out string senderName)
    {
        senderName = string.Empty;

        int tagStart = text.IndexOf("[n:", StringComparison.OrdinalIgnoreCase);
        if (tagStart < 0)
            return false;

        int nameStart = tagStart + 3;
        int nameEnd = text.IndexOf(']', nameStart);
        if (nameEnd <= nameStart)
            return false;

        senderName = text[nameStart..nameEnd];
        return !string.IsNullOrWhiteSpace(senderName);
    }

    private static bool IsTeamMessage(string senderName, Color color)
    {
        int playerIndex = FindActivePlayer(senderName);
        if (playerIndex < 0)
            return false;

        Player player = Main.player[playerIndex];
        if (player.team <= 0 || player.team >= Main.teamColor.Length)
            return false;

        return SameRgb(color, Main.teamColor[player.team]);
    }

    private static int FindActivePlayer(string name)
    {
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];
            if (player?.active == true && player.name == name)
                return i;
        }

        return -1;
    }

    private static string MakePrefix(string text, Color color)
    {
        return $"[c/{color.R:X2}{color.G:X2}{color.B:X2}:{text}] ";
    }

    private static bool SameRgb(Color left, Color right)
    {
        return left.R == right.R && left.G == right.G && left.B == right.B;
    }

    private static bool HasOwnPrefix(string text)
    {
        return text.Contains(AllPrefixText, StringComparison.Ordinal) ||
            text.Contains(TeamPrefixText, StringComparison.Ordinal) ||
            text.Contains(SystemPrefixText, StringComparison.Ordinal);
    }
}