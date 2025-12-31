using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Steamworks;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.IO;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Core.SSC;


/// <summary>
/// Managages player save files by SteamID and world
/// and handles network packet routing for SSC operations.
/// </summary>
[Autoload(Side = ModSide.Both)]
public class SSC : ModSystem
{
    // PvPAdventureSSC/([MapID:world-name])/[SteamID:0-9]/zzp198.plr
    public static string PATH => Path.Combine(Main.SavePath, "PvPAdventureSSC");

    public static string MapID
    {
        get
        {
            var name = Main.ActiveWorldFileData?.Name ?? "";
            if (name == "")
            {
                return Main.ActiveWorldFileData.UniqueId.ToString();
            }

            var invalid = Path.GetInvalidFileNameChars();
            return new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        }
    }

    public static string CachedPID = "";

    public static string GetPID()
    {
        if (CachedPID == "")
        {
            try
            {
                CachedPID = SteamUser.GetSteamID().m_SteamID.ToString();
            }
            catch (Exception e)
            {
                ChatHelper.DisplayMessageOnClient(NetworkText.FromLiteral(e.ToString()), Color.Red, Main.myPlayer);
                ChatHelper.DisplayMessageOnClient(NetworkText.FromKey("Mods.SSC.SteamIDWrong"), Color.Red,
                    Main.myPlayer);
                CachedPID = Main.clientUUID;
            }
        }

        return CachedPID;
    }

