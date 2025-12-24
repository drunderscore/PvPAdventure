using Steamworks;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent.UI.States;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Core.SSC;

/// <summary>
/// Server-Side Characters (SSC).
/// Saves per-player characters on the host/server at:
/// <c>.../tModLoader/PvPAdventureSSC/{worldId}/{steamId}/{PlayerName.plr}</c>
/// On world join the client requests a bind; the server replies with Load (existing) or Create (new).
/// </summary>
/// <remarks>
/// Inspired by zzp198's SSC mod:
/// https://github.com/zzp198/SSC
/// </remarks>

[Autoload(Side = ModSide.Both)]
internal class SSCSystem : ModSystem
{
    internal enum SSCPacketType : byte
    {
        BindRequest,
        UploadPlayer,
        CreatePlayer,
        LoadPlayer
    }

    private bool requested;
    private string steamId;
    private uint saveTimer;
    private string boundStem;

    public override void OnWorldLoad()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient || requested)
        {
            return;
        }

        requested = true;
        saveTimer = 0;

        if (string.IsNullOrEmpty(steamId))
        {
            try
            {
                steamId = SteamUser.GetSteamID().m_SteamID.ToString();
            }
            catch
            {
                steamId = Main.clientUUID;
            }
        }

        string joinName = string.Empty;
        if (Main.LocalPlayer != null)
        {
            joinName = Main.LocalPlayer.name;
        }

        var packet = Mod.GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.SSC);
        packet.Write((byte)SSCPacketType.BindRequest);
        packet.Write(steamId);
        packet.Write(joinName);
        packet.Send();
    }

    public override void OnWorldUnload()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient &&
            Main.ActivePlayerFileData != null &&
            Main.ActivePlayerFileData.ServerSideCharacter)
        {
            SendSave("ExitSave.SSC");
        }

        requested = false;
        saveTimer = 0;
        boundStem = null;
    }

    public override void PostUpdateEverything()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient ||
            Main.ActivePlayerFileData == null ||
            !Main.ActivePlayerFileData.ServerSideCharacter)
        {
            return;
        }

        saveTimer++;

        if (saveTimer < 60u * 10u)
        {
            return;
        }

        saveTimer = 0;
        SendSave("AutoSave.SSC");
    }

    private void SendSave(string fileName)
    {
        var character = Main.LocalPlayer;
        if (character == null)
        {
            return;
        }

        var fileData = new PlayerFileData(fileName, false)
        {
            Metadata = FileMetadata.FromCurrentSettings(FileType.Player),
            Player = character
        };
        fileData.MarkAsServerSide();

        byte[] plr = Player.SavePlayerFile_Vanilla(fileData);
        TagCompound tplr = PlayerIO.SaveData(character);

        var packet = Mod.GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.SSC);
        packet.Write((byte)SSCPacketType.UploadPlayer);
        packet.Write(steamId);
        packet.Write(character.name);
        packet.Write(plr.Length);
        packet.Write(plr);
        TagIO.Write(tplr, packet);
        packet.Send();

#if DEBUG
        string worldId = Main.ActiveWorldFileData.UniqueId.ToString();
        string root = Path.Combine(Main.SavePath, "PvPAdventureSSC", worldId, steamId);

        string stem = boundStem;
        if (string.IsNullOrEmpty(stem))
        {
            stem = SanitizeFileName(character.name);
        }

        string path = Path.Combine(root, stem + ".plr");
        Main.NewText($"Saved {path} at {DateTime.Now:HH:mm:ss}");
