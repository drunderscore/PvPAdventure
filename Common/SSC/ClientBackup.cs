using PvPAdventure.Core.Debug;
using Steamworks;
using System;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Common.SSC;

/// <summary>
/// Creates backup copies, client-sided, of player data files
/// </summary>
internal static class ClientBackup
{
    private static string BackupRoot => Path.Combine(Main.SavePath, "PvPAdventureSSCClient");
    private static string MapID => Main.ActiveWorldFileData?.Name ?? "UnknownWorld";

    public static void WritePlayerBackup(string playerName, byte[] plr, TagCompound tplr)
    {
        try
        {
            string steamId = SteamUser.GetSteamID().m_SteamID.ToString();
            string safeName = playerName;

            string dir = Path.Combine(BackupRoot, MapID, steamId);
            Directory.CreateDirectory(dir);

            string plrPath = Path.Combine(dir, safeName + ".plr");
            string tplrPath = Path.Combine(dir, safeName + ".tplr");

            WriteAllBytesWithBak(plrPath, plr);
            WriteTagWithBak(tplrPath, tplr);

            Log.Chat("Client created SSC backup for " + safeName);
        }
        catch (Exception e)
        {
            Log.Chat("Client failed creating SSC backup");
            ModContent.GetInstance<PvPAdventure>().Logger.Error(e);
        }
    }

    private static void WriteAllBytesWithBak(string path, byte[] data)
    {
        string bakPath = path + ".bak";

        if (File.Exists(path))
            File.Copy(path, bakPath, overwrite: true);

        File.WriteAllBytes(path, data);
    }

    private static void WriteTagWithBak(string path, TagCompound tag)
    {
        string bakPath = path + ".bak";

        if (File.Exists(path))
            File.Copy(path, bakPath, overwrite: true);

        TagIO.ToFile(tag, path);
    }
}

