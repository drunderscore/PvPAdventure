using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PvPAdventure.Common.MainMenu.State;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace PvPAdventure.Common.MainMenu.Achievements.UI;

public sealed class AchievementsUIState : MainMenuPageUIState
{
    private const float ScrollbarWidth = 20f;

    private UIList list = null!;
    private UIScrollbar scrollbar = null!;

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
        //RefreshAchievements();
        SetCurrentAsyncState(AsyncProviderState.Completed);
    }

    private void ResetAchievements()
    {
        SoundEngine.PlaySound(SoundID.Grab);

        //ProfileStorage.EnsureLoaded();

        //ProfileStorage.Achievements = new AchievementProgress();
        //ProfileStorage.Save();

        //ProfileStorage.RebuildAchievements(matches);
        //ProfileStorage.RebuildGems(matches);

        RefreshAchievements();
        SetCurrentAsyncState(AsyncProviderState.Completed);
    }

    private void RefreshAchievements()
    {
        list.Clear();
        scrollbar.ViewPosition = 0f;

        //MainMenuProfileState state = MainMenuProfileState.Instance;

        //List<AchievementsUIRow> claimed = [];
        //List<AchievementsUIRow> completed = [];
        //List<AchievementsUIRow> inProgress = [];

        //foreach ((string id, AchievementDefinition def) in Achievements.All)
        //{
        //    int target = Math.Max(def.Target, 1);
        //    int progress = Math.Clamp(state.GetAchievementProgress(id), 0, target);
        //    AchievementsUIRow row = new(id, def, RefreshAchievements);

        //    if (state.IsAchievementCollected(id))
        //        claimed.Add(row);
        //    else if (progress >= target)
        //        completed.Add(row);
        //    else
        //        inProgress.Add(row);
        //}

        //foreach (AchievementsUIRow row in claimed)
        //    list.Add(row);

        //foreach (AchievementsUIRow row in completed)
        //    list.Add(row);

        //foreach (AchievementsUIRow row in inProgress)
        //    list.Add(row);

        //list.Recalculate();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        UILinkPointNavigator.Shortcuts.BackButtonCommand = 7;
    }
}