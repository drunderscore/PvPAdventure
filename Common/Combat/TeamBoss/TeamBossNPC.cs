using PvPAdventure.Common.Statistics;
using PvPAdventure.Core.Config;
using PvPAdventure.Core.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Common.Combat.TeamBoss;

public sealed class TeamBossNPC : GlobalNPC
{
    #region Fields
    public override bool InstancePerEntity => true;

    public DamageInfo LastDamageFromPlayer { get; set; }

    private readonly Dictionary<Team, int> _teamLife = new();
    public IReadOnlyDictionary<Team, int> TeamLife => _teamLife;

    private readonly HashSet<Team> _hasBeenHurtByTeam = new();
    public IReadOnlySet<Team> HasBeenHurtByTeam => _hasBeenHurtByTeam;

    private Team _lastStrikeTeam;
    #endregion

    public class DamageInfo(byte who)
    {
        public byte Who { get; } = who;
    }

    public override void Load()
    {
        On_NPC.PlayerInteraction += OnNPCPlayerInteraction;
        On_NPC.StrikeNPC_HitInfo_bool_bool += OnNPCStrikeNPC;
        On_NetMessage.SendStrikeNPC += OnNetMessageSendStrikeNPC;
    }

    public override void SetDefaults(NPC entity)
    {
        if (entity.isLikeATownNPC && entity.type != NPCID.Guide)
            // FIXME: Should be marked as dontTakeDamage instead, doesn't function for some reason.
            entity.immortal = true;

        // Can't construct an NPCDefinition too early -- it'll call GetName and won't be graceful on failure.
        if (NPCID.Search.TryGetName(entity.type, out var name))
        {
            var adventureConfig = ModContent.GetInstance<ServerConfig>();
            var definition = new NPCDefinition(entity.type);

            if (adventureConfig.BossBalance.TryGetValue(definition, out var entry))
            {
                float lifeMult = entry.LifeMaxMultiplier;
                entity.lifeMax = (int)(entity.lifeMax * lifeMult);
                entity.life = entity.lifeMax;

                float dmgMult = entry.DamageMultiplier;
                entity.damage = (int)(entity.damage * dmgMult);

                // FIXME: What if config changes mid game? might desync form the config which might break some contracts?
                //foreach (var team in Enum.GetValues<Team>())
                //{
                //    if (team == Team.None)
                //        continue;

                //    _teamLife[team] = entity.lifeMax;
                //}
            }
        }
    }

    private static void OnNPCPlayerInteraction(On_NPC.orig_PlayerInteraction orig, NPC self, int player)
    {
        orig(self, player);

        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        // If this is part of the Eater of Worlds, then mark ALL segments as last damaged by this player.
        if (IsPartOfEaterOfWorlds((short)self.type))
        {
            foreach (var npc in Main.ActiveNPCs)
            {
                if (!IsPartOfEaterOfWorlds((short)npc.type))
                    continue;

                npc.GetGlobalNPC<TeamBossNPC>().LastDamageFromPlayer = new DamageInfo((byte)player);
            }
        }
        else if (IsPartOfTheDestroyer((short)self.type))
        {
            foreach (var npc in Main.ActiveNPCs)
            {
                if (!IsPartOfTheDestroyer((short)npc.type))
                    continue;

                npc.GetGlobalNPC<TeamBossNPC>().LastDamageFromPlayer = new DamageInfo((byte)player);
            }
        }
        else
        {
            self.GetGlobalNPC<TeamBossNPC>().LastDamageFromPlayer = new DamageInfo((byte)player);
        }
    }

    public override void OnKill(NPC npc)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        var lastDamageInfo = npc.GetGlobalNPC<TeamBossNPC>().LastDamageFromPlayer;
        if (lastDamageInfo == null)
            return;

        var lastDamager = Main.player[lastDamageInfo.Who];
        if (lastDamager == null || !lastDamager.active)
            return;

