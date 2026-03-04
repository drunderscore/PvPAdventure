using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Skins;

[Autoload(Side = ModSide.Client)]
internal static class SkinProjectileCatalog
{
    private readonly record struct Key(string SkinId, int ProjType);

    private static readonly Dictionary<Key, Asset<Texture2D>> byKey = Build();

    public static bool TryGet(string skinId, int projType, out Asset<Texture2D> asset)
    {
        return byKey.TryGetValue(new Key(skinId, projType), out asset);
    }

    private static Dictionary<Key, Asset<Texture2D>> Build()
    {
        Dictionary<Key, Asset<Texture2D>> dict = [];

         dict.Add(new Key("influx_waver_cyberblade", ProjectileID.InfluxWaver), Ass.InfluxWaverCyberblade_Beam);

        return dict;
    }
}