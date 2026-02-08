using PvPAdventure.Core.Config;
using PvPAdventure.Core.Net;
using Steamworks;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Security;

/// <summary>
/// Client-side: Sends Steam ID to server when joining
/// </summary>
[Autoload(Side = ModSide.Client)]
public class WhitelistPlayerCheck : ModPlayer
{
    public override void OnEnterWorld()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        var steamId = SteamUser.GetSteamID().m_SteamID.ToString();
        Log.Debug($"Sending Steam ID to server: {steamId}");

        var packet = Mod.GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.WhitelistPlayerCheck);
        packet.Write(steamId);
        packet.Send();
    }
}

/// <summary>
/// Server-side: Checks if player's Steam ID is whitelisted
/// </summary>
internal class WhitelistPlayerHandler
{
    public static void HandlePacket(BinaryReader reader, int from)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        string steamId = reader.ReadString();
        var config = ModContent.GetInstance<ServerConfig>();

        Log.Debug($"Checking whitelist for player {from} with Steam ID: {steamId}");

        // If whitelist is disabled, allow everyone
        if (config.WhitelistPlayers.AllowAnyPlayerToJoin)
            return;

        // Check if Steam ID is in whitelist
        if (!config.WhitelistPlayers.AllowedPlayerSteamIds.Contains(steamId))
        {
            Log.Debug($"Player {from} ({steamId}) is not whitelisted. Kicking...");
            NetMessage.BootPlayer(
                from,
                NetworkText.FromLiteral($"You are not whitelisted on this server")
            );
        }
        else
        {
            Log.Debug($"Success! Player {from} ({steamId}) is whitelisted. Joining...");
        }
    }
}