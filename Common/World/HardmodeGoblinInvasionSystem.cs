using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Common.World;
/// <summary>
/// - Triggers a Goblin Army invasion on the second dawn after Wall of Flesh is killed
/// <summary>
public class HardmodeGoblinInvasionSystem : ModSystem
{
    private bool wasHardmode = false;
    private bool pendingGoblinInvasion = false;

    private bool hasSeenNightSinceHardmode = false;

    private bool wasDaytime = false;

    public override void PostUpdateWorld()
    {
        if (Main.hardMode && !wasHardmode)
        {
            if (!NPC.downedGoblins)
            {
                pendingGoblinInvasion = true;
                hasSeenNightSinceHardmode = false;
                hasSeenNightSinceHardmode = !Main.dayTime;
            }
            wasHardmode = true;
        }

        if (!Main.hardMode)
            wasHardmode = false;

        if (pendingGoblinInvasion)
        {
            bool isDaytime = Main.dayTime;
            if (!isDaytime)
                hasSeenNightSinceHardmode = true;

            if (isDaytime && !wasDaytime && hasSeenNightSinceHardmode)
            {
                Main.StartInvasion(InvasionID.GoblinArmy);
                pendingGoblinInvasion = false;
                hasSeenNightSinceHardmode = false;

                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.WorldData);
            }

            wasDaytime = isDaytime;
        }
        else
        {
            wasDaytime = Main.dayTime;
        }
    }

    public override void SaveWorldData(TagCompound tag)
    {
        tag["wasHardmode"] = wasHardmode;
        tag["pendingGoblinInvasion"] = pendingGoblinInvasion;
        tag["hasSeenNightSinceHardmode"] = hasSeenNightSinceHardmode;
    }

    public override void LoadWorldData(TagCompound tag)
    {
        wasHardmode = tag.GetBool("wasHardmode");
        pendingGoblinInvasion = tag.GetBool("pendingGoblinInvasion");
        hasSeenNightSinceHardmode = tag.GetBool("hasSeenNightSinceHardmode");

        if (Main.hardMode)
            wasHardmode = true;

        wasDaytime = Main.dayTime;
    }
}