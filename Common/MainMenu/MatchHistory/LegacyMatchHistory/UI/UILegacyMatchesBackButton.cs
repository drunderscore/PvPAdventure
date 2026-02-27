using Microsoft.Xna.Framework;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.MainMenu.MatchHistory.LegacyMatchHistory.UI;

internal class UILegacyMatchesBackButton : UIText
{
    public UILegacyMatchesBackButton(string text, float textScale = 1, bool large = false) : base(text, textScale, large)
    {
        this.TextColor = Color.Gray;
    }

    public override void OnInitialize()
    {
        base.OnInitialize();
    }
    public override void OnActivate()
    {
        base.OnActivate();
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        TPVPAUIState.OpenState(() => new TPVPAUIState());
    }

    public override void MouseOver(UIMouseEvent evt)
    {
        base.MouseOver(evt);
        this.TextColor = Color.White;
    }

    public override void MouseOut(UIMouseEvent evt)
    {
        base.MouseOut(evt);
        this.TextColor = Color.Gray;
    }
}
