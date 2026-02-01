using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.SSC.Commands;

public class SaveCommand : ModCommand
{
    public override string Command => "save";

    public override string Description => "Saves your player file on the server.";

    public override CommandType Type => CommandType.Chat;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        var fileData = Main.ActivePlayerFileData;

        if (fileData == null)
        {
            Main.NewText("No active player data.", Color.Red);
            return;
        }

        // Send save request to server
        ModContent.GetInstance<SSCSaveSystem>().SendPacketToSavePlayerFile();

        // Notify player
        string time = DateTime.Now.ToString("HH:mm:ss");
        Main.NewText($"{Main.LocalPlayer.name} saved manually at {time}", Color.MediumPurple);
    }
}
