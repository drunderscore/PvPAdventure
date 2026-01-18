using Microsoft.Xna.Framework;
using PvPAdventure.Common.Arenas;
using SubworldLibrary;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.Common.Arenas.UI;

public class ArenasJoinUI : UIState
{
    // Player count
    private static int arenaPlayerCount;

    private UITextPanel<string> enterButton;

    public static void SetPlayerCount(int count)
    {
        arenaPlayerCount = count;
    }

    // UI
    private DraggableElement Root;
    private UIPanel Container;

    public override void OnActivate()
    {
        RemoveAllChildren();

        Root = new DraggableElement
        {
            Width = new StyleDimension(240f, 0f),
            Top = new StyleDimension(100f, 0f),
            Height = new StyleDimension(150f, 0f),
            HAlign = 0.5f
        };
        Append(Root);

        // Title
        var title = new UITextPanel<string>("Arenas", 0.6f, large: true)
        {
            HAlign = 0.5f,
            BackgroundColor = new Color(73, 94, 171)
        };
        //title.SetPadding(0f);
        title.Width.Set(0f, 1f);

        title.OnLeftMouseDown += (evt, _) => Root.BeginDrag(evt);
        title.OnLeftMouseUp += (evt, _) => Root.EndDrag(evt);

        Root.Append(title);

        // Force a layout pass so we can measure the title height
        Root.Recalculate();
        float panelHeight = title.GetOuterDimensions().Height;

        Container = new UIPanel
        {
            BackgroundColor = new Color(33, 43, 79) * 0.8f
        };
        Container.Top.Set(panelHeight, 0f);        
        Container.Width.Set(0f, 1f);
        Container.Height.Set(-panelHeight, 1f);   
        Root.Append(Container);

        var list = new UIList
        {
            PaddingTop = 0f,
            ListPadding = 8f
        };
        list.Width.Set(0f, 1f);
        list.Height.Set(0f, 1f);
        list.Left.Set(0f, 0f);
        list.Top.Set(0f, 0f);
        Container.Append(list);

        enterButton = CreateButton("Enter Arenas", () => SubworldSystem.Enter<ArenasSubworld>());
        enterButton.SetPadding(8f);
        enterButton.MinHeight.Set(panelHeight, 0f); // same height as title (derived from scale)
        list.Add(enterButton);

        var closeButton = CreateButton("Close Menu", ArenasUISystem.Close);
        closeButton.SetPadding(8f);
        closeButton.MinHeight.Set(panelHeight, 0f);
        list.Add(closeButton);

        // Recalc after modifications
        Root.Recalculate();
    }


    public static UITextPanel<string> CreateButton(string text, Action onClick)
    {
        var button = new UITextPanel<string>(text, 0.6f, large: true);
        button.HAlign = 0.5f;
        button.SetPadding(0f);
        button.Width.Set(0f, 1f);

        button.OnLeftClick += (_, _) => onClick();
        button.OnMouseOver += (_, _) => button.BorderColor = Color.Yellow;
        button.OnMouseOut += (_, _) => button.BorderColor = Color.Black;

        return button;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
    }
}