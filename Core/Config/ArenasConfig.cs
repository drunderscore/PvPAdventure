using PvPAdventure.Common.Arenas;
using System.Collections.Generic;
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace PvPAdventure.Core.Config;

internal class ArenasConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ServerSide;

    [Header("Arenas")]
    [BackgroundColor(90, 70, 160)]
    [DefaultValue(true)]
    public bool IsArenasEnabled { get; set; } = true;

    [BackgroundColor(90, 70, 160)]
    [DefaultValue(true)]
    public bool RevealMap { get; set; } = true;

    [Expand(false, false)]
    [BackgroundColor(90, 70, 160)]
    public List<Loadout> ArenaLoadouts { get; set; } = [];
}

