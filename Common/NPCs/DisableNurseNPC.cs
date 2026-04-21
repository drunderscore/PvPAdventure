using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.NPCs;

internal class DisableNurseNPC : ModSystem
{
    public override void PostUpdateTime()
    {
        // The Nurse is never allowed to spawn.
        Main.townNPCCanSpawn[NPCID.Nurse] = false;

        /// Shouldn't this also be here, so we can scrap <see cref="DisableNPCs"/>?
        //Main.townNPCCanSpawn[NPCID.BartenderUnconscious] = false
    }

}
