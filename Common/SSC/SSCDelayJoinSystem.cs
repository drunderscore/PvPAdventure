using PvPAdventure.Core.Config;
using PvPAdventure.Core.Net;
using Steamworks;
using System;
using PvPAdventure.Common.Discord;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tModPorter;
using static PvPAdventure.Common.SSC.SSC;
using static PvPAdventure.Core.Config.SSCConfig;

namespace PvPAdventure.Common.SSC;

/// <summary>
/// Joins the world as a ghost, 
/// and after a small delay sends a request to join as a proper SSC character.
/// Hopefully reworked in the future for smoother player experience.
/// </summary>
[Autoload(Side = ModSide.Client)]
public class SSCDelayJoinSystem : ModSystem
{
    private bool _sent;
    private int _delayTicks;
    public override void OnWorldLoad()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        if (!SSC.IsEnabled)
            return;

        _sent = false;
        _delayTicks = 30*1; // wait a second to ensure vanilla hooks run properly.
        // in the future, the delay may be 0 or we may use a hook that runs earlier for a smoother join experience.

        // Enter as a ghost initially
        Main.LocalPlayer.ghost = true;

#if DEBUG
        // DEBUG:
        // Be the local char for 5 seconds.
        // Some extra time to properly debug the SSC flow of joining with a local char.
        // Also disable ghost to see the real player.
        //_delayTicks = 60 * 5;
        //Main.LocalPlayer.ghost = false;
#endif
    }

    public override void PostUpdateEverything()
    {
        //Main.ActivePlayerFileData.Player.Spawn(PlayerSpawnContext.SpawningIntoWorld);
        //Player.Hooks.EnterWorld(Main.myPlayer);

        if (_sent)
            return;

        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        if (_delayTicks > 0)
        {
            if (_delayTicks % 60 == 0)
            {
                Log.Chat("Joining in ticks: " + _delayTicks);
            }
            _delayTicks--;
            return;
        }

        _sent = true;

        // Join immediately if arenas is off.
        var config = ModContent.GetInstance<ArenasConfig>();
        if (!config.IsArenasEnabled)
        {
            SendJoinRequest();
        }
    }

    public override void OnWorldUnload()
    {
        _sent = false;
        _delayTicks = 0;
    }

    public static void SendJoinRequest()
    {
        if (!SSC.IsEnabled)
            return;

        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        // Get the desired name based on config setting
        string desiredName = GetDesiredPlayerName();

        Player appearanceSource = SSCGhostJoinSystem.JoinPlayerSnapshot ?? Main.LocalPlayer;

        // Send packet
        var packet = ModContent.GetInstance<PvPAdventure>().GetPacket();

        packet.Write((byte)AdventurePacketIdentifier.SSC);
        packet.Write((byte)SSCPacketType.ClientJoin);
        packet.Write(desiredName);
        Appearance.WriteAppearence(packet, appearanceSource);

        packet.Send();
    }

    #region Helpers

    public static string GetDesiredPlayerName()
    {
        var config = ModContent.GetInstance<SSCConfig>();

        if (Main.LocalPlayer == null)
        {
            Log.Error("Houston we have a problem: LocalPlayer is null when trying to get desired player name for SSC. This should never happen. Returning default name.");
            return "TPVPAPlayer";
        }

        string desiredName = config.SSCPlayerNames switch
        {
            SSCPlayerNameType.Default => Main.LocalPlayer.name,
            SSCPlayerNameType.Steam => SanitizePlayerName(GetSteamName()),
            SSCPlayerNameType.Discord => SanitizePlayerName(GetDiscordName()),
            SSCPlayerNameType.Numbered => GetFirstFreeNumberedPlayerName(),
            _ => Main.LocalPlayer.name
        };

        return desiredName;
    }

    public static string GetSteamName()
    {
        return SteamFriends.GetPersonaName();
    }

    public static string GetDiscordName()
    {
        return ModContent.GetInstance<DiscordSocialManager>().CurrentUser?.GlobalName ?? Main.LocalPlayer.name;
    }

    public static string SanitizePlayerName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        name = name.Replace("\r", "")
                   .Replace("\n", "")
                   .Replace("\t", "")
                   .Trim();

        // FIXME: Change Player.nameLen to be way bigger so hopefully this would be not needed?
        const int maxLen = 16;
        if (name.Length > maxLen)
            name = name.Substring(0, maxLen);

        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    public static string GetFirstFreeNumberedPlayerName()
    {
        const string prefix = "Player";
        const int max = 100;

        bool[] used = new bool[max];

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player p = Main.player[i];
            if (p == null || !p.active)
                continue;

            string name = p.name;
            if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            string suffix = name.Substring(prefix.Length);
            if (!int.TryParse(suffix, out int number))
                continue;

            if (number > 0 && number < max)
                used[number] = true;
        }

        for (int i = 1; i < max; i++)
        {
            if (!used[i])
                return prefix + i;
        }

        return prefix;
    }

    #endregion

}
