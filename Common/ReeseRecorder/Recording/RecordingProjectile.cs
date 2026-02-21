using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.ReeseRecorder.Recording;

public sealed class RecordingProjectile : GlobalProjectile
{
    public override void PostAI(Projectile projectile)
    {
        if (Main.netMode == NetmodeID.Server && RecordingSystem.GetRecordingMode() == RecordingMode.Recording)
            projectile.netImportant = true;
    }
}
