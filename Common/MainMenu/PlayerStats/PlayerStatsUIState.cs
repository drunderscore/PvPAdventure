using Microsoft.Xna.Framework;
using PvPAdventure.Common.MainMenu.State;
using PvPAdventure.Core.Utilities;
using PvPAdventure.UI;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.MainMenu.PlayerStats;

public sealed class PlayerStatsUIState : MainMenuPageUIState
{
    private readonly UISelectableTextPanel<string>[] tabs = new UISelectableTextPanel<string>[2];
    private readonly UIText[] pages = new UIText[2];

    private UIElement content = null!;
    private int selectedTab;

    protected override string HeaderLocalizationKey => "Mods.PvPAdventure.MainMenu.Stats";

    protected override void Populate(UIPanel panel)
    {
        UIElement top = new()
        {
            Width = StyleDimension.Fill,
            Height = StyleDimension.FromPixels(50f)
        };
        top.SetPadding(0f);
        top.PaddingTop = 4f;
        panel.Append(top);

        top.Append(new UIHorizontalSeparator
        {
            Width = StyleDimension.FromPixelsAndPercent(-20f, 1f),
            Top = StyleDimension.FromPixels(16f),
            VAlign = 1f,
            HAlign = 0.5f,
            Color = Color.Lerp(Color.White, new Color(63, 65, 151, 255), 0.85f) * 0.9f
        });

        content = new UIElement
        {
            Top = StyleDimension.FromPixels(56f),
            Width = StyleDimension.Fill,
            Height = StyleDimension.FromPixelsAndPercent(-62f, 1f)
        };
        content.SetPadding(0f);
        panel.Append(content);

        const float outerPad = 10f;
        const float gap = 6f;

        for (int i = 0; i < tabs.Length; i++)
        {
            int index = i;

            UISelectableTextPanel<string> tab = new("", 0.7f, true)
            {
                Width = StyleDimension.FromPixelsAndPercent(-(outerPad + gap * 0.5f), 0.5f),
                Height = StyleDimension.FromPixels(46f)
            };
            tab.SetPadding(0f);
            tab.Top.Set(6f, 0f);
            tab.Left.Set(i == 0 ? outerPad : gap * 0.5f, i == 0 ? 0f : 0.5f);
            tab.BackgroundColor = new Color(63, 82, 151) * 0.8f;
            tab.BorderColor = Color.Black;

            tab.OnMouseOver += (_, _) =>
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                tab.BackgroundColor = new Color(73, 94, 171);
                tab.TextColor = Color.White;
            };

            tab.OnMouseOut += (_, _) =>
            {
                bool isSelected = selectedTab == index;
                tab.BackgroundColor = isSelected ? new Color(73, 94, 171) : new Color(63, 82, 151) * 0.8f;
                tab.TextColor = isSelected ? Color.White : Color.LightGray;
            };

            tab.OnLeftMouseDown += (_, _) =>
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                SelectTab(index);
            };

            tab.Append(new UIImage(i == 0 ? Ass.Icon_Attack : Ass.Icon_Trophy)
            {
                HAlign = 0.5f,
                VAlign = 0.5f,
                ImageScale = i == 0 ? 1f : 0.75f
            });

            string tooltip = i == 0 ? "Player Stats" : "Game Stats";
            tab.OnUpdate += element =>
            {
                if (element.IsMouseHovering)
                    Main.instance.MouseText(tooltip);
            };

            top.Append(tab);
            tabs[index] = tab;

            pages[index] = new UIText("", 0.9f)
            {
                Width = StyleDimension.Fill,
                Height = StyleDimension.Fill,
                TextOriginX = 0f,
                TextOriginY = 0f,
                IsWrapped = false,
                PaddingLeft = 10f,
                PaddingTop = 16f
            };
        }

        SetStats(default);
        SelectTab(0);
    }

    protected override void RefreshContent()
    {
        SetCurrentAsyncState(AsyncProviderState.Loading);

        bool buildExampleContent = true;
        PlayerStatsUIContent stats = buildExampleContent
            ? PlayerStatsExampleContent.Create()
            : default;

        if (!buildExampleContent)
        {
            // TODO: Call the player stats API here and map the response into PlayerStatsUIContent.
        }

        SetStats(stats);
        SetCurrentAsyncState(AsyncProviderState.Completed);
    }

    private void SelectTab(int index)
    {
        selectedTab = index;
        ShowSelectedPage();

        for (int i = 0; i < tabs.Length; i++)
        {
            int tabIndex = i;
            bool isSelected = tabIndex == index;

            tabs[i].IsSelected = _ => selectedTab == tabIndex;
            tabs[i].TextColor = isSelected ? Color.White : Color.LightGray;
            tabs[i].BackgroundColor = isSelected ? new Color(73, 94, 171) : new Color(63, 82, 151) * 0.8f;
            tabs[i].Recalculate();
        }
    }

    private void ShowSelectedPage()
    {
        content.RemoveAllChildren();
        content.Append(pages[selectedTab]);
        content.Recalculate();
    }

    private void ShowContentMessage(string message)
    {
        content.RemoveAllChildren();
        content.Append(CreateWrappedMessageElement(message, 0.9f, 140f));
        content.Recalculate();
    }

    private void SetStats(PlayerStatsUIContent stats)
    {
        static string Ratio(int a, int b)
        {
            if (b <= 0)
                return a <= 0 ? "0" : "INF";

            return ((float)a / b).ToString("0.00");
        }

        pages[0].SetText(
            $"Total Kills: {stats.Kills}\n" +
            $"Total Deaths: {stats.Deaths}\n" +
            $"Total K/D: {Ratio(stats.Kills, stats.Deaths)}"
        );

        pages[1].SetText(
            $"Total Wins: {stats.Wins}\n" +
            $"Total Losses: {stats.Losses}\n" +
            $"Total W/L: {Ratio(stats.Wins, stats.Losses)}\n" +
            $"Total Points: {stats.TeamPointsTotal}"
        );

        ShowSelectedPage();
    }
}
