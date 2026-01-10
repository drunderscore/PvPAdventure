using System;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Core.SSC;

public class PlaytimeCommand : ModCommand
{
    public override string Command => "playtime";

    public override string Description => "Shows your player's playtime in this world.";

    public override CommandType Type => CommandType.Chat;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        var fileData = Main.ActivePlayerFileData;

        if (fileData == null)
        {
            Main.NewText("No active player data.", Color.Red);
            return;
        }

        var playTime = fileData.GetPlayTime();
        Main.NewText($"{Main.LocalPlayer.name} - Playtime: {SSC.FormatPlayTime(playTime)}", Color.MediumPurple);
    }
}

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
