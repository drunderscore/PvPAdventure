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

        var packet = Mod.GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.SSC);
        packet.Write((byte)SSCPacketType.BindRequest);
        packet.Write(steamId);
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

        var fileData = new PlayerFileData(fileName, false)
        {
            Metadata = FileMetadata.FromCurrentSettings(FileType.Player),
            Player = character
        };
        fileData.MarkAsServerSide();

        var plr = Player.SavePlayerFile_Vanilla(fileData);
        var tplr = PlayerIO.SaveData(character);

        var packet = Mod.GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.SSC);
        packet.Write((byte)SSCPacketType.UploadPlayer);
        packet.Write(steamId);
        packet.Write(plr.Length);
        packet.Write(plr);
        TagIO.Write(tplr, packet);
        packet.Send();

#if DEBUG
        var worldId = Main.ActiveWorldFileData.UniqueId.ToString();
        var path = Path.Combine(Main.SavePath, "PvPAdventureSSC", worldId, steamId, "ServerCharacterTest.plr");
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

                    var id = reader.ReadString();
                    var worldId = Main.ActiveWorldFileData.UniqueId.ToString();
                    var root = Path.Combine(Main.SavePath, "PvPAdventureSSC", worldId, id);

                    var plrPath = Path.Combine(root, "ServerCharacterTest.plr");
                    var tplrPath = Path.Combine(root, "ServerCharacterTest.tplr");

                    if (File.Exists(plrPath) && File.Exists(tplrPath))
                    {
                        var plr = File.ReadAllBytes(plrPath);
                        var tplr = TagIO.FromFile(tplrPath);

                        var packet = Mod.GetPacket();
                        packet.Write((byte)AdventurePacketIdentifier.SSC);
                        packet.Write((byte)SSCPacketType.LoadPlayer);
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

                    var id = reader.ReadString();
                    var plr = reader.ReadBytes(reader.ReadInt32());
                    var tplr = TagIO.Read(reader);

                    var worldId = Main.ActiveWorldFileData.UniqueId.ToString();
                    var root = Path.Combine(Main.SavePath, "PvPAdventureSSC", worldId, id);

                    Directory.CreateDirectory(root);

                    File.WriteAllBytes(Path.Combine(root, "ServerCharacterTest.plr"), plr);
                    TagIO.ToFile(tplr, Path.Combine(root, "ServerCharacterTest.tplr"));

                    break;
                }

            case SSCPacketType.CreatePlayer:
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        return;
                    }

                    var character = new Player();
                    var creation = new UICharacterCreation(character);

                    character.name = "ServerCharacterTest";
                    character.difficulty = PlayerDifficultyID.SoftCore;

                    creation.SetupPlayerStatsAndInventoryBasedOnDifficulty();

                    var localPlayer = Main.LocalPlayer;
                    character.skinVariant = localPlayer.skinVariant;
                    character.skinColor = localPlayer.skinColor;
                    character.eyeColor = localPlayer.eyeColor;
                    character.hair = localPlayer.hair;
                    character.hairColor = localPlayer.hairColor;
                    character.shirtColor = localPlayer.shirtColor;
                    character.underShirtColor = localPlayer.underShirtColor;
                    character.pantsColor = localPlayer.pantsColor;
                    character.shoeColor = localPlayer.shoeColor;

                    var fileData = new PlayerFileData("Create.SSC", false)
                    {
                        Metadata = FileMetadata.FromCurrentSettings(FileType.Player),
                        Player = character
                    };
                    fileData.MarkAsServerSide();

                    var plr = Player.SavePlayerFile_Vanilla(fileData);
                    var tplr = PlayerIO.SaveData(character);

                    var packet = Mod.GetPacket();
                    packet.Write((byte)AdventurePacketIdentifier.SSC);
                    packet.Write((byte)SSCPacketType.UploadPlayer);
                    packet.Write(steamId);
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

                    var plr = reader.ReadBytes(reader.ReadInt32());
                    var tplr = TagIO.Read(reader);

                    var ms = new MemoryStream();
                    TagIO.ToStream(tplr, ms);

                    var fileData = new PlayerFileData(Path.Combine(Main.PlayerPath, "ServerCharacterTest.SSC"), false)
                    {
                        Metadata = FileMetadata.FromCurrentSettings(FileType.Player)
                    };

                    Player.LoadPlayerFromStream(fileData, plr, ms.ToArray());
                    fileData.MarkAsServerSide();
                    fileData.SetAsActive();

                    NetMessage.SendData(MessageID.PlayerInfo, number: Main.myPlayer);

                    fileData.Player.Spawn(PlayerSpawnContext.SpawningIntoWorld);
                    Player.Hooks.EnterWorld(Main.myPlayer);

                    break;
                }
        }
    }
}
