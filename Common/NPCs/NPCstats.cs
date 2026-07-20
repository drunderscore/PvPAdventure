﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace PvPAdventure.Common.NPCs;
internal class NPCstats : GlobalNPC
{
    public override void SetDefaults(NPC entity)
    {
        if (entity.type == NPCID.EaterofWorldsBody || entity.type == NPCID.EaterofWorldsHead || entity.type == NPCID.EaterofWorldsTail)
        {
            entity.lifeMax *= 2;
            entity.life = entity.lifeMax;
        }
    }
}