using System.Reflection;
using Terraria;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Misc;

internal class BossProgressBarBugFix : ModSystem
{
    private static readonly FieldInfo _bigProgressBarSystemCurrentBarField =
        typeof(BigProgressBarSystem).GetField("_currentBar", BindingFlags.NonPublic | BindingFlags.Instance);

    public override void ClearWorld()
    {
        // Terraria/TML bug: Remove boss bar when clearing the world
        // FIXME: Don't put this here!
        _bigProgressBarSystemCurrentBarField.SetValue(Main.BigBossProgressBar, null);
    }
}
