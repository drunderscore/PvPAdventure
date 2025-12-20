using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.Spectate;

public class SpectateElement : UIElement
{
    private const int Slot = 52;
    public float _hAlign = 0.9f;
    public float _vAlign = 1f;

    public override void OnInitialize()
    {
        Height.Set(Slot + 26, 0f);

        var cfg = ModContent.GetInstance<AdventureClientConfig>();
        if (cfg != null)
        {
            _hAlign = cfg.SpectateUIPosition.X;
            _vAlign = cfg.SpectateUIPosition.Y;
        }

        HAlign = _hAlign;
        VAlign = _vAlign;

        Rebuild();
    }

    public void Rebuild()
    {
        for (int i = Elements.Count - 1; i >= 0; i--)
            RemoveChild(Elements[i]);

        var cfg = ModContent.GetInstance<AdventureClientConfig>();
        if (cfg != null)
        {
            _hAlign = cfg.SpectateUIPosition.X;
            _vAlign = cfg.SpectateUIPosition.Y;
        }

        var sp = Main.LocalPlayer.GetModPlayer<SpectatePlayer>();
        bool all = ModContent.GetInstance<SpectateSystem>()?.ShowAllPlayers == true;
        List<int> ids = sp.GetTeammateIds(all);

        if (ids.Count == 0)
        {
            Width.Set(Slot * 4, 0f);

            var noTeammatesText = new UIText("No teammates to spectate");
            noTeammatesText.HAlign = 0.5f;
            Append(noTeammatesText);

            Recalculate();
            return;
        }

        Width.Set(ids.Count * Slot, 0f);

        for (int i = 0; i < ids.Count; i++)
        {
            var slot = new SlotElement(ids[i]);
            slot.Width.Set(Slot, 0f);
            slot.Height.Set(Slot, 0f);
            slot.Left.Set(i * Slot, 0f);
            Append(slot);
        }

        string spectateText = "Click a teammate to spectate";
        if (sp.TargetPlayerIndex is int ti && ti >= 0 && ti < Main.maxPlayers && Main.player[ti]?.active == true)
            spectateText = "Spectating: " + Main.player[ti].name;

        UIText text = new(spectateText);
        text.HAlign = 0.5f;
        text.Top.Set(Slot + 6, 0f);
        Append(text);

        Recalculate();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (IsMouseHovering)
        {
            Main.LocalPlayer.mouseInterface = true;
        }
        VAlign = _vAlign;
        HAlign = _hAlign;
    }

    private sealed class SlotElement : UIElement
    {
        private readonly int playerIndex;
        private readonly Bg bg;
        private readonly Head head;

        public SlotElement(int playerIndex)
        {
            this.playerIndex = playerIndex;

            bg = new Bg();
            bg.Width.Set(0f, 1f);
            bg.Height.Set(0f, 1f);
            Append(bg);

            head = new Head(playerIndex);
            head.Width.Set(0f, 1f);
            head.Height.Set(0f, 1f);
            head.IgnoresMouseInteraction = true;
            Append(head);

            OnLeftClick += Click;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (IsMouseHovering)
            {
                Player p = Main.player[playerIndex];
                if (p != null && p.active)
                {
                    string text = p.name;
                    var ss = ModContent.GetInstance<SpectateSystem>();

                    if (p.team != 0 && ss != null && ss.ShowAllPlayers)
                    {
                        var team = (Terraria.Enums.Team)p.team;
                        text += $" ({team})";
                    }

                    Main.instance.MouseText(text);
                }
            }

            var sp = Main.LocalPlayer.GetModPlayer<SpectatePlayer>();
            bool selected = sp.TargetPlayerIndex == playerIndex;
            float s = selected ? 1f : 0.75f;

            var img = selected
                ? TextureAssets.InventoryBack14
                : (IsMouseHovering ? TextureAssets.InventoryBack15 : TextureAssets.InventoryBack7);

            bg.Set(img, s);
        }

        private void Click(UIMouseEvent evt, UIElement listeningElement)
        {
            var sp = Main.LocalPlayer.GetModPlayer<SpectatePlayer>();

            if (sp.TargetPlayerIndex == playerIndex)
            {
                sp.SetTarget(null);
                sp.SnapBackToSelf();
            }
            else
            {
                sp.SetTarget(playerIndex);
            }

            if (Parent is SpectateElement row)
                row.Rebuild();
        }

        private sealed class Bg : UIElement
        {
            private Asset<Texture2D> img = TextureAssets.InventoryBack7;
            private float scale = 0.75f;

            public void Set(Asset<Texture2D> img, float scale)
            {
                this.img = img;
                this.scale = scale;
            }

            protected override void DrawSelf(SpriteBatch sb)
            {
                var d = GetDimensions();
                Texture2D t = img.Value;

                sb.Draw(
                    t, d.Center(), null, Color.White, 0f,
                    new Vector2(t.Width, t.Height) * 0.5f,
                    scale, SpriteEffects.None, 0f
                );
            }
        }

        private sealed class Head : UIElement
        {
            private readonly int playerIndex;

            public Head(int playerIndex) => this.playerIndex = playerIndex;

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                base.DrawSelf(spriteBatch);

                Player p = Main.player[playerIndex];
                if (p == null || !p.active)
                    p = Main.LocalPlayer;

                var sp = Main.LocalPlayer.GetModPlayer<SpectatePlayer>();
                float scale = sp.TargetPlayerIndex == playerIndex ? 1f : 0.75f;
                Vector2 headPos = GetDimensions().Center() + new Vector2(-3, -2);

                Main.MapPlayerRenderer.DrawPlayerHead(Main.Camera, p, headPos, scale, scale, Color.White);
            }
        }
    }
}
