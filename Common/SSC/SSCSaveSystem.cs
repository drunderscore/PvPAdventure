using PvPAdventure.Common.Statistics;
using PvPAdventure.Core.Debug;
using PvPAdventure.Core.Net;
using Steamworks;
using System;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using static PvPAdventure.Common.SSC.SSC;

namespace PvPAdventure.Common.SSC;

/// <summary>
/// Ensures that data is handled by the server rather than saved locally.
/// Intercepts player file save events to redirect saving to the server.
[Autoload(Side = ModSide.Client)]
internal class SSCSaveSystem : ModSystem
{
    public override void Load()
    {
        if (!SSC.IsEnabled)
            return;

        On_Player.InternalSavePlayerFile += OverrideSavePlayerFile;
    }

    public override void Unload()
    {
        On_Player.InternalSavePlayerFile -= OverrideSavePlayerFile;
    }

    public override void PreSaveAndQuit()
    {
        if (!SSC.IsEnabled)
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

    public void SendPacketToSavePlayerFile()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        try
        {
            var steamID = SteamUser.GetSteamID().m_SteamID.ToString();
            var fileData = Main.ActivePlayerFileData;
            var name = fileData.Player.name;

            // Save map data! Does this work?
            /// Update: Probably not! We save map data in custom <see cref="MapIOHooks"/> now.
            //try
            //{
            //    Player.InternalSaveMap(false);
            //}
            //catch (Exception e)
            //{
            //    Mod.Logger.Warn("SSC: InternalSaveMap failed; map data may not persist. " + e);
            //}

            // Save plr and tplr files
            var plr = Player.SavePlayerFile_Vanilla(fileData);
            var tplr = PlayerIO.SaveData(fileData.Player);

            // Save player stats
            var stats = fileData.Player.GetModPlayer<StatisticsPlayer>();

            var sscTag = new TagCompound
            {
                ["kills"] = stats.Kills,
                ["deaths"] = stats.Deaths,
                ["itemPickups"] = stats.ItemPickups.ToArray(),
                ["team"] = fileData.Player.team
            };

            // Save player position for this world
            PlayerPositionSystem.SavePlayerPosition(fileData.Player, sscTag);

            // Merge sscTag into tplr
            tplr["PvPAdventureSSC"] = sscTag;

            // Save client backup plr and tplr files
            ClientBackup.WritePlayerBackup(name, plr, tplr);

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