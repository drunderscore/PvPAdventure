using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
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
using Terraria.ModLoader.IO;
using static PvPAdventure.AdventureItem;

namespace PvPAdventure;

public class AdventureNpc : GlobalNPC
{
    public override bool InstancePerEntity => true;
    public DamageInfo LastDamageFromPlayer { get; set; }

    private readonly Dictionary<Team, int> _teamLife = new();
    public IReadOnlyDictionary<Team, int> TeamLife => _teamLife;

    private readonly HashSet<Team> _hasBeenHurtByTeam = new();
    public IReadOnlySet<Team> HasBeenHurtByTeam => _hasBeenHurtByTeam;

    private Team _lastStrikeTeam;

    public class DamageInfo(byte who)
    {
        public byte Who { get; } = who;
    }

    public override void Load()
    {
        if (Main.dedServ)
            On_NPC.PlayerInteraction += OnNPCPlayerInteraction;

        // Prevent Empress of Light from targeting players during daytime, so she will despawn. REMOVED
        //On_NPC.TargetClosest += OnNPCTargetClosest;
        // Prevent Empress of Light from being enraged, so she won't instantly kill players. REMOVED
         //On_NPC.ShouldEmpressBeEnraged += OnNPCShouldEmpressBeEnraged;
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
        // Make Guide Voodoo Doll spawn Wall of Flesh without the Guide NPC being alive.
        On_Item.CheckLavaDeath += OnItemCheckLavaDeath;
        // Prevent some global drop rules from being registered.
        On_ItemDropDatabase.RegisterToGlobal += AdventureDropDatabase.OnItemDropDatabaseRegisterToGlobal;

        On_NPC.StrikeNPC_HitInfo_bool_bool += OnNPCStrikeNPC;
        On_NetMessage.SendStrikeNPC += OnNetMessageSendStrikeNPC;
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
        if (entity.isLikeATownNPC && entity.type != NPCID.Guide)
            // FIXME: Should be marked as dontTakeDamage instead, doesn't function for some reason.
            entity.immortal = true;

        // Can't construct an NPCDefinition too early -- it'll call GetName and won't be graceful on failure.
        if (NPCID.Search.TryGetName(entity.type, out var name))
        {
            var adventureConfig = ModContent.GetInstance<AdventureServerConfig>();
            var definition = new NPCDefinition(entity.type);

            if (adventureConfig.BossBalance.TryGetValue(definition, out var entry))
            {
                float lifeMult = entry.LifeMaxMultiplier;
                entity.lifeMax = (int)(entity.lifeMax * lifeMult);
                entity.life = entity.lifeMax;

                float dmgMult = entry.DamageMultiplier;
                entity.damage = (int)(entity.damage * dmgMult);

                // FIXME: What if config changes mid game? might desync form the config which might break some contracts?
                foreach (var team in Enum.GetValues<Team>())
                {
                    if (team == Team.None)
                        continue;

                    _teamLife[team] = entity.lifeMax;
                }
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

        var adventureConfig = ModContent.GetInstance<AdventureServerConfig>();

        if (adventureConfig.NpcSpawnAnnouncements.Contains(new NPCDefinition(npc.type)))
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                Main.NewText(Language.GetTextValue("Announcement.HasAwoken", npc.TypeName), 175, 75);
            else if (Main.netMode == NetmodeID.Server)
                ChatHelper.BroadcastChatMessage(NetworkText.FromKey("Announcement.HasAwoken", npc.GetTypeNetName()),
                    new(175, 75, 255));
        }
    }
    public class TillDeathDoUsPart : GlobalNPC
    {
        public override bool PreKill(NPC npc)
        {
            if (npc.type == NPCID.Spazmatism || npc.type == NPCID.Retinazer)
            {
                NPC otherTwin = FindOtherTwin(npc);
                if (otherTwin != null && otherTwin.life > 1)
                {
                    npc.life = 1;
                    return false;
                }
                return true;
            }
            return true;
        }

        public override bool CheckDead(NPC npc)
        {
            if (npc.type == NPCID.Spazmatism || npc.type == NPCID.Retinazer)
            {
                NPC otherTwin = FindOtherTwin(npc);
                if (otherTwin != null && otherTwin.life > 1)
                {
                    npc.life = 1;
                    return false;
                }
            }
            return true;
        }

        public override void OnKill(NPC npc)
        {
            if (npc.type == NPCID.Spazmatism || npc.type == NPCID.Retinazer)
            {
                NPC otherTwin = FindOtherTwin(npc);
                if (otherTwin != null && otherTwin.active && otherTwin.life <= 1)
                {
                  
                    int killingPlayer = npc.lastInteraction;

                    if (killingPlayer != 255 && killingPlayer < Main.maxPlayers && Main.player[killingPlayer].active)
                    {
                        Player player = Main.player[killingPlayer];
                        // AI slop solution because I can't figure out how kill credit works

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int projectile = Projectile.NewProjectile(
                                npc.GetSource_Death(),
                                otherTwin.Center.X,
                                otherTwin.Center.Y,
                                0f,
                                0f,
                                ProjectileID.DD2ExplosiveTrapT3Explosion,
                                200,
                                0f,
                                killingPlayer
                            );


                            if (projectile >= 0 && projectile < Main.maxProjectiles)
                            {
                                Projectile proj = Main.projectile[projectile];

                                proj.penetrate = 1;
                                proj.timeLeft = 120;
                            }


                            if (Main.netMode == NetmodeID.Server && projectile >= 0)
                            {
                                NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, projectile);
                            }
                        }
                    }
                }
            }
        }

