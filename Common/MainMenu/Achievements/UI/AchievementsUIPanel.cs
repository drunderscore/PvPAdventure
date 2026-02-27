using Microsoft.Xna.Framework;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace PvPAdventure.Common.MainMenu.Achievements.UI;

/// <summary>
/// Base UI displaying a list of achievements. Each achievement is represented by a <see cref="AchievementsUIRow"/>.
/// </summary>
internal sealed class AchievementsUIPanel : UIPanel
{
    private const float ScrollbarWidth = 20f;

    private UIList _list;
    private UIScrollbar _scrollbar;

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
            Height = new StyleDimension(0, 1f),
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

        // Add the completed achievements first, then the incomplete ones, so that completed ones are at the top of the list.
        for (int i = 0; i < Achievements.All.Length; i++)
        {
            var (id, def) = Achievements.All[i];
            int target = Math.Max(def.Target, 1);
            int progress = Math.Clamp(AchievementStorage.Data.Get(id), 0, target);

            if (progress >= target)
                _list.Add(new AchievementsUIRow(id, def));
        }

        // Add the incompleted achievements.
        for (int i = 0; i < Achievements.All.Length; i++)
        {
            var (id, def) = Achievements.All[i];
            int target = Math.Max(def.Target, 1);
            int progress = Math.Clamp(AchievementStorage.Data.Get(id), 0, target);

            if (progress < target)
                _list.Add(new AchievementsUIRow(id, def));
        }
    }
}
