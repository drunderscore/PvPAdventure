using Terraria;
using Terraria.ID;

namespace PvPAdventure.Core.Config.ConfigElements.LoadoutItems;

/// <summary>
/// Filtered ItemDefinition picker elements for Arena Loadouts.
/// Drop-in: add [CustomModConfigItem(typeof(...Element))] on the corresponding ItemDefinition property.
/// </summary>
internal static class LoadoutItemDefinitionElements
{
    // Intentionally empty static container; elements are below.
}

#region Armor

internal sealed class ItemHeadDefinitionElement : FilteredItemDefinitionElement
{
    protected override bool IsValidItem(int type)
    {
        if (type == ItemID.None)
            return true;

        if (!ContentSamples.ItemsByType.TryGetValue(type, out var sample))
            return false;

        return sample.headSlot >= 0;
    }
}

internal sealed class ItemBodyDefinitionElement : FilteredItemDefinitionElement
{
    protected override bool IsValidItem(int type)
    {
        if (type == ItemID.None)
            return true;

        if (!ContentSamples.ItemsByType.TryGetValue(type, out var sample))
            return false;

        return sample.bodySlot >= 0;
    }
}

internal sealed class ItemLegsDefinitionElement : FilteredItemDefinitionElement
{
    protected override bool IsValidItem(int type)
    {
        if (type == ItemID.None)
            return true;

        if (!ContentSamples.ItemsByType.TryGetValue(type, out var sample))
            return false;

        return sample.legSlot >= 0;
    }
}

#endregion

#region Accessories

internal sealed class ItemAccessoryDefinitionElement : FilteredItemDefinitionElement
{
    protected override bool IsValidItem(int type)
    {
        if (type == ItemID.None)
            return true;

        if (!ContentSamples.ItemsByType.TryGetValue(type, out var sample))
            return false;

        return sample.accessory;
    }
}

#endregion

#region Equipment

internal sealed class ItemMountDefinitionElement : FilteredItemDefinitionElement
{
    protected override bool IsValidItem(int type)
    {
        if (type == ItemID.None)
            return true;

        if (!ContentSamples.ItemsByType.TryGetValue(type, out var sample))
            return false;

        // Vanilla: -1 means not a mount item.
        return sample.mountType >= 0;
    }
}

internal sealed class ItemGrapplingHookDefinitionElement : FilteredItemDefinitionElement
{
    protected override bool IsValidItem(int type)
    {
        if (type == ItemID.None)
            return true;

        if (!ContentSamples.ItemsByType.TryGetValue(type, out var sample))
            return false;

        int projType = sample.shoot;
        if (projType <= ProjectileID.None)
            return false;

        return projType >= 0 && projType < Main.projHook.Length && Main.projHook[projType];
    }
}

#endregion
