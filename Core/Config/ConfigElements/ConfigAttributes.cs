using System;

namespace PvPAdventure.Core.Config.ConfigElements;

public enum ConfigIconPlacement
{
    Inside,
    Cut
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class ConfigIconAttribute : Attribute
{
    public string AssFieldName { get; } // nameof(Ass.Icon_XYZ)
    public int ItemId { get; } = -1;
    public ConfigIconPlacement Placement { get; }
    public string OffAssFieldName { get; }
    public bool GrayWhenOff { get; }

    public ConfigIconAttribute(int itemId, ConfigIconPlacement placement = ConfigIconPlacement.Inside)
    {
        ItemId = itemId;
        Placement = placement;
    }

    public ConfigIconAttribute(string assFieldName, ConfigIconPlacement placement = ConfigIconPlacement.Inside)
    {
        AssFieldName = assFieldName;
        Placement = placement;
    }

    public ConfigIconAttribute(string assFieldName, string offAssFieldName, ConfigIconPlacement placement = ConfigIconPlacement.Inside, bool grayWhenOff = false)
    {
        AssFieldName = assFieldName;
        OffAssFieldName = offAssFieldName;
        Placement = placement;
        GrayWhenOff = grayWhenOff;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class HeaderIconAttribute : Attribute
{
    public string AssFieldName { get; }
    public int ItemId { get; } = -1;

    public HeaderIconAttribute(string assFieldName)
    {
        AssFieldName = assFieldName;
    }

    public HeaderIconAttribute(int itemId)
    {
        ItemId = itemId;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class RequiresFieldAttribute : Attribute
{
    public string FieldName { get; }

    public RequiresFieldAttribute(string fieldName)
    {
        FieldName = fieldName;
    }
}
