using Microsoft.Xna.Framework;
using PvPAdventure.Common.Statistics;
using PvPAdventure.Core.Config;
using PvPAdventure.Core.Net;
using Steamworks;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using static PvPAdventure.Common.SSC.Appearance;

namespace PvPAdventure.Common.SSC;

/// <summary>
/// Provides server-side character (SSC) functionality.
/// Stores player files on the server at ..tModLoader/PvPAdventureSSC/[WorldName]/[SteamID]/[PlayerName].plr
/// Stores temporary player data at ..tModLoader/Players/[SteamID].SSC
/// Also <see cref="SSCJoinSystem"/> <seealso cref="SSCSaveSystem"/>
/// </summary>
[Autoload(Side = ModSide.Both)]
public class SSC : ModSystem
{
    // Whether SSC is enabled from config
    public static bool IsEnabled
    {
        get
        {
            var config = ModContent.GetInstance<SSCConfig>();
            if (config == null)
            {
                Log.Warn("ServerConfig not loaded – SSC disabled by default");
                return false;
            }

            return config.IsSSCEnabled;
        }
    }

    // Folder to store SSC files
    private static string SSCFolder => Path.Combine(Main.SavePath, "PvPAdventureSSC");
    private static string MapID => Main.ActiveWorldFileData?.Name ?? "UnknownWorld";

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
                SavePlayer(reader);
                break;
        }
    }

    private static void ClientJoin(BinaryReader reader, int from)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        // Get data from client
        string steamIdFromClient = reader.ReadString();
        string nameFromClient = reader.ReadString();
        PlayerAppearance appearance = ReadAppearence(reader);

        byte[] data;
        TagCompound root;
        bool isNew;

        lock (ioLock)
        {
            Directory.CreateDirectory(Path.Combine(SSCFolder, MapID));

            string dir = Path.Combine(SSCFolder, MapID, steamIdFromClient);
            Directory.CreateDirectory(dir);

            string plrPath = Path.Combine(dir, nameFromClient + ".plr");
            string tplrPath = Path.Combine(dir, nameFromClient + ".tplr");

            isNew = !File.Exists(plrPath) || !File.Exists(tplrPath);

            if (isNew)
            {
                CreateNewPlayer(plrPath, nameFromClient, appearance);
            }

            data = File.ReadAllBytes(plrPath);
            root = TagIO.FromFile(tplrPath);
            ApplySscStatsToServerPlayer(from, root);
        }

        var p = ModContent.GetInstance<PvPAdventure>().GetPacket();
        p.Write((byte)AdventurePacketIdentifier.SSC);
        p.Write((byte)SSCPacketType.LoadPlayer);
        p.Write(isNew);
        p.Write(data.Length);
        p.Write(data);
        TagIO.Write(root, p);
        p.Send(toClient: from);

        Log.Chat("Server sent SSC load for " + nameFromClient + " bytes=" + data.Length);
    }

    private static void ApplySscStatsToServerPlayer(int whoAmI, TagCompound root)
    {
        if (whoAmI < 0 || whoAmI >= Main.maxPlayers)
            return;

        Player p = Main.player[whoAmI];
        if (p == null || !p.active)
            return;

        if (!root.ContainsKey("PvPAdventureSSC"))
            return;

        TagCompound ssc = root.GetCompound("PvPAdventureSSC");

        var stats = p.GetModPlayer<StatisticsPlayer>();
        stats.ApplySscOverride(ssc);

        // Push the corrected values to everyone (including the joining client)
        // so the server does not overwrite SSC-loaded client state with 0/0.
        typeof(StatisticsPlayer)
            .GetMethod("SyncStatistics", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.Invoke(stats, [-1, -1]);
    }

    private static void LoadPlayer(BinaryReader reader)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        bool isNew = reader.ReadBoolean();

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

        string steamId = SteamUser.GetSteamID().m_SteamID.ToString();
        //string steamName = SteamFriends.GetPersonaName();

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
                var fileData = new PlayerFileData(tempPlayerPath, cloudSave: false)
                {
                    Metadata = FileMetadata.FromCurrentSettings(FileType.Player),
                };

                Player.LoadPlayerFromStream(fileData, plrBytes, tplrBytes);

                fileData.Player.hbLocked = true; // force hb locked (user preference)

                fileData.MarkAsServerSide();
                fileData.SetAsActive();

                // Re-Enter the world with SSC character.
                fileData.Player.Spawn(PlayerSpawnContext.SpawningIntoWorld);
                Player.Hooks.EnterWorld(Main.myPlayer);

                TagCompound sscData = null;
                if (root.ContainsKey("PvPAdventureSSC"))
                {
                    sscData = root.GetCompound("PvPAdventureSSC");
                }

                bool positionRestored = PlayerPositionSystem.TryLoadPlayerPosition(fileData.Player, sscData);

                // Set max life and mana again just to make sure it gets applied.
                if (fileData.Player.statLife != fileData.Player.statLifeMax)
                    fileData.Player.statLife = fileData.Player.statLifeMax;
                if (fileData.Player.statMana != fileData.Player.statManaMax)
                    fileData.Player.statMana = fileData.Player.statManaMax;

                // Request map load to update position on map
                MapLoadSystem.Request(delayTicks: 30);

                // Prepare position text
                Vector2 appliedPos = fileData.Player.position;
                string positionText;
                if (positionRestored && sscData != null && sscData.ContainsKey("posX") && sscData.ContainsKey("posY"))
                {
                    float savedX = sscData.GetFloat("posX");
                    float savedY = sscData.GetFloat("posY");

                    Vector2 savedTile = new(savedX / 16f, savedY / 16f);
                    Vector2 appliedTile = new(appliedPos.X / 16f, appliedPos.Y / 16f);

                    bool clamped =
                        Math.Abs(appliedTile.X - savedTile.X) > 0.01f ||
                        Math.Abs(appliedTile.Y - savedTile.Y) > 0.01f;

                    positionText = $" - Position: {appliedTile.X:0}, {appliedTile.Y:0} (saved from previous session)";
                }
                else
                {
                    Vector2 appliedTile = new(appliedPos.X / 16f, appliedPos.Y / 16f);
                    //positionText = $"{appliedTile.X:0}, {appliedTile.Y:0} (world spawn)";
                    positionText = "";
                }

                // Print welcome message
                Main.NewText(
                    $"Welcome, {Main.LocalPlayer.name}! — " +
                    //$"Welcome, {steamName}! — " +
                    $"Playtime: {FormatPlayTime(Main.ActivePlayerFileData.GetPlayTime())}" +
                    $"{positionText}",
                    Color.MediumPurple
                );
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

    private static void SavePlayer(BinaryReader reader)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        try
        {
            // Read data from client (trusted for storage again)
            string steamIdFromClient = reader.ReadString();
            string nameFromClient = reader.ReadString();

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
                Log.Chat("Server failed reading SSC tplr data for " + nameFromClient);
                ModContent.GetInstance<PvPAdventure>().Logger.Error(e);
                return;
            }

            lock (ioLock)
            {
                Utils.TryCreatingDirectory(Path.Combine(SSCFolder, MapID, steamIdFromClient));

                string plrPath = Path.Combine(SSCFolder, MapID, steamIdFromClient, nameFromClient + ".plr");
                string tplrPath = Path.Combine(SSCFolder, MapID, steamIdFromClient, nameFromClient + ".tplr");

                File.WriteAllBytes(plrPath, data);
                TagIO.ToFile(root, tplrPath);
            }

            Log.Chat("Server received SSC save for " + nameFromClient + " bytes=" + len);

            var config = ModContent.GetInstance<ClientConfig>();
            if (config.ShowSavePlayerMessages)
            {
                string time = DateTime.Now.ToString("HH:mm:ss");
                Main.NewText($"{nameFromClient} saved at {time}", Color.MediumPurple);
            }
        }
        catch (Exception e)
        {
            Log.Chat("Server failed saving SSC player with error: " + e);
            ModContent.GetInstance<PvPAdventure>().Logger.Error(e);
        }
    }

    #region Helpers
    public static string FormatPlayTime(TimeSpan t)
    {
        int hours = (int)t.TotalHours;
        return $"{hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
    }
    #endregion
}