using System;
using Microsoft.Xna.Framework;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;

namespace PvPAdventure.Core.Features.MainMenu.UI
{
    public sealed class HoverButton : UITextPanel<string>
    {
        private readonly Color _idleBorder = Color.Black;
        private readonly Color _hoverBorder = new(255, 240, 20);
        private readonly Color _idleBg = new Color(63, 82, 151) * 0.70f;
        private bool _playedTick;

        public HoverButton(string text, float scale = 0.9f, bool large = true) : base(text, scale, large)
        {
            Width.Set(180f, 0f);
            Height.Set(44f, 0f);
            DrawPanel = true;
            BackgroundColor = _idleBg;
            BorderColor = _idleBorder;

            OnMouseOver += (_, __) =>
            {
                BorderColor = _hoverBorder;
                if (!_playedTick)
                {
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    _playedTick = true;
                }
            };
            OnMouseOut += (_, __) =>
            {
                BorderColor = _idleBorder;
                _playedTick = false;
            };
        }

        public void SetLabel(string text) => SetText(text);
    }
}
