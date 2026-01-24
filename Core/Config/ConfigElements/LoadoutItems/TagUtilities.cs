using System.Linq;
using Terraria.ModLoader.Config;

namespace PvPAdventure.Core.Config.ConfigElements.LoadoutItems;

public static class TagUtilities
{
    /// <summary>
    /// Converts an ItemDefinition to a tag string like "[i:1234]"
    /// </summary>
    public static string Tag(ItemDefinition d)
    {
        int type = d?.Type ?? 0;
        return type > 0 ? $"[i:{type}]" : "";
    }

    /// <summary>
    /// Concatenates the specified strings with spaces between each item.
    /// </summary>
    public static string Join(params string[] parts)
    {
        //string result = "";
        //for (int i = 0; i < parts.Length; i++)
        //{
        //    string p = parts[i];
        //    if (string.IsNullOrWhiteSpace(p))
        //        continue;

        //    if (result.Length > 0)
        //        result += " ";

        //    result += p;
        //}
        //return result;

        return string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }
}
