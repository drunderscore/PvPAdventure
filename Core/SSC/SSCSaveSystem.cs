using Microsoft.Xna.Framework.Input;
using Steamworks;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.Engine;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Core.SSC;

/// <summary>
/// Ensures that data is handled by the server rather than saved locally.
/// Intercepts player file save events to redirect saving to the server.
[Autoload(Side = ModSide.Client)]
internal class SSCSaveSystem : ModSystem
{
    public override void Load()
    {
        if (!SSCEnabled.IsEnabled)
            return;

        On_Player.InternalSavePlayerFile += OverrideSavePlayerFile;
    }

    public override void Unload()
    {
        On_Player.InternalSavePlayerFile -= OverrideSavePlayerFile;
    }

    public override void PreSaveAndQuit()
    {
        if (!SSCEnabled.IsEnabled)
            return;

        // Save player file before quitting
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            SendPacketToSavePlayerFile();
        }
    }

    // Do not save SSC player files locally; send to server instead.
    private void OverrideSavePlayerFile(On_Player.orig_InternalSavePlayerFile orig, PlayerFileData fileData)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient &&
            fileData.ServerSideCharacter && fileData.Path.EndsWith("SSC"))
        {
            SendPacketToSavePlayerFile();

            return;
        }

        orig(fileData);
    }

    private void SendPacketToSavePlayerFile()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        try
        {
            var steamID = SteamUser.GetSteamID().m_SteamID.ToString();
            var fileData = Main.ActivePlayerFileData;
            var name = fileData.Player.name;
            var plr = Player.SavePlayerFile_Vanilla(fileData);
            var tplr = PlayerIO.SaveData(fileData.Player);

            var packet = Mod.GetPacket();
            packet.Write((byte)AdventurePacketIdentifier.SSC);
            packet.Write((byte)SSCPacketType.SavePlayer);
            packet.Write(steamID);
            packet.Write(name);
            packet.Write(plr.Length);
            packet.Write(plr);
            TagIO.Write(tplr, packet);
            packet.Send();

            //Log.Chat("Client sent packet to save " + fileData.Player.name);
        }
        catch (Exception e)
        {
            Mod.Logger.Error(e);
            Log.Chat(e);
        }

    }
}
