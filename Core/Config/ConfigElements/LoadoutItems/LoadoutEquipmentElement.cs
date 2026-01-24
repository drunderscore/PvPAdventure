using PvPAdventure.Common.Arenas;
using Terraria.ModLoader.Config.UI;

namespace PvPAdventure.Core.Config.ConfigElements.LoadoutItems;

internal sealed class LoadoutEquipmentElement : ObjectElement
{
    public override void OnBind()
    {
        base.OnBind();

        TextDisplayFunction = () =>
        {
            string baseLabel = Label ?? MemberInfo?.Name ?? "Equipment";
            string summary = EquipmentSummary(Value as Equipment);
            return string.IsNullOrWhiteSpace(summary) ? baseLabel : $"{baseLabel} {summary}";
        };
    }

    private static string EquipmentSummary(Equipment e)
    {
        if (e == null)
            return "";

        return TagUtilities.Join(
            TagUtilities.Tag(e.GrapplingHook), 
            TagUtilities.Tag(e.Mount));
    }
}
