using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.SSC.Commands;

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

