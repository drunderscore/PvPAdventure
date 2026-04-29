using Microsoft.Xna.Framework;
using PvPAdventure.Common.Authentication;
using PvPAdventure.Core.Config;
using PvPAdventure.Core.Net;
using Steamworks;
using System;
using System.IO;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.IO;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using static PvPAdventure.Common.SSC.Appearance;

namespace PvPAdventure.Common.SSC;

/// <summary>
/// Provides server-side character (SSC) functionality.
/// Stores player files on the server at ..tModLoader/PvPAdventureSSC/[WorldName]/[SteamID].plr
/// Stores temporary player data at ..tModLoader/Players/[SteamID].SSC
/// Also <see cref="SSCDelayJoinSystem"/> <seealso cref="SSCSaveSystem"/>
/// </summary>
[Autoload(Side = ModSide.Both)]
public class SSC : ModSystem
{
    // Whether SSC is enabled from config
    public static bool IsEnabled => ModContent.GetInstance<SSCConfig>().IsSSCEnabled;

    // Folder to store SSC files
    private static string SSCFolder => Path.Combine(Main.SavePath, "PvPAdventureSSC");
    private static string MapID => Main.ActiveWorldFileData?.Name ?? "UnknownWorld";
    private static string WorldSSCFolder => Path.Combine(SSCFolder, MapID);

    private static string GetPlayerPath(string steamId)
    {
        return Path.Combine(WorldSSCFolder, steamId + ".plr");
    }

    private static string GetTPlayerPath(string steamId)
    {
        return Path.Combine(WorldSSCFolder, steamId + ".tplr");
    }

    // Lock object used for IO operations
    private static readonly object ioLock = new();

    // Packet types
    public enum SSCPacketType : byte
    {
        ClientJoin,
        LoadPlayer,
        SavePlayer
    }

    private const int MaxPlayerFileBytes = 1024 * 1024; // = 1 MB

    public static void HandlePacket(BinaryReader reader, int from)
    {
        if (!IsEnabled)
            return;

        var msg = (SSCPacketType)reader.ReadByte();

        switch (msg)
        {
            case SSCPacketType.ClientJoin:
                ClientJoin(reader, from);
                break;
            case SSCPacketType.LoadPlayer:
                LoadPlayer(reader);
                break;
            case SSCPacketType.SavePlayer:
                SavePlayer(reader, from);
                break;
        }
    }

    private static void ClientJoin(BinaryReader reader, int from)
    {
        Log.Debug("Received ClientJoin packet from ID: " + from);

        if (Main.netMode != NetmodeID.Server)
            return;

        // Get data from client
        //string steamIdFromClient = reader.ReadString();
        string nameFromClient = reader.ReadString();
        PlayerAppearance appearance = ReadAppearance(reader);

        //Main.player[from].name = nameFromClient;
        //NetMessage.SendData(MessageID.PlayerInfo, -1, -1, null, from);

        ulong? authenticatedSteamId = Main.player[from].GetModPlayer<AuthenticatedPlayer>().SteamId;

        if (!authenticatedSteamId.HasValue)
        {
            Log.Warn($"Rejected SSC join for player {from}: Steam authentication is missing or pending");

            ChatHelper.SendChatMessageToClient(
                NetworkText.FromLiteral("Server has not verified your Steam identity yet. Try rejoining if this persists."),
                Color.OrangeRed,
                from);

            return;
        }

        string steamId = authenticatedSteamId.Value.ToString();

        Log.Debug($"Server accepted SSC join for authenticated SteamID: {steamId}, player name: {nameFromClient}, skin color: {appearance.SkinColor}");

        byte[] data;
        TagCompound root;
        bool isNew;

        lock (ioLock)
        {
            //string dir = Path.Combine(SSCFolder, MapID, Main.player[from].GetModPlayer<AuthenticatedPlayer>().SteamId.ToString());

            Directory.CreateDirectory(WorldSSCFolder);

            string plrPath = GetPlayerPath(steamId);
            string tplrPath = GetTPlayerPath(steamId);

            isNew = !File.Exists(plrPath) || !File.Exists(tplrPath);

            if (isNew)
            {
                CreateNewPlayer(plrPath, nameFromClient, appearance);
            }

            data = File.ReadAllBytes(plrPath);
            root = TagIO.FromFile(tplrPath);
            PvPAdventureSSCData.ApplyStatsToServerPlayer(from, root);
        }

        var p = ModContent.GetInstance<PvPAdventure>().GetPacket();
        p.Write((byte)AdventurePacketIdentifier.SSC);
        p.Write((byte)SSCPacketType.LoadPlayer);
        p.Write(isNew);
        p.Write(nameFromClient);
        Appearance.WriteAppearance(p, appearance);
        p.Write(data.Length);
        p.Write(data);
        TagIO.Write(root, p);
        p.Send(toClient: from);

        Log.Chat("Server sent SSC load for " + nameFromClient + " bytes=" + data.Length);
    }

    

