using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace PvPAdventure.System;

[Autoload(Side = ModSide.Client)]
public class DeathSummaryManager : ModSystem
{
    private const int TimeUntilReset = 20 * 60;

    private readonly List<Player.HurtInfo> _damageTaken = [];
    private int _timeSinceFirstDamage;
    private int _resetCounter;

    public void AddDamageTaken(Player.HurtInfo info)
    {
        _damageTaken.Add(info);
        _resetCounter = TimeUntilReset;
    }

    public void Visualize()
    {
        var uiDeathSummary = new UIDeathSummary(new List<Player.HurtInfo>(_damageTaken), _timeSinceFirstDamage);
        Main.InGameUI.SetState(uiDeathSummary);
        Reset();
    }

    private void Reset()
    {
        _damageTaken.Clear();
        _timeSinceFirstDamage = 0;
    }

    public override void PostUpdateTime()
    {
        if (_damageTaken.Count > 0)
        {
            if (--_resetCounter == 0)
                Reset();
            else
                _timeSinceFirstDamage++;
        }
    }

    public class UIDeathSummary(IEnumerable<Player.HurtInfo> damageTaken, int time) : UIState
    {
        public override void OnInitialize()
        {
            var root = new UIElement
            {
                Top = { Percent = 0.4f },
                Width = { Percent = 1.0f },
                Height = { Percent = 1.0f }
            };

            var container = new UIPanel
            {
                Width = { Percent = 0.30f },
                Height = { Percent = 0.05f }
            };

            Dictionary<DamageClass, int> damageClassDamage = new();

            var total = 0;
            foreach (var info in damageTaken)
            {
                if (info.DamageSource.SourceItem != null)
                {
                    total += info.Damage;
                    damageClassDamage[info.DamageSource.SourceItem.DamageType] =
                        damageClassDamage.GetValueOrDefault(info.DamageSource.SourceItem.DamageType, 0) + info.Damage;
                }
            }

            total = 150;
            damageClassDamage[DamageClass.Magic] = 100;
            damageClassDamage[DamageClass.Ranged] = 50;

            // DamageClass[] damageClasses = [DamageClass.Magic, DamageClass.Ranged, DamageClass.Melee, DamageClass.Summon, DamageClass.Generic];

            var segments = new UIElement
            {
                Width = { Percent = 1.0f },
                Height = { Percent = 0.5f },
            };
            // FIXME: Fixed order for damage classes.
            var left = 0.0f;
            foreach (var (damageClass, count) in damageClassDamage)
            {
                var percent = (float)count / total;
                var segment = new UISegment(damageClass, percent, count)
                {
                    Left = { Percent = left },
                    Width = { Percent = percent },
                    Height = { Percent = 0.80f }
                };

                segments.Append(segment);

                left += percent;
            }

            container.Append(segments);

            if (time == 0)
                time = (25 * 60) + 24;

            var fightTimeText = new UIText($"Fight time: {time / 60.0f:F2}s")
            {
                Top = { Percent = 0.6f },
                MarginRight = 10.0f
            };
            container.Append(fightTimeText);

            container.Append(new UIText($"Total damage: {total}")
            {
                Top = { Percent = 0.6f },
                Left = { Pixels = fightTimeText.GetOuterDimensions().Width }
            });

            root.Append(container);

            Append(root);

            root.Left = new()
            {
                Percent = 0.5f,
                Pixels = -container.GetOuterDimensions().Width / 2.0f
            };
        }

        public class UISegment(DamageClass damageClass, float percent, int total) : UIPanel
        {
            private static readonly Color DefaultColor = new(160, 160, 160);
            private static readonly Color GenericColor = new(221, 80, 71);
            private static readonly Color SummonColor = new(79, 203, 203);
            private static readonly Color MeleeColor = new(219, 220, 49);

            private static readonly Dictionary<DamageClass, Color> DamageClassColors = new()
            {
                { DamageClass.Default, DefaultColor },
                { DamageClass.Generic, GenericColor },
                { DamageClass.Melee, MeleeColor },
                { DamageClass.MeleeNoSpeed, MeleeColor },
                { DamageClass.Ranged, new(21, 194, 86) },
                { DamageClass.Magic, new(204, 31, 198) },
                { DamageClass.Summon, SummonColor },
                { DamageClass.SummonMeleeSpeed, SummonColor }
            };

            public override void OnInitialize()
            {
                BackgroundColor = DamageClassColors.GetValueOrDefault(damageClass, GenericColor);
            }

            public override void MouseOver(UIMouseEvent evt)
            {
                base.MouseOver(evt);
                BorderColor = Color.White;
            }

            public override void MouseOut(UIMouseEvent evt)
            {
                base.MouseOut(evt);
                BorderColor = Color.Black;
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                base.DrawSelf(spriteBatch);
                if (IsMouseHovering)
                {
                    var text = Main.keyState.PressingShift() ? total.ToString() : $"{percent * 100:F0}%";

                    // FIXME: Multiple of these likely don't add up to 100%
                    UICommon.TooltipMouseText($"{text}{damageClass.DisplayName}");
                }
            }
        }
    }

    public class VisualizeCommand : ModCommand
    {
        public override void Action(CommandCaller caller, string input, string[] args)
        {
            ModContent.GetInstance<DeathSummaryManager>().Visualize();
        }

        public override string Command => "visualize";
        public override CommandType Type => CommandType.Chat;
    }
}