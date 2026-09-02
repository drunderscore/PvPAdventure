using Terraria;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.World;

/// <summary>
/// Increases the frequency and duration of Sandstorm events
/// This is dogshit and temporary 
/// </summary>
public class IncreasedSandstormSystem : ModSystem
{
    private const bool IncreaseSandstormFrequency = true;

    private int sandstormCheckTimer = 0;
    private const int SandstormCheckInterval = 60;

    private double lastMainTime = -1.0;

    public override void PostUpdateWorld()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !IncreaseSandstormFrequency)
            return;

        bool timeIsProgressing = lastMainTime >= 0 && Main.time != lastMainTime;
        lastMainTime = Main.time;

        if (!timeIsProgressing)
            return;

        sandstormCheckTimer++;
        if (sandstormCheckTimer >= SandstormCheckInterval)
        {
            sandstormCheckTimer = 0;

            if (!Sandstorm.Happening && !Main.raining && !Main.bloodMoon && !Main.eclipse)
            {
                if (Main.rand.NextBool(600))
                {
                    StartSandstorm();
                }
            }
        }
    }

    private void StartSandstorm()
    {
        if (Main.windSpeedCurrent < 0.5f)
            Main.windSpeedCurrent = Main.rand.NextFloat(0.5f, 0.8f);

        Sandstorm.StartSandstorm();
        Sandstorm.TimeLeft = Main.rand.Next(72000, 72000); // for now its always 20 minutes 
        Sandstorm.Severity = Main.rand.NextFloat(0.3f, 0.9f);
        Sandstorm.IntendedSeverity = Sandstorm.Severity;

        if (Main.netMode == NetmodeID.Server)
        {
            NetMessage.SendData(MessageID.WorldData);
        }
    }
}