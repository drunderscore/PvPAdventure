using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Arenas;
using PvPAdventure.Common.SSC;
using PvPAdventure.Core.Utilities;
using SubworldLibrary;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.Common.Arenas.UI;

public class ArenasJoinUI : UIState
{
    // Player count
    //private static int arenaPlayerCount;

    //private UITextPanel<string> enterButton;

    //public static void SetPlayerCount(int count)
    //{
    //    arenaPlayerCount = count;
    //}


    // UI
    private DraggableElement Root;
    private UIPanel Container;

    public override void OnActivate()
    {
        RemoveAllChildren();

        Root = new DraggableElement
        {
            Width = new StyleDimension(290f, 0f),
            Top = new StyleDimension(100f, 0f),
            Height = new StyleDimension(162f, 0f),
            HAlign = 0.5f
        };
        Append(Root);

            // Title
        var title = new UITextPanel<string>("Choose Your World", 0.6f, large: true)
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

        // Enter Arenas (with icon)
        var arenasRow = CreateButtonWithIcon(
            Ass.Icon_Arenas_v2.Value,
            "Enter Arenas",
            () => SubworldSystem.Enter<ArenasSubworld>(),
            panelHeight
        );
        list.Add(arenasRow);

        // Enter Main World (with globe icon)
        var mainWorldRow = CreateButtonWithIcon(
            Ass.Icon_StartGame.Value, // replace with your actual globe asset field
            "Enter Main World",
            EnterMainWorld,
            panelHeight
        );
        list.Add(mainWorldRow);

        // Recalc after modifications
        Root.Recalculate();
    }

    private void EnterMainWorld()
    {
        ArenasUISystem.Close();
        SSCDelayJoinSystem.SendJoinRequest();
    }

    private static UIElement CreateButtonWithIcon(Texture2D icon, string text, Action onClick, float minHeight)
    {
        var row = new UIElement();
        row.Width.Set(0f, 1f);
        row.MinHeight.Set(minHeight, 0f);

        var panel = new UIPanel();
        panel.Width.Set(0f, 1f);
        panel.MinHeight.Set(minHeight, 0f);
        panel.SetPadding(0f);

        // Icon image
        var iconImage = new UIImage(icon);

        if (icon == Ass.Icon_StartGame.Value)
        {
            iconImage.Top.Set(3, 0);
            iconImage.Left.Set(11, 0f);
            iconImage.ImageScale = 0.85f;
        }
        else
        {
            iconImage.Top.Set(-4, 0);
            iconImage.Left.Set(3, 0f);
            iconImage.ImageScale = 0.76f;
        }

        // Panel handlers
        panel.OnLeftClick += (_, _) => onClick();
        panel.OnMouseOver += (_, _) =>
        {
            panel.BorderColor = Color.Yellow;
            //if (iconImage != null && icon == Ass.Icon_Arenas_v2.Value)
            //{
            //    iconImage.SetImage(Ass.Icon_Arenas_v2_Highlighted);
            //}
        };
        panel.OnMouseOut += (_, _) =>
        {
            panel.BorderColor = Color.Black;
            //if (iconImage != null && iconImage._texture == Ass.Icon_Arenas_v2_Highlighted)
            //{
            //    iconImage.SetImage(Ass.Icon_Arenas_v2);
            //}
        };

        row.Append(panel);

        panel.Append(iconImage);

        // Icon hover
        iconImage.OnMouseOver += (_, _) =>
        {
            if (iconImage != null && icon == Ass.Icon_Arenas_v2.Value)
            {
                iconImage.SetImage(Ass.Icon_Arenas_v2_Highlighted);
            }
        };
        iconImage.OnMouseOut += (_, _) =>
        {
            if (iconImage != null && iconImage._texture == Ass.Icon_Arenas_v2_Highlighted)
            {
                iconImage.SetImage(Ass.Icon_Arenas_v2);
            }
        };

        // Text that starts AFTER the icon
        var label = new UIText(text, 0.5f, large: true);
        label.TextOriginX = 0;
        label.TextOriginY = 0;
        label.Left.Set(50, 0f);
        label.VAlign = 0.5f;
        panel.Append(label);

        return row;
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