using PvPAdventure.Core.HealthBars;
using Terraria.ModLoader;
using Terraria.ModLoader.Config.UI;

namespace PvPAdventure.Core.ConfigElements;

internal class HealthbarScaleElement : FloatElement
{
    public override void OnBind()
    {
        base.OnBind();

        string label = Label ?? MemberInfo.Name;
        TextDisplayFunction = () => $"{label}: {GetValue():0.0}";
    }

    protected override void SetValue(object value)
    {
        base.SetValue(value);

        Terraria.ModLoader.UI.Interface.modConfig.SetPendingChanges();

        if (Item is AdventureClientConfig cfg)
        {
            var hb = ModContent.GetInstance<HealthbarSystem>();
            if (hb == null)
                return;

            hb.Scale = cfg.HealthbarScale;
        }
    }
}