    private static void LoadPlayer(BinaryReader reader)
    {
        Log.Debug("LoadPlayer packet received");

        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        bool isNew = reader.ReadBoolean();
        string desiredName = reader.ReadString();
        PlayerAppearance appearance = ReadAppearance(reader);

        int len = reader.ReadInt32();
        if (len < 0 || len > MaxPlayerFileBytes)
        {
            Log.Chat("Client received invalid SSC player length: " + len);
            return;
        }

        byte[] plrBytes = reader.ReadBytes(len);
        if (plrBytes.Length != len)
        {
            Log.Chat("Client received truncated SSC player data: " + plrBytes.Length + "/" + len);
            return;
        }

        TagCompound root = TagIO.Read(reader);

        //string steamId = SteamAuthentication.ClientSteamId.ToString();
        //string steamName = SteamFriends.GetPersonaName();
        string steamId = SteamUser.GetSteamID().m_SteamID.ToString();

        byte[] tplrBytes;
        using (var ms = new MemoryStream())
        {
            TagIO.ToStream(root, ms);
            tplrBytes = ms.ToArray();
        }

        string tempPlayerPath = Path.Combine(Main.PlayerPath, $"{steamId}.SSC");

        Main.QueueMainThreadAction(() =>
        {
            try
            {
                Log.Debug($"Client trying to load tempPlayerPath: {tempPlayerPath} with skin color: {appearance.SkinColor}");

                var fileData = new PlayerFileData(tempPlayerPath, cloudSave: false)
                {
                    Metadata = FileMetadata.FromCurrentSettings(FileType.Player),
                };

                Player.LoadPlayerFromStream(fileData, plrBytes, tplrBytes);
                fileData.Player.name = desiredName;
                Appearance.ApplyAppearance(fileData.Player, appearance);

                fileData.Player.hbLocked = true; // force hb locked (user preference)

                fileData.MarkAsServerSide();
                fileData.SetAsActive();

                // Sync to server! Might be redundant, might crash the game!
                NetMessage.SendData(MessageID.PlayerInfo, number: Main.myPlayer);
                NetMessage.SendData(MessageID.SyncPlayer, number: Main.myPlayer);
                NetMessage.SendData(MessageID.SyncEquipment, number: Main.myPlayer);

                Log.Debug($"Spawning into world as: {fileData.Player.name} with skin color: {appearance.SkinColor}");

                // Re-Enter the world with SSC character.
                fileData.Player.Spawn(PlayerSpawnContext.SpawningIntoWorld);
                Player.Hooks.EnterWorld(Main.myPlayer);

                TagCompound sscData = null;
                if (root.ContainsKey("PvPAdventureSSC"))
                {
                    sscData = root.GetCompound("PvPAdventureSSC");
                }

                PvPAdventureSSCData.ApplyStatsToServerPlayer(Main.myPlayer, root);

                bool positionRestored = PlayerPositionSystem.TryLoadPlayerPosition(fileData.Player, sscData);

                // Set max life and mana again just to make sure it gets applied.
                if (fileData.Player.statLife != fileData.Player.statLifeMax)
                    fileData.Player.statLife = fileData.Player.statLifeMax;
                if (fileData.Player.statMana != fileData.Player.statManaMax)
                    fileData.Player.statMana = fileData.Player.statManaMax;

                // Request map load to update world map data for the client based on their steam id
                MapLoadSystem.Request(delayTicks: 30);

                // Print player position
                PlayerPositionSystem.PrintWelcomeMessage(fileData.Player, sscData, positionRestored);
                SSCDelayJoinSystem.NotifySSCLoaded();

            }
            catch (Exception e)
            {
                Log.Chat("Client failed loading SSC player: " + e);
                Log.Error("Client failed loading SSC player: " + e);
            }
        });
    }