        private NPC FindOtherTwin(NPC currentTwin)
        {
            int targetType = currentTwin.type == NPCID.Spazmatism ? NPCID.Retinazer : NPCID.Spazmatism;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.type == targetType)
                {
                    return npc;
                }
            }
            return null;
        }
    }

    private static void OnNPCPlayerInteraction(On_NPC.orig_PlayerInteraction orig, NPC self, int player)
    {
        orig(self, player);

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

        if (self.isLikeATownNPC && self.type != NPCID.Guide)
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

    private void OnItemCheckLavaDeath(On_Item.orig_CheckLavaDeath orig, Item self, int i)
    {
        if (self.type == ItemID.GuideVoodooDoll)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            self.active = false;
            self.type = ItemID.None;
            self.stack = 0;

            NPC.SpawnWOF(self.position);

            if (Main.dedServ)
                NetMessage.SendData(MessageID.SyncItem, number: i);
        }
        else
        {
            orig(self, i);
        }
    }

    // FIXME: This only covers strikes, would be good to support DOTs/debuffs
    private int OnNPCStrikeNPC(On_NPC.orig_StrikeNPC_HitInfo_bool_bool orig, NPC self, NPC.HitInfo hit, bool fromNet,
        bool noPlayerInteraction)
    {
        if (!Main.dedServ && !fromNet)
            PlayHitMarker(hit.Damage);

        var adventureNpc = self.GetGlobalNPC<AdventureNpc>();
        var realLifeNpc = self.realLife != -1 ? Main.npc[self.realLife] : null;
        var realLifeAdventureNpc = realLifeNpc?.GetGlobalNPC<AdventureNpc>();
        var teamLife = realLifeAdventureNpc?._teamLife ?? adventureNpc._teamLife;
        var currentLife = realLifeNpc?.life ?? self.life;

        // If this isn't from the network, then we did this ourselves.
        if (!fromNet)
            adventureNpc.MarkNextStrikeForTeam(self, (Team)Main.LocalPlayer.team);

        // If this was a non-player strike, treat it as damage for all teams.
        if (adventureNpc._lastStrikeTeam == Team.None)
        {
            foreach (var team in teamLife.Keys)
                teamLife[team] = Math.Max(0, teamLife[team] - hit.Damage);

            return orig(self, hit, fromNet, noPlayerInteraction);
        }

        // Always hide PvE combat text with a team, we display our own.
        hit.HideCombatText = true;

        CombatText.NewText(new Rectangle(
            (int)self.position.X,
            (int)self.position.Y,
            self.width,
            self.height
        ), Main.teamColor[(int)adventureNpc._lastStrikeTeam], hit.Damage, hit.Crit);

        // Save the previous immortal value, we might clobber it later.
        var previousImmortal = self.immortal;

        try
        {
            if (teamLife.TryGetValue(adventureNpc._lastStrikeTeam, out var life))
            {
                // FIXME: wrong place to get damage sorta, what abt InstantKill etc?
                var newTeamLife = Math.Max(0, life - hit.Damage);
                teamLife[adventureNpc._lastStrikeTeam] = newTeamLife;

                var damage = Math.Max(0, currentLife - newTeamLife);

                var adventureConfig = ModContent.GetInstance<AdventureServerConfig>();
                var npcDefinition = new NPCDefinition((realLifeNpc ?? self).type);

                // Look up team-life share for this NPC from config
                float share = 0f;

                if (adventureConfig.BossBalance.TryGetValue(npcDefinition, out var entry))
                    share = entry.TeamLifeShare;

                if (share > 0f)
                {
                    // Copy keys to avoid modifying the dictionary while iterating
                    foreach (var team in teamLife.Keys.ToList())
                    {
                        if (team == adventureNpc._lastStrikeTeam)
                            continue;

                        var currentTeamLife = teamLife[team];
                        var newTeamLifeForOther = currentTeamLife - (int)(damage * share);

                        if (newTeamLifeForOther < currentLife)
                            newTeamLifeForOther = currentLife;

                        teamLife[team] = newTeamLifeForOther;
                    }
                }

                // Can't deal 0 damage!
                if (damage == 0)
                    self.immortal = true;

                hit.Damage = damage;

                // FIXME: holy fuck
                // FIXME: i think we want to update whomever teamlife was reduced
                (realLifeNpc ?? self).netUpdate = true;
            }

            return orig(self, hit, fromNet, noPlayerInteraction);
        }
        finally
        {
            self.immortal = previousImmortal;
        }
    }

    private void OnNetMessageSendStrikeNPC(On_NetMessage.orig_SendStrikeNPC orig, NPC npc, ref NPC.HitInfo hit,
        int ignoreClient)
    {
        var adventureNpc = npc.GetGlobalNPC<AdventureNpc>();

        if (Main.dedServ)
        {
            // FIXME: Proper packet
            var packet = Mod.GetPacket();
            packet.Write((byte)AdventurePacketIdentifier.NpcStrikeTeam);
            packet.Write((short)npc.whoAmI);
            packet.Write((byte)adventureNpc._lastStrikeTeam);

            packet.Send(ignoreClient: ignoreClient);
        }

        orig(npc, ref hit, ignoreClient);
    }

    public override bool? CanBeHitByProjectile(NPC npc, Projectile projectile)
    {
        var config = ModContent.GetInstance<AdventureServerConfig>();

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

        AdventureDropDatabase.ModifyNPCLoot(npc, npcLoot);
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
        // NOT USING TS FOR NOW
        // The Steampunker sells the Jetpack at moon phase 4 and after during hardmode.
        // Change it to be during moon phase 5 and later.
        //if (shop.NpcType == NPCID.Steampunker && shop.TryGetEntry(ItemID.Jetpack, out var entry))
        //{
        //    if (((List<Condition>)entry.Conditions).Remove(Condition.MoonPhasesHalf1))
        //        entry.AddCondition(Condition.MoonPhaseWaxingCrescent);
        //    else
        //        Mod.Logger.Warn(
        //            "Failed to remove moon phase condition for Steampunker's Jetpack shop entry -- not changing it any further.");
        //}

    }

    public override bool CheckDead(NPC npc)
    {
        if (npc.type == NPCID.Guide)
        {
            if (Collision.LavaCollision(npc.position, npc.width, npc.height))
            {
                NPC.SpawnWOF(npc.position);

                // If the Wall of Flesh is alive, good enough, we can die.
                // NOTE: This will cause DoDeathEvents to then invoke NPC.SpawnWOF again, but that's okay, it'll just fail.
                if (NPC.AnyNPCs(NPCID.WallofFlesh))
                    return true;
            }

            npc.life = 1;
            return false;
        }

        return true;
    }

    // FIXME: might be costly!
    public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        var adventureNpc = npc.GetGlobalNPC<AdventureNpc>();

        binaryWriter.Write((byte)adventureNpc.TeamLife.Count);
        foreach (var (team, life) in adventureNpc.TeamLife)
        {
            binaryWriter.Write((byte)team);
            binaryWriter.Write7BitEncodedInt(life);
        }
    }

    // FIXME: might be costly!
    public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
    {
        var adventureNpc = npc.GetGlobalNPC<AdventureNpc>();
        adventureNpc._teamLife.Clear();

        {
            var count = binaryReader.ReadByte();
            for (var i = 0; i < count; i++)
            {
                var team = (Team)binaryReader.ReadByte();
                var life = binaryReader.Read7BitEncodedInt();

                adventureNpc._teamLife[team] = life;
            }
        }
    }

    public void MarkNextStrikeForTeam(NPC npc, Team team)
    {
        _lastStrikeTeam = team;
        _hasBeenHurtByTeam.Add(team);

        // If our life pool is someone else's, count it as hurting them too.
        if (npc.realLife != -1 && npc.realLife != npc.whoAmI)
        {
            var realLife = Main.npc[npc.realLife];
            realLife.GetGlobalNPC<AdventureNpc>().MarkNextStrikeForTeam(realLife, team);
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

    public class TavernkeepDespawn : GlobalNPC
    {
        public override void PostAI(NPC npc)
        {
            if (npc.type == NPCID.BartenderUnconscious)
            {
                npc.active = false;
                npc.life = 0;

            }
        }
    }
    public class DemonReplacement : GlobalNPC
    {
        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            bool anyMechBossDefeated = NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3;

            if (anyMechBossDefeated && npc.type == NPCID.Demon)
            {
                Vector2 position = npc.position;
                Vector2 velocity = npc.velocity;
                int target = npc.target;
                int direction = npc.direction;
                int spriteDirection = npc.spriteDirection;
                float rotation = npc.rotation;

                npc.SetDefaults(NPCID.VoodooDemon);

                // Restore the state
                npc.position = position;
                npc.velocity = velocity;
                npc.target = target;
                npc.direction = direction;
                npc.spriteDirection = spriteDirection;
                npc.rotation = rotation;

                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
                }
            }
        }
    }
}
public class Target : ModNPC
{
    public override string Texture => $"PvPAdventure/Assets/NPC/Target";
    public int targetPlayerIndex = -1;

