using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.World;

/// <summary>
/// - Increases the frequency of rain events <br/>
/// - Periodically checks world conditions server-side <br/>
/// - Starts rain with custom duration and intensity <br/>
/// - Syncs weather changes to clients <br/>
/// </summary>
public class IncreasedRainSystem : ModSystem
{
    private int rainCheckTimer = 0;
    private const int RainCheckInterval = 60;

    public override void PostUpdateWorld()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        rainCheckTimer++;

        if (rainCheckTimer >= RainCheckInterval)
        {
            rainCheckTimer = 0;

            if (!Main.raining && !Main.bloodMoon && !Main.eclipse)
            {
                if (Main.rand.NextBool(1200)) //this is just the chance each second that rain starts, so basically 1/20 every minute, so 1 rain every 20 mins
                {
                    StartRain();
                }
            }
        }
    }

    private void StartRain()
    {
        Main.StartRain();

        // Set a reasonable rain duration
        Main.rainTime = Main.rand.Next(18000, 54000); // 5-15 minutes
        Main.maxRaining = Main.rand.NextFloat(0.3f, 0.9f);

        // Sync to clients in multiplayer
        if (Main.netMode == NetmodeID.Server)
        {
            NetMessage.SendData(MessageID.WorldData);
        }
    }
}
