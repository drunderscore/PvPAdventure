using System.Collections.Generic;
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace PvPAdventure.Core.Config;

internal class SSCConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ServerSide;

    [Header("SSC")]
    [BackgroundColor(90, 40, 110)]
    [DefaultValue(true)]
    public bool IsSSCEnabled { get; set; } = true;

    [BackgroundColor(90, 40, 110)]
    [Expand(false)]
    public Dictionary<ItemDefinition, int> StartItems { get; set; } = [];

    [BackgroundColor(90, 40, 110)]
    [Slider]
    [Increment(20)]
    [Range(100, 500)]
    [DefaultValue(100)]
    public int StartLife { get; set; } = 200;

    [BackgroundColor(90, 40, 110)]
    [Slider]
    [Increment(20)]
    [Range(20, 200)]
    [DefaultValue(40)]
    public int StartMana { get; set; } = 40;
}
