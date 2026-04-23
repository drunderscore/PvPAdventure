using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Debug;

#if DEBUG
internal sealed class DebugParticleOrchestrator : ModSystem
{
    public override void Load()
    {
        if (Main.dedServ)
            return;

        On_ParticleOrchestrator.SpawnParticlesDirect += OnSpawnParticlesDirect;
    }

    public override void Unload()
    {
        if (Main.dedServ)
            return;

        On_ParticleOrchestrator.SpawnParticlesDirect -= OnSpawnParticlesDirect;
    }

    private static void OnSpawnParticlesDirect(
        On_ParticleOrchestrator.orig_SpawnParticlesDirect orig,
        ParticleOrchestraType type,
        ParticleOrchestraSettings settings)
    {
        //Log.Debug($"ParticleOrchestraType: {type}");

        if (type == ParticleOrchestraType.TrueExcalibur)
        {
            SpawnTrueExcaliburBlueGreen(settings);
            return; // Skip vanilla red/white burst.
        }

        orig(type, settings);
    }

    private static void SpawnTrueExcaliburBlueGreen(ParticleOrchestraSettings settings)
    {
        Vector2 pos = settings.PositionInWorld;
        Vector2 baseVel = settings.MovementVector;

        int count = 24 + Main.rand.Next(10);

        for (int i = 0; i < count; i++)
        {
            Vector2 v = baseVel.RotatedByRandom(0.8f) * Main.rand.NextFloat(0.2f, 1.2f);
            v += Main.rand.NextVector2Circular(2f, 2f);

            int dustType = Main.rand.NextBool() ? DustID.BlueTorch : DustID.GreenTorch;

            Dust d = Dust.NewDustPerfect(pos + Main.rand.NextVector2Circular(10f, 10f), dustType, v, 150, default, Main.rand.NextFloat(1.0f, 1.6f));
            d.noGravity = true;
            d.fadeIn = Main.rand.NextFloat(0.6f, 1.2f);
        }
    }
}
#endif