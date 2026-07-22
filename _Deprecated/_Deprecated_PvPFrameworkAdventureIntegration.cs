//using Microsoft.Xna.Framework;
//using PvPAdventure.Common.Game;
//using PvPAdventure.Common.Game.StatTrackers;
//using PvPAdventure.Common.Statistics;
//using PvPAdventure.Common.Travel.Beds;
//using PvPAdventure.Common.World.Outlines.TileOutlines;
//using PvPAdventure.Content.NPCs;
//using PvPAdventure.Content.Portals;
//using PvPFramework.Common.Combat.TeamBoss;
//using PvPFramework.Common.NPCs;
//using PvPFramework.Common.Spawnbox;
//using PvPFramework.Common.Visualization.ProjectileOutlines;
//using PvPFramework.Common.World.Outlines.TileOutlines;
//using Terraria;
//using Terraria.Enums;
//using Terraria.ID;
//using Terraria.ModLoader;

//namespace PvPAdventure.Common;

//internal sealed class PvPFrameworkAdventureIntegration : ModSystem
//{
//    public override void Load()
//    {
//        SpawnBoxSystem.CanExitProvider = () => ModContent.GetInstance<GameManager>().CurrentPhase == GameManager.Phase.Playing;
//        TeamOwnedTownNPC.AdditionalAppliesTo = npc => npc.ModNPC is BoundNPC;
//        BedOutlineTile.TeamResolver = ResolveBedTeam;
//        TeamBossNPC.ProjectileSourceItemResolver = ResolveSourceItem;
//        TeamBossNPC.NpcKilledByPlayer += AwardNpcKill;
//        TeamBossNPC.BossDamageDealt += RecordBossDamage;
//        ProjectileOutlineBanlist.Ban(ModContent.ProjectileType<PortalCreationProjectile>());
//    }

//    public override void Unload()
//    {
//        TeamBossNPC.ProjectileSourceItemResolver = null;
//        TeamBossNPC.NpcKilledByPlayer -= AwardNpcKill;
//        TeamBossNPC.BossDamageDealt -= RecordBossDamage;
//        ProjectileOutlineBanlist.Unban(ModContent.ProjectileType<PortalCreationProjectile>());
//    }

//    private static Team? ResolveBedTeam(Point point) =>
//        ModContent.GetInstance<TeamBedSystem>().TryGetTeam(point, out Team team) ? team : null;
//    private static int ResolveSourceItem(Projectile projectile) =>
//        projectile.GetGlobalProjectile<StatisticsProjectile>().SourceItem?.type ?? ItemID.None;
//    private static void AwardNpcKill(Player player, NPC npc) =>
//        ModContent.GetInstance<PointsManager>().AwardNpcKillToTeam((Team)player.team, npc);
//    private static void RecordBossDamage(Player player, uint damage, int itemType) =>
//        MatchStatsPlayer.RecordServerStat(player, MatchStatKey.BossDamageDealt, damage, itemType);
//}
