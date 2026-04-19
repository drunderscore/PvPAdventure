using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.NPCs;

// Despawns the unconscious tavernkeep NPC immediately, as well as the cultist archer
public class DisableNPCs : GlobalNPC
{
    public override void PostAI(NPC npc)
    {
        if (npc.type == NPCID.BartenderUnconscious)
        {
            npc.active = false;
            npc.life = 0;

        }
        if (npc.type == NPCID.CultistArcherBlue)
        {
            npc.active = false;
            npc.life = 0;

        }
    }
}

