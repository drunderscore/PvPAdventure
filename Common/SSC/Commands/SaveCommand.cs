using Microsoft.Xna.Framework;
using PvPAdventure.Core.Config;
using System;
using Terraria;
using Terraria.Chat;
using Terraria.Localization;
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
            Main.NewText("Error: No active player data.", Color.Red);
            return;
        }

        // Send save request to server
        ModContent.GetInstance<SSCSaveSystem>().SendPacketToSavePlayerFile();

        var config = ModContent.GetInstance<ClientConfig>();

        if (config.ShowSavePlayerMessages)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            string playtime = PlayerPositionSystem.FormatPlayTime(Main.ActivePlayerFileData.GetPlayTime());

            Main.NewText($"Saved {Main.LocalPlayer.name} at {time} — Playtime: {playtime}",  Color.MediumPurple);
        }
    }
}
