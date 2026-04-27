//using Microsoft.Xna.Framework;
//using PvPAdventure.Common.Spectator.UI;
//using PvPAdventure.Core.Utilities;
//using PvPAdventure.UI;
//using Terraria.GameContent.UI.Elements;
//using Terraria.UI;

//namespace PvPAdventure.Common.Spectator._Deprecated;

//public class SpectatorJoinPanel : UIElement
//{
//    public SpectatorJoinPanel()
//    {
//        Width.Set(0f, 1f);
//        Height.Set(0f, 1f);

//        UIDraggableElement root = new() { HAlign = 0.5f };
//        root.Width.Set(290f, 0f);
//        root.Height.Set(156f, 0f);
//        root.Top.Set(100f, 0f);
//        Append(root);

//        UITextPanel<string> title = new("Choose Player Mode", 0.6f, true)
//        {
//            HAlign = 0.5f,
//            BackgroundColor = new Color(73, 94, 171)
//        };
//        title.Width.Set(0f, 1f);
//        title.OnLeftMouseDown += (evt, _) => root.BeginDrag(evt);
//        title.OnLeftMouseUp += (evt, _) => root.EndDrag(evt);
//        root.Append(title);

//        root.Recalculate();
//        float titleHeight = title.GetOuterDimensions().Height;

//        UIPanel container = new()
//        {
//            BackgroundColor = new Color(33, 43, 79) * 0.8f
//        };
//        container.SetPadding(0f);
//        container.Top.Set(titleHeight, 0f);
//        container.Width.Set(0f, 1f);
//        container.Height.Set(-titleHeight, 1f);
//        root.Append(container);

//        UITextActionPanel playerRow = new("Player", SpectatorUISystem.TryEnterPlayerMode, titleHeight, 0.5f, true, Ass.Icon_Player.Value);
//        playerRow.Left.Set(8f, 0f);
//        playerRow.Top.Set(8f, 0f);
//        playerRow.Width.Set(-16f, 1f);

//        UITextActionPanel spectateRow = new("Spectator", SpectatorUISystem.TryEnterSpectateMode, titleHeight, 0.5f, true, Ass.Icon_Eye.Value);
//        spectateRow.Left.Set(8f, 0f);
//        spectateRow.Top.Set(8f + titleHeight + 8f, 0f);
//        spectateRow.Width.Set(-16f, 1f);

//        container.Append(playerRow);
//        container.Append(spectateRow);

//        root.Recalculate();
//    }
//}