    public override void SetStaticDefaults()
    {
        NPCID.Sets.NPCBestiaryDrawOffset[Type] = new NPCID.Sets.NPCBestiaryDrawModifiers()
        {
            Hide = true
        };
    }

    public override void SetDefaults()
    {
        NPC.width = 0;
        NPC.height = 0;
        NPC.lifeMax = 999999;
        NPC.defense = 0;
        NPC.immortal = false;
        NPC.dontTakeDamage = false; // Don't use this
        NPC.noGravity = true;
        NPC.noTileCollide = true;
        NPC.aiStyle = -1;
        NPC.alpha = 255;
        NPC.HideStrikeDamage = true;
        NPC.friendly = false;
        NPC.chaseable = false; // we don't want this functionality right now, so let's have ts be false
    }

    public override bool? CanBeHitByProjectile(Projectile projectile)
    {
        return false;
    }

    public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
    {
        // Zero out all damage components
        modifiers.SourceDamage *= 0;
        modifiers.FinalDamage *= 0;
        modifiers.Knockback *= 0;
        modifiers.HideCombatText();
    }

    public override bool CheckDead()
    {
        // Never die
        NPC.life = NPC.lifeMax;
        return false;
    }

    public override void AI()
    {
        // Keep at full health always
        NPC.life = NPC.lifeMax;

        if (targetPlayerIndex >= 0 && targetPlayerIndex < Main.maxPlayers)
        {
            Player target = Main.player[targetPlayerIndex];
            if (target == null || !target.active || !target.HasBuff<Anathema>())
            {
                NPC.active = false;
                return;
            }
            NPC.Center = target.Center;
        }
        else
        {
            NPC.active = false;
        }
    }
}