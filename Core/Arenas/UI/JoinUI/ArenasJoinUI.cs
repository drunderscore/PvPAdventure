using SubworldLibrary;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using System;

namespace PvPAdventure.Core.Arenas.UI.JoinUI;

public class ArenasJoinUI : UIState
{
    private const float TitleHeight = 52f;

    private DraggableElement Root;
    private UIPanel Container;

    public override void OnActivate()
    {
        RemoveAllChildren();

        Root = new DraggableElement
        {
            Width = new StyleDimension(400f, 0f),
            Top = new StyleDimension(50f, 0f),
            Height = new StyleDimension(210f, 0f),
            HAlign = 0.5f
        };
        Append(Root);

        var title = new UITextPanel<string>("Arenas", 0.7f, large: true)
        {
            HAlign = 0.5f,
            Height = new StyleDimension(TitleHeight, 0f),
            BackgroundColor = new Color(73, 94, 171)
        };
        title.SetPadding(15f);
        title.OnLeftMouseDown += (evt, _) => Root.BeginDrag(evt);
        title.OnLeftMouseUp += (evt, _) => Root.EndDrag(evt);

        Container = new UIPanel
        {
            BackgroundColor = new Color(33, 43, 79) * 0.8f
        };
        Container.Top.Set(TitleHeight - 12f, 0f);
        Container.Width.Set(0f, 1f);
        Container.Height.Set(-TitleHeight, 1f);
        Root.Append(Container);

        var list = new UIList
        {
            PaddingTop = 8f
        };
        list.Width.Set(-24f, 1f);
        list.Height.Set(-24f, 1f);
        list.Left.Set(12f, 0f);
        list.Top.Set(12f, 0f);
        Container.Append(list);

        var enterButton = CreateButton(
            "Enter Arena",
            () => SubworldSystem.Enter<ArenasSubworld>()
        );

        var exitButton = CreateButton(
            "Exit Arena",
            () => SubworldSystem.Exit()
        );

        list.Add(enterButton);
        list.Add(exitButton);

        Root.Append(title);
    }

    private static UITextPanel<string> CreateButton(string text, Action onClick)
    {
        var button = new UITextPanel<string>(text, 0.6f, large: true)
        {
            HAlign = 0.5f
        };

        button.SetPadding(10f);
        button.Width.Set(0f, 1f);

        button.OnLeftClick += (_, _) => onClick();
        button.OnMouseOver += (_, _) => button.BorderColor = Color.Yellow;
        button.OnMouseOut += (_, _) => button.BorderColor = Color.Black;

        return button;
    }
}
