using DragonLens.Core.Systems;
using PvPAdventure.Common.Arenas;
using PvPAdventure.Core.Config.ConfigElements.LoadoutItems;
using System.Collections.Generic;
using System.ComponentModel;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace PvPAdventure.Core.Config;

internal class SSCConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ServerSide;

    #region Members
    [Header("SSC")]
    [BackgroundColor(90, 70, 160)]
    [DefaultValue(true)]
    public bool IsSSCEnabled { get; set; } = true;

    [BackgroundColor(90, 70, 160)]
    [DefaultValue(SSCPlayerNameType.Default)]
    public SSCPlayerNameType SSCPlayerNames { get; set; } = SSCPlayerNameType.Default;

    [BackgroundColor(90, 70, 160)]
    [Slider]
    [Increment(20)]
    [Range(100, 500)]
    [DefaultValue(200)]
    public int StartLife { get; set; } = 200;

    [BackgroundColor(90, 70, 160)]
    [Slider]
    [Increment(20)]
    [Range(20, 200)]
    [DefaultValue(100)]
    public int StartMana { get; set; } = 100;

    [Header("StartingItems")]
    [BackgroundColor(90, 70, 160)]
    [CustomModConfigItem(typeof(LoadoutItemListElement))]
    public List<LoadoutItem> StartItems { get; set; } = [];
    #endregion

    #region Data types
    public enum SSCPlayerNameType
    {
        Default,
        Steam,
        Discord,
        Numbered
    }
    #endregion

    #region Hooks / methods
    public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref NetworkText message)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
            return true;

        if (!ModLoader.HasMod("DragonLens"))
        {
            message = NetworkText.FromLiteral("SSC changes require DragonLens admin (DragonLens not loaded).");
            return false;
        }

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

        StartItems ??= [];
        if (StartItems.Count == 0)
            StartItems = CreateDefaultStartItems();
    }

    public override void OnChanged()
    {
        base.OnChanged();
    }

    private static List<LoadoutItem> CreateDefaultStartItems()
    {
        int adventureMirrorType = ItemID.None;
        if (ModContent.TryFind<ModItem>("PvPAdventure", "AdventureMirror", out ModItem adventureMirror))
            adventureMirrorType = adventureMirror.Type;

        return
        [
            new LoadoutItem
            {
                Item = new ItemDefinition(ItemID.CopperPickaxe)
            },
            new LoadoutItem
            {
                Item = new ItemDefinition(ItemID.CopperAxe)
            },
            new LoadoutItem
            {
                Item = new ItemDefinition(ItemID.CopperShortsword)
            },
            new LoadoutItem
            {
                Item = new ItemDefinition(ItemID.Wood),
                Stack = 20
            },
            new LoadoutItem
            {
                Item = new ItemDefinition(ItemID.Torch),
                Stack = 20
            },
            new LoadoutItem
            {
                Item = new ItemDefinition(adventureMirrorType)
            },
            new LoadoutItem
            {
                Item = new ItemDefinition(ItemID.Bed)
            }
        ];
    }
    #endregion
}