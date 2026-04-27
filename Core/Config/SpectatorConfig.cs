using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace PvPAdventure.Core.Config;

internal class SpectatorConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ServerSide;

    [Header("Spectate")]
    [BackgroundColor(40, 40, 110)]
    [DefaultValue(true)]
    public bool AllowSpectating = true;

    [BackgroundColor(40, 40, 110)]
    [DefaultValue(false)]
    public bool ForceSpectating = false;
}