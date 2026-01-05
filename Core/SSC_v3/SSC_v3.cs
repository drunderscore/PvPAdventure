using Steamworks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.IO;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Core.SSC_v3;

[Autoload(Side = ModSide.Both)]
public class SSC_v3 : ModSystem
{
    public static string PATH => Path.Combine(Main.SavePath, "PvPAdventureSSC");
    public static string MapID => Main.ActiveWorldFileData?.Name ?? "NoWorldLoaded";

    private static readonly object _ioLock = new();
    private static readonly Dictionary<int, string> _steamIdByWho = [];

    private static bool _joinSentThisSession;

    public override void OnWorldUnload()
    {
        _joinSentThisSession = false;
        _steamIdByWho.Clear();
    }

    private static void EnsureWorldDirectory_Server()
    {
        lock (_ioLock)
        {
            Directory.CreateDirectory(Path.Combine(PATH, MapID));
        }
    }

    private static (string plrPath, string tplrPath, string dir) GetServerPaths(string steamId, string playerName)
    {
        var dir = Path.Combine(PATH, MapID, steamId);
        return (
            Path.Combine(dir, $"{playerName}.plr"),
            Path.Combine(dir, $"{playerName}.tplr"),
            dir
        );
    }

    public static void SendJoinRequestOnce()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        if (_joinSentThisSession)
            return;

        _joinSentThisSession = true;

        string steamId = SteamUser.GetSteamID().m_SteamID.ToString();
        var name = Main.LocalPlayer.name;
        
        var p = ModContent.GetInstance<PvPAdventure>().GetPacket();
        p.Write((byte)AdventurePacketIdentifier.SSC);
        p.Write((byte)SSCPacketType.ClientJoin);
        p.Write(steamId);
        p.Write(name);
        p.Send();

        Log.Chat($"Client sent join request with name {name}");
    }

    public static void HandlePacket(BinaryReader reader, int from)
    {
        var msg = (SSCPacketType)reader.ReadByte();

        switch (msg)
        {
            case SSCPacketType.ClientJoin:
                //Log.Chat($"ClientJoin packet from " + from);
                ClientJoin(reader, from);
                break;
            case SSCPacketType.ServerSendPlayerToClient:
                //Log.Chat($"ServerSendPlayerToClient packet from " + from);
                ClientHandleReceivedPlayer(reader, from);
                break;
            case SSCPacketType.ServerSavePlayer:
                //Log.Chat($"ServerSavePlayer packet from " + from);
                SavePlayer(reader, from);
                break;
        }
    }

    private static void ClientJoin(BinaryReader reader, int from)
    {
        if (Main.netMode == NetmodeID.Server)
        {
            var steamId = reader.ReadString();
            var name = reader.ReadString();

            _steamIdByWho[from] = steamId;

            EnsureWorldDirectory_Server();

            var (plrPath, tplrPath, dir) = GetServerPaths(steamId, name);

            lock (_ioLock)
            {
                Directory.CreateDirectory(dir);

                // Check if player files exists
                bool isNew = !File.Exists(plrPath) || !File.Exists(tplrPath);

                if (isNew)
                {
                    Log.Chat($"Creating new player: {name}");
                    CreateNewPlayer(plrPath, name);
                }
                else
                {
                    Log.Chat($"Player found! Loading player!");
                }

                // Read file
                byte[] data = File.ReadAllBytes(plrPath);
                TagCompound root = TagIO.FromFile(tplrPath);

                // Send packet
                var p = ModContent.GetInstance<PvPAdventure>().GetPacket();
                p.Write((byte)AdventurePacketIdentifier.SSC);
                p.Write((byte)SSCPacketType.ServerSendPlayerToClient);
                p.Write(isNew);
                p.Write(data.Length);
                p.Write(data);
                TagIO.Write(root, p);
                p.Send(toClient: from);
            }
        }
    }

    private static void ClientHandleReceivedPlayer(BinaryReader reader, int from)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        bool isNew = reader.ReadBoolean();

        int len = reader.ReadInt32();
        byte[] data = reader.ReadBytes(len);
        TagCompound root = TagIO.Read(reader);

        string steamId = SteamUser.GetSteamID().m_SteamID.ToString();

        var ms = new MemoryStream();
        TagIO.ToStream(root, ms);

        var fileData = new PlayerFileData(Path.Combine(Main.PlayerPath, $"{steamId}.SSC"),cloudSave: false)
        {
            Metadata = FileMetadata.FromCurrentSettings(FileType.Player),
        };

        Player.LoadPlayerFromStream(fileData, data, ms.ToArray());
        fileData.MarkAsServerSide();
        fileData.SetAsActive();

        fileData.Player.Spawn(PlayerSpawnContext.SpawningIntoWorld);
        Player.Hooks.EnterWorld(Main.myPlayer);

        Log.Chat(isNew ? "Loaded NEW SSC player" : "Loaded SSC player");
    }

    private static void CreateNewPlayer(string plrPath, string name)
    {
        Player player = new Player();

        player.name = "aooga";
        player.difficulty = PlayerDifficultyID.SoftCore;

        player.statLifeMax2 = 500;
        player.statLife = 500;

        player.statManaMax = 40;
        player.statManaMax2 = 40;
        player.statMana = 40;

        var fileData = new PlayerFileData(plrPath, cloudSave: false)
        {
            Metadata = FileMetadata.FromCurrentSettings(FileType.Player),
            Player = player,
        };
        Log.Chat("Created new player " + name + " with " + player.statLifeMax2 + " HP");

        Player.InternalSavePlayerFile(fileData);
    }

    private static void SavePlayer(BinaryReader reader, int fromWho)
    {
        Log.Chat("Server trying to save player");
        var steamID = reader.ReadString();
        var name = reader.ReadString();
        var data = reader.ReadBytes(reader.ReadInt32());
        var root = TagIO.Read(reader);
        var first = reader.ReadBoolean();

        // Write to file
        Utils.TryCreatingDirectory(Path.Combine(PATH, MapID, steamID));

        File.WriteAllBytes(Path.Combine(PATH, MapID, steamID, $"{name}.plr"), data);
        TagIO.ToFile(root, Path.Combine(PATH, MapID, steamID, $"{name}.tplr"));

        var stream = new MemoryStream();
        TagIO.ToStream(root, stream);
        stream.Flush();
        Log.Chat("Server successfully saved player");
    }
}

public enum SSCPacketType : byte
{
    ClientJoin,
    ServerCreateNewPlayer,
    ServerSendPlayerToClient,
    ServerSavePlayer
}
