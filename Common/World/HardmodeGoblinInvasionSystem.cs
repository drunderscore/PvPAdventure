using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Common.World;

/// <summary>
/// - Triggers a Goblin Army invasion when entering Hardmode
/// - Ensures the invasion only happens once
/// - Persists Hardmode state across world loads
/// </summary>
public class HardmodeGoblinInvasionSystem : ModSystem
{
    private bool wasHardmode = false;

    public override void PostUpdateWorld()
    {
        // Check if world just entered hardmode
        if (Main.hardMode && !wasHardmode)
        {
            // Check if goblin army hasn't been defeated yet
            if (!NPC.downedGoblins)
            {
                // Start goblin invasion
                Main.StartInvasion(InvasionID.GoblinArmy);

                // Send message to all players
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.WorldData);
                }
            }

            wasHardmode = true;
        }

        // Update state
        if (!Main.hardMode)
        {
            wasHardmode = false;
        }
    }

    public override void SaveWorldData(TagCompound tag)
    {
        // Save whether we were in hardmode
        tag["wasHardmode"] = wasHardmode;
    }

    public override void LoadWorldData(TagCompound tag)
    {
        // Load saved state
        wasHardmode = tag.GetBool("wasHardmode");

        // If loading into hardmode, set flag appropriately
        if (Main.hardMode)
        {
            wasHardmode = true;
        }
    }
}
