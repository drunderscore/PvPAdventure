using Microsoft.Xna.Framework;
using PvPAdventure.Common.MainMenu.State;
using PvPAdventure.Common.MainMenu.UI;
using PvPAdventure.Core.Utilities;
using PvPAdventure.UI;
using System.Linq;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.MainMenu.Leaderboards;

public class LeaderboardsUIState : MainMenuPageUIState
{
    private const float ButtonWidth = 275f;
    private const float ButtonSmallWidth = 80f;

    private LeaderboardsUIContent content;

    private UISortableTable<LeaderboardEntryContent> leaderboardTable = null!;

    protected override string HeaderLocalizationKey => "Mods.PvPAdventure.MainMenu.Leaderboards";

    protected override void Populate(UIPanel panel)
    {
        base.Populate(panel);
        leaderboardTable = new UISortableTable<LeaderboardEntryContent>(
            [
                new SortableTableColumn<LeaderboardEntryContent>(
                    "Rank",
                    Ass.Button_Small,
                    Ass.Button_Small_Border,
                    ButtonSmallWidth,
                    entry => entry.Rank.ToString(),
                    (items, ascending) => ascending
                        ? items.OrderBy(x => x.Rank)
                        : items.OrderByDescending(x => x.Rank)),
                new SortableTableColumn<LeaderboardEntryContent>(
                    "Player",
                    Ass.Button,
                    Ass.Button_Border,
                    ButtonWidth,
                    entry => entry.Player,
                    (items, ascending) => ascending
                        ? items.OrderBy(x => x.Player)
                        : items.OrderByDescending(x => x.Player),
                    leftAligned: true,
                    textInset: 10f),
                new SortableTableColumn<LeaderboardEntryContent>(
                    "Kills",
                    Ass.Button_Small,
                    Ass.Button_Small_Border,
                    ButtonSmallWidth,
                    entry => entry.Kills.ToString(),
                    (items, ascending) => ascending
                        ? items.OrderBy(x => x.Kills).ThenBy(x => x.Rank)
                        : items.OrderByDescending(x => x.Kills).ThenBy(x => x.Rank)),
                new SortableTableColumn<LeaderboardEntryContent>(
                    "Deaths",
                    Ass.Button_Small,
                    Ass.Button_Small_Border,
                    ButtonSmallWidth,
                    entry => entry.Deaths.ToString(),
                    (items, ascending) => ascending
                        ? items.OrderBy(x => x.Deaths).ThenBy(x => x.Rank)
                        : items.OrderByDescending(x => x.Deaths).ThenBy(x => x.Rank)),
                new SortableTableColumn<LeaderboardEntryContent>(
                    "Games",
                    Ass.Button_Small,
                    Ass.Button_Small_Border,
                    ButtonSmallWidth,
                    entry => entry.Games.ToString(),
                    (items, ascending) => ascending
                        ? items.OrderBy(x => x.Games).ThenBy(x => x.Rank)
                        : items.OrderByDescending(x => x.Games).ThenBy(x => x.Rank))
            ]);

        panel.Append(leaderboardTable);
    }

    protected override void RefreshContent()
    {
        SetCurrentAsyncState(AsyncProviderState.Loading);

        bool buildExampleContent = true;
        content = buildExampleContent
            ? LeaderboardsExampleContent.Create()
            : new LeaderboardsUIContent([]);

        if (!buildExampleContent)
        {
            // TODO: Call the leaderboards API here and map the response into LeaderboardsUIContent.
        }

        RefreshList();
        SetCurrentAsyncState(AsyncProviderState.Completed);
    }

    private void RefreshList()
    {
        leaderboardTable.SetItems(content.Entries);
    }

    public override void ScrollWheel(UIScrollWheelEvent evt)
    {
        base.ScrollWheel(evt);

        if (leaderboardTable != null)
            leaderboardTable.Scrollbar.ViewPosition -= evt.ScrollWheelValue;
    }
}
