using System;
using System.ComponentModel;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using PvPAdventure.Common.Config.ConfigElements;
using PvPAdventure.Core.Spectate;
using Terraria.Audio;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace PvPAdventure;

public class AdventureClientConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Header("General")]
    [BackgroundColor(50, 70, 120)]
    [DefaultValue(true)] public bool ShiftEnterOpensAllChat;

    [BackgroundColor(50, 70, 120)]
    [DefaultValue(true)] public bool IsVanillaDashEnabled;

    [BackgroundColor(50, 70, 120)]
    [Expand(false, false)]
    public PlayerOutlineConfig PlayerOutline = new();

    [Header("SoundEffects")]
    [BackgroundColor(90, 50, 130)]
    [Expand(false,false)]
    public SoundEffectConfig SoundEffect = new();

    [Header("AdventureMirror")]
    [BackgroundColor(30, 90, 90)]
    [DefaultValue(true)] public bool OpenMapAfterRecall;

    [BackgroundColor(30, 90, 90)]
    [DefaultValue(true)] public bool ShowPopupText;
    [BackgroundColor(30, 90, 90)]
    [DefaultValue(true)] public bool PlaySound;

    [Header("Spectate")]
    [BackgroundColor(200, 30, 30, 150)]
    [DefaultValue(true)] public bool SpectateTeammatesOnDeath;

    [DefaultValue(typeof(Vector2), "0.5,0.9")]
    [BackgroundColor(200, 30, 30, 150)]
    [CustomModConfigItem(typeof(SpectateUIPositionConfigElement))]
    public Vector2 SpectateUIPosition = new(0.5f,0.9f);


    #region Configs
    public class PlayerOutlineConfig
    {
        public bool Self = true;
        public bool Team = true;
    }
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
        var ss = ModContent.GetInstance<SpectateSystem>();
        if (ss != null)
        {
            ss.spectateElement._hAlign = SpectateUIPosition.X;
            ss.spectateElement._vAlign = SpectateUIPosition.Y;
        }
    }
    #endregion
}
