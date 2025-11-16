using System;
using System.ComponentModel;
using Newtonsoft.Json;
using Terraria.Audio;
using Terraria.ModLoader.Config;

namespace PvPAdventure;

public class AdventureClientConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;

    public PlayerOutlineConfig PlayerOutline { get; set; } = new();
    public bool ShiftEnterOpensAllChat { get; set; }

    [DefaultValue(true)]
    public bool IsVanillaDashEnabled { get; set; }
    [DefaultValue(true)] public bool ShowPauseMessage { get; set; }

    public class PlayerOutlineConfig
    {
        public bool Self { get; set; } = true;
        public bool Team { get; set; } = true;
    }

    public class SoundEffectConfig
    {
        public abstract class MarkerConfig<TEnum>
        {
            public const int HitMarkerMinimumDamage = 10;
            public const int HitMarkerMaximumDamage = 200;

            public TEnum Sound { get; set; }

            [DefaultValue(1.0f)] public float Volume { get; set; }

            // FIXME: Description number should come from constant
            [Description("Desired pitch when dealing minimum damage (<=10)")]
            [Range(-1.0f, 1.0f)]
            [DefaultValue(0.75f)]
            public float PitchMinimum { get; set; }

            // FIXME: Description number should come from constant
            [Description("Desired pitch when dealing maximum damage (>=200)")]
            [Range(-1.0f, 1.0f)]
            [DefaultValue(-0.75f)]
            public float PitchMaximum { get; set; }

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
            public bool SilenceVanilla { get; set; }
        }

        public class PlayerKillMarkerConfig : KillMarkerConfig
        {
            public bool SilenceVanilla { get; set; }
        }

        [DefaultValue(null)] [NullAllowed] public HitMarkerConfig NpcHitMarker { get; set; }
        [DefaultValue(null)] [NullAllowed] public PlayerHitMarkerConfig PlayerHitMarker { get; set; }
        [DefaultValue(null)] [NullAllowed] public PlayerKillMarkerConfig PlayerKillMarker { get; set; }
    }

    public SoundEffectConfig SoundEffect { get; set; } = new();
}