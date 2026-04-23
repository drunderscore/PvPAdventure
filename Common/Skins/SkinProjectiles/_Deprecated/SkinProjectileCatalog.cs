//using Microsoft.Xna.Framework.Graphics;
//using PvPAdventure.Common.MainMenu.Shop;
//using PvPAdventure.Core.Utilities;
//using ReLogic.Content;
//using System.Collections.Generic;
//using Terraria.ID;
//using Terraria.ModLoader;

//namespace PvPAdventure.Common.Skins.SkinProjectiles;

//[Autoload(Side = ModSide.Client)]
//internal static class SkinProjectileCatalog
//{
//    private readonly record struct Key(SkinIdentity Identity, int ProjType);

//    private static readonly Dictionary<Key, Asset<Texture2D>> byKey = Build();

//    public static bool TryGet(SkinIdentity identity, int projType, out Asset<Texture2D> asset)
//    {
//        return byKey.TryGetValue(new Key(identity, projType), out asset);
//    }

//    private static Dictionary<Key, Asset<Texture2D>> Build()
//    {
//        Dictionary<Key, Asset<Texture2D>> dict = [];

//        dict.Add(new Key(new SkinIdentity("influx_waver", "cyberblade"), ProjectileID.InfluxWaver), Ass.InfluxWaverCyberbladeProjectile);
//        dict.Add(new Key(new SkinIdentity("staff_of_earth", "avalanche_staff"), ProjectileID.BoulderStaffOfEarth), Ass.StaffOfEarthAvalancheStaffProjectile);
//        dict.Add(new Key(new SkinIdentity("light_disc", "light_disc"), ProjectileID.LightDisc), Ass.LightDisc);

//        return dict;
//    }
//}