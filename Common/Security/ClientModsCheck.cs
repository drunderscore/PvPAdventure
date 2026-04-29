using PvPAdventure.Core.Config;
using PvPAdventure.Core.Net;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Security;

public class ClientModsCheck : ModPlayer
{
    /// <summary>
    /// Sends a packet to the server with the list of client mods when joining a world.
    /// Packet is received in <see cref="ClientModHandler.HandlePacket"/> and the server checks if any client mods are not allowed."/>
    /// </summary>
    public override void OnEnterWorld()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        if (ModContent.GetInstance<ServerConfig>().ClientMods.AllowAnyClientMods)
            return;

        List<string> clientMods =
            ModLoader.Mods
                .Where(m => m.Side == ModSide.Client)
                .Select(m => m.Name)
                .ToList();

        var packet = Mod.GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.ClientModCheck);
        packet.Write(clientMods.Count);

        foreach (string name in clientMods)
            packet.Write(name);

        packet.Send();
    }
}

/// <summary>
/// Checks for client mods when joining.
/// </summary>
internal class ClientModHandler
{
    public static void HandlePacket(BinaryReader reader, int from)
    {
        // Safety: Ensure we have at least 4 bytes to read the Int32 'num'
        if (reader.BaseStream.Length - reader.BaseStream.Position < 4)
        {
            Log.Warn($"[ClientModCheck] Malformed packet from {from}. Not enough data to read count.");
            return;
        }

        int num = reader.ReadInt32();

        // Safety: Prevent an impossible loop if 'num' is a gargantuan number from corrupted data
        long remainingBytes = reader.BaseStream.Length - reader.BaseStream.Position;
        if (num < 0 || num > 500) // No one has 500 client mods
        {
            Log.Warn($"[ClientModCheck] Invalid mod count ({num}) from {from}. Skipping.");
            return;
        }

        List<string> allowedClientMods = [];

        allowedClientMods.AddRange(
            from mod in ModLoader.Mods
            where mod.Side == ModSide.Client
            select mod.Name
        );

        allowedClientMods.AddRange(
            ModContent.GetInstance<ServerConfig>().ClientMods.AllowedClientMods
        );

        allowedClientMods.Add("PrivateCheats"); // holy fkn airball

        List<string> unallowedClientMods = [];

        for (int i = 0; i < num; i++)
        {
            string name = reader.ReadString();
            if (!allowedClientMods.Contains(name))
            {
                unallowedClientMods.Add(name);
            }
        }

        if (unallowedClientMods.Count > 0)
        {
            string names = string.Join(", ", unallowedClientMods);
            Log.Debug($"Player id {from} has {unallowedClientMods.Count} unallowed client mods: {names}");
            NetMessage.BootPlayer(
                from,
                NetworkText.FromLiteral($"Unallowed client mods: {names}")
            );
        }
    }
}
