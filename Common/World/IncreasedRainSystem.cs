using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using PvPAdventure.Core.Config;

namespace PvPAdventure.Common.World;

/// <summary>
/// Increases the frequency and duration of rain events
/// This is dogshit and temporary 
/// </summary>
public class IncreasedRainSystem : ModSystem
{
    private int rainCheckTimer = 0;
    private const int RainCheckInterval = 60;

    private double lastMainTime = -1.0;

    public override void PostUpdateWorld()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !ModContent.GetInstance<ServerConfig>().IncreaseRainFrequency)
            return;

        bool timeIsProgressing = lastMainTime >= 0 && Main.time != lastMainTime;
        lastMainTime = Main.time;

        if (!timeIsProgressing)
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
        Main.rainTime = Main.rand.Next(72000, 72000); // for now its just always 20 minutes
        Main.maxRaining = Main.rand.NextFloat(0.3f, 0.9f);
        if (Main.netMode == NetmodeID.Server)
        {
            NetMessage.SendData(MessageID.WorldData);
        }
    }
}