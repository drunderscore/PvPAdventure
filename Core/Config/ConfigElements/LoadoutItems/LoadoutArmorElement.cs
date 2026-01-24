using PvPAdventure.Common.Arenas;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;

namespace PvPAdventure.Core.Config.ConfigElements.LoadoutItems;

internal sealed class LoadoutArmorElement : ObjectElement
{
    public override void OnBind()
    {
        base.OnBind();

        TextDisplayFunction = () =>
        {
            string baseLabel = Label ?? MemberInfo?.Name ?? "Armor";
            string summary = ArmorSummary(Value as Armor);
            return string.IsNullOrWhiteSpace(summary) ? baseLabel : $"{baseLabel} {summary}";
        };
    }

    private static string ArmorSummary(Armor a)
    {
        if (a == null)
            return "";

        return TagUtilities.Join(
            TagUtilities.Tag(a.Head),
            TagUtilities.Tag(a.Body),
            TagUtilities.Tag(a.Legs));
    }
}
