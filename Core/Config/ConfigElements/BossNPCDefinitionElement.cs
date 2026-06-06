using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;

namespace PvPAdventure.Core.Config.ConfigElements;

public class BossNPCDefinitionElement : NPCDefinitionElement
{
    protected override List<DefinitionOptionElement<NPCDefinition>> CreateDefinitionOptionElementList()
    {
        var options = base.CreateDefinitionOptionElementList();

        // options.RemoveAll(option =>
        // {
        //     int type = option.Definition.Type;
        //
        //     if (type == 0)
        //         return false;
        //
        //     return !IsBossDefinition(type);
        // });

        return options;
    }

    // private static bool IsBossDefinition(int type)
    // {
    //     if (type is NPCID.EaterofWorldsHead or NPCID.EaterofWorldsBody or NPCID.EaterofWorldsTail)
    //         return true;
    //
    //     return ContentSamples.NpcsByNetId.TryGetValue(type, out NPC npc) && npc.boss;
    // }
}
