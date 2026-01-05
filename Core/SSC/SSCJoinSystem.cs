using Steamworks;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;

namespace PvPAdventure.Core.SSC;

[Autoload(Side = ModSide.Client)]
public class SSCJoinSystem : ModSystem
{
    private bool _sent;
    private int _delayTicks;
    public override void OnWorldLoad()
    {
        if (!SSCEnabled.IsEnabled)
            return;

        _sent = false;
        _delayTicks = 60; // 1 seconds

        string steamId = SteamUser.GetSteamID().m_SteamID.ToString();

        var fileData = new PlayerFileData(Path.Combine(Main.PlayerPath, $"{steamId}.plr"), false)
        {
            Metadata = FileMetadata.FromCurrentSettings(FileType.Player),
            Player = new()
            {
                name = Main.LocalPlayer.name,
                difficulty = PlayerDifficultyID.SoftCore,
                statLife = 0,
                statMana = 0,
                dead = true,
                ghost = true,
                respawnTimer = int.MaxValue,
                lastTimePlayerWasSaved = long.MaxValue,
                savedPerPlayerFieldsThatArentInThePlayerClass = new Player.SavedPlayerDataWithAnnoyingRules()
            }
        };

        // Enter as a ghost
        fileData.SetAsActive();
    }

    public override void PostUpdateEverything()
    {
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
        SSC.SendJoinRequest();
    }

    public override void OnWorldUnload()
    {
        _sent = false;
        _delayTicks = 0;
    }
}
