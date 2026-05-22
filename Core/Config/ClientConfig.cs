using PvPAdventure.Core.Config.ConfigElements;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PvPAdventure.Common.Travel.UI;
using System;
using System.ComponentModel;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace PvPAdventure.Core.Config;

public class ClientConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;

    public enum TravelUIPosition
    {
        Top,
        Bottom,
    }

    public enum TravelUISize
    {
        VerySmall,
        Small,
        Medium,
        Big,
    }

    [Header("Visualization")]
    [HeaderIcon(nameof(Ass.ConfigPlayerOutline))]

    [BackgroundColor(126, 62, 88)]
    [Expand(false, false)]
    public OutlinesConfig Outlines = new();

    [BackgroundColor(126, 62, 88)]
    [DefaultValue(false)] 
    [ConfigIcon(ItemID.PlumbersShirt)]
    public bool ShowVanityVisuals = false;

    [Header("Movement")]
    [HeaderIcon(ItemID.HermesBoots)]
    [ConfigIcon(nameof(Ass.ConfigDash))]
    [BackgroundColor(72, 108, 74)]
    [DefaultValue(true)] public bool IsVanillaDashEnabled = true;

    [Header("UI")]
    [BackgroundColor(36, 104, 118)]
    [DefaultValue(TravelUIPosition.Top)]
    [JsonConverter(typeof(StringEnumConverter))]
    public TravelUIPosition PortalTravelUIPosition = TravelUIPosition.Top;

    [BackgroundColor(36, 104, 118)]
    [DefaultValue(TravelUISize.Small)]
    [JsonConverter(typeof(StringEnumConverter))]
    public TravelUISize PortalTravelUISize = TravelUISize.Small;

    [Header("Sound")]
    [HeaderIcon(ItemID.FairyBell)]
    [Expand(false, false)]
    [BackgroundColor(138, 70, 126)]
    public SoundEffectConfig SoundEffect = new();

    [Header("Chat")]
    [HeaderIcon(nameof(Ass.ConfigChat))]

    [BackgroundColor(70, 92, 126)]
    [DefaultValue(true)]
    public bool ShowTeleportPlayerMessages = true;

    [BackgroundColor(70, 92, 126)]
    [DefaultValue(false)]
    public bool ShowDebugMessages = false;

    #region NestedConfigTypes
    public class OutlinesConfig
    {
        [ConfigIcon(nameof(Ass.IconCheckGreen), nameof(Ass.IconXGray), grayWhenOff: true)]
        [BackgroundColor(126, 62, 88)]
        [DefaultValue(true)]
        public bool DrawOutlines = true;

        [RequiresField(nameof(DrawOutlines))]
        [ConfigIcon(nameof(Ass.ConfigPlayerOutline), nameof(Ass.ConfigPlayerHead))]
        [BackgroundColor(126, 62, 88)]
        [DefaultValue(true)]
        public bool PlayerOutlines = true;

        [RequiresField(nameof(DrawOutlines))]
        [ConfigIcon(nameof(Ass.ConfigBoundNPCOutline), nameof(Ass.ConfigBoundNPC))]
        [BackgroundColor(126, 62, 88)]
        [DefaultValue(true)]
        public bool TownNPCOutlines = true;

        [RequiresField(nameof(DrawOutlines))]
        [ConfigIcon(nameof(Ass.ConfigBedOutline), nameof(Ass.ConfigBed))]
        [BackgroundColor(126, 62, 88)]
        [DefaultValue(true)]
        public bool BedOutlines = true;

        [RequiresField(nameof(DrawOutlines))]
        [ConfigIcon(nameof(Ass.ConfigTreasureBagOutline), nameof(Ass.ConfigTreasureBag))]
        [BackgroundColor(126, 62, 88)]
        [DefaultValue(true)]
        public bool TreasureBagOutlines = true;

        [RequiresField(nameof(DrawOutlines))]
        [ConfigIcon(nameof(Ass.ConfigProjectileOutline), nameof(Ass.ConfigProjectile))]
        [BackgroundColor(126, 62, 88)]
        [DefaultValue(true)]
        public bool ProjectileOutlines = true;
    }

    public class SoundEffectConfig
    {
        public abstract class MarkerConfig<TEnum>
        {
            public const int HitMarkerMinimumDamage = 10;
            public const int HitMarkerMaximumDamage = 200;

            public TEnum Sound;

            [DefaultValue(1.0f)] public float Volume = 1.0f;

            // FIXME: Description number should come from constant
            [Description("Desired pitch when dealing minimum damage (<=10)")]
            [Range(-1.0f, 1.0f)]
            [DefaultValue(0.75f)]
            public float PitchMinimum = 0.75f;

            // FIXME: Description number should come from constant
            [Description("Desired pitch when dealing maximum damage (>=200)")]
            [Range(-1.0f, 1.0f)]
            [DefaultValue(-0.75f)]
            public float PitchMaximum = -0.75f;

            [JsonIgnore] public abstract string SoundPath { get; }

            private float CalculatePitch(int damage) => ((float)damage).Remap(
                HitMarkerMinimumDamage,
                HitMarkerMaximumDamage,
                PitchMinimum,
                PitchMaximum
            );

            public SoundStyle Create(int damage) => new(SoundPath)
            {
                MaxInstances = 0,
                Volume = Volume,
                Pitch = CalculatePitch(damage)
            };
        }

        public class HitMarkerConfig : MarkerConfig<HitMarkerConfig.Hitsound>
        {
            public enum Hitsound
            {
                OlBetsy,
                Buwee,
                Blip,
                Crash,
                Squelchy,
                MarrowMurder,
                Part1
            }

            public override string SoundPath =>
                $"Terraria/Sounds/{Sound switch
                {
                    Hitsound.OlBetsy => "Item_178",
                    Hitsound.Buwee => "Item_150",
                    Hitsound.Blip => "Item_85",
                    Hitsound.Crash => "Item_144",
                    Hitsound.Squelchy => "NPC_Hit_19",
                    Hitsound.MarrowMurder => "Custom/dd2_skeleton_hurt_2",
                    Hitsound.Part1 => "Item_16",
                    _ => throw new ArgumentOutOfRangeException(nameof(Sound))
                }}";
        }

        public class KillMarkerConfig : MarkerConfig<KillMarkerConfig.Killsound>
        {
            public enum Killsound
            {
                Zacharry,
                Ronaldoz,
                Sharkron,
                Shronker,
                FatalCarCrash,
                Part2
            }

            public override string SoundPath =>
                $"Terraria/Sounds/{Sound switch
                {
                    Killsound.Zacharry => "Thunder_6",
                    Killsound.Ronaldoz => "Item_116",
                    Killsound.Sharkron => "Item_84",
                    Killsound.Shronker => "Item_61",
                    Killsound.FatalCarCrash => "Custom/dd2_kobold_death_1",
                    Killsound.Part2 => "Item_16",
                    _ => throw new ArgumentOutOfRangeException(nameof(Sound))
                }}";
        }

        public class PlayerHitMarkerConfig : HitMarkerConfig
        {
            public bool SilenceVanilla;
        }

        public class PlayerKillMarkerConfig : KillMarkerConfig
        {
            public bool SilenceVanilla;
        }

        [DefaultValue(null)][NullAllowed] public HitMarkerConfig NpcHitMarker;
        [DefaultValue(null)][NullAllowed] public PlayerHitMarkerConfig PlayerHitMarker;
        [DefaultValue(null)][NullAllowed] public PlayerKillMarkerConfig PlayerKillMarker;

    }
    #endregion

    #region Methods
    public override void OnChanged()
    {
        base.OnChanged();
        Log.Chat("Client config changed");

        // Rebuild travel UI
        var travelUISystem = ModContent.GetInstance<TravelUISystem>();
        if (travelUISystem != null)
        {
            travelUISystem?.travelUIState?.ForceRebuildNextUpdate();
        }
    }
    #endregion
}
