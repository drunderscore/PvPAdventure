using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using PvPAdventure.System;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace PvPAdventure;

public class AdventureNpc : GlobalNPC
{
    public override bool InstancePerEntity => true;
    public DamageInfo LastDamageFromPlayer { get; set; }
    //test
    //test
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
        // Ensure that transformed NPCs (usually those bound) are also immortal.
        On_NPC.Transform += OnNPCTransform;
        On_NPC.ScaleStats += OnNPCScaleStats;
        // Spawn the Old Man if Skeletron naturally despawns.
        IL_NPC.CheckActive += EditNPCCheckActive;
        // Remove requirement for no existing NPCs in the world for some natural NPC spawns.
        IL_NPC.SpawnNPC += EditNPCSpawnNPC;
    }

    private void OnNPCScaleStats(On_NPC.orig_ScaleStats orig, NPC self, int? activeplayerscount,
        GameModeData gamemodedata, float? strengthoverride)
    {
        try
        {
            // If we aren't in expert mode, don't even try to change anything.
            if (!Main.expertMode)
                return;

            // If this is a boss, we want it to scale based on the number of players on a specific team...
            if (self.boss || IsPartOfEaterOfWorlds((short)self.type) || IsPartOfTheDestroyer((short)self.type))
            {
                // FIXME: Ignore None team
                var closestPlayerIndex = self.FindClosestPlayer();
                if (closestPlayerIndex == -1)
                {
                    Mod.Logger.Warn(
                        $"Cannot find closest player to scale boss stats of {self.whoAmI}/{self.type}/{self.FullName}, bailing.");
                    return;
                }

                var closestPlayer = Main.player[closestPlayerIndex];

                var numberOfPlayersOnThisTeam = Main.player
                    .Where(player => player.active)
                    .Where(player => !player.ghost)
                    .Where(player => player.team == closestPlayer.team)
                    .Count();

                activeplayerscount = numberOfPlayersOnThisTeam;
            }
            // ...but otherwise, we want it to scale as if it were normal mode.
            else
            {
                gamemodedata = GameModeData.NormalMode;
            }
        }
        finally
        {
            orig(self, activeplayerscount, gamemodedata, strengthoverride);
        }
    }

    public override void SetDefaults(NPC entity)
    {
        if (entity.isLikeATownNPC)
            // FIXME: Should be marked as dontTakeDamage instead, doesn't function for some reason.
            entity.immortal = true;

        var adventureConfig = ModContent.GetInstance<AdventureConfig>();

        // Can't construct an NPCDefinition too early -- it'll call GetName and won't be graceful on failure.
        if (NPCID.Search.TryGetName(entity.type, out var name))
        {
            {
                if (adventureConfig.NpcBalance.LifeMaxMultipliers.TryGetValue(new(name), out var multiplier))
                    entity.lifeMax = (int)(entity.lifeMax * multiplier.Value);
            }

            {
                if (adventureConfig.NpcBalance.DamageMultipliers.TryGetValue(new(name), out var multiplier))
                    entity.damage = (int)(entity.damage * multiplier.Value);
            }
        }
    }

    public override void OnSpawn(NPC npc, IEntitySource source)
    {
        // Due to the new bound NPCs we've added, it's now possible that a town NPC moving in can conflict with a bound
        // NPC already spawned in the world. We'll have to remove all of them, as natural spawns take precedent.
        // This check is here because it's cheap and likely to always be the case for our bound NPCs.
        if (npc.isLikeATownNPC)
        {
            foreach (var worldNpc in Main.ActiveNPCs)
            {
                if (worldNpc.whoAmI == npc.whoAmI)
                    continue;

                // This NPC in the world is a bound NPC of ours, and it transforms into the NPC that just spawned...
                if (worldNpc.ModNPC is BoundNpc boundWorldNpc && npc.type == boundWorldNpc.TransformInto)
                {
                    // ...so now it must go.
                    worldNpc.life = 0;
                    worldNpc.netSkip = -1;
                    NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
                }
            }
        }

        var adventureConfig = ModContent.GetInstance<AdventureConfig>();

        if (adventureConfig.NpcSpawnAnnouncements.Contains(new NPCDefinition(npc.type)))
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                Main.NewText(Language.GetTextValue("Announcement.HasAwoken", npc.TypeName), 175, 75);
            else if (Main.netMode == NetmodeID.Server)
                ChatHelper.BroadcastChatMessage(NetworkText.FromKey("Announcement.HasAwoken", npc.GetTypeNetName()),
                    new(175, 75, 255));
        }
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

    private void OnNPCTransform(On_NPC.orig_Transform orig, NPC self, int newtype)
    {
        orig(self, newtype);

        if (self.isLikeATownNPC)
            // FIXME: Should be marked as dontTakeDamage instead, doesn't function for some reason.
            self.immortal = true;
    }

    private void EditNPCCheckActive(ILContext il)
    {
        var cursor = new ILCursor(il);

        // First, find the assignment to Entity.active...
        cursor.GotoNext(i => i.MatchStfld<Entity>("active"));

        // ...and go past the assignment...
        cursor.Index += 1;

        // ...to load this...
        cursor.EmitLdarg0()
            // ...and emit a delegate to possibly spawn the Old Man.
            .EmitDelegate((NPC npc) =>
            {
                // Only for Skeletron
                if (npc.type != NPCID.SkeletronHead)
                    return;

                // Not on multiplayer clients
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    return;

                // Only if Skeletron hasn't been defeated already
                if (NPC.downedBoss3)
                    return;

                // Only if there isn't already an Old Man
                if (Main.npc.Any(predicateNpc => predicateNpc.active && predicateNpc.type == NPCID.OldMan))
                    return;

                Mod.Logger.Info("Spawning Old Man at the dungeon due to Skeletron despawn");
                var oldMan = NPC.NewNPC(
                    Entity.GetSource_TownSpawn(),
                    Main.dungeonX * 16 + 8,
                    Main.dungeonY * 16,
                    NPCID.OldMan
                );

                if (oldMan != Main.maxNPCs)
                {
                    Main.npc[oldMan].homeless = false;
                    Main.npc[oldMan].homeTileX = Main.dungeonX;
                    Main.npc[oldMan].homeTileY = Main.dungeonY;

                    NetMessage.SendData(MessageID.SyncNPC, number: oldMan);
                }
            });
    }

    private void EditNPCSpawnNPC(ILContext il)
    {
        var cursor = new ILCursor(il);

        void RemoveAnyNPCsCalls(int type)
        {
            // Go back to the beginning.
            cursor.Index = 0;

            // For every NPC.AnyNPCs(type) call we can match...
            while (cursor.TryGotoNext(i => i.MatchCall<NPC>("AnyNPCs") && i.Previous.MatchLdcI4(type)))
            {
                // ...go back to the constant load...
                cursor.Index -= 1;
                // ...to remove it, the call, and the branch.
                cursor.RemoveRange(3);
            }
        }

        RemoveAnyNPCsCalls(NPCID.WyvernHead);
        RemoveAnyNPCsCalls(NPCID.Mothron);
        RemoveAnyNPCsCalls(NPCID.BigMimicCorruption);
        RemoveAnyNPCsCalls(NPCID.BigMimicCrimson);
        RemoveAnyNPCsCalls(NPCID.BigMimicHallow);
        RemoveAnyNPCsCalls(NPCID.BigMimicJungle);
    }

    public override bool? CanBeHitByProjectile(NPC npc, Projectile projectile)
    {
        var config = ModContent.GetInstance<AdventureConfig>();

        var isBoss = npc.boss
                     || IsPartOfEaterOfWorlds((short)npc.type)
                     || IsPartOfTheDestroyer((short)npc.type);

        if (isBoss && config.BossInvulnerableProjectiles.Any(projectileDefinition =>
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

    public override bool? CanChat(NPC npc)
    {
        // This is now a possibility from our multiplayer pause.
        if (Main.gamePaused)
            return false;

        return null;
    }

    public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
    {
        void AddNonExpertBossLoot(int id)
        {
            npcLoot.Add(ItemDropRule.ByCondition(new Conditions.LegacyHack_IsBossAndNotExpert(), id));
        }

        if (IsPartOfEaterOfWorlds((short)npc.type) || npc.type == NPCID.BrainofCthulhu)


            AddNonExpertBossLoot(ItemID.WormScarf);
        else
        {
            switch (npc.type)
            {
                case NPCID.KingSlime:
                    AddNonExpertBossLoot(ItemID.RoyalGel);
                    break;
                case NPCID.EyeofCthulhu:
                    AddNonExpertBossLoot(ItemID.EoCShield);
                    break;
                case NPCID.QueenBee:
                    AddNonExpertBossLoot(ItemID.HiveBackpack);
                    break;
                case NPCID.Deerclops:
                    AddNonExpertBossLoot(ItemID.BoneHelm);
                    break;
                case NPCID.SkeletronHead:
                    AddNonExpertBossLoot(ItemID.BoneGlove);
                    break;
                case NPCID.QueenSlimeBoss:
                    AddNonExpertBossLoot(ItemID.VolatileGelatin);
                    break;
                case NPCID.TheDestroyer:
                    AddNonExpertBossLoot(ItemID.MechanicalWagonPiece);
                    break;
                case NPCID.Retinazer:
                case NPCID.Spazmatism:
                    AddNonExpertBossLoot(ItemID.MechanicalWheelPiece);
                    break;
                case NPCID.SkeletronPrime:
                    AddNonExpertBossLoot(ItemID.MechanicalBatteryPiece);
                    break;
                case NPCID.Plantera:
                    AddNonExpertBossLoot(ItemID.SporeSac);
                    break;
            }
        }



        foreach (var rule in npcLoot.Get())
        {
            if (rule is DropBasedOnExpertMode drop)
            {
                if (drop.ruleForNormalMode is CommonDrop normalDropRule)
                {
                    switch (normalDropRule.itemId)
                    {
                        case ItemID.Tabi:
                            normalDropRule.chanceDenominator = 4;
                            break;

                        case ItemID.PaladinsHammer:
                        case ItemID.PaladinsShield:
                            normalDropRule.chanceNumerator = 3;
                            normalDropRule.chanceDenominator = 20;
                            break;
                       
                        //probably doesnt work FIXME
                        case ItemID.Stinger:
                            if (npc.type == NPCID.MossHornet)
                            {
                                normalDropRule.chanceDenominator = 1;
                            }
                            break;

                        case ItemID.ShadowbeamStaff:
                        case ItemID.SpectreStaff:
                        case ItemID.InfernoFork:
                        case ItemID.SniperRifle:
                        case ItemID.TacticalShotgun:
                        case ItemID.RocketLauncher:
                            normalDropRule.chanceNumerator = 1;
                            normalDropRule.chanceDenominator = 10;
                            break;

                        case ItemID.MaceWhip:
                            normalDropRule.chanceNumerator = 3;
                            normalDropRule.chanceDenominator = 400;
                            break;

                        case ItemID.SpiderFang:
                            normalDropRule.chanceNumerator = 4;
                            normalDropRule.chanceDenominator = 5;
                            break;
                    }
                }
                else if (npc.type == NPCID.GoblinSummoner && drop.ruleForNormalMode is OneFromOptionsDropRule optionsRule)
                {
                    optionsRule.chanceDenominator = 1;
                }
            }

            if (rule is ItemDropWithConditionRule conditionalDrop)
            {
                if (conditionalDrop.itemId == ItemID.ButterflyDust)
                {
                    conditionalDrop.chanceNumerator = 1;
                    conditionalDrop.chanceDenominator = 1;
                }
            }

            if (rule is CommonDrop commonDrop)
            {
                if (commonDrop.itemId == ItemID.MagicQuiver)
                {
                    commonDrop.chanceNumerator = 1;
                    commonDrop.chanceDenominator = 30;
                }
            }
        }
   
        //power cell drop rete changers
        if (npc.netID == NPCID.FlyingSnake || npc.netID == NPCID.Lihzahrd || npc.netID == NPCID.LihzahrdCrawler)
        {
            // Clear the existing loot rules (optional, depending on your needs)
            npcLoot.RemoveWhere(rule => true);

            // Add the new loot rule with a 5% chance to drop 1 power cell
            npcLoot.Add(ItemDropRule.Common(ItemID.LihzahrdPowerCell, 33, 1, 1)); // 1 in 20 chance (~5%)

            // Add a 33% chance to drop a Solar Tablet Fragment
            npcLoot.Add(ItemDropRule.Common(ItemID.LunarTabletFragment, 3, 1, 1)); // 1 in 3 chance (~33.33%)



        }
        //loot pool seperators
        if (npc.netID == NPCID.Golem)
        {
            // Clear existing loot
            npcLoot.RemoveWhere(rule => true);

            // Add your existing drops
            npcLoot.Add(ItemDropRule.Common(ItemID.BeetleHusk, 1, 4, 8));
            npcLoot.Add(ItemDropRule.Common(ItemID.GolemTrophy, 10, 1, 1));
            npcLoot.Add(ItemDropRule.Common(ItemID.GolemMask, 7, 1, 3));
            npcLoot.Add(ItemDropRule.Common(ItemID.StyngerBolt, 1, 60, 99));

            npcLoot.Add(ItemDropRule.OneFromOptions(1, ItemID.Stynger, ItemID.PossessedHatchet, ItemID.GolemFist, ItemID.HeatRay, ItemID.StaffofEarth));
            npcLoot.Add(ItemDropRule.OneFromOptions(1, ItemID.Picksaw, ItemID.EyeoftheGolem, ItemID.SunStone));
        }
        if (npc.netID == NPCID.QueenSlimeBoss)
        {
            // Clear existing loot
            npcLoot.RemoveWhere(rule => true);

            // Add your existing drops
            npcLoot.Add(ItemDropRule.Common(ItemID.GelBalloon, 1, 25, 75));
            npcLoot.Add(ItemDropRule.Common(ItemID.VolatileGelatin, 1, 1, 1));
            npcLoot.Add(ItemDropRule.Common(ItemID.QueenSlimeTrophy, 10, 1, 1));
            npcLoot.Add(ItemDropRule.Common(ItemID.QueenSlimeMask, 7, 1, 3));

            npcLoot.Add(ItemDropRule.OneFromOptions(1, ItemID.CrystalNinjaHelmet, ItemID.CrystalNinjaChestplate, ItemID.CrystalNinjaLeggings));
            npcLoot.Add(ItemDropRule.OneFromOptions(1, ItemID.Smolstar, ItemID.QueenSlimeHook, ItemID.QueenSlimeMountSaddle));
        }
    }

    // This only runs on the attacking player
    public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
    {
        if (!Main.dedServ)
            PlayHitMarker(damageDone);
    }

    // This only runs on the attacking player
    public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
    {
        if (!Main.dedServ)
            PlayHitMarker(damageDone);
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

    public override void ModifyShop(NPCShop shop)
    {
        // The Steampunker sells the Jetpack at moon phase 4 and after during hardmode.
        // Change it to be during moon phase 5 and later.
        if (shop.NpcType == NPCID.Steampunker && shop.TryGetEntry(ItemID.Jetpack, out var entry))
        {
            if (((List<Condition>)entry.Conditions).Remove(Condition.MoonPhasesHalf1))
                entry.AddCondition(Condition.MoonPhaseWaxingCrescent);
            else
                Mod.Logger.Warn(
                    "Failed to remove moon phase condition for Steampunker's Jetpack shop entry -- not changing it any further.");
        }
    }

    private static void PlayHitMarker(int damage)
    {
        var marker = ModContent.GetInstance<AdventureClientConfig>().SoundEffect.NpcHitMarker;
        if (marker != null)
            SoundEngine.PlaySound(marker.Create(damage));
    }

    public static bool IsPartOfEaterOfWorlds(short type) =>
        type is NPCID.EaterofWorldsHead or NPCID.EaterofWorldsBody or NPCID.EaterofWorldsTail;

    public static bool IsPartOfTheDestroyer(short type) =>
        type is NPCID.TheDestroyer or NPCID.TheDestroyerBody or NPCID.TheDestroyerTail;
}