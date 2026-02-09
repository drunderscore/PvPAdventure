using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Utilities;

/// <summary>
/// Provides static access to miscallaneous texture assets within the PvPAdventure mod.
/// Automatically initializes when the mod system loads.
/// All asset fields are intended for global access throughout the mod.
/// </summary>
public static class Ass
{
    // Spawn selector assets
    public static Asset<Texture2D> CustomPlayerBackground;
    public static Asset<Texture2D>[] MapBG;
    public static Asset<Texture2D> Icon_Dead; // 32x32
    public static Asset<Texture2D> Icon_Forbidden;
    public static Asset<Texture2D> Icon_Question_Mark;
    public static Asset<Texture2D> Spawnbox;

    // Admin tools assets
    public static Asset<Texture2D> Icon_Reset;
    public static Asset<Texture2D> Icon_Resize;
    public static Asset<Texture2D> Slider;
    public static Asset<Texture2D> SliderHighlight;
    public static Asset<Texture2D> SliderGradient;

    // Admin tool icons
    public static Asset<Texture2D> Icon_StartGame;
    public static Asset<Texture2D> Icon_PauseGame;
    public static Asset<Texture2D> Icon_EndGame;
    public static Asset<Texture2D> Icon_TeamAssigner;
    public static Asset<Texture2D> Icon_PointsSetter;
    public static Asset<Texture2D> Icon_AdminManager;
    public static Asset<Texture2D> Icon_ConfigOpen;
    public static Asset<Texture2D> Icon_ConfigClose;

    // On/off icons for admin manager
    public static Asset<Texture2D> Icon_On;
    public static Asset<Texture2D> Icon_On_Hover;
    public static Asset<Texture2D> Icon_Off;
    public static Asset<Texture2D> Icon_Off_Hover;

    // Arenas
    public static Asset<Texture2D> Icon_Arenas;

    // Matchmaking
    public static Asset<Texture2D> Button;
    public static Asset<Texture2D> Button_Small;
    public static Asset<Texture2D> Button_Border;
    public static Asset<Texture2D> Button_Small_Border;

    // Match history
    public static Asset<Texture2D> Icon_Trophy; // 48x48
    public static Asset<Texture2D> Icon_TeamBoss; // 54x54
    public static Asset<Texture2D> Icon_Attack; // 26x27
    public static Asset<Texture2D> Icon_Gold; // 64x64
    public static Asset<Texture2D> Icon_Silver; // 64x64
    public static Asset<Texture2D> Icon_Bronze; // 64x64
    public static Asset<Texture2D> Icon_Medal1; // 64x64
    public static Asset<Texture2D> Icon_Medal2; // 64x64
    public static Asset<Texture2D> Icon_Medal3; // 64x64

    // Initialization flag
    public static bool Initialized { get; set; }

    /// <summary>
    /// Initializes static assets
    /// Automatically runs once the mod system loads via <see cref="AssetLoader"/>
    /// </summary>
    static Ass()
    {
        // Load MapBGs
        MapBG = new Asset<Texture2D>[42];
        for (int i = 1; i <= 42; i++)
        {
            MapBG[i - 1] = ModContent.Request<Texture2D>(
                $"PvPAdventure/Assets/Custom/MapBGs/MapBG{i}",
                AssetRequestMode.AsyncLoad);
        }

        // Load all assets from Assets/Custom
        foreach (FieldInfo f in typeof(Ass).GetFields())
        {
            if (f.FieldType == typeof(Asset<Texture2D>))
            {
                var asset = ModContent.Request<Texture2D>(
                    $"PvPAdventure/Assets/Custom/{f.Name}",
                    AssetRequestMode.AsyncLoad);
                f.SetValue(null, asset);
            }
        }
    }
}

/// <summary>
/// Initializes asset loading for the mod when the system is loaded with all assets in <see cref="Ass"/>
/// </summary>
public class AssetLoader : ModSystem
{
    public override void Load() => _ = Ass.Initialized;
}
