using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Core.SSC;

public class SSCPlaytimeCommand : ModCommand
{
    public override string Command => "playtime";

    public override string Description => "Shows your playtime for this world.";

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
        $"{Main.LocalPlayer.name} - Playtime: {SSC.FormatPlayTime(playTime)}", Color.MediumPurple);
    }
}
