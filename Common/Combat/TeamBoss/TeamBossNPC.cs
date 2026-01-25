using Microsoft.Xna.Framework;
using PvPAdventure.Common.Statistics;
using PvPAdventure.Core.Config;
using PvPAdventure.Core.Net;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Common.Combat.TeamBoss;

public sealed class TeamBossNPC : GlobalNPC
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
        On_NPC.PlayerInteraction += OnNPCPlayerInteraction;
        On_NPC.StrikeNPC_HitInfo_bool_bool += OnNPCStrikeNPC;
        On_NetMessage.SendStrikeNPC += OnNetMessageSendStrikeNPC;
    }

    public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
    {
        if (player == null || !player.active)
            return;

        Team team = (Team)player.team;
        if (team == Team.None)
            return;

        // Record attacker team for StrikeNPC (works for items in all modes).
        RecordHit(npc, player.whoAmI, team);
    }

    public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
    {
        if (projectile == null)
            return;

        int ownerIndex = projectile.owner;
        if (ownerIndex < 0 || ownerIndex >= Main.maxPlayers)
            return;

        Player player = Main.player[ownerIndex];
        if (player == null || !player.active)
            return;

        Team team = (Team)player.team;
        if (team == Team.None)
            return;

        // Record attacker team for StrikeNPC (works for projectiles in all modes).
        RecordHit(npc, ownerIndex, team);
    }

    public override void SetDefaults(NPC entity)
    {
        if (entity.isLikeATownNPC && entity.type != NPCID.Guide)
            entity.immortal = true;

        // Can't construct an NPCDefinition too early from int -- it may call GetName before lookups exist.
        if (!NPCID.Search.TryGetName(entity.type, out string name))
            return;

        var config = ModContent.GetInstance<ServerConfig>();
        var definition = new NPCDefinition(name);

        if (!config.BossBalance.TryGetValue(definition, out var entry))
            return;

        entity.lifeMax = (int)(entity.lifeMax * entry.LifeMaxMultiplier);
        entity.life = entity.lifeMax;
        entity.damage = (int)(entity.damage * entry.DamageMultiplier);
    }

    private static void OnNPCPlayerInteraction(On_NPC.orig_PlayerInteraction orig, NPC self, int player)
    {
        orig(self, player);

        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        if (player < 0 || player >= Main.maxPlayers)
            return;

        Player p = Main.player[player];
        if (p == null || !p.active)
            return;

        Team team = (Team)p.team;
        if (team == Team.None)
            return;

        // Ensures NPC.kill credit and team attribution for cases where interaction is the only "touch".
        RecordHit(self, player, team);
    }

    private static void RecordHit(NPC npc, int playerIndex, Team team)
    {
        // For segmented bosses, consolidate state on the owning NPC (realLife).
        NPC owner = GetOwner(npc);

        var ownerG = owner.GetGlobalNPC<TeamBossNPC>();
        ownerG.LastDamageFromPlayer = new DamageInfo((byte)playerIndex);
        ownerG._lastStrikeTeam = team;
        ownerG._hasBeenHurtByTeam.Add(team);

        if (npc.whoAmI != owner.whoAmI)
        {
            var segG = npc.GetGlobalNPC<TeamBossNPC>();
            segG.LastDamageFromPlayer = new DamageInfo((byte)playerIndex);
            segG._lastStrikeTeam = team;
            segG._hasBeenHurtByTeam.Add(team);
        }
    }

    public override void OnKill(NPC npc)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        // If this was a segment, attribute kill credit to the owner state.
        NPC owner = GetOwner(npc);
        var g = owner.GetGlobalNPC<TeamBossNPC>();

        if (g.LastDamageFromPlayer == null)
            return;

        Player lastDamager = Main.player[g.LastDamageFromPlayer.Who];
        if (lastDamager == null || !lastDamager.active)
            return;

        ModContent.GetInstance<PointsManager>().AwardNpcKillToTeam((Team)lastDamager.team, npc);
    }

    // FIXME: This only covers strikes (direct hits). DOTs/debuffs are not attributed.
    private int OnNPCStrikeNPC(
        On_NPC.orig_StrikeNPC_HitInfo_bool_bool orig,
        NPC self,
        NPC.HitInfo hit,
        bool fromNet,
        bool noPlayerInteraction)
    {
        NPC owner = GetOwner(self);
        var boss = owner.GetGlobalNPC<TeamBossNPC>();

        Team strikeTeam = boss._lastStrikeTeam;

        // Always suppress vanilla PvE combat text; we draw our own.
        hit.HideCombatText = true;

        if (!Main.dedServ && strikeTeam != Team.None)
        {
            CombatText.NewText(
                new Rectangle((int)self.position.X, (int)self.position.Y, self.width, self.height),
                Main.teamColor[(int)strikeTeam],
                hit.Damage,
                hit.Crit);
        }

        var config = ModContent.GetInstance<ServerConfig>();

        // Runtime is generally safe, but name-based keeps this consistent with SetDefaults safety constraints.
        if (!NPCID.Search.TryGetName(owner.type, out string ownerName))
            return orig(self, hit, fromNet, noPlayerInteraction);

        var definition = new NPCDefinition(ownerName);

        // Not configured: do vanilla damage/behavior, but still keep our custom combat text above.
        if (!config.BossBalance.TryGetValue(definition, out var balanceEntry))
            return orig(self, hit, fromNet, noPlayerInteraction);

        if (Main.netMode == NetmodeID.MultiplayerClient)
            return orig(self, hit, fromNet, noPlayerInteraction);

        // Only configured bosses participate in TeamLife.
        var teamLife = boss._teamLife;

        if (teamLife.Count == 0)
        {
            foreach (var team in Enum.GetValues<Team>())
            {
                if (team == Team.None)
                    continue;

                teamLife[team] = owner.lifeMax;
            }
        }

        if (strikeTeam == Team.None || !IsTeamActive(strikeTeam))
            return orig(self, hit, fromNet, noPlayerInteraction);

        int currentLife = owner.life;

        Team leadingTeam = Team.None;
        int leadingLife = int.MaxValue;

        foreach (var kv in teamLife)
        {
            Team t = kv.Key;
            if (t == Team.None || !IsTeamActive(t))
                continue;

            if (kv.Value < leadingLife)
            {
                leadingLife = kv.Value;
                leadingTeam = t;
            }
        }

        int strikerOld = teamLife[strikeTeam];
        int strikerNew = Math.Max(0, strikerOld - hit.Damage);
        teamLife[strikeTeam] = strikerNew;

        // Save/restore immortality, we temporarily flip it to block "real" HP changes.
        bool prevSelfImmortal = self.immortal;
        bool prevOwnerImmortal = owner.immortal;

        try
        {
            if (strikeTeam != leadingTeam)
            {
                hit.Damage = 0;
                self.immortal = true;
                owner.immortal = true;

                owner.netUpdate = true;
                return orig(self, hit, fromNet, noPlayerInteraction);
            }

            int allowed = Math.Max(0, currentLife - strikerNew);

            if (allowed <= 0)
            {
                hit.Damage = 0;
                self.immortal = true;
                owner.immortal = true;
            }
            else
            {
                hit.Damage = allowed;

                float share = balanceEntry.TeamLifeShare;

                foreach (var team in Enum.GetValues<Team>())
                {
                    if (team == Team.None || team == strikeTeam)
                        continue;

                    int reduced = teamLife[team] - (int)(allowed * share);
                    teamLife[team] = Math.Max(currentLife, reduced);
                }
            }

            owner.netUpdate = true;
            return orig(self, hit, fromNet, noPlayerInteraction);
        }
        finally
        {
            self.immortal = prevSelfImmortal;
            owner.immortal = prevOwnerImmortal;
        }
    }

    public override void ApplyDifficultyAndPlayerScaling(NPC npc, int numPlayers, float balance, float bossAdjustment)
    {
        NPC owner = GetOwner(npc);
        var boss = owner.GetGlobalNPC<TeamBossNPC>();

        if (boss._teamLife.Count > 0)
            return;

        var config = ModContent.GetInstance<ServerConfig>();

        if (!NPCID.Search.TryGetName(owner.type, out string ownerName))
            return;

        var definition = new NPCDefinition(ownerName);

        // Only configured bosses receive per-team pools.
        if (!config.BossBalance.ContainsKey(definition))
            return;

        foreach (var team in Enum.GetValues<Team>())
        {
            if (team == Team.None)
                continue;

            boss._teamLife[team] = owner.lifeMax;
        }

        owner.netUpdate = true;
    }

    private void OnNetMessageSendStrikeNPC(
        On_NetMessage.orig_SendStrikeNPC orig,
        NPC npc,
        ref NPC.HitInfo hit,
        int ignoreClient)
    {
        if (Main.netMode == NetmodeID.Server)
        {
            NPC owner = GetOwner(npc);
            var boss = owner.GetGlobalNPC<TeamBossNPC>();

            // FIXME: Proper packet format/versioning.
            var packet = Mod.GetPacket();
            packet.Write((byte)AdventurePacketIdentifier.NpcStrikeTeam);
            packet.Write((short)npc.whoAmI);
            packet.Write((byte)boss._lastStrikeTeam);
            packet.Send(ignoreClient: ignoreClient);
        }

        orig(npc, ref hit, ignoreClient);
    }

    public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        // FIXME: Might be costly if used on many NPCs.
        binaryWriter.Write((byte)_teamLife.Count);

        foreach (var (team, life) in _teamLife)
        {
            binaryWriter.Write((byte)team);
            binaryWriter.Write7BitEncodedInt(life);
        }
    }

    public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
    {
        // FIXME: Might be costly if used on many NPCs.
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

    private static NPC GetOwner(NPC npc)
    {
        if (npc.realLife != -1 && npc.realLife >= 0 && npc.realLife < Main.maxNPCs)
            return Main.npc[npc.realLife];

        return npc;
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

    public void MarkNextStrikeForTeam(NPC npc, Team team)
    {
        // Called from client packet handling to tag the next local strike color/team.
        _lastStrikeTeam = team;
        _hasBeenHurtByTeam.Add(team);

        NPC owner = GetOwner(npc);

        if (owner.whoAmI != npc.whoAmI)
        {
            var boss = owner.GetGlobalNPC<TeamBossNPC>();
            boss._lastStrikeTeam = team;
            boss._hasBeenHurtByTeam.Add(team);
        }
    }

    
}
