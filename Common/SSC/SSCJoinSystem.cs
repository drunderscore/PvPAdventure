using Microsoft.Xna.Framework;
using PvPAdventure.Common.Bounties;
using PvPAdventure.Core.Config;
using PvPAdventure.Core.Debug;
using PvPAdventure.Core.Net;
using Steamworks;
using System;
using System.Linq;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using static PvPAdventure.Common.SSC.SSC;

namespace PvPAdventure.Common.SSC;

/// <summary>
/// Joins the world as a ghost, 
/// and after a small delay sends a request to join as a proper SSC character.
/// Hopefully reworked in the future for smoother player experience.
/// </summary>
[Autoload(Side = ModSide.Client)]
public class SSCJoinSystem : ModSystem
{
    private bool _sent;
    private int _delayTicks;
    public override void OnWorldLoad()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        if (!SSC.IsEnabled)
            return;

        _sent = false;
        _delayTicks = 60; // 1 second

        // Enter as a ghost initially
        Main.LocalPlayer.ghost = true;
    }

    public override void PostUpdateEverything()
    {
        // Force ghost to be stuck inside spawnbox
        if (Main.LocalPlayer.ghost)
        {
            var player = Main.LocalPlayer;

            player.position = new Vector2(Main.spawnTileX * 16f, Main.spawnTileY * 16f-48);
            player.velocity = Vector2.Zero;
            player.direction = 1;

            //int playerTileX = (int)(player.position.X / 16f);
            //int playerTileY = (int)(player.position.Y / 16f);

            //int spawnTileX = Main.spawnTileX;
            //int spawnTileY = Main.spawnTileY;

            //int distanceX = Math.Abs(playerTileX - spawnTileX);
            //int distanceY = Math.Abs(playerTileY - spawnTileY);

            //if (distanceX >= 25 || distanceY >= 25)
            //{
            //    player.position = new Vector2(Main.spawnTileX * 16f, Main.spawnTileY * 16f);
            //    player.velocity = Vector2.Zero;
            //}
        }

        if (_sent)
            return;

        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        if (_delayTicks > 0)
        {
            _delayTicks--;
            return;
        }

        _sent = true;

        var config = ModContent.GetInstance<ArenasConfig>();
        if (!config.IsArenasEnabled)
        {
            SendJoinRequest();
        }
    }

    public override void OnWorldUnload()
    {
        _sent = false;
        _delayTicks = 0;
    }

    public static void SendJoinRequest()
    {
        if (!SSC.IsEnabled)
            return;

        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        Player player = Main.LocalPlayer;

        var packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.SSC);
        packet.Write((byte)SSCPacketType.ClientJoin);

        packet.Write(SteamUser.GetSteamID().m_SteamID.ToString());
        packet.Write(player.name);

        // Appearance
        Appearance.WriteAppearence(packet, player);

        packet.Send();
    }
}
