using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PvPAdventure.Common.Travel.UI;
using PvPAdventure.Core.Utilities;
using System;
using System.ComponentModel;
using Terraria.Audio;
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
    [BackgroundColor(50, 70, 120)]
    [DefaultValue(true)] 
    public bool PlayerOutlines = true;

    [BackgroundColor(50, 70, 120)]
    [DefaultValue(true)] 
    public bool BedOutlines = true;

    [BackgroundColor(50, 70, 120)]
    [DefaultValue(true)] 
    public bool LootOutlines = true;

    [BackgroundColor(50, 70, 120)]
    [DefaultValue(false)] 
    public bool ShowVanityVisuals = false;

    [BackgroundColor(50, 70, 120)]
    [DefaultValue(true)]
    public bool DrawSpectators = true;

    [Header("Movement")]
    [BackgroundColor(50, 70, 120)]
    [DefaultValue(true)] public bool IsVanillaDashEnabled;

    [Header("PortalTravel")]
    [BackgroundColor(30, 150, 150)]
    [DefaultValue(TravelUIPosition.Top)]
    [JsonConverter(typeof(StringEnumConverter))]
    public TravelUIPosition travelUIPosition;

    [BackgroundColor(30, 150, 150)]
    [DefaultValue(TravelUISize.Big)]
    [JsonConverter(typeof(StringEnumConverter))]
    public TravelUISize travelUISize;

    [Header("Sound")]
    [Expand(false, false)]
    [BackgroundColor(200, 80, 150)]
    public SoundEffectConfig SoundEffect = new();

    [Header("Chat")]
    [DefaultValue(true)] public bool ShowChatChannelPrefixes;
    [DefaultValue(true)] public bool ShowSavePlayerMessages;
    [DefaultValue(true)] public bool ShowTeleportPlayerMessages;
    [DefaultValue(false)] public bool ShowDebugMessages;

    #region NestedConfigTypes
    public class SoundEffectConfig
    {
        public abstract class MarkerConfig<TEnum>
        {
            public const int HitMarkerMinimumDamage = 10;
            public const int HitMarkerMaximumDamage = 200;

            public TEnum Sound;

            [DefaultValue(1.0f)] public float Volume;

            // FIXME: Description number should come from constant
            [Description("Desired pitch when dealing minimum damage (<=10)")]
            [Range(-1.0f, 1.0f)]
            [DefaultValue(0.75f)]
            public float PitchMinimum;

            // FIXME: Description number should come from constant
            [Description("Desired pitch when dealing maximum damage (>=200)")]
            [Range(-1.0f, 1.0f)]
            [DefaultValue(-0.75f)]
            public float PitchMaximum;

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

        var travelUISystem = ModContent.GetInstance<TravelUISystem>();
        if (travelUISystem != null)
        {
            travelUISystem?.travelUIState?.ForceRebuildNextUpdate();
        }
    }
    #endregion
}
