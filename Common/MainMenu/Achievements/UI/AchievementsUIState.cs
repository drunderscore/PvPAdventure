using System;
using Microsoft.Xna.Framework;
using PvPAdventure.Common.MainMenu.State;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace PvPAdventure.Common.MainMenu.Achievements.UI;

public sealed class AchievementsUIState : MainMenuPageUIState
{
    private const float ScrollbarWidth = 20f;

    private UIList list = null!;
    private UIScrollbar scrollbar = null!;

    protected override float MainPanelMinWidth => 850f;
    protected override string HeaderLocalizationKey => "Mods.PvPAdventure.MainMenu.Achievements";

    protected override void Populate(UIPanel panel)
    {
        list = new UIList
        {
            Top = new StyleDimension(6f, 0f),
            Width = new StyleDimension(-ScrollbarWidth - 6f, 1f),
            Height = new StyleDimension(0f, 1f),
            ListPadding = 8f,
            ManualSortMethod = _ => { }
        };
        list.SetPadding(0f);
        list.PaddingTop = 8f;
        panel.Append(list);

        scrollbar = new UIScrollbar
        {
            Top = list.Top,
            Height = list.Height,
            Width = new StyleDimension(ScrollbarWidth, 0f),
            Left = new StyleDimension(-ScrollbarWidth, 1f)
        };
        scrollbar.Height.Pixels -= 6f;
        scrollbar.SetView(100f, 1000f);
        panel.Append(scrollbar);

        list.SetScrollbar(scrollbar);
        RefreshAchievements();
    }

    protected override void RefreshContent()
    {
        SetCurrentAsyncState(AsyncProviderState.Loading);
        RefreshAchievements();
        SetCurrentAsyncState(AsyncProviderState.Completed);
    }

    private void RefreshAchievements()
    {
        bool buildExampleContent = true;

        AchievementsUIContent content = buildExampleContent
            ? AchievementsExampleContent.Create()
            : new AchievementsUIContent([]);

        if (!buildExampleContent)
        {
            // TODO: Call the achievements API here and map the response into AchievementsUIContent.
        }

        BuildAchievements(content);
    }

    private void BuildAchievements(AchievementsUIContent content)
    {
        list.Clear();
        scrollbar.ViewPosition = 0f;

        for (int i = 0; i < content.Entries.Length; i++)
            list.Add(new AchievementsUIRow(content.Entries[i]));

        list.Recalculate();
    }
}
