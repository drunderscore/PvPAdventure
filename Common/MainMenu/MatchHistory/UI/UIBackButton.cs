using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace PvPAdventure.Common.MainMenu.MatchHistory.UI;

public sealed class UIBackButton<TText> : UITextPanel<TText>
{
    public UIBackButton(TText text, Action onClick, float textScale = 0.7f, bool large = true) : base(text, textScale, large)
    {
        Width = new StyleDimension(-10f, 0.5f);
        Height = new StyleDimension(50f, 0f);
        PaddingLeft = 10f;
        PaddingRight = 10f;
        PaddingTop = 10f;
        PaddingBottom = 10f;
        BackgroundColor = UICommon.DefaultUIBlueMouseOver;
        BorderColor = Color.Black;

        bool playedTick = false;

        OnMouseOver += (_, __) =>
        {
            BackgroundColor = UICommon.DefaultUIBlue;
            BorderColor = Colors.FancyUIFatButtonMouseOver;

            if (!playedTick)
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                playedTick = true;
            }
        };

        OnMouseOut += (_, __) =>
        {
            BackgroundColor = UICommon.DefaultUIBlueMouseOver;
            BorderColor = Color.Black;
            playedTick = false;
        };

        OnLeftClick += (_, __) =>
        {
            onClick?.Invoke();
        };
    }
}
