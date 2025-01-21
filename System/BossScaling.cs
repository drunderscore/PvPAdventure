using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.System;

[Autoload(Side = ModSide.Both)]
public class BossScaling : GlobalNPC
{
    public static List<int> bosses = 
    [
        NPCID.KingSlime, NPCID.EyeofCthulhu, NPCID.EaterofWorldsHead, NPCID.EaterofWorldsBody, 
        NPCID.EaterofWorldsTail, NPCID.BrainofCthulhu, NPCID.Creeper, NPCID.Skeleton, NPCID.SkeletronHand,
        NPCID.Deerclops, NPCID.QueenBee, NPCID.WallofFlesh, NPCID.Retinazer, NPCID.Spazmatism,
        NPCID.TheDestroyer, NPCID.TheDestroyerBody, NPCID.TheDestroyerTail, NPCID.SkeletronPrime,
        NPCID.PrimeCannon, NPCID.PrimeLaser, NPCID.PrimeSaw, NPCID.PrimeVice, NPCID.Probe,
        NPCID.Plantera, NPCID.PlanterasHook, NPCID.PlanterasTentacle, NPCID.Golem, NPCID.GolemHead,
        NPCID.GolemFistLeft, NPCID.GolemFistRight, NPCID.GolemHeadFree, NPCID.DukeFishron, NPCID.HallowBoss,
        NPCID.CultistBoss, NPCID.AncientDoom, NPCID.AncientLight, NPCID.AncientCultistSquidhead,
        NPCID.CultistDragonHead, NPCID.CultistDragonBody1, NPCID.CultistDragonBody2, NPCID.CultistDragonBody3,
        NPCID.CultistDragonBody4, NPCID.CultistDragonTail, NPCID.ServantofCthulhu
    ];
    public override void Load()
    {
        On_NPC.ScaleStats += OnNPCScaleStats;
    }

    private void OnNPCScaleStats(On_NPC.orig_ScaleStats orig, NPC self, int? activePlayersCount, GameModeData gameModeData, float? strengthOverride)
    {
        if (bosses.Contains(self.type))
        {
            int activeTeamMembers = 0;
            Player player = Main.player[0];
            if (self.HasValidTarget)
                player = Main.player[self.target];

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active && Main.player[i].team == player.team)
                    activeTeamMembers++;
            }
            activePlayersCount = activeTeamMembers;
            gameModeData = GameModeData.ExpertMode;
        }
        orig(self, activePlayersCount, gameModeData, strengthOverride);
    }
}
