using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Helpers;

public static class Ass
{
    // Spawn selector
    public static Asset<Texture2D> Question_Mark;
    public static Asset<Texture2D>[] MapBG;
    public static Asset<Texture2D> CustomPlayerBackground;
    public static Asset<Texture2D> Spawnbox;
    public static Asset<Texture2D> Stop_Icon;

    // Admin tools
    public static Asset<Texture2D> TeamAssignerIcon;
    public static Asset<Texture2D> Pause;
    public static Asset<Texture2D> Play;
    public static Asset<Texture2D> Reset;
    public static Asset<Texture2D> Slider;
    public static Asset<Texture2D> SliderHighlight;
    public static Asset<Texture2D> SliderGradient;

    // Healthbar
    public static Asset<Texture2D> HP_Fancy; // visual placeholder only
    public static Asset<Texture2D> HP_Golden;
    public static Asset<Texture2D> HP_Golden_Left;
    public static Asset<Texture2D> HP_Golden_Mid;
    public static Asset<Texture2D> HP_Leaf;
    public static Asset<Texture2D> HP_Leaf_Left;
    public static Asset<Texture2D> HP_Leaf_Mid;

    // Temp assets with incorrect dimensions
    public static Asset<Texture2D> HP_Retro;
    public static Asset<Texture2D> HP_Sticks;
    public static Asset<Texture2D> HP_StoneGold;
    public static Asset<Texture2D> HP_Tribute;
    public static Asset<Texture2D> HP_TwigLeaf;
    public static Asset<Texture2D> HP_Valkyrie;

    // Temp vanilla health bar
    public static Asset<Texture2D> HPVanillaConfigItem;

    public static bool Initialized { get; set; }

    static Ass()
    {
        // Load MapBG
        MapBG = new Asset<Texture2D>[42];
        for (int i = 1; i <= 42; i++)
        {
            MapBG[i - 1] = ModContent.Request<Texture2D>(
                $"PvPAdventure/Assets/Ass/MapBG{i}",
                AssetRequestMode.AsyncLoad);
        }

        // Load Ass folder
        foreach (FieldInfo f in typeof(Ass).GetFields())
        {
            if (f.FieldType == typeof(Asset<Texture2D>))
            {
                var asset = ModContent.Request<Texture2D>(
                    $"PvPAdventure/Assets/Ass/{f.Name}",
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
