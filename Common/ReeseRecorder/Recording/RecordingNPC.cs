using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.ReeseRecorder.Recording;

public sealed class RecordingNPC : GlobalNPC
{
    public override void PostAI(NPC npc)
    {
        if (Main.netMode == NetmodeID.Server && RecordingSystem.GetRecordingMode() == RecordingMode.Recording)
            npc.netAlways = true;
    }
}
