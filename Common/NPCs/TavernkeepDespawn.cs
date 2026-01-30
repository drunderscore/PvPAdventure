using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.NPCs;

// Despawns the unconscious tavernkeep NPC immediately
public class TavernkeepDespawn : GlobalNPC
{
    public override void PostAI(NPC npc)
    {
        if (npc.type == NPCID.BartenderUnconscious)
        {
            npc.active = false;
            npc.life = 0;

        }
    }
}

