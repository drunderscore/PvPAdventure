using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace PvPAdventure.Common.NPCs;
internal class MechBossReworks : GlobalNPC
{
    public override void SetDefaults(NPC entity)
    {
        // Prime's four arms cannot be killed
        if (entity.type == NPCID.PrimeVice ||
            entity.type == NPCID.PrimeLaser ||
            entity.type == NPCID.PrimeSaw ||
            entity.type == NPCID.PrimeCannon)
        {
            entity.dontTakeDamage = true;
        }

        // Destroyer probes take no knockback.
        if (entity.type == NPCID.Probe)
        {
            entity.knockBackResist = 0f;
            entity.lifeMax *= 2;
            entity.life = entity.lifeMax;
        }
    }
}