    public void HandlePacket(BinaryReader reader, int from)
    {
        var msg = reader.ReadByte();
        switch ((SSCMessageID)msg)
        {
            case SSCMessageID.MessageSegment:
            {
                MessageManager.ProcessMessage(reader, from);
                break;
            }
            case SSCMessageID.SaveSSC:
            {
                // Only the server executes this branch; 'from' is always the client ID
                var id = reader.ReadString();
                var name = reader.ReadString();
                var data = reader.ReadBytes(reader.ReadInt32());
                var root = TagIO.Read(reader);
                var first = reader.ReadBoolean();

                // DisplayMessageOnClient always shows the server as sender (server -> client).
                // SendChatMessageToClient allows a custom sender (client -> server -> client) and hides the sender tag.
                if (string.IsNullOrWhiteSpace(name))
                {
                    ChatHelper.DisplayMessageOnClient(NetworkText.FromKey("Mods.PvPAdventure.SSC.EmptyName"), Color.Red, from);
                    return;
                }

                if (name.Length > 16) // SteamID is 17 digits; this avoids matching the initialized ghost character name
                {
                    ChatHelper.DisplayMessageOnClient(NetworkText.FromKey("Mods.PvPAdventure.SSC.NameTooLong"), Color.Red, from);
                    return;
                }

                if (name.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
                {
                    ChatHelper.DisplayMessageOnClient(NetworkText.FromKey("Mods.PvPAdventure.SSC.NameError"), Color.Red, from);
                    return;
                }

                // Enforce max 3 characters per player per world
                var playerDir = Path.Combine(PATH, MapID, id);
                if (first && Directory.Exists(playerDir) && Directory.GetFiles(playerDir, "*.plr").Length >= 3)
                {
                    ChatHelper.DisplayMessageOnClient(NetworkText.FromLiteral("You can only have 3 SSC characters."), Color.Red, from);
                    return;
                }

                switch (first)
                {
                    // We don't need this rn, so commenting it out.

                    // Search all worlds' roots to avoid duplicate names when creating a new character
                    // case true when Directory.GetFiles(PATH, $"{name}.plr", SearchOption.AllDirectories).Any():
                    // {
                        // ChatHelper.DisplayMessageOnClient(NetworkText.FromKey(Lang.mp[5].Key, name), Color.Red, from);
                        // return;
                    // }

                    // Source file must exist on save to avoid hardcore rollback scenarios
                    case false when !File.Exists(Path.Combine(PATH, MapID, id, $"{name}.plr")):
                    {
                        ChatHelper.DisplayMessageOnClient(NetworkText.FromLiteral("Save failed, archive not found!"), Color.Red, from);
                        return;
                    }
                }

                Utils.TryCreatingDirectory(Path.Combine(PATH, MapID, id));

                File.WriteAllBytes(Path.Combine(PATH, MapID, id, $"{name}.plr"), data);
                TagIO.ToFile(root, Path.Combine(PATH, MapID, id, $"{name}.tplr"));

                var stream = new MemoryStream();
                TagIO.ToStream(root, stream);
                stream.Flush();

#if DEBUG
                // Notify the client when the save succeeds
                var KB = (data.LongLength + stream.Length) / 1024.0;
                var size = $"[c/{(KB < 64 ? Color.Green.Hex3() : Color.Yellow.Hex3())}:{KB:N2} KB]";
                var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                ChatHelper.DisplayMessageOnClient(NetworkText.FromKey("Mods.PvPAdventure.SSC.SaveSuccessful", name, size, time),
                    Color.Green, from);
#endif

                if (first)
                {
                    NetMessage.TrySendData(MessageID.WorldData, from);
                }

                break;
            }
            case SSCMessageID.GoGoSSC:
            {
                // Bidirectional flow: client -> server -> client
                if (Main.netMode == NetmodeID.Server)
                {
                    var id = reader.ReadString();
                    var name = reader.ReadString();

                    // Validate again during login
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        ChatHelper.DisplayMessageOnClient(NetworkText.FromKey("Net.EmptyName"), Color.Red, from);
                        return;
                    }

                    if (name.Length > 16) // SteamID is 17 digits; prevents matching the initialized ghost character name
                    {
                        ChatHelper.DisplayMessageOnClient(NetworkText.FromKey("Net.NameTooLong"), Color.Red, from);
                        return;
                    }

                    if (name.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
                    {
                        ChatHelper.DisplayMessageOnClient(NetworkText.FromKey("Mods.SSC.NameError"), Color.Red, from);
                        return;
                    }
                    
                    if (Netplay.Clients.Where(c => c.IsActive).Any(c => Main.player[c.Id].name == name)) // Prevent duplicate names online
                    {
                        ChatHelper.DisplayMessageOnClient(NetworkText.FromKey(Lang.mp[5].Key, name), Color.Red, from);
                        return;
                    }

                    var file_data = Player.LoadPlayer(Path.Combine(PATH, MapID, id, $"{name}.plr"), false);

                    if (!SystemLoader.CanWorldBePlayed(file_data, Main.ActiveWorldFileData, out var mod))
                    {
                        var message = mod.WorldCanBePlayedRejectionMessage(file_data, Main.ActiveWorldFileData);
                        ChatHelper.DisplayMessageOnClient(NetworkText.FromLiteral(message), Color.Red, from);
                        return;
                    }

                    // Send all data only to the logging-in client. Broadcasting to all clients causes display issues and brief desyncs; other clients don't need these details.
                    var data = File.ReadAllBytes(Path.Combine(PATH, MapID, id, $"{name}.plr"));
                    var root = TagIO.FromFile(Path.Combine(PATH, MapID, id, $"{name}.tplr"));

                    var mp = ModContent.GetInstance<PvPAdventure>().GetPacket();
                    mp.Write((byte)AdventurePacketIdentifier.SSC);
                    mp.Write((byte)SSCMessageID.GoGoSSC);
                    mp.Write(data.Length);
                    mp.Write(data);
                    TagIO.Write(root, mp);
                    MessageManager.FrameSend(mp, from);

                    // Ensure all message fragments are mounted before the PlayerInfo packet arrives.
                    // Sync server data from the client's mounted data; without this, leave messages are wrong and late joiners may not see earlier players (death can clear it).
                    NetMessage.SendData(Terraria.ID.MessageID.PlayerInfo, from);

                    return;
                }

                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    var data = reader.ReadBytes(reader.ReadInt32());
                    var root = TagIO.Read(reader);

                    var memoryStream = new MemoryStream();
                    TagIO.ToStream(root, memoryStream);

                    // Set file_data.Path to SSC to enable cloud saves, and keep PlayTime so new saves retain time played.
                    var file_data = new PlayerFileData(Path.Combine(Main.PlayerPath, $"{GetPID()}.SSC"), false)
                    {
                        Metadata = FileMetadata.FromCurrentSettings(FileType.Player),
                    };
                    // data includes playtime and it will be added into file_data
                    Player.LoadPlayerFromStream(file_data, data, memoryStream.ToArray());
                    file_data.MarkAsServerSide();
                    file_data.SetAsActive();

                    file_data.Player.Spawn(PlayerSpawnContext.SpawningIntoWorld);
                    try
                    {
                        Player.Hooks.EnterWorld(Main.myPlayer); // Other mods may throw if they lack defensive coding
                    }
                    catch (Exception e)
                    {
                        ChatHelper.DisplayMessageOnClient(NetworkText.FromLiteral(e.ToString()), Color.Red,
                            Main.myPlayer);
                    }
                    finally
                    {
                        ModContent.GetInstance<ServerSystem>().UI?.SetState(null);
                    }
                }

                break;
            }
            case SSCMessageID.EraseSSC:
            {
                // Only the server executes this branch; 'from' is always the client ID
                var id = reader.ReadString();
                var name = reader.ReadString();

                if (File.Exists(Path.Combine(PATH, MapID, id, $"{name}.plr")))
                {
                    File.Delete(Path.Combine(PATH, MapID, id, $"{name}.plr"));
                    File.Delete(Path.Combine(PATH, MapID, id, $"{name}.tplr"));
                    ChatHelper.SendChatMessageToClient(NetworkText.FromKey("Mods.PvPAdventure.SSC.EraseSuccessful", name), Color.Green,
                        from);
                }

                NetMessage.TrySendData(Terraria.ID.MessageID.WorldData, from);
                break;
            }
            default:
            {
                switch (Main.netMode)
                {
                    case NetmodeID.MultiplayerClient:
                        Netplay.Disconnect = true;
                        Main.statusText = $"Unexpected message id: {msg}";
                        Main.menuMode = 14;
                        break;
                    case NetmodeID.Server:
                        NetMessage.BootPlayer(from, NetworkText.FromLiteral($"Unexpected message id: {msg}"));
                        break;
                }

                break;
            }
        }
    }
}

public enum SSCMessageID : byte
{
    MessageSegment, // Split packets larger than 65536 bytes into segments

    SaveSSC, // Client -> server: id, name, data, root, first

    GoGoSSC, // Client -> server: id, name. Server -> client: data, root.

    EraseSSC, // Client -> server: id, name
}
