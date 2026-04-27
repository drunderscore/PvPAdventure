using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.Core.Debug;

/// <summary>
/// Figure out why the silly overflowhidden doesn't work
/// </summary>
public class DebugContentPanel : UIPanel
{
    public UIList list;
    protected UIScrollbar scrollbar;

    protected bool Active;

    public DebugContentPanel()
    {
        Width.Set(350f, 0f);
        Height.Set(460f, 0f);
        Top.Set(-70f, 0f);
        Left.Set(-20f, 0f);
        VAlign = 0.8f;
        HAlign = 0.8f;
        BackgroundColor = UICommon.DefaultUIBlueMouseOver;

        Rebuild();
    }

    public void ToggleActiveAndRebuild()
    {
        Active = !Active;
        Rebuild();
    }

    public override void Update(GameTime gameTime)
    {
        if (!Active)
            return;

        base.Update(gameTime);

        if (IsMouseHovering)
            Main.LocalPlayer.mouseInterface = true;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!Active)
            return;

        base.Draw(spriteBatch);
    }

    private void Rebuild()
    {
        RemoveAllChildren();

        var list = new UIGrid
        {
            Width = { Percent = 1f },
            Height = { Percent = 1f, Pixels = -20f },
            Top = { Pixels = 24f },
            ListPadding = 0f
        };

        scrollbar = new UIScrollbar
        {
            Height = { Percent = 1f, Pixels = -82f },
            HAlign = 1f,
            Top = { Pixels = 47f }
        };

        Append(list);
        list.SetScrollbar(scrollbar);
        Append(scrollbar);

        for (int i = 0; i < 40; i++)
        {
            UIPanel panel = new()
            {
                Width = { Percent = 0.95f },
                Height = { Pixels = Main.rand.Next(15, 101) },
                BackgroundColor = Color.Orange * 0.5f
            };

            list.Add(panel);
        }

        Recalculate();
    }
}