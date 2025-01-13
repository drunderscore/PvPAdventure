using System.Linq;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using PvPAdventure.System;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure;

public class AdventureNpc : GlobalNPC
{
    public override bool InstancePerEntity => true;
    public DamageInfo LastDamageFromPlayer { get; set; }

    public class DamageInfo(byte who)
    {
        public byte Who { get; } = who;
    }

    public override void Load()
    {
        if (Main.dedServ)
            On_NPC.PlayerInteraction += OnNPCPlayerInteraction;

        // Prevent Empress of Light from targeting players during daytime, so she will despawn.
        On_NPC.TargetClosest += OnNPCTargetClosest;
        // Prevent Empress of Light from being enraged, so she won't instantly kill players.
        On_NPC.ShouldEmpressBeEnraged += OnNPCShouldEmpressBeEnraged;
        // Clients and servers sync the Shimmer buff upon all collisions constantly for NPCs.
        // Mark it as quiet so just the server does this.
        IL_NPC.Collision_WaterCollision += EditNPCCollision_WaterCollision;
    }

    public override void OnSpawn(NPC npc, IEntitySource source)
    {
        if (npc.isLikeATownNPC)
            // FIXME: Should be marked as dontTakeDamage instead, doesn't function for some reason.
            npc.immortal = true;
    }

    private static void OnNPCPlayerInteraction(On_NPC.orig_PlayerInteraction orig, NPC self, int player)
    {
        orig(self, player);

        // If this is part of the Eater of Worlds, then mark ALL segments as last damaged by this player.
        if (IsPartOfEaterOfWorlds((short)self.type))
        {
            foreach (var npc in Main.ActiveNPCs)
            {
                if (!IsPartOfEaterOfWorlds((short)npc.type))
                    continue;

                npc.GetGlobalNPC<AdventureNpc>().LastDamageFromPlayer = new DamageInfo((byte)player);
            }
        }
        else if (IsPartOfTheDestroyer((short)self.type))
        {
            foreach (var npc in Main.ActiveNPCs)
            {
                if (!IsPartOfTheDestroyer((short)npc.type))
                    continue;

                npc.GetGlobalNPC<AdventureNpc>().LastDamageFromPlayer = new DamageInfo((byte)player);
            }
        }
        else
        {
            self.GetGlobalNPC<AdventureNpc>().LastDamageFromPlayer = new DamageInfo((byte)player);
        }
    }

    private void OnNPCTargetClosest(On_NPC.orig_TargetClosest orig, NPC self, bool facetarget)
    {
        if (self.type == NPCID.HallowBoss && Main.IsItDay())
        {
            self.target = -1;
            return;
        }

        orig(self, facetarget);
    }

    private bool OnNPCShouldEmpressBeEnraged(On_NPC.orig_ShouldEmpressBeEnraged orig)
    {
        if (Main.remixWorld)
            return orig();

        return false;
    }

    private void EditNPCCollision_WaterCollision(ILContext il)
    {
        var cursor = new ILCursor(il);
        // Find the store to shimmerWet...
        cursor.GotoNext(i => i.MatchStfld<Entity>("shimmerWet"));
        // ...to find the call to AddBuff...
        cursor.GotoNext(i => i.MatchCall<NPC>("AddBuff"));
        // ...to go back to the "quiet" parameter...
        cursor.Index -= 1;
        // ...to remove it...
        cursor.Remove();
        // ...and replace it with true.
        cursor.Emit(OpCodes.Ldc_I4_1);
    }


    public override bool? CanBeHitByProjectile(NPC npc, Projectile projectile)
    {
        var config = ModContent.GetInstance<AdventureConfig>();

        if (npc.boss &&
            config.BossInvulnerableProjectiles.Any(projectileDefinition =>
                projectileDefinition.Type == projectile.type))
            return false;

        return null;
    }

    public override void OnKill(NPC npc)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        var lastDamageInfo = npc.GetGlobalNPC<AdventureNpc>().LastDamageFromPlayer;
        if (lastDamageInfo == null)
            return;

        var lastDamager = Main.player[lastDamageInfo.Who];
        if (lastDamager == null || !lastDamager.active)
            return;

        ModContent.GetInstance<PointsManager>().AwardNpcKillToTeam((Team)lastDamager.team, npc);
    }

    public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
    {
        if (ModContent.GetInstance<GameManager>()?.CurrentPhase == GameManager.Phase.Waiting)
            maxSpawns = 0;
    }

    public override void PostAI(NPC npc)
    {
        // Reduce the timeLeft requirement for Queen Bee despawn.
        if (npc.type == NPCID.QueenBee && npc.timeLeft <= NPC.activeTime - (4.5 * 60))
            npc.active = false;
    }

    public static bool IsPartOfEaterOfWorlds(short type) =>
        type is NPCID.EaterofWorldsHead or NPCID.EaterofWorldsBody or NPCID.EaterofWorldsTail;

    public static bool IsPartOfTheDestroyer(short type) =>
        type is NPCID.TheDestroyer or NPCID.TheDestroyerBody or NPCID.TheDestroyerTail;
}