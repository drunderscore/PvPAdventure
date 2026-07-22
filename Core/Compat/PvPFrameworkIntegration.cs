using Microsoft.Xna.Framework;
using PvPAdventure.Common.Game;
using PvPAdventure.Common.Statistics;
using PvPAdventure.Common.Travel.Beds;
using PvPFramework.Common.Visualization.TileOutlines;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Compat;

// Wires PvP Adventure's match rules into PvP Framework's shared systems.
public class PvPFrameworkIntegration : ModSystem
{
    public override void Load()
    {
        // Confine players to the spawn box until the match actually begins.
        // While Waiting (pre-game), CanExit is false and the spawn box border blocks movement out.
        PvPFramework.Common.Spawnbox.SpawnBoxSystem.CanExitProvider = () =>
            ModContent.GetInstance<GameManager>().CurrentPhase == GameManager.Phase.Playing;

        // PvP Framework draws bed outlines; Adventure supplies the synchronized team ownership.
        BedOutlineTile.TeamResolver = ResolveBedTeam;

        // Award team points when a team lands the killing blow on a boss/scoring NPC.
        PvPFramework.Common.Combat.TeamBoss.TeamBossNPC.NpcKilledByPlayer += AwardNpcKill;
    }

    public override void Unload()
    {
        PvPFramework.Common.Combat.TeamBoss.TeamBossNPC.NpcKilledByPlayer -= AwardNpcKill;
        BedOutlineTile.TeamResolver = null;
        // Drop our delegate so it doesn't retain a reference to an unloaded GameManager.
        PvPFramework.Common.Spawnbox.SpawnBoxSystem.CanExitProvider = static () => true;
    }

    private static Team? ResolveBedTeam(Point origin) =>
        ModContent.GetInstance<TeamBedSystem>().TryGetTeam(origin, out Team team) ? team : null;

    private static void AwardNpcKill(Player player, NPC npc) =>
        ModContent.GetInstance<PointsManager>().AwardNpcKillToTeam((Team)player.team, npc);
}

// Awards team points for PvP kills that happen during a match.
public class AdventureMatchPlayer : ModPlayer
{
    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        // Points are decided server-side, and only while a match is being played.
        if (Main.netMode == NetmodeID.MultiplayerClient ||
            ModContent.GetInstance<GameManager>().CurrentPhase != GameManager.Phase.Playing)
            return;

        // Prefer the direct killer; fall back to whoever most recently dealt PvP damage
        // (covers indirect kills like DoTs, knockback into hazards, etc.).
        int killerId = damageSource.SourcePlayerIndex;
        if (killerId < 0 || killerId >= Main.maxPlayers || killerId == Player.whoAmI)
            killerId = Player.GetModPlayer<PvPFramework.Common.Combat.RecentDamagePlayer>().Attacker ?? -1;

        if (killerId < 0 || killerId >= Main.maxPlayers || killerId == Player.whoAmI)
            return;

        Player killer = Main.player[killerId];
        if (killer?.active == true)
            ModContent.GetInstance<PointsManager>().AwardPlayerKillToTeam(killer, Player);
    }
}
