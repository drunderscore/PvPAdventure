using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PvPAdventure.Common.MainMenu.Profile;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace PvPAdventure.Common.MainMenu.Achievements.UI;

internal sealed class AchievementsUIPanel : UIPanel
{
    private const float ScrollbarWidth = 20f;

    private UIList _list = null!;
    private UIScrollbar _scrollbar = null!;

    public AchievementsUIPanel()
    {
        BorderColor = Color.Black;
        BackgroundColor = new Color(33, 43, 79) * 0.8f;
        Refresh();
    }

    public void Refresh()
    {
        RemoveAllChildren();

        Append(new UITextPanel<string>(Language.GetTextValue("Mods.PvPAdventure.MainMenu.Achievements"), 0.9f, true)
        {
            Top = new StyleDimension(-48f, 0f),
            HAlign = 0.5f,
            BackgroundColor = UICommon.DefaultUIBlue
        });

        _list = new UIList
        {
            Top = new StyleDimension(6f, 0f),
            Width = new StyleDimension(-ScrollbarWidth - 6f, 1f),
            Height = new StyleDimension(0f, 1f),
            ListPadding = 8f,
            ManualSortMethod = _ => { }
        };
        _list.SetPadding(0f);
        _list.PaddingTop = 8f;
        Append(_list);

        _scrollbar = new UIScrollbar
        {
            Top = _list.Top,
            Height = _list.Height,
            Width = new StyleDimension(ScrollbarWidth, 0f),
            Left = new StyleDimension(-ScrollbarWidth, 1f)
        };
        _scrollbar.Height.Pixels -= 6f;
        _scrollbar.SetView(100f, 1000f);
        Append(_scrollbar);

        _list.SetScrollbar(_scrollbar);

        var state = MainMenuProfileState.Instance;

        List<AchievementsUIRow> claimed = [];
        List<AchievementsUIRow> completed = [];
        List<AchievementsUIRow> inProgress = [];

        foreach (var (id, def) in Achievements.All)
        {
            int target = Math.Max(def.Target, 1);
            int progress = Math.Clamp(state.GetAchievementProgress(id), 0, target);
            var row = new AchievementsUIRow(id, def);

            if (state.IsAchievementCollected(id))
                claimed.Add(row);
            else if (progress >= target)
                completed.Add(row);
            else
                inProgress.Add(row);
        }

        foreach (var row in claimed)
            _list.Add(row);

        foreach (var row in completed)
            _list.Add(row);

        foreach (var row in inProgress)
            _list.Add(row);
    }
}