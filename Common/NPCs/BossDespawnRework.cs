using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.NPCs;

/// <summary>
/// This makes bosses not despawn when too far away from a nearby player. Instead, they will go back to their spawn position if a player is not close enough for them to aggro
/// </summary>
internal class BossDespawnRework : GlobalNPC
{
    public override bool InstancePerEntity => true;

    private const float AggroRangeTiles = 400f;
    private const float AggroRange = AggroRangeTiles * 16f;
    private const float GolemLeashRangeTiles = 175f;
    private const float GolemLeashRange = GolemLeashRangeTiles * 16f;
    private const float CultistLeashRangeTiles = 300f;
    private const float CultistLeashRange = CultistLeashRangeTiles * 16f;
    private const double DespawnAtDawnTicks = 600.0;

    private const float ReturnSpeed = 12f;
    private const int ResumeTimeLeft = 3600;
    private Vector2 spawnCenter;
    private bool initialized;
    private bool wasAway;
    private bool golemChildrenSpawned;
    private bool despawningForDawn;
    private static bool IsTrackedNonBoss(NPC npc) => npc.type is
        NPCID.PlanterasHook or
        NPCID.Golem or
        NPCID.GolemHead or
        NPCID.GolemHeadFree or
        NPCID.GolemFistLeft or
        NPCID.GolemFistRight;

    private static bool IsGolemPart(NPC npc) => npc.type is
        NPCID.Golem or
        NPCID.GolemHead or
        NPCID.GolemHeadFree or
        NPCID.GolemFistLeft or
        NPCID.GolemFistRight;

    private static bool IsTracked(NPC npc) =>
        npc.boss || IsTrackedNonBoss(npc);

    private static bool IsWallOfFlesh(NPC npc) =>
        npc.type == NPCID.WallofFlesh || npc.type == NPCID.WallofFleshEye;

    private static bool IsNocturnal(NPC npc) => npc.type is
        NPCID.EyeofCthulhu or
        NPCID.TheDestroyer or
        NPCID.SkeletronHead or
        NPCID.Retinazer or
        NPCID.Spazmatism or
        NPCID.SkeletronPrime or
        NPCID.HallowBoss;

    private static bool TeleportsHome(NPC npc) => npc.type is
        NPCID.Golem or
        NPCID.KingSlime or
        NPCID.Deerclops or
        NPCID.QueenSlimeBoss;
    public override bool CheckActive(NPC npc)
    {
        if (IsTracked(npc))
            return false;

        return base.CheckActive(npc);
    }

    public override bool PreAI(NPC npc)
    {
        if (!IsTracked(npc))
            return true;

        if (IsWallOfFlesh(npc))
        {
            if (npc.type == NPCID.WallofFlesh)
                npc.localAI[1] = 0f;

            if (HasNearbyPlayer(npc))
            {
                ResumeVanillaAI(npc);
                return true;
            }

            wasAway = true;
            npc.velocity = Vector2.Zero;
            npc.netUpdate = true;
            return false;
        }

        if (npc.type == NPCID.CultistBoss)
        {
            var pause = npc.GetGlobalNPC<BossPausePeriod>();

            if (HasNearbyPlayer(npc))
            {
                pause.ForceUnpause(npc);
                ResumeVanillaAI(npc);
                return true;
            }

            wasAway = true;
            pause.ForcePause(npc);
            npc.netUpdate = true;
            return true;
        }

        if (!initialized)
        {
            spawnCenter = npc.Center;
            initialized = true;
        }

        if (IsNocturnal(npc) && Main.dayTime && Main.time >= DespawnAtDawnTicks)
        {
            npc.active = false;
            despawningForDawn = true;
            return false;
        }

        if (npc.type == NPCID.Golem && !golemChildrenSpawned)
        {
            golemChildrenSpawned = true;
            var source = npc.GetSource_FromAI();
            NPC.NewNPC(source, (int)npc.Center.X - 84, (int)npc.Center.Y - 9, NPCID.GolemFistLeft);
            NPC.NewNPC(source, (int)npc.Center.X + 78, (int)npc.Center.Y - 9, NPCID.GolemFistRight);
            NPC.NewNPC(source, (int)npc.Center.X - 3, (int)npc.Center.Y - 57, NPCID.GolemHead);
            npc.localAI[0] = 1f;
            NPC.golemBoss = npc.whoAmI;
        }

        if (HasNearbyPlayer(npc))
        {
            ResumeVanillaAI(npc);
            return true;
        }

        wasAway = true;

        if (TeleportsHome(npc))
        {
            npc.Center = spawnCenter;
            npc.velocity = Vector2.Zero;
        }
        else
        {
            npc.noGravity = true;
            npc.noTileCollide = true;
            MoveTowardSpawn(npc);
        }

        npc.netUpdate = true;
        return false;
    }

    public override void PostAI(NPC npc)
    {
        if (!IsTracked(npc))
            return;

        if (despawningForDawn)
        {
            despawningForDawn = false;
            return;
        }

        if (!npc.active && npc.life > 0)
        {
            npc.active = true;
            npc.netUpdate = true;
        }

        if (npc.type == NPCID.WallofFlesh)
            npc.localAI[1] = 0f;
    }

    private void ResumeVanillaAI(NPC npc)
    {
        if (!wasAway)
            return;

        wasAway = false;

        for (int i = 0; i < NPC.maxAI; i++)
        {
            if (npc.ai[i] < 0f)
                npc.ai[i] = 0f;
            if (npc.localAI[i] < 0f)
                npc.localAI[i] = 0f;
        }

        npc.timeLeft = ResumeTimeLeft;

        npc.TargetClosest(faceTarget: true);
        npc.netUpdate = true;
    }

    private static bool HasNearbyPlayer(NPC npc)
    {
        float range = IsGolemPart(npc) ? GolemLeashRange
            : npc.type == NPCID.CultistBoss ? CultistLeashRange
            : AggroRange;
        float rangeSquared = range * range;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player p = Main.player[i];
            if (p.active && !p.dead && Vector2.DistanceSquared(npc.Center, p.Center) <= rangeSquared)
                return true;
        }
        return false;
    }

    private void MoveTowardSpawn(NPC npc)
    {
        Vector2 toSpawn = spawnCenter - npc.Center;

        if (toSpawn.LengthSquared() <= ReturnSpeed * ReturnSpeed)
        {
            npc.Center = spawnCenter;
            npc.velocity = Vector2.Zero;
        }
        else
        {
            npc.velocity = Vector2.Normalize(toSpawn) * ReturnSpeed;
        }
    }
}