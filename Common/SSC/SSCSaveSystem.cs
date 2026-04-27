using Microsoft.Xna.Framework;
using PvPAdventure.Common.Statistics;
using PvPAdventure.Core.Config;
using PvPAdventure.Core.Net;
using Steamworks;
using System;
using System.Linq;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.IO;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Engine;
using Terraria.ModLoader.IO;
using static PvPAdventure.Common.SSC.SSC;

namespace PvPAdventure.Common.SSC;

/// <summary>
/// Ensures that data is handled by the server rather than saved locally.
/// Intercepts player file save events to redirect saving to the server.
/// </summary>
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
        Log.Debug("Vanilla save player file was called");

        if (Main.LocalPlayer.ghost)
            return;

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
        if (Main.LocalPlayer.ghost)
            return;

        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        try
        {
            //var steamID = SteamUser.GetSteamID().m_SteamID.ToString(); // keep this legacy code
            var fileData = Main.ActivePlayerFileData;
            var name = fileData.Player.name;
            //var name = SteamFriends.GetPersonaName();

            // Save plr and tplr files
            var plr = Player.SavePlayerFile_Vanilla(fileData);
            var tplr = PlayerIO.SaveData(fileData.Player);

            // Save player stats
            var stats = fileData.Player.GetModPlayer<StatisticsPlayer>();

            // Save kills, deaths, item pickups, team and player position
            PvPAdventureSSCData.SavePvPAdventureStats(fileData.Player, tplr);

            // Save client backup plr and tplr files
            ClientBackup.WritePlayerBackup(name, plr, tplr);

            var packet = Mod.GetPacket();
            packet.Write((byte)AdventurePacketIdentifier.SSC);
            packet.Write((byte)SSCPacketType.SavePlayer);
            //packet.Write(steamID); // keep this legacy code
            packet.Write(name);
            packet.Write(plr.Length);
            packet.Write(plr);
            TagIO.Write(tplr, packet);
            packet.Send();

            Log.Debug($"Client sent packet to server to save: {fileData.Player.name}, k/d: {stats.Kills}/{stats.Deaths}, itemPickups: {stats.ItemPickups.ToArray()}, team: {(Terraria.Enums.Team)fileData.Player.team}");

            var config = ModContent.GetInstance<ClientConfig>();

            if (config.ShowSavePlayerMessages)
            {
                string time = DateTime.Now.ToString("HH:mm:ss");
                string playtime = PlayerPositionSystem.FormatPlayTime(Main.ActivePlayerFileData.GetPlayTime());

                Main.NewText($"Saved {Main.LocalPlayer.name} at {time} — Playtime: {playtime}", Color.MediumPurple);
            }
        }
        catch (Exception e)
        {
            Mod.Logger.Error(e);
            Log.Chat(e);
        }

    }
}