#endif
    }

    public void HandlePacket(BinaryReader reader, int whoAmI)
    {
        var type = (SSCPacketType)reader.ReadByte();

        switch (type)
        {
            case SSCPacketType.BindRequest:
                {
                    if (Main.netMode != NetmodeID.Server)
                    {
                        return;
                    }

                    string id = reader.ReadString();
                    string joinName = reader.ReadString();

                    string worldId = Main.ActiveWorldFileData.UniqueId.ToString();
                    string root = Path.Combine(Main.SavePath, "PvPAdventureSSC", worldId, id);

                    string stem = GetBoundCharacterStem(root, joinName);

                    string plrPath = Path.Combine(root, stem + ".plr");
                    string tplrPath = Path.Combine(root, stem + ".tplr");

                    if (File.Exists(plrPath) && File.Exists(tplrPath))
                    {
                        byte[] plr = File.ReadAllBytes(plrPath);
                        TagCompound tplr = TagIO.FromFile(tplrPath);

                        var packet = Mod.GetPacket();
                        packet.Write((byte)AdventurePacketIdentifier.SSC);
                        packet.Write((byte)SSCPacketType.LoadPlayer);
                        packet.Write(stem);
                        packet.Write(plr.Length);
                        packet.Write(plr);
                        TagIO.Write(tplr, packet);
                        packet.Send(toClient: whoAmI);
                    }
                    else
                    {
                        var packet = Mod.GetPacket();
                        packet.Write((byte)AdventurePacketIdentifier.SSC);
                        packet.Write((byte)SSCPacketType.CreatePlayer);
                        packet.Write(stem);
                        packet.Write(joinName);
                        packet.Send(toClient: whoAmI);
                    }

                    break;
                }

            case SSCPacketType.UploadPlayer:
                {
                    if (Main.netMode != NetmodeID.Server)
                    {
                        return;
                    }

                    string id = reader.ReadString();
                    string name = reader.ReadString();
                    byte[] plr = reader.ReadBytes(reader.ReadInt32());
                    TagCompound tplr = TagIO.Read(reader);

                    string worldId = Main.ActiveWorldFileData.UniqueId.ToString();
                    string root = Path.Combine(Main.SavePath, "PvPAdventureSSC", worldId, id);

                    Directory.CreateDirectory(root);

                    string stem = GetBoundCharacterStem(root, name);

                    File.WriteAllBytes(Path.Combine(root, stem + ".plr"), plr);
                    TagIO.ToFile(tplr, Path.Combine(root, stem + ".tplr"));

                    break;
                }

            case SSCPacketType.CreatePlayer:
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        return;
                    }

                    string stem = reader.ReadString();
                    string displayName = reader.ReadString();

                    var character = new Player();
                    var creation = new UICharacterCreation(character);

                    character.name = displayName;
                    character.difficulty = PlayerDifficultyID.SoftCore;

                    creation.SetupPlayerStatsAndInventoryBasedOnDifficulty();

                    var localPlayer = Main.LocalPlayer;
                    if (localPlayer != null)
                    {
                        character.skinVariant = localPlayer.skinVariant;
                        character.skinColor = localPlayer.skinColor;
                        character.eyeColor = localPlayer.eyeColor;
                        character.hair = localPlayer.hair;
                        character.hairColor = localPlayer.hairColor;
                        character.shirtColor = localPlayer.shirtColor;
                        character.underShirtColor = localPlayer.underShirtColor;
                        character.pantsColor = localPlayer.pantsColor;
                        character.shoeColor = localPlayer.shoeColor;
                    }

                    var fileData = new PlayerFileData("Create.SSC", false)
                    {
                        Metadata = FileMetadata.FromCurrentSettings(FileType.Player),
                        Player = character
                    };
                    fileData.MarkAsServerSide();

                    byte[] plr = Player.SavePlayerFile_Vanilla(fileData);
                    TagCompound tplr = PlayerIO.SaveData(character);

                    ApplyLoadedPlayer(stem, plr, tplr);

                    var packet = Mod.GetPacket();
                    packet.Write((byte)AdventurePacketIdentifier.SSC);
                    packet.Write((byte)SSCPacketType.UploadPlayer);
                    packet.Write(steamId);
                    packet.Write(character.name);
                    packet.Write(plr.Length);
                    packet.Write(plr);
                    TagIO.Write(tplr, packet);
                    packet.Send();

                    break;
                }

            case SSCPacketType.LoadPlayer:
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        return;
                    }

                    string stem = reader.ReadString();
                    byte[] plr = reader.ReadBytes(reader.ReadInt32());
                    TagCompound tplr = TagIO.Read(reader);

                    ApplyLoadedPlayer(stem, plr, tplr);

                    break;
                }
        }
    }

    private void ApplyLoadedPlayer(string stem, byte[] plr, TagCompound tplr)
    {
        boundStem = stem;

        var ms = new MemoryStream();
        TagIO.ToStream(tplr, ms);

        var fileData = new PlayerFileData(Path.Combine(Main.PlayerPath, stem + ".SSC"), false)
        {
            Metadata = FileMetadata.FromCurrentSettings(FileType.Player)
        };

        Player.LoadPlayerFromStream(fileData, plr, ms.ToArray());
        fileData.MarkAsServerSide();
        fileData.SetAsActive();

        NetMessage.SendData(MessageID.PlayerInfo, number: Main.myPlayer);

        fileData.Player.Spawn(PlayerSpawnContext.SpawningIntoWorld);
        Player.Hooks.EnterWorld(Main.myPlayer);
    }

    private static string GetBoundCharacterStem(string root, string fallbackName)
    {
        Directory.CreateDirectory(root);

        string bindPath = Path.Combine(root, "bound.txt");

        if (File.Exists(bindPath))
        {
            string existing = File.ReadAllText(bindPath).Trim();
            if (!string.IsNullOrWhiteSpace(existing))
            {
                return existing;
            }
        }

        string stem = GetOldestCharacterStem(root);
        if (string.IsNullOrEmpty(stem))
        {
            stem = SanitizeFileName(fallbackName);
        }

        File.WriteAllText(bindPath, stem);
        return stem;
    }

    private static string GetOldestCharacterStem(string root)
    {
        if (!Directory.Exists(root))
        {
            return null;
        }

        string bestStem = null;
        DateTime bestTime = DateTime.MaxValue;

        foreach (string plrPath in Directory.EnumerateFiles(root, "*.plr"))
        {
            string stem = Path.GetFileNameWithoutExtension(plrPath);
            if (string.IsNullOrEmpty(stem))
            {
                continue;
            }

            string tplrPath = Path.Combine(root, stem + ".tplr");
            if (!File.Exists(tplrPath))
            {
                continue;
            }

            DateTime time = File.GetLastWriteTimeUtc(plrPath);
            if (time < bestTime)
            {
                bestTime = time;
                bestStem = stem;
            }
        }

        return bestStem;
    }

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Player";
        }

        string sanitized = name.Trim();

        foreach (char c in Path.GetInvalidFileNameChars())
        {
            sanitized = sanitized.Replace(c, '_');
        }

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return "Player";
        }

        return sanitized;
    }
}