        ModContent.GetInstance<PointsManager>().AwardNpcKillToTeam((Team)lastDamager.team, npc);
    }



    // FIXME: This only covers strikes, would be good to support DOTs/debuffs
    private int OnNPCStrikeNPC(
    On_NPC.orig_StrikeNPC_HitInfo_bool_bool orig,
    NPC self,
    NPC.HitInfo hit,
    bool fromNet,
    bool noPlayerInteraction)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return orig(self, hit, fromNet, noPlayerInteraction);

        var adventureNpc = self.GetGlobalNPC<TeamBossNPC>();
        NPC owner = self.realLife != -1 ? Main.npc[self.realLife] : self;
        var boss = owner.GetGlobalNPC<TeamBossNPC>();

        var teamLife = boss._teamLife;
        int currentLife = owner.life;

        // Resolve strike team
        Team strikeTeam = Team.None;

        var attackerInfo = boss.LastDamageFromPlayer ?? adventureNpc.LastDamageFromPlayer;
        int who = attackerInfo != null ? attackerInfo.Who : self.lastInteraction;

        if (who >= 0 && who < Main.maxPlayers)
        {
            Player p = Main.player[who];
            if (p != null && p.active)
                strikeTeam = (Team)p.team;
        }

        // If we can't attribute, do NOT mutate pools and do vanilla damage.
        if (strikeTeam == Team.None || !IsTeamActive(strikeTeam))
            return orig(self, hit, fromNet, noPlayerInteraction);

        // If pool not initialized (edge case), initialize just this team.
        if (!teamLife.ContainsKey(strikeTeam))
            teamLife[strikeTeam] = owner.lifeMax;

        // Determine leading team = team with lowest remaining life among active teams
        Team leadingTeam = Team.None;
        int leadingLife = int.MaxValue;

        foreach (var kv in teamLife)
        {
            Team t = kv.Key;
            if (t == Team.None)
                continue;

            if (!IsTeamActive(t))
                continue;

            int life = kv.Value;
            if (life < leadingLife)
            {
                leadingLife = life;
                leadingTeam = t;
            }
        }

        // Track that this team has participated (for UI)
        boss._hasBeenHurtByTeam.Add(strikeTeam);

        // Always subtract from the striker's pool (so their bar progresses)
        int strikerOld = teamLife[strikeTeam];
        int strikerNew = Math.Max(0, strikerOld - hit.Damage);
        teamLife[strikeTeam] = strikerNew;

        // Gate real boss damage: only leading team can reduce boss HP
        bool allowRealDamage = strikeTeam == leadingTeam;

        // Hide vanilla combat text; you can keep your custom CombatText if you want.
        hit.HideCombatText = true;

        var previousImmortal = self.immortal;

        try
        {
            if (!allowRealDamage)
            {
                // Block real damage but keep pool damage (immortal revert behavior)
                hit.Damage = 0;
                self.immortal = true;

                owner.netUpdate = true;
                return orig(self, hit, fromNet, noPlayerInteraction);
            }

            // Leading team: allow real damage, but clamp to not exceed the gap between current HP and that team's pool.
            int allowed = Math.Max(0, currentLife - strikerNew);

            if (allowed <= 0)
            {
                hit.Damage = 0;
                self.immortal = true;
            }
            else
            {
                hit.Damage = allowed;
            }

            boss._lastStrikeTeam = strikeTeam;

            owner.netUpdate = true;
            return orig(self, hit, fromNet, noPlayerInteraction);
        }
        finally
        {
            self.immortal = previousImmortal;
        }
    }

    public override void ApplyDifficultyAndPlayerScaling( NPC npc,int numPlayers,float balance, float bossAdjustment)
    {
        // Resolve owner (important for segmented bosses)
        NPC owner = npc.realLife != -1 ? Main.npc[npc.realLife] : npc;
        var boss = owner.GetGlobalNPC<TeamBossNPC>();

        // Only initialize once
        if (boss._teamLife.Count > 0)
            return;

        // Now npc.lifeMax is FINAL and correct
        foreach (var team in Enum.GetValues<Team>())
        {
            if (team == Team.None)
                continue;

            boss._teamLife[team] = owner.lifeMax;
        }

        owner.netUpdate = true;
    }


    private void OnNetMessageSendStrikeNPC(On_NetMessage.orig_SendStrikeNPC orig, NPC npc, ref NPC.HitInfo hit,
        int ignoreClient)
    {
        var adventureNpc = npc.GetGlobalNPC<TeamBossNPC>();

        if (Main.netMode == NetmodeID.Server)
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

    public void MarkNextStrikeForTeam(NPC npc, Team team)
    {
        _lastStrikeTeam = team;
        _hasBeenHurtByTeam.Add(team);

        //Log.Chat("hit: " + npc + ", team: " + team);

        // If our life pool is someone else's, count it as hurting them too.
        if (npc.realLife != -1 && npc.realLife != npc.whoAmI)
        {
            var realLife = Main.npc[npc.realLife];
            realLife.GetGlobalNPC<TeamBossNPC>().MarkNextStrikeForTeam(realLife, team);
        }
    }

    // FIXME: might be costly!
    public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        var adventureNpc = npc.GetGlobalNPC<TeamBossNPC>();

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
        _teamLife.Clear();
        _hasBeenHurtByTeam.Clear();

        int count = binaryReader.ReadByte();

        for (int i = 0; i < count; i++)
        {
            Team team = (Team)binaryReader.ReadByte();
            int life = binaryReader.Read7BitEncodedInt();
            _teamLife[team] = life;
        }

        int full = npc.lifeMax;

        foreach (int v in _teamLife.Values)
        {
            if (v > full)
                full = v;
        }

        foreach (var kv in _teamLife)
        {
            if (kv.Key != Team.None && kv.Value < full)
                _hasBeenHurtByTeam.Add(kv.Key);
        }
    }

    private static bool IsTeamActive(Team team)
    {
        if (team == Team.None)
            return false;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player p = Main.player[i];

            if (p == null || !p.active)
                continue;

            if ((Team)p.team == team)
                return true;
        }

        return false;
    }

    #region Public helpers
    public static bool IsPartOfEaterOfWorlds(short type) =>
        type is NPCID.EaterofWorldsHead or NPCID.EaterofWorldsBody or NPCID.EaterofWorldsTail;

    public static bool IsPartOfTheDestroyer(short type) =>
        type is NPCID.TheDestroyer or NPCID.TheDestroyerBody or NPCID.TheDestroyerTail;
    #endregion
}