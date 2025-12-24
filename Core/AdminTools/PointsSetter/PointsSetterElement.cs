using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Core.AdminTools.TeamAssigner;
using PvPAdventure.Core.AdminTools.UI;
using PvPAdventure.System;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.AdminTools.PointsSetter;

internal class PointsSetterElement : DraggablePanel
{
    public PointsSetterElement() : base(title: Language.GetTextValue("Mods.PvPAdventure.Tools.DLPointsSetterTool.TitlePanelName"))
    {
        Height.Set(210, 0);
    }

    public override void OnInitialize()
    {
        base.OnInitialize();
        Rebuild();
    }

    public void Rebuild()
    {
        ContentPanel.RemoveAllChildren();

        var pm = ModContent.GetInstance<PointsManager>();
        if (pm?.Points == null)
            return;

        const int rowH = 30;

        int i = 0;
        foreach (var (team, points) in pm.Points.OrderBy(k => (int)k.Key))
        {
            if (team == Team.None)
                continue;

            var row = new TeamRow(team, points, RequestSetPoints)
            {
                Top = { Pixels = (i * rowH) },
                Width = { Percent = 1f },
                Height = { Pixels = rowH },
            };

            ContentPanel.Append(row);
            i++;
        }

        ContentPanel.Recalculate();
    }

    private static void RequestSetPoints(Team team, int value)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            ModContent.GetInstance<PointsManager>().SetTeamPoints(team, value);
            return;
        }

        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            var mod = ModContent.GetInstance<PvPAdventure>();
            var p = mod.GetPacket();
            p.Write((byte)AdventurePacketIdentifier.SetPointsRequest);
            p.Write((byte)team);
            p.Write(value);
            p.Send();
        }
    }

    protected override void OnClosePanelLeftClick()
    {
        ModContent.GetInstance<PointsSetterSystem>().ToggleActive();
    }

    protected override void OnRefreshPanelLeftClick()
    {
        Rebuild();
    }

    private sealed class TeamRow : UIPanel
    {
        private readonly Team _team;
        private readonly Action<Team, int> _commit;
        private readonly UIText _label;

        public TeamRow(Team team, int points, Action<Team, int> commit)
        {
            _team = team;
            _commit = commit;

            SetPadding(0f);
            BorderColor = Color.Black;

            int t = (int)team;
            if (t < 0 || t >= TeamAssignerElement.TeamColors.Length)
                t = 0;

            BackgroundColor = TeamAssignerElement.TeamColors[t];

            const float leftPad = 10f;
            const float iconBox = 32f;

            Append(new TeamIcon(team)
            {
                Left = { Pixels = leftPad },
                VAlign = 0.5f
            });

            _label = new UIText(string.Empty, textScale: 1.0f)
            {
                Left = { Pixels = leftPad + iconBox + 8f },
                VAlign = 0.5f
            };
            Append(_label);

            SetPointsText(points);

            const float inputW = 78f;

            var input = new MiniTextBox("points")
            {
                Width = { Pixels = inputW },
                Height = { Pixels = 24f },
                Left = { Percent = 1f, Pixels = -10f - inputW },
                VAlign = 0.5f
            };

            input.OnEnterPressed += () =>
            {
                if (!int.TryParse(input.Text.Trim(), out int v))
                    return;

                SetPointsText(v);

                var pm = ModContent.GetInstance<PointsManager>();
                pm._points[_team] = v;
                pm.UiScoreboard?.Invalidate();

                _commit(_team, v);

                // Clear text box
                input.SetText(string.Empty);
            };

            Append(input);
        }

        private void SetPointsText(int points)
        {
            _label.SetText($"{_team}: {points}");
        }
    }

    private sealed class TeamIcon : UIElement
    {
        private readonly Team _team;

        public TeamIcon(Team team)
        {
            _team = team;
            Width.Set(32f, 0f);
            Height.Set(32f, 0f);
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            base.DrawSelf(sb);

            var tex = TextureAssets.Pvp[1].Value;
            if (tex == null)
                return;

            var src = tex.Frame(6, 1, (int)_team);
            var dims = GetDimensions();

            var pos = new Vector2(
                dims.X + (dims.Width - src.Width) * 0.5f,
                dims.Y + (dims.Height - src.Height) * 0.5f
            );

            sb.Draw(tex, pos, src, Color.White);
        }
    }

    private sealed class MiniTextBox : UIPanel
    {
        private const int MaxLen = 9;

        private readonly string _hint;
        private bool _focused;
        private int _blinkCount;
        private int _blinkState;

        public string Text { get; private set; } = string.Empty;
        public event Action OnEnterPressed;

        public MiniTextBox(string hint)
        {
            _hint = hint;

            SetPadding(0f);
            BackgroundColor = Color.White;
            BorderColor = Color.Black;
        }

        public override bool ContainsPoint(Vector2 point)
        {
            bool inside = base.ContainsPoint(point);

            if (inside && Main.mouseLeft)
            {
                Main.mouseLeftRelease = false;
                Focus();
            }

            return inside;
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            Focus();
            base.LeftClick(evt);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (!_focused)
                return;

            Main.LocalPlayer.mouseInterface = true;

            var mp = new Vector2(Main.mouseX, Main.mouseY);
            if (!base.ContainsPoint(mp) && (Main.mouseLeft || Main.mouseRight))
                Unfocus();
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            base.DrawSelf(sb);

            if (_focused)
            {
                Terraria.GameInput.PlayerInput.WritingText = true;
                Main.instance.HandleIME();

                string newText = Main.GetInputText(Text) ?? string.Empty;
                newText = FilterToInt(newText);

                if (!newText.Equals(Text, StringComparison.Ordinal))
                    SetText(newText);

                if (JustPressed(Keys.Enter))
                {
                    Main.drawingPlayerChat = false;
                    Unfocus();
                    OnEnterPressed?.Invoke();
                    
                }

                if (++_blinkCount >= 20)
                {
                    _blinkState = (_blinkState + 1) & 1;
                    _blinkCount = 0;
                }
            }

            var pos = GetDimensions().Position() + new Vector2(6f, 4f);
            var font = FontAssets.MouseText.Value;

            string s = Text;
            if (_focused && _blinkState == 1)
                s = string.IsNullOrEmpty(s) ? "|" : s + "|";

            if (string.IsNullOrEmpty(Text) && !_focused)
                sb.DrawString(font, _hint, pos, Color.DimGray, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
            else
                sb.DrawString(font, s, pos, Color.Black, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
        }

        private void Focus()
        {
            if (_focused)
                return;

            Main.clrInput();
            _focused = true;
            Main.blockInput = true;
        }

        private void Unfocus()
        {
            if (!_focused)
                return;

            _focused = false;
            Main.blockInput = false;
        }
        public void SetText(string text)
        {
            Text = text ?? string.Empty;
            if (Text.Length > MaxLen)
                Text = Text[..MaxLen];
        }

        private static bool JustPressed(Keys key) =>
            Main.inputText.IsKeyDown(key) && !Main.oldInputText.IsKeyDown(key);

        private static string FilterToInt(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;

            bool neg = s[0] == '-';
            var chars = new List<char>(s.Length);

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c is >= '0' and <= '9')
                    chars.Add(c);
                else if (i == 0 && c == '-')
                    neg = true;
            }

            if (chars.Count == 0)
                return neg ? "-" : string.Empty;

            string digits = new(chars.ToArray());
            return neg ? "-" + digits : digits;
        }
    }

}
