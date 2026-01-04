using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.Engine;
using Terraria.ModLoader.IO;
using static Terraria.GameContent.NetModules.NetTeleportPylonModule;

namespace PvPAdventure.Core.SSC_v3;

[Autoload(Side = ModSide.Both)]
public class SSC_v3 : ModSystem
{
    public static string PATH => Path.Combine(Main.SavePath, "PvPAdventureSSC");
    public static string MapID => Main.ActiveWorldFileData?.Name ?? "NoWorldLoaded";

    private static readonly object _ioLock = new();
    private static readonly Dictionary<int, string> _steamIdByWho = new();

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

        Log.Chat($"ClientJoin with steamId={steamId}, name={name}");

        var p = ModContent.GetInstance<PvPAdventure>().GetPacket();
        p.Write((byte)AdventurePacketIdentifier.SSC);
        p.Write((byte)SSCPacketType.ClientJoin);
        p.Write(steamId);
        p.Write(name);
        p.Send();
    }

    public static void HandlePacket(BinaryReader reader, int from)
    {
        var msg = (SSCPacketType)reader.ReadByte();

        switch (msg)
        {
            case SSCPacketType.ClientJoin:
                Log.Chat($"ClientJoin packet from " + from);
                HandleClientJoin_Server(reader, from);
                break;

            case SSCPacketType.ServerCreateNewPlayer:
                Log.Chat($"ServerCreateNewPlayer packet from " + from);
                break;
        }
    }

    private static void HandleClientJoin_Server(BinaryReader reader, int from)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        var steamId = reader.ReadString();
        var name = reader.ReadString();

        _steamIdByWho[from] = steamId;

        EnsureWorldDirectory_Server();

        var (plrPath, tplrPath, dir) = GetServerPaths(steamId, name);

        lock (_ioLock)
        {
            Directory.CreateDirectory(dir);

            // Check if player files exists
            if (!File.Exists(plrPath) && !File.Exists(tplrPath))
            {
                Log.Chat($"Creating new player: " + name);

                // Send to client
                var p = ModContent.GetInstance<PvPAdventure>().GetPacket();

                p.Write((byte)AdventurePacketIdentifier.SSC);
                p.Write((byte)SSCPacketType.ServerCreateNewPlayer);
                p.Write(steamId);
                p.Write(name);
                p.Send(toClient: from);
            }
            else
            {
                //plrBytes = null;
                //tplrBytes = null;
                Log.Chat($"Player found! Loading player!");
            }
        }

        
    }

    private static void CreateNewPlayer(BinaryReader reader)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        var steamId = reader.ReadString();
        var name = reader.ReadString();

        var fileData = new PlayerFileData(Path.Combine(Main.PlayerPath, $"{steamId}.plr"), false)
        {
            Metadata = FileMetadata.FromCurrentSettings(FileType.Player),
            Player = new Player
            {
                name = name,
                difficulty = 0,
                statLife = 500,
                statMana = 40,
                //dead = true,
                //ghost = true,
                // Prevent the automatic revive on entry from desyncing client and server
                //respawnTimer = int.MaxValue,
                //lastTimePlayerWasSaved = long.MaxValue,
                //savedPerPlayerFieldsThatArentInThePlayerClass = new Player.SavedPlayerDataWithAnnoyingRules()
            }
        };
    }

    //private static void ReadPlayerData(BinaryReader reader)
    //{
    //    if (Main.netMode == NetmodeID.MultiplayerClient)
    //    {
    //        var data = reader.ReadBytes(reader.ReadInt32());
    //        var root = TagIO.Read(reader);

    //        var memoryStream = new MemoryStream();
    //        TagIO.ToStream(root, memoryStream);

    //        // Set file_data.Path to SSC to enable cloud saves, and keep PlayTime so new saves retain time played.
    //        var file_data = new PlayerFileData(Path.Combine(Main.PlayerPath, $"{GetPID()}.SSC"), false)
    //        {
    //            Metadata = FileMetadata.FromCurrentSettings(FileType.Player),
    //        };
    //        // data includes playtime and it will be added into file_data
    //        Player.LoadPlayerFromStream(file_data, data, memoryStream.ToArray());
    //        file_data.MarkAsServerSide();
    //        file_data.SetAsActive();

    //        file_data.Player.Spawn(PlayerSpawnContext.SpawningIntoWorld);
    //        Player.Hooks.EnterWorld(Main.myPlayer); 
    //    }
    //}
}

public enum SSCPacketType : byte
{
    ClientJoin,
    ServerCreateNewPlayer
}
