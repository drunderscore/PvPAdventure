using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Core.SSC;

public class SSCCommand : ModCommand
{
    public override string Command => "ssc";

    public override CommandType Type => CommandType.Chat;
    public override string Description => "Re-select a character";
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        // This command is only meaningful on the client
        if (Main.netMode == NetmodeID.Server)
        {
            return;
        }

        Player player = Main.LocalPlayer;
        if (player == null)
        {
            return;
        }

        // Force ghost state
        player.dead = true;
        player.ghost = true;
        player.statLife = 0;
        player.respawnTimer = int.MaxValue;
        player.velocity = Vector2.Zero;

        // Re-open SSC UI
        var serverSystem = ModContent.GetInstance<ServerSystem>();
        serverSystem.UI?.SetState(new ServerViewer());

        // Request world data from server (authoritative character list)
        //if (Main.netMode == NetmodeID.MultiplayerClient)
        //{
        //    NetMessage.SendData(MessageID.WorldData);
        //}

        // Send a packet to sync
    }
}
