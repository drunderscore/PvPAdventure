using DragonLens.Core.Systems;
using PvPAdventure.Common.Arenas;
using PvPAdventure.Core.Config.ConfigElements;
using System.Collections.Generic;
using System.ComponentModel;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace PvPAdventure.Core.Config;

internal class ArenasConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ServerSide;

    [Header("Arenas")]
    [BackgroundColor(90, 40, 110)]
    [DefaultValue(true)]
    public bool IsArenasEnabled { get; set; } = true;

    [BackgroundColor(90, 40, 110)]
    [Slider]
    [Increment(20)]
    [Range(100, 500)]
    [DefaultValue(100)]
    public int MaxHealth { get; set; } = 400;

    [BackgroundColor(90, 40, 110)]
    [DefaultValue(true)]
    public bool RevealMap { get; set; } = true;

    [Header("Loadouts")]
    [BackgroundColor(90, 40, 110)]
    [Expand(false)]
    [CustomModConfigItem(typeof(LoadoutListElement))]
    public List<Loadout> ArenaLoadouts { get; set; } = [];

    #region Hooks / methods
    public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref NetworkText message)
    {
        // Singleplayer always allowed
        if (Main.netMode == NetmodeID.SinglePlayer)
            return true;

        // If dragonlens isn't loaded, disallow modifying the config.
        if (!ModLoader.HasMod("DragonLens"))
        {
            message = NetworkText.FromLiteral("Server config changes require DragonLens admin (DragonLens not loaded).");
            return false;
        }

        // DragonLens admin check
        return AcceptClientChanges_DragonLens(whoAmI, ref message);
    }

    [JITWhenModsEnabled("DragonLens")]
    private static bool AcceptClientChanges_DragonLens(int whoAmI, ref NetworkText message)
    {
        Player player = Main.player[whoAmI];

        if (!PermissionHandler.CanUseTools(player))
        {
            message = NetworkText.FromLiteral("You must be a DragonLens admin to modify this config.");
            return false;
        }
        message = NetworkText.FromLiteral("Saved!");

        return true;
    }

    public override void HandleAcceptClientChangesReply(bool success, int player, NetworkText message)
    {
        Log.Chat("Server accepted changes!");
        base.HandleAcceptClientChangesReply(success, player, message);
    }
    public override void OnLoaded()
    {
        base.OnLoaded();
    }
    public override void OnChanged()
    {
        base.OnChanged();
    }
    #endregion
}

