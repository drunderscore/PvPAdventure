//using Steamworks;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using Terraria;
//using Terraria.ID;
//using Terraria.IO;
//using Terraria.ModLoader;
//using Terraria.ModLoader.IO;

//namespace PvPAdventure.Core.SSC_v2;

///// <summary>
///// Stores and manages server-side character data.
///// </summary>
///// <remarks>
///// Creates a folder in the server's machine's directory called "PvPAdventureSSC" to store character data files.
///// </remarks>
//[Autoload(Side = ModSide.Both)]
//public class SSC_v2 : ModSystem
//{
//    // PvPAdventureSSC/{worldName}/{steamID}/{player}.plr
//    // PvPAdventureSSC/{worldName}/{steamID}/{player}.tplr
//    public static string PATH => Path.Combine(Main.SavePath, "PvPAdventureSSC");
//    public static string MapID => Main.ActiveWorldFileData?.Name.ToString() ?? "NoWorldLoaded";

//    // DO NOT use this on dedicated server.
//    public static string SteamID => TryGetLocalSteamId(out var id) ? id : Main.clientUUID;
//    private static readonly object _ioLock = new();

//    // Track the steamID we were told by each client (key: steamId, value: playerName)
//    private static readonly Dictionary<int, string> _steamIdByWho = [];

//    private static bool _joinSentThisSession;

//    public override void OnWorldUnload()
//    {
//        _joinSentThisSession = false;
//    }

//    public static void SendJoinRequestOnce()
//    {
//        if (Main.netMode != NetmodeID.MultiplayerClient)
//            return;

//        if (_joinSentThisSession)
//            return;

//        _joinSentThisSession = true;
//        SendJoinRequest();
//    }

//    public override void OnWorldLoad()
//    {
//        // Ensure base folders exist on server when a world is loaded.
//        if (Main.netMode == NetmodeID.Server)
//        {
//            EnsureWorldDirectory();
//        }
//    }

//    private static void EnsureWorldDirectory()
//    {
//        lock (_ioLock)
//        {
//            Directory.CreateDirectory(Path.Combine(PATH, MapID));
//            Log.Chat("Created directory at " + PATH + ", " + MapID);
//        }
//    }

//    private static (string plrPath, string tplrPath, string dir) GetServerPaths(string steamId, string playerName)
//    {
//        var safeSteam = string.IsNullOrWhiteSpace(steamId) ? "UnknownSteam" : steamId.Trim();

//        var dir = Path.Combine(PATH, MapID, safeSteam);
//        var plrPath = Path.Combine(dir, $"{playerName}.plr");
//        var tplrPath = Path.Combine(dir, $"{playerName}.tplr");
//        return (plrPath, tplrPath, dir);
//    }

//    public static void SendJoinRequest()
//    {
//        if (Main.netMode != NetmodeID.MultiplayerClient)
//            return;

//        var steamId = SteamID;
//        var name = Main.LocalPlayer.name;

//        Log.Debug("Joining with " + SteamID + ", " + name + ", sending packet join player");

//        var p = ModContent.GetInstance<PvPAdventure>().GetPacket();
//        p.Write((byte)AdventurePacketIdentifier.SSC);
//        p.Write((byte)SSCPacketType.JoinPlayer);
//        p.Write(steamId);
//        p.Write(name);
//        p.Send();
//    }

//    public static void SendSaveToServer(bool first)
//    {
//        if (Main.netMode != NetmodeID.MultiplayerClient)
//            return;

//        var steamId = SteamID;
//        var name = Main.LocalPlayer.name;

//        // Build save payload.
//        var fileData = Main.ActivePlayerFileData;
//        if (fileData?.Player == null)
//            return;

//        byte[] plrBytes = Player.SavePlayerFile_Vanilla(fileData);

//        // Serialize .tplr to compressed bytes (same general format as on disk).
//        TagCompound tplrRoot = PlayerIO.SaveData(fileData.Player);
//        byte[] tplrBytes;
//        using (var ms = new MemoryStream())
//        {
//            TagIO.ToStream(tplrRoot, ms, compress: true);
//            tplrBytes = ms.ToArray();
//        }

//        // Send.
//        var p = ModContent.GetInstance<PvPAdventure>().GetPacket();
//        p.Write((byte)AdventurePacketIdentifier.SSC);
//        p.Write((byte)SSCPacketType.SavePlayer);
//        p.Write(steamId);
//        p.Write(name);

//        p.Write(plrBytes.Length);
//        p.Write(plrBytes);

//        p.Write(tplrBytes.Length);
//        p.Write(tplrBytes);

//        p.Write(first);
//        p.Send();
//    }

//    public static void HandlePacket(BinaryReader reader, int from)
//    {
//        var msg = (SSCPacketType)reader.ReadByte();
//        string netmode = Main.netMode == NetmodeID.Server ? "Server" : "Client";

//        switch (msg)
//        {
//            case SSCPacketType.JoinPlayer:
//                Log.Chat($"JoinPlayer from {from}", netmode);
//                JoinPlayer(reader, from);
//                break;

//            case SSCPacketType.ResetPlayer:
//                Log.Chat($"ResetPlayer from {from}", netmode);
//                ResetPlayer(reader, from);
//                break;

