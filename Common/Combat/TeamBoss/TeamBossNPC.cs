using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat.TeamBoss;

public sealed class TeamBossNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;

    public DamageInfo LastDamageFromPlayer { get; set; }

    private readonly Dictionary<Team, int> teamLife = new Dictionary<Team, int>();
    public IReadOnlyDictionary<Team, int> TeamLife => teamLife;

    private readonly HashSet<Team> hasBeenHurtByTeam = new HashSet<Team>();
    public IReadOnlySet<Team> HasBeenHurtByTeam => hasBeenHurtByTeam;

    private readonly List<TeamLifeEntry> sortedTeamLifeCache = new List<TeamLifeEntry>();
    public IReadOnlyList<TeamLifeEntry> SortedTeamLifeCache => sortedTeamLifeCache;

    private bool isTeamLifeCacheDirty;
    private Team pendingStrikeTeam;

    public sealed class DamageInfo
    {
        public DamageInfo(byte who)
        {
            Who = who;
        }

        public byte Who { get; }
    }

    public readonly struct TeamLifeEntry
    {
        public TeamLifeEntry(Team team, int life)
        {
            Team = team;
            Life = life;
        }

        public Team Team { get; }
        public int Life { get; }
    }

    public override void OnSpawn(NPC npc, IEntitySource source)
    {
        teamLife.Clear();
        hasBeenHurtByTeam.Clear();
        sortedTeamLifeCache.Clear();

        pendingStrikeTeam = Team.None;
        isTeamLifeCacheDirty = false;
    }

    public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
    {
        ApplyTeamDamage(npc, damageDone);
    }

    public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
    {
        ApplyTeamDamage(npc, damageDone);
    }

    public void MarkNextStrikeForTeam(NPC npc, Team team)
    {
        NPC bossNpc = ResolveBossEntity(npc);
        TeamBossNPC bossData = bossNpc.GetGlobalNPC<TeamBossNPC>();

        bossData.pendingStrikeTeam = team;
        bossData.hasBeenHurtByTeam.Add(team);

        if (!bossData.teamLife.ContainsKey(team))
        {
            bossData.teamLife[team] = bossNpc.lifeMax;
        }

        bossData.isTeamLifeCacheDirty = true;
    }

    public void RebuildTeamLifeCacheIfNeeded(NPC npc)
    {
        NPC bossNpc = ResolveBossEntity(npc);
        TeamBossNPC bossData = bossNpc.GetGlobalNPC<TeamBossNPC>();

        if (!bossData.isTeamLifeCacheDirty)
        {
            return;
        }

        bossData.sortedTeamLifeCache.Clear();

        foreach (KeyValuePair<Team, int> kv in bossData.teamLife)
        {
            Team team = kv.Key;

            if (!bossData.hasBeenHurtByTeam.Contains(team))
            {
                continue;
            }

            bossData.sortedTeamLifeCache.Add(new TeamLifeEntry(team, kv.Value));
        }

        bossData.sortedTeamLifeCache.Sort(CompareTeamLifeDescending);
        bossData.isTeamLifeCacheDirty = false;
    }

    public static NPC ResolveBossEntity(NPC npc)
    {
        if (npc.realLife == -1)
        {
            return npc;
        }

        if (npc.realLife == npc.whoAmI)
        {
            return npc;
        }

        int index = npc.realLife;
        if (index < 0 || index >= Main.maxNPCs)
        {
            return npc;
        }

        NPC realLife = Main.npc[index];
        if (realLife == null)
        {
            return npc;
        }

        return realLife;
    }

    private void ApplyTeamDamage(NPC npc, int damageDone)
    {
        NPC bossNpc = ResolveBossEntity(npc);
        TeamBossNPC bossData = bossNpc.GetGlobalNPC<TeamBossNPC>();

        Team team = bossData.pendingStrikeTeam;
        bossData.pendingStrikeTeam = Team.None;

        if (team == Team.None)
        {
            return;
        }

        if (damageDone <= 0)
        {
            return;
        }

        bossData.hasBeenHurtByTeam.Add(team);

        int remainingLife;
        if (!bossData.teamLife.TryGetValue(team, out remainingLife))
        {
            remainingLife = bossNpc.lifeMax;
        }

        remainingLife -= damageDone;

        if (remainingLife < 0)
        {
            remainingLife = 0;
        }

        bossData.teamLife[team] = remainingLife;
        bossData.isTeamLifeCacheDirty = true;
    }

    private static int CompareTeamLifeDescending(TeamLifeEntry a, TeamLifeEntry b)
    {
        return b.Life.CompareTo(a.Life);
    }
}