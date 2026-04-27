using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Arenas;
using PvPAdventure.Common.Spectator.UI;
using PvPAdventure.Common.SSC;
using PvPAdventure.Core.Utilities;
using PvPAdventure.UI;
using SubworldLibrary;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.Common.Arenas.UI;

public class ArenasJoinUIState : UIState
{
    // Player count
    //private static int arenaPlayerCount;

    //private UITextPanel<string> enterButton;

    //public static void SetPlayerCount(int count)
    //{
    //    arenaPlayerCount = count;
    //}


    // UI
    private UIDraggableElement Root;
    private UIPanel Container;

    public override void OnActivate()
    {
        RemoveAllChildren();

        Root = new UIDraggableElement
        {
            Width = new StyleDimension(290f, 0f),
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
        var arenasRow = new UITextActionPanel("Enter Arenas", () => SubworldSystem.Enter<ArenasSubworld>(), panelHeight, 0.5f, true, Ass.Icon_Arenas.Value);
        var mainWorldRow = new UITextActionPanel("Enter Main World", EnterMainWorld, panelHeight, 0.5f, true, Ass.Icon_StartGame.Value);

        list.Add(arenasRow);
        list.Add(mainWorldRow);

        // Recalc after modifications
        Root.Recalculate();
    }

    private void EnterMainWorld()
    {
        ArenasUISystem.Close();
        SSCDelayJoinSystem.SendJoinRequest();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
    }
}