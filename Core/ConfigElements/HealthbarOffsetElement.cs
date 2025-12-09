using PvPAdventure.Core.HealthBars;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.ModLoader.UI;

namespace PvPAdventure.Core.ConfigElements;

internal class HealthbarOffsetElement : IntRangeElement
{
    public new Func<int> GetValue;
    public new Action<int> SetValue;

    private int _min;
    private int _max;

    protected override float Proportion
    {
        get => (_max == _min) ? 0f : (GetValue() - _min) / (float)(_max - _min);
        set
        {
            int v = _min + (int)Math.Round(value * (_max - _min));
            v = Utils.Clamp(v, _min, _max);
            SetValue(v); 
        }
    }

    public override void OnBind()
    {
        base.OnBind();

        var rangeAttr = ConfigManager.GetCustomAttributeFromMemberThenMemberType<RangeAttribute>(MemberInfo, Item, null);
        if (rangeAttr != null)
        {
            _min = (int)Convert.ChangeType(rangeAttr.Min, typeof(int));
            _max = (int)Convert.ChangeType(rangeAttr.Max, typeof(int));
        }
        else
        {
            _min = 0;
            _max = 100;
        }

        GetValue = () => (int)MemberInfo.GetValue(Item);

        SetValue = v =>
        {
            if (!MemberInfo.CanWrite)
                return;

            MemberInfo.SetValue(Item, v);
            Interface.modConfig.SetPendingChanges();

            if (Item is AdventureClientConfig cfg && MemberInfo.Name == nameof(AdventureClientConfig.HealthbarYOffset))
            {
                var hb = ModContent.GetInstance<HealthbarSystem>();
                if (hb == null) return;
                hb.Offset = v;
                hb.Scale = cfg.HealthbarScale;
            }
        };

        string label = Label ?? MemberInfo.Name;
        TextDisplayFunction = () => $"{label}: {GetValue()}";
    }
}
