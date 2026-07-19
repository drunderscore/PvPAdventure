//using Microsoft.Xna.Framework;
//using PvPAdventure.Common.Game;
//using PvPAdventure.Common.Game.StatTrackers;
//using PvPAdventure.Common.Statistics;
//using PvPAdventure.Common.Travel.Beds;
//using PvPAdventure.Content.Portals;
//using PvPAdventure.Content.NPCs;
//using Terraria;
//using Terraria.Enums;
//using Terraria.ModLoader;

//namespace PvPAdventure.Common;

//internal sealed class PvPFrameworkAdventureIntegration : ModSystem
//{
//    public override void Load()
//    {
//        SpawnBoxSystem.CanExitProvider = () => ModContent.GetInstance<GameManager>().CurrentPhase == GameManager.Phase.Playing;
//        TeamOwnedTownNPC.AdditionalAppliesTo = npc => npc.ModNPC is BoundNPC;
//        BedOutlineTile.TeamResolver = ResolveBedTeam;
//        TeamBossNPC.NpcKilledByPlayer += AwardNpcKill;
//        TeamBossNPC.BossDamageDealt += RecordBossDamage;
//        ProjectileOutlineBanlist.Ban(ModContent.ProjectileType<PortalCreationProjectile>());
//    }

//    public override void Unload()
//    {
//        TeamBossNPC.NpcKilledByPlayer -= AwardNpcKill;
//        TeamBossNPC.BossDamageDealt -= RecordBossDamage;
//        ProjectileOutlineBanlist.Unban(ModContent.ProjectileType<PortalCreationProjectile>());
//    }

//    private static Team? ResolveBedTeam(Point point) =>
//        ModContent.GetInstance<TeamBedSystem>().TryGetTeam(point, out Team team) ? team : null;

//    private static void AwardNpcKill(Player player, NPC npc) =>
//        ModContent.GetInstance<PointsManager>().AwardNpcKillToTeam((Team)player.team, npc);

//    private static void RecordBossDamage(Player player, uint damage, int itemType) =>
//        MatchStatsPlayer.RecordServerStat(player, MatchStatKey.BossDamageDealt, damage, itemType);
//}

//internal sealed class AdventureMatchPlayer : ModPlayer
//{
//    public override void PostHurt(Player.HurtInfo info) => DamageTracker.RecordPostHurt(Player, info);

//    public override void Kill(double damage, int hitDirection, bool pvp, Terraria.DataStructures.PlayerDeathReason damageSource)
//    {
//        if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient ||
//            ModContent.GetInstance<GameManager>().CurrentPhase != GameManager.Phase.Playing)
//            return;

//        int killerId = damageSource.SourcePlayerIndex;
//        if (killerId < 0 || killerId >= Main.maxPlayers || killerId == Player.whoAmI)
//            killerId = Player.GetModPlayer<RecentDamagePlayer>().Attacker ?? -1;

//        if (killerId < 0 || killerId >= Main.maxPlayers || killerId == Player.whoAmI)
//            return;

//        Player killer = Main.player[killerId];
//        if (killer?.active == true)
//            ModContent.GetInstance<PointsManager>().AwardPlayerKillToTeam(killer, Player);
//    }
//}
