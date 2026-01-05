using Steamworks;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Core.SSC;

/// <summary>
/// Provides server-side character (SSC) functionality.
/// Stores player files on the server at ..tModLoader/PvPAdventureSSC/[WorldName]/[SteamID]/[PlayerName].plr
/// Stores temporary player data at ..tModLoader/Players/[SteamID].SSC
/// </summary>
[Autoload(Side = ModSide.Both)]
public class SSC : ModSystem
{
    private static string SSCFolder => Path.Combine(Main.SavePath, "PvPAdventureSSC");
    private static string MapID => Main.ActiveWorldFileData?.Name ?? "UnknownWorld";
    private static readonly object ioLock = new();

    public static void SendJoinRequest()
    {
        if (!SSCEnabled.IsEnabled)
            return;

        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            string steamId = SteamUser.GetSteamID().m_SteamID.ToString();
            var name = Main.LocalPlayer.name;

            var p = ModContent.GetInstance<PvPAdventure>().GetPacket();
            p.Write((byte)AdventurePacketIdentifier.SSC);
            p.Write((byte)SSCPacketType.ClientJoin);
            p.Write(steamId);
            p.Write(name);
            p.Send();

            Log.Chat($"Client joined with name {name}");
        }
    }

    public static void HandlePacket(BinaryReader reader, int from)
    {
        var msg = (SSCPacketType)reader.ReadByte();

        switch (msg)
        {
            case SSCPacketType.ClientJoin:
                ClientJoin(reader, from);
                break;
            case SSCPacketType.LoadPlayer:
                LoadPlayer(reader, from);
                break;
            case SSCPacketType.SavePlayer:
                SavePlayer(reader);
                break;
        }
    }

    private static void ClientJoin(BinaryReader reader, int from)
    {
        if (Main.netMode == NetmodeID.Server)
        {
            // Get data from client
            var steamId = reader.ReadString();
            var name = reader.ReadString();

            byte[] data;
            TagCompound root;
            bool isNew;

            // Lock IO operations to prevent race conditions
            lock (ioLock)
            {
                // Create directories for SSC and player if they don't exist
                Directory.CreateDirectory(Path.Combine(SSCFolder, MapID));

                var dir = Path.Combine(SSCFolder, MapID, steamId);
                Directory.CreateDirectory(dir);

                var plrPath = Path.Combine(dir, $"{name}.plr");
                var tplrPath = Path.Combine(dir, $"{name}.tplr");

                isNew = !File.Exists(plrPath) || !File.Exists(tplrPath);

                if (isNew)
                {
                    CreateNewPlayer(plrPath, name);
                }

                // Read player data from files
                data = File.ReadAllBytes(plrPath);
                root = TagIO.FromFile(tplrPath);
            }

            // Send player data back to client
            var p = ModContent.GetInstance<PvPAdventure>().GetPacket();
            p.Write((byte)AdventurePacketIdentifier.SSC);
            p.Write((byte)SSCPacketType.LoadPlayer);
            p.Write(isNew);
            p.Write(data.Length);
            p.Write(data);
            TagIO.Write(root, p);
            p.Send(toClient: from);
        }
    }

    private static void LoadPlayer(BinaryReader reader, int from)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            bool isNew = reader.ReadBoolean();

            int len = reader.ReadInt32();
            byte[] data = reader.ReadBytes(len);
            TagCompound root = TagIO.Read(reader);

            string steamId = SteamUser.GetSteamID().m_SteamID.ToString();

            var ms = new MemoryStream();
            TagIO.ToStream(root, ms);

            var fileData = new PlayerFileData(Path.Combine(Main.PlayerPath, $"{steamId}.SSC"), cloudSave: false)
            {
                Metadata = FileMetadata.FromCurrentSettings(FileType.Player),
            };

            Player.LoadPlayerFromStream(fileData, data, ms.ToArray());
            fileData.MarkAsServerSide();
            fileData.SetAsActive();

            fileData.Player.Spawn(PlayerSpawnContext.SpawningIntoWorld);
            Player.Hooks.EnterWorld(Main.myPlayer);

            // Apply life and mana again to ensure
            //SSCStarterItems.ApplyStartLife(Main.LocalPlayer);
            //SSCStarterItems.ApplyStartMana(Main.LocalPlayer);

            Log.Chat(isNew ? "Loaded new SSC player " : "Loaded existing SSC player " + fileData.Player.name);

            // Print chat to players with player and playtime
            Main.NewText($"Welcome to TPVPA, {Main.LocalPlayer.name}! — Playtime: {FormatPlayTime(Main.ActivePlayerFileData.GetPlayTime())}", Color.MediumPurple);
        }
    }

    public static string FormatPlayTime(TimeSpan t)
    {
        int hours = (int)t.TotalHours;
        return $"{hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
    }

    private static void CreateNewPlayer(string plrPath, string name)
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

        // Apply config options
        SSCStarterItems.ApplyStartItems(fileData.Player);
        SSCStarterItems.ApplyStartLife(fileData.Player);
        SSCStarterItems.ApplyStartMana(fileData.Player);

        // Save the player
        //fileData.MarkAsServerSide();
        Player.InternalSavePlayerFile(fileData);

        Log.Chat("Created and saved new player " + name);
    }

    private static void SavePlayer(BinaryReader reader)
    {
        // Receive player data from client save system and save to server disk
        if (Main.netMode == NetmodeID.Server)
        {
            // Read data from client
            var steamID = reader.ReadString();
            var name = reader.ReadString();
            var data = reader.ReadBytes(reader.ReadInt32());
            var root = TagIO.Read(reader);

            // Ensure directory exists
            Utils.TryCreatingDirectory(Path.Combine(SSCFolder, MapID, steamID));

            // Write to file
            File.WriteAllBytes(Path.Combine(SSCFolder, MapID, steamID, $"{name}.plr"), data);
            TagIO.ToFile(root, Path.Combine(SSCFolder, MapID, steamID, $"{name}.tplr"));

            // Flush to ensure data is written
            var stream = new MemoryStream();
            TagIO.ToStream(root, stream);
            stream.Flush();

            Log.Chat("Saved player " + name);
        }
    }
}

public enum SSCPacketType : byte
{
    ClientJoin,
    LoadPlayer,
    SavePlayer
}
