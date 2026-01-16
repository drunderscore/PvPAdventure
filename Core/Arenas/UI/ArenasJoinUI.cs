using Microsoft.Xna.Framework;
using SubworldLibrary;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.Core.Arenas.UI;

public class ArenasJoinUI : UIState
{
    // Player count
    private static int arenaPlayerCount;

    private UITextPanel<string> enterButton;

    public static void SetPlayerCount(int count)
    {
        arenaPlayerCount = count;
    }
    private static string GetEnterText()
    {
        return $"Enter Arena 1";
    }

    // UI
    private const float TitleHeight = 52f;
    private DraggableElement Root;
    private UIPanel Container;

    public override void OnActivate()
    {
        RemoveAllChildren();

        Root = new DraggableElement
        {
            Width = new StyleDimension(380f, 0f),
            Top = new StyleDimension(100f, 0f),
            Height = new StyleDimension(150f, 0f),
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

        enterButton = CreateButton(
            GetEnterText(),
            () =>
            {
                SubworldSystem.Enter<ArenasSubworld>();
            }
        );
        list.Add(enterButton);

        Root.Append(title);
    }

    public static UITextPanel<string> CreateButton(string text, Action onClick)
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

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (enterButton != null)
            enterButton.SetText(GetEnterText());
    }
}