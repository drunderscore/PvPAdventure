//using System.Collections.Generic;
//using Terraria;
//using Terraria.ID;
//using Terraria.ModLoader;

//namespace PvPAdventure.Common.World.Outlines.ProjectileOutlines.Strategies;

//[Autoload(Side = ModSide.Client)]
//internal sealed class ProjectileRecolorRegistry : ModSystem
//{
//    private readonly Dictionary<int, IProjectileRecolorStrategy> byType = [];
//    private readonly Dictionary<int, IProjectileRecolorStrategy> byAiStyle = [];

//    public static ProjectileRecolorRegistry Instance
//        => ModContent.GetInstance<ProjectileRecolorRegistry>();

//    // Register by exact projectile type (e.g. ProjectileID.ShadowBeamFriendly)
//    public void RegisterType(int projectileType, IProjectileRecolorStrategy strategy)
//        => byType[projectileType] = strategy;

//    // Register by aiStyle — applies to ALL projectiles with that style
//    // (useful for e.g. all flamethrower variants sharing aiStyle 38)
//    public void RegisterAiStyle(int aiStyle, IProjectileRecolorStrategy strategy)
//        => byAiStyle[aiStyle] = strategy;

//    public bool TryGet(Projectile projectile, out IProjectileRecolorStrategy strategy)
//    {
//        // Exact type match takes priority over aiStyle
//        if (byType.TryGetValue(projectile.type, out strategy))
//            return true;

//        if (byAiStyle.TryGetValue(projectile.aiStyle, out strategy))
//            return true;

//        return false;
//    }

//    public override void Load()
//    {
//        // --- Dust-driven (PostAI) ---
//        RegisterType(ProjectileID.CursedFlameFriendly, new DustRecolorStrategy(dustTypes: [75], proximityRadius: 24f));
//        RegisterAiStyle(ProjAIStyleID.Flames, new DustRecolorStrategy(dustTypes: [6], proximityRadius: 20f));
//        RegisterType(ProjectileID.ShadowBeamFriendly, new ShadowBeamDustStrategy());

//        // --- Sprite-driven (GetAlpha) ---
//        RegisterType(ProjectileID.LostSoulFriendly, new SpriteRecolorStrategy()); // spectre staff

//        // --- Custom draw (PreDraw) ---
//        // Excalibur/TrueExcalibur are handled separately in their DrawProj_ methods
//        // via GetProjTeamColor — no strategy needed here unless you want to unify them
//    }
//}