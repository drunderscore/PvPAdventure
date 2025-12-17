using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Spectate;
using System;
using System.Globalization;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.ModLoader.UI;

namespace PvPAdventure.Core.ConfigElements;

internal class SpectateUIPositionConfigElement : ConfigElement
{
    private class Vec2Obj
    {
        private readonly PropertyFieldWrapper member;
        private readonly object item;
        private readonly Action<Vector2> liveApply;
        private readonly Func<float, float> q;
        private Vector2 cur;

        [LabelKey("$Config.Vector2.X.Label")]
        public float X { get => cur.X; set { cur.X = q(value); Update(); } }

        [LabelKey("$Config.Vector2.Y.Label")]
        public float Y { get => cur.Y; set { cur.Y = q(value); Update(); } }

        public Vec2Obj(PropertyFieldWrapper member, object item, Action<Vector2> liveApply, Func<float, float> q)
        {
            this.member = member;
            this.item = item;
            this.liveApply = liveApply;
            this.q = q;

            object v = member.GetValue(item);
            cur = v is Vector2 vv ? vv : new Vector2(0.5f, 0.9f);
            cur.X = q(cur.X);
            cur.Y = q(cur.Y);
        }

        private void Update()
        {
            member.SetValue(item, cur);
            Interface.modConfig.SetPendingChanges();
            liveApply?.Invoke(cur);
        }
    }

    private int height;
    private Vec2Obj c;

    private float min = 0f;
    private float max = 1f;
    private float inc = 0.01f;
    private int decimals = 2;

    public override void OnBind()
    {
        base.OnBind();

        height = 30;

        if (RangeAttribute != null && RangeAttribute.Min is float rmin && RangeAttribute.Max is float rmax)
        {
            min = rmin;
            max = rmax;
        }

        if (IncrementAttribute != null && IncrementAttribute.Increment is float rinc && rinc > 0f)
            inc = rinc;

        decimals = DecimalsFromIncrement(inc);

        c = new Vec2Obj(MemberInfo, Item, LiveApply, Quantize);

        int order = 0;
        foreach (PropertyFieldWrapper variable in ConfigManager.GetFieldsAndProperties(c))
        {
            var wrapped = UIModConfig.WrapIt(this, ref height, variable, c, order++);

            if (wrapped.Item2 is FloatElement fe)
            {
                fe.Min = min;
                fe.Max = max;
                fe.Increment = inc;
                fe.DrawTicks = Attribute.IsDefined(MemberInfo.MemberInfo, typeof(DrawTicksAttribute));
            }
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);

        Rectangle r = GetInnerDimensions().ToRectangle();
        r = new Rectangle(r.Right - 30, r.Y, 30, 30);

        spriteBatch.Draw(TextureAssets.MagicPixel.Value, r, Color.AliceBlue);

        float x = (c.X - min) / (max - min);
        float y = (c.Y - min) / (max - min);

        Vector2 p = r.TopLeft();
        p.X += x * r.Width;
        p.Y += y * r.Height;

        if (x >= 0f && x <= 1f && y >= 0f && y <= 1f)
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)p.X - 2, (int)p.Y - 2, 4, 4), Color.Black);

        if (IsMouseHovering && r.Contains(Main.MouseScreen.ToPoint()) && Main.mouseLeft)
        {
            c.X = min + Utils.Clamp((Main.mouseX - r.X) / (float)r.Width, 0f, 1f) * (max - min);
            c.Y = min + Utils.Clamp((Main.mouseY - r.Y) / (float)r.Height, 0f, 1f) * (max - min);
        }
    }

    internal float GetHeight() => height;

    private void LiveApply(Vector2 pos)
    {
        if (Main.dedServ)
            return;

        var ss = ModContent.GetInstance<SpectateSystem>();
        var e = ss?.spectateElement;
        if (e == null)
            return;

        e._hAlign = pos.X;
        e._vAlign = pos.Y;
        e.HAlign = pos.X;
        e.VAlign = pos.Y;
        e.Recalculate();
    }

    private float Quantize(float v)
    {
        v = Utils.Clamp(v, min, max);

        decimal d = (decimal)v;
        decimal di = (decimal)inc;

        decimal snapped = Math.Round(d / di, 0, MidpointRounding.AwayFromZero) * di;
        snapped = Math.Round(snapped, decimals, MidpointRounding.AwayFromZero);

        string s = snapped.ToString($"F{decimals}", CultureInfo.InvariantCulture);
        return float.Parse(s, CultureInfo.InvariantCulture);
    }

    private static int DecimalsFromIncrement(float inc)
    {
        for (int d = 0; d <= 6; d++)
            if (MathF.Abs(inc - MathF.Round(inc, d)) < 1e-7f) return d;
        return 2;
    }
}
