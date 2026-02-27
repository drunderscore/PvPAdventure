using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
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
    // Map backgrounds
    public static Asset<Texture2D>[] MapBG;

    // Spawn selector assets
    public static Asset<Texture2D> CustomPlayerBackground;
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
    public static Asset<Texture2D> Icon_Arenas_v2; // 50x48
    public static Asset<Texture2D> Icon_Arenas_v2_Highlighted; // 54 x 52

    // Matchmaking
    public static Asset<Texture2D> Button;
    public static Asset<Texture2D> Button_Small;
    public static Asset<Texture2D> Button_Border;
    public static Asset<Texture2D> Button_Small_Border;

    // Match history
    public static Asset<Texture2D> Icon_Trophy; // 48x48
    public static Asset<Texture2D> Icon_TeamBoss; // 54x54
    public static Asset<Texture2D> Icon_Attack; // 26x27
    public static Asset<Texture2D> Icon_Gold; // 74x102
    public static Asset<Texture2D> Icon_Silver; //74x102
    public static Asset<Texture2D> Icon_Bronze; // 74x102
    public static Asset<Texture2D> Icon_Medal1; // 64x64
    public static Asset<Texture2D> Icon_Medal2; // 64x64
    public static Asset<Texture2D> Icon_Medal3; // 64x64

    // Achievements
    public static Asset<Texture2D> Achievements; /// Spritesheet, that includes vanilla and TPVPA achievements.

    // Shop
    public static Asset<Texture2D> Icon_Gem;

    // Main menu TPVPA state assets
    public static Asset<Texture2D> MenuIconBackground; 
    public static Asset<Texture2D> Icon_PlayMenu;
    public static Asset<Texture2D> Icon_Achievements;
    public static Asset<Texture2D> Icon_MatchHistory;
    public static Asset<Texture2D> Icon_More;
    public static Asset<Texture2D> Icon_Shop;
    public static Asset<Texture2D> Icon_Stats;
    public static Asset<Texture2D> Icon_Checkmark; // for collecting achievements rewards

    // PvP players crossing swords assets
    public static Asset<Texture2D> Icon_PvPBalancing;
    public static Asset<Texture2D> Icon_PvPBalancingv2;

    // Shop assets
    public static Asset<Texture2D> PinkSniperRifle;
    public static Asset<Texture2D> RedSniperRifle;

    private static readonly HashSet<string> ShopFields =
    [
        nameof(PinkSniperRifle),
        nameof(RedSniperRifle),
    ];


    /// --- Special Initialization flag ---
    public static bool Initialized { get; set; }

    /// <summary>
    /// Initializes static assets
    /// Automatically runs once the mod system loads via <see cref="AssetLoader"/>
    /// </summary>
    static Ass()
    {
        if (Main.dedServ)
        {
            Initialized = true;
            return;
        }

        MapBG = new Asset<Texture2D>[42];
        for (int i = 1; i <= 42; i++)
            MapBG[i - 1] = ModContent.Request<Texture2D>($"PvPAdventure/Assets/Custom/MapBGs/MapBG{i}", AssetRequestMode.AsyncLoad);

        var fields = typeof(Ass).GetFields(BindingFlags.Public | BindingFlags.Static);

        for (int i = 0; i < fields.Length; i++)
        {
            FieldInfo f = fields[i];
            if (f.FieldType != typeof(Asset<Texture2D>))
                continue;

            if (ShopFields.Contains(f.Name))
                continue;

            f.SetValue(null, ModContent.Request<Texture2D>($"PvPAdventure/Assets/Custom/{f.Name}", AssetRequestMode.AsyncLoad));
        }

        for (int i = 0; i < fields.Length; i++)
        {
            FieldInfo f = fields[i];
            if (f.FieldType != typeof(Asset<Texture2D>))
                continue;

            if (!ShopFields.Contains(f.Name))
                continue;

            f.SetValue(null, ModContent.Request<Texture2D>($"PvPAdventure/Assets/Shop/{f.Name}", AssetRequestMode.AsyncLoad));
        }

        Initialized = true;
    }
}

/// <summary>
/// Initializes asset loading for the mod when the system is loaded with all assets in <see cref="Ass"/>
/// </summary>
public class AssetLoader : ModSystem
{
    public override void Load() => _ = Ass.Initialized;
}
