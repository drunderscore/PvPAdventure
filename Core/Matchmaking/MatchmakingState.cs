using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace PvPAdventure.Core.Matchmaking;

public class MatchmakingState : UIState
{
    private UIElement _buttonBack;

    private UIElement _buttonLogs;

    public override void OnInitialize()
    {
        base.OnInitialize();
        int num = 20;
        int num2 = 250;
        int num3 = 50 + num * 2;
        int num4 = Main.minScreenH;
        int num5 = num4 - num2 - num3;
        UIElement uIElement = new UIElement();
        uIElement.Width.Set(600f, 0f);
        uIElement.Top.Set(num2, 0f);
        uIElement.Height.Set(num4 - num2, 0f);
        uIElement.HAlign = 0.5f;
        int num6 = 284;
        UIPanel uIPanel = new UIPanel();
        uIPanel.Width.Set(0f, 1f);
        uIPanel.Height.Set(num5, 0f);
        uIPanel.BackgroundColor = new Color(33, 43, 79) * 0.8f;
        UIElement uIElement2 = new UIElement();
        uIElement2.Width.Set(0f, 1f);
        uIElement2.Height.Set(num6, 0f);
        uIElement2.SetPadding(0f);
        UITextPanel<string> uITextPanel = new UITextPanel<string>("Matchmaking", 0.8f, large: true);
        uITextPanel.HAlign = 0.5f;
        uITextPanel.Top.Set(-46f, 0f);
        uITextPanel.SetPadding(15f);
        uITextPanel.BackgroundColor = new Color(73, 94, 171);
        UITextPanel<LocalizedText> uITextPanel2 = new UITextPanel<LocalizedText>(Language.GetText("UI.Back"), 0.7f, large: true);
        uITextPanel2.Width.Set(-10f, 0.5f);
        uITextPanel2.Height.Set(50f, 0f);
        uITextPanel2.VAlign = 1f;
        uITextPanel2.HAlign = 0f;
        uITextPanel2.Top.Set(-num, 0f);
        uITextPanel2.OnMouseOver += FadedMouseOver;
        uITextPanel2.OnMouseOut += FadedMouseOut;
        uITextPanel2.OnLeftClick += GoBackClick;
        //uITextPanel2.SetSnapPoint("Back", 0);
        uIElement.Append(uITextPanel2);
        this._buttonBack = uITextPanel2;
        UITextPanel<string> uITextPanel3 = new("Play", 0.7f, large: true);
        uITextPanel3.Width.Set(-10f, 0.5f);
        uITextPanel3.Height.Set(50f, 0f);
        uITextPanel3.VAlign = 1f;
        uITextPanel3.HAlign = 1f;
        uITextPanel3.Top.Set(-num, 0f);
        uITextPanel3.OnMouseOver += FadedMouseOver;
        uITextPanel3.OnMouseOut += FadedMouseOut;
        uITextPanel3.OnLeftClick += GoLogsClick;
        //uITextPanel3.SetSnapPoint("Logs", 0);
        uIElement.Append(uITextPanel3);
        this._buttonLogs = uITextPanel3;
        uIPanel.Append(uIElement2);
        uIElement.Append(uIPanel);
        uIElement.Append(uITextPanel);
        base.Append(uIElement);
    }
  
    private void GoBackClick(UIMouseEvent evt, UIElement listeningElement)
    {
        Main.menuMode = 0;
    }

    private void GoLogsClick(UIMouseEvent evt, UIElement listeningElement)
    {
        Main.IssueReporterIndicator.Hide();
        Main.OpenReportsMenu();
        SoundEngine.PlaySound(SoundID.MenuOpen);
    }

    private void FadedMouseOver(UIMouseEvent evt, UIElement listeningElement)
    {
        SoundEngine.PlaySound(SoundID.MenuTick);
        ((UIPanel)evt.Target).BackgroundColor = new Color(73, 94, 171);
        ((UIPanel)evt.Target).BorderColor = Colors.FancyUIFatButtonMouseOver;
    }

    private void FadedMouseOut(UIMouseEvent evt, UIElement listeningElement)
    {
        ((UIPanel)evt.Target).BackgroundColor = new Color(63, 82, 151) * 0.8f;
        ((UIPanel)evt.Target).BorderColor = Color.Black;
    }
    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
    }
}