//            case SSCPacketType.LoadPlayer:
//                Log.Chat($"LoadPlayer from {from}", netmode);
//                LoadPlayer(reader, from);
//                break;

//            case SSCPacketType.SavePlayer:
//                Log.Chat($"SavePlayer from {from}", netmode);
//                SavePlayer(reader, from);
//                break;
//        }
//    }

//    private static void JoinPlayer(BinaryReader reader, int from)
//    {
//        if (Main.netMode != NetmodeID.Server)
//            return;

//        var steamId = reader.ReadString();
//        var name = reader.ReadString();

//        _steamIdByWho[from] = steamId;

//        Log.Debug("Server recied joined player" + name);

//        EnsureWorldDirectory();

//        var (plrPath, tplrPath, dir) = GetServerPaths(steamId, name);

//        Directory.CreateDirectory(dir);

//        if (File.Exists(plrPath) && File.Exists(tplrPath))
//        {
//            Log.Chat("Server found plr and tplr file " + Path.GetFileNameWithoutExtension(plrPath));

//            // Send LoadPlayer response
//            var plrBytes = File.ReadAllBytes(plrPath);
//            var tplrBytes = File.ReadAllBytes(tplrPath);

//            var p = ModContent.GetInstance<PvPAdventure>().GetPacket();
//            p.Write((byte)AdventurePacketIdentifier.SSC);
//            p.Write((byte)SSCPacketType.LoadPlayer);
//            p.Write(name);

//            p.Write(plrBytes.Length);
//            p.Write(plrBytes);

//            p.Write(tplrBytes.Length);
//            p.Write(tplrBytes);

//            p.Send(toClient: from);
//        }
//        else
//        {
//            Log.Chat("Server found no player and is sending reset player packet");

//            // First time: tell client to keep current and upload
//            var p = ModContent.GetInstance<PvPAdventure>().GetPacket();
//            p.Write((byte)AdventurePacketIdentifier.SSC);
//            p.Write((byte)SSCPacketType.ResetPlayer);
//            p.Write(name);
//            p.Send(toClient: from);
//        }
//    }

//    private static void ResetPlayer(BinaryReader reader, int from)
//    {
//        if (Main.netMode != NetmodeID.MultiplayerClient)
//            return;

//        var name = reader.ReadString();

//        // Immediately upload what the client currently has as the initial SSC snapshot.
//        SendSaveToServer(first: true);
//    }

//    private static void LoadPlayer(BinaryReader reader, int from)
//    {
//        if (Main.netMode != NetmodeID.MultiplayerClient)
//            return;

//        var plrLen = reader.ReadInt32();
//        var plrBytes = reader.ReadBytes(plrLen);

//        var tplrLen = reader.ReadInt32();
//        var tplrBytes = reader.ReadBytes(tplrLen);

//        // Apply to local client.
//        var fileData = new PlayerFileData(Path.Combine(Main.PlayerPath, "PvPAdventureSSC.temp.plr"), cloudSave: false)
//        {
//            Metadata = FileMetadata.FromCurrentSettings(FileType.Player),
//        };

//        Player.LoadPlayerFromStream(fileData, plrBytes, tplrBytes);

//        // Ensure correct slot.
//        fileData.Player.whoAmI = Main.myPlayer;
//        Main.player[Main.myPlayer] = fileData.Player;

//        fileData.SetAsActive();

//        // Re-spawn to ensure health/mana/inventory visuals refresh.
//        fileData.Player.Spawn(PlayerSpawnContext.SpawningIntoWorld);
//    }

//    private static void SavePlayer(BinaryReader reader, int from)
//    {
//        if (Main.netMode != NetmodeID.Server)
//            return;

//        var steamId = reader.ReadString();
//        var name = reader.ReadString();
//        var plrLen = reader.ReadInt32();
//        var plrBytes = reader.ReadBytes(plrLen);
//        var tplrLen = reader.ReadInt32();
//        var tplrBytes = reader.ReadBytes(tplrLen);

//        EnsureWorldDirectory();

//        var (plrPath, tplrPath, dir) = GetServerPaths(steamId, name);

//        Directory.CreateDirectory(dir);

//        File.WriteAllBytes(plrPath, plrBytes);
//        File.WriteAllBytes(tplrPath, tplrBytes);
//    }

    

//    private static bool TryGetLocalSteamId(out string steamId)
//    {
//        steamId = "";
//        try
//        {
//            // Steamworks is only valid on clients with Steam initialized.
//            steamId = SteamUser.GetSteamID().m_SteamID.ToString();
//            return !string.IsNullOrWhiteSpace(steamId);
//        }
//        catch
//        {
//            return false;
//        }
//    }
//}

//public enum SSCPacketType : byte
//{
//    JoinPlayer,  // Client -> server: steamId, name. Server -> client: LoadPlayer or ResetPlayer.
//    ResetPlayer, // Server -> client: name. Client -> server: SavePlayer(first=true)
//    LoadPlayer,  // Server -> client: name, plrBytes, tplrBytes
//    SavePlayer   // Client -> server: steamId, name, plrBytes, tplrBytes, first
//}