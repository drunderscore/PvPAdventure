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
    public static Asset<Texture2D> Icon_Portal; // 32x32
    public static Asset<Texture2D> Icon_Portal2; // 28x38
    public static Asset<Texture2D> Icon_Portal3; // 28x24 (compact)
    public static Asset<Texture2D> Icon_PortalMinimap; // 28x30
    public static Asset<Texture2D> Portal; // 36x368 (vertical spritesheet with 8 sprites)
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
    public static Asset<Texture2D> Icon_Arenas; // 50x48
    public static Asset<Texture2D> Icon_Arenas_Highlighted; // 54 x 52

    // Spectate
    public static Asset<Texture2D> BG_Shimmer; 
    public static Asset<Texture2D> BG_Biome;
    public static Asset<Texture2D> BG_WorldInfo;
    public static Asset<Texture2D> Shimmer; // 32x32
    public static Asset<Texture2D> Biome; // 32x32

    public static Asset<Texture2D> HeldItem; // 32x32
    public static Asset<Texture2D> ButtonTeleport; // 32x32
    public static Asset<Texture2D> ButtonEye; // 32x32
    public static Asset<Texture2D> Icon_Player; // 32x32
    public static Asset<Texture2D> Icon_PlayerHead; // 32x32
    public static Asset<Texture2D> Icon_World; // 32x32
    public static Asset<Texture2D> Icon_Eye; // 32x32
    public static Asset<Texture2D> Icon_Eye2; // 32x32
    public static Asset<Texture2D> Icon_Eye3; // 32x32
    public static Asset<Texture2D> Icon_NPC; // 32x32
    public static Asset<Texture2D> List; // 32x32
    public static Asset<Texture2D> Grid; // 32x32
    public static Asset<Texture2D> Sort; // 32x32
    public static Asset<Texture2D> MinionCount; // 32x32
    public static Asset<Texture2D> Distance; // 32x32
    public static Asset<Texture2D> Ping; // 32x32
    public static Asset<Texture2D> Time; // 32x32
    public static Asset<Texture2D> PvE; // 32x32
    public static Asset<Texture2D> PvP; // 32x32
    public static Asset<Texture2D> BossDamage; // 32x32
    public static Asset<Texture2D> InventoryCount; // 32x32

    // Match history
    public static Asset<Texture2D> Icon_Trophy; // 48x48
    public static Asset<Texture2D> Icon_TeamBoss; // 54x54
    public static Asset<Texture2D> Icon_Attack; // 26x27
    public static Asset<Texture2D> Icon_Medal1; // 46x60
    public static Asset<Texture2D> Icon_Medal2; // 46x60
    public static Asset<Texture2D> Icon_Medal3; // 46x60

    // Achievements
    public static Asset<Texture2D> Achievements; /// Spritesheet, that includes ALL vanilla and TPVPA achievements.

    // Shop
    public static Asset<Texture2D> Icon_Gem;

    // Main menu TPVPA state assets
    public static Asset<Texture2D> MenuIconBackground; 
    public static Asset<Texture2D> Icon_PlayMenu;
    public static Asset<Texture2D> Icon_Achievements;
    public static Asset<Texture2D> Icon_Leaderboards;
    public static Asset<Texture2D> Icon_MatchHistory;
    public static Asset<Texture2D> Icon_More;
    public static Asset<Texture2D> Icon_Shop;
    public static Asset<Texture2D> Icon_Stats;

    // Main menu shop
    public static Asset<Texture2D> Icon_CheckmarkGreen; // for collecting achievements rewards
    public static Asset<Texture2D> Icon_CheckmarkGray;
    public static Asset<Texture2D> Icon_CheckmarkGrayBox;
    public static Asset<Texture2D> Icon_CheckmarkGreenBox;

    // PvP players crossing swords assets
    public static Asset<Texture2D> Icon_PvPBalancing;
    public static Asset<Texture2D> Icon_PvPBalancingv2;

    // Shop item skins, sorted alphabetically
    public static Asset<Texture2D> AdventureMirrorShimmer;
    public static Asset<Texture2D> InfluxWaverCyberblade;
    public static Asset<Texture2D> LightDisc;
    public static Asset<Texture2D> StaffOfEarthAvalancheStaff;
    public static Asset<Texture2D> StaffOfEarthAvalancheStaffProjectile;
    public static Asset<Texture2D> SniperRifleBlue;
    public static Asset<Texture2D> SniperRifleGreen;
    public static Asset<Texture2D> SniperRiflePink;
    public static Asset<Texture2D> SniperRifleRed;
    public static Asset<Texture2D> SniperRifleYellow;
    public static Asset<Texture2D> TrueExcaliburBlossomHaze;
    //public static Asset<Texture2D> VolcanoMolten;

    // Projectile skins
    public static Asset<Texture2D> InfluxWaverCyberbladeProjectile; // projectile texture

    // Main menu server list
    public static Asset<Texture2D> Button;
    public static Asset<Texture2D> Button_Border;
    public static Asset<Texture2D> Button_Small_Border;
    public static Asset<Texture2D> Button_Small;

    // Unused
    public static Asset<Texture2D> DaedalusStormbowMolten;
    public static Asset<Texture2D> DartRifleMolten;
    public static Asset<Texture2D> FlamelashCryolash;
    public static Asset<Texture2D> HeatRayGammaRay;
    public static Asset<Texture2D> LaserMachineGunMolten;
    public static Asset<Texture2D> LightDiscMolten;
    public static Asset<Texture2D> PaladinsHammerHallowed;
    public static Asset<Texture2D> RainbowRodMolten;
    public static Asset<Texture2D> ShadowbeamStaffMolten;
    public static Asset<Texture2D> ShadowJoustingLanceMoltenJoustingLance;
    public static Asset<Texture2D> StarfuryMolten;
    public static Asset<Texture2D> StyngerMolten;
    public static Asset<Texture2D> ThornChakramMolten;
    public static Asset<Texture2D> TrueExcaliburMolten;

    /// --- Special Initialization flag, do not touch ---
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

        // Initialize Assets/Custom/MapBGs
        MapBG = new Asset<Texture2D>[42];
        for (int i = 1; i <= 42; i++)
            MapBG[i - 1] = ModContent.Request<Texture2D>($"PvPAdventure/Assets/Custom/MapBGs/MapBG{i}", AssetRequestMode.AsyncLoad);

        // Initialize Assets/Custom and Assets/Shop
        var fields = typeof(Ass).GetFields(BindingFlags.Public | BindingFlags.Static);
        foreach (FieldInfo f in fields)
        {
            if (f.FieldType != typeof(Asset<Texture2D>))
                continue;

            string[] folders = ["Custom", "Shop"];
            foreach (string folder in folders)
            {
                string path = $"PvPAdventure/Assets/{folder}/{f.Name}";
                if (ModContent.HasAsset(path))
                {
                    f.SetValue(null, ModContent.Request<Texture2D>(path, AssetRequestMode.AsyncLoad));
                    break;
                }
            }
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
