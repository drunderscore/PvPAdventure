using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Helpers;

public static class Ass
{
    public static Asset<Texture2D> Question_Mark;
    public static Asset<Texture2D>[] MapBG;
    public static Asset<Texture2D> Arrow;
    public static Asset<Texture2D> CustomPlayerBackground;
    public static Asset<Texture2D> Spawnbox;
    public static Asset<Texture2D> Pause;

    public static bool Initialized { get; set; }

    static Ass()
    {
        // Load MapBG1..MapBG42
        MapBG = new Asset<Texture2D>[42];
        for (int i = 1; i <= 42; i++)
        {
            MapBG[i - 1] = ModContent.Request<Texture2D>(
                $"PvPAdventure/Assets/SpawnSelector/MapBG{i}",
                AssetRequestMode.AsyncLoad);
        }

        // Load single texture fields (NOT MapBG array)
        foreach (FieldInfo f in typeof(Ass).GetFields())
        {
            if (f.FieldType == typeof(Asset<Texture2D>))
            {
                var asset = ModContent.Request<Texture2D>(
                    $"PvPAdventure/Assets/SpawnSelector/{f.Name}",
                    AssetRequestMode.AsyncLoad);
                f.SetValue(null, asset);
            }
        }
    }
}

public class LoadAssets : ModSystem
{
    public override void Load() => _ = Ass.Initialized;
}