    private static void CreateNewPlayer(string plrPath, string name, PlayerAppearance appearance)
    {
        // Create a brand new empty player on the server
        var fileData = new PlayerFileData(plrPath, cloudSave: false)
        {
            Metadata = FileMetadata.FromCurrentSettings(FileType.Player),
            Player = new()
            {
                name = name,
                difficulty = PlayerDifficultyID.SoftCore,
            }
        };
        // Apply appearance
        Appearance.ApplyAppearance(fileData.Player, appearance);

        fileData.Player.hbLocked = true; // force hb locked (user preference)

        // Apply config options
        StartingItems.ApplyStartItems(fileData.Player);
        StartingItems.ApplyStartLife(fileData.Player);
        StartingItems.ApplyStartMana(fileData.Player);

        // Save the player
        //fileData.MarkAsServerSide();
        Player.InternalSavePlayerFile(fileData);

        Log.Chat("Created and saved new player " + name);
    }

    private static void SavePlayer(BinaryReader reader, int from)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        string nameFromClient = reader.ReadString();

        if (string.IsNullOrWhiteSpace(nameFromClient))
            nameFromClient = "TPVPAPlayer";

        int len = reader.ReadInt32();

        if (len < 0 || len > MaxPlayerFileBytes)
        {
            Log.Chat("Server received invalid SSC save length: " + len);
            return;
        }

        byte[] data = reader.ReadBytes(len);

        if (data.Length != len)
        {
            Log.Chat("Server received truncated SSC save: " + data.Length + "/" + len);
            return;
        }

        TagCompound root;

        try
        {
            root = TagIO.Read(reader);
        }
        catch (Exception e)
        {
            Log.Chat("Server failed reading SSC tplr data for " + nameFromClient + ", error: " + e);
            Log.Error("Server failed reading SSC tplr data for " + nameFromClient + ", error: " + e);
            return;
        }

        try
        {
            ulong? authenticatedSteamId = Main.player[from].GetModPlayer<AuthenticatedPlayer>().SteamId;
            //string steamId = reader.ReadString(); // old code where we sent steamID, keep for legacy's sake
            if (!authenticatedSteamId.HasValue)
            {
                Log.Warn($"Rejected SSC save for player {from}: Steam authentication is missing or pending");

                ChatHelper.SendChatMessageToClient(
                    NetworkText.FromLiteral($"{nameFromClient} Server FAILED to save because Steam ID authentication failed or is missing."),
                    Color.OrangeRed,
                    from);

                return;
            }

            string steamId = authenticatedSteamId.Value.ToString();
            int playerTeam = -1;

            lock (ioLock)
            {
                Directory.CreateDirectory(WorldSSCFolder);

                string plrPath = GetPlayerPath(steamId);
                string tplrPath = GetTPlayerPath(steamId);

                File.WriteAllBytes(plrPath, data);
                TagIO.ToFile(root, tplrPath);

                if (root.ContainsKey("PvPAdventureSSC"))
                {
                    TagCompound ssc = root.GetCompound("PvPAdventureSSC");

                    if (ssc.ContainsKey("team"))
                        playerTeam = ssc.GetInt("team");
                }
            }

            Log.Debug($"Server successfully received SSC save for {nameFromClient}, authenticated SteamID: {steamId}, bytes={len}, team={(Terraria.Enums.Team)playerTeam}");
        }
        catch (Exception e)
        {
            Log.Chat("Server failed saving SSC player with error: " + e);
        }
    }

}
