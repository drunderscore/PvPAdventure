using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.MainMenu.State;
using PvPAdventure.Core.Utilities;
using PvPAdventure.UI;
using ReLogic.Content;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.MainMenu.Leaderboards;

public class LeaderboardsUIState : MainMenuPageUIState
{
    private const float PanelPadding = 12f;
    private const float ScrollbarWidth = 20f;
    private const float HeaderHeight = 28f;

    private const float ButtonWidth = 275f;
    private const float ButtonSmallWidth = 80f;

    private const float RankColumnWidth = ButtonSmallWidth;
    private const float PlayerColumnWidth = ButtonWidth;
    private const float KillsColumnWidth = ButtonSmallWidth;
    private const float DeathsColumnWidth = ButtonSmallWidth;
    private const float GamesColumnWidth = ButtonSmallWidth;

    private const float RankColumnLeft = 0f;
    private const float PlayerColumnLeft = RankColumnLeft + RankColumnWidth;
    private const float KillsColumnLeft = PlayerColumnLeft + PlayerColumnWidth;
    private const float DeathsColumnLeft = KillsColumnLeft + KillsColumnWidth;
    private const float GamesColumnLeft = DeathsColumnLeft + DeathsColumnWidth;

    private const float HeaderContentWidth = RankColumnWidth + PlayerColumnWidth + KillsColumnWidth + DeathsColumnWidth + GamesColumnWidth;

    private LeaderboardsUIContent content;

    private UIList list = null!;
    private UIScrollbar scrollbar = null!;

    private SortColumn sortColumn = SortColumn.Rank;
    private bool sortAscending = true;

    private enum SortColumn
    {
        Rank,
        Player,
        Kills,
        Deaths,
        Games
    }

    internal sealed class LeaderboardRow : UIPanel
    {
        public List<UIText> Texts { get; } = [];
    }

    protected override string HeaderLocalizationKey => "Mods.PvPAdventure.MainMenu.Leaderboards";

    protected override void Populate(UIPanel panel)
    {
        base.Populate(panel);
        UIElement headerBar = new();
        headerBar.Width.Set(HeaderContentWidth, 0f);
        headerBar.Height.Set(32f, 0f);
        headerBar.Top.Set(6f, 0f);
        panel.Append(headerBar);

        UIImageButton AddHeader(Asset<Texture2D> asset, string text, float left, float width, SortColumn column)
        {
            UIImageButton header = new(asset)
            {
                Left = { Pixels = left },
                Top = { Pixels = 0f },
                Width = { Pixels = width },
                Height = { Pixels = HeaderHeight }
            };
            header.SetHoverImage(asset == Ass.Button ? Ass.Button_Border : Ass.Button_Small_Border);
            header.SetVisibility(1f, 1f);
            headerBar.Append(header);

            UIText label = new(text, 1f);
            label.HAlign = 0.5f;
            label.VAlign = 0.48f;
            header.Append(label);

            header.OnLeftClick += (_, _) => ToggleSort(column);
            header.OnUpdate += _ =>
            {
                if (!header.IsMouseHovering)
                    return;

                string dir = sortColumn == column ? sortAscending ? "Ascending" : "Descending" : "Not active";
                Main.instance.MouseText($"Sort by {text} ({dir})");
            };

            return header;
        }

        AddHeader(Ass.Button_Small, "Rank", RankColumnLeft, RankColumnWidth, SortColumn.Rank);
        AddHeader(Ass.Button, "Player", PlayerColumnLeft, PlayerColumnWidth, SortColumn.Player);
        AddHeader(Ass.Button_Small, "Kills", KillsColumnLeft, KillsColumnWidth, SortColumn.Kills);
        AddHeader(Ass.Button_Small, "Deaths", DeathsColumnLeft, DeathsColumnWidth, SortColumn.Deaths);
        AddHeader(Ass.Button_Small, "Games", GamesColumnLeft, GamesColumnWidth, SortColumn.Games);

        UIElement contentRoot = new();
        contentRoot.Width.Set(-(ScrollbarWidth + 2f), 1f);
        contentRoot.Height.Set(-(HeaderHeight + 10f), 1f);
        contentRoot.Top.Set(HeaderHeight + 8f, 0f);
        panel.Append(contentRoot);

        scrollbar = new UIScrollbar();
        scrollbar.HAlign = 1f;
        scrollbar.Top.Set(HeaderHeight + 8f, 0f);
        scrollbar.Width.Set(ScrollbarWidth, 0f);
        scrollbar.Height.Set(-(HeaderHeight + 10f), 1f);
        panel.Append(scrollbar);

        list = new UIList();
        list.Width.Set(0f, 1f);
        list.Height.Set(0f, 1f);
        list.ListPadding = 2f;
        list.ManualSortMethod += _ => { };
        list.SetScrollbar(scrollbar);
        contentRoot.Append(list);
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
        list.Clear();

        IEnumerable<LeaderboardEntryContent> sorted = sortColumn switch
        {
            SortColumn.Rank => sortAscending ? content.Entries.OrderBy(x => x.Rank) : content.Entries.OrderByDescending(x => x.Rank),

            SortColumn.Player => sortAscending ? content.Entries.OrderBy(x => x.Player) : content.Entries.OrderByDescending(x => x.Player),

            SortColumn.Kills => sortAscending ? content.Entries.OrderBy(x => x.Kills).ThenBy(x => x.Rank) : content.Entries.OrderByDescending(x => x.Kills).ThenBy(x => x.Rank),

            SortColumn.Deaths => sortAscending ? content.Entries.OrderBy(x => x.Deaths).ThenBy(x => x.Rank) : content.Entries.OrderByDescending(x => x.Deaths).ThenBy(x => x.Rank),

            SortColumn.Games => sortAscending ? content.Entries.OrderBy(x => x.Games).ThenBy(x => x.Rank) : content.Entries.OrderByDescending(x => x.Games).ThenBy(x => x.Rank),

            _ => content.Entries
        };

        foreach (LeaderboardEntryContent entry in sorted)
        {
            LeaderboardRow row = new();
            row.BackgroundColor = new Color(63, 82, 151) * 0.35f;
            row.BorderColor = new Color(89, 116, 213) * 0.25f;
            row.Width.Set(0f, 1f);
            row.Height.Set(30f, 0f);
            row.SetPadding(0f);

            row.Append(MakeColumn(row, RankColumnLeft, RankColumnWidth, entry.Rank.ToString(), false));
            row.Append(MakeColumn(row, PlayerColumnLeft, PlayerColumnWidth, entry.Player, true));
            row.Append(MakeColumn(row, KillsColumnLeft, KillsColumnWidth, entry.Kills.ToString(), false));
            row.Append(MakeColumn(row, DeathsColumnLeft, DeathsColumnWidth, entry.Deaths.ToString(), false));
            row.Append(MakeColumn(row, GamesColumnLeft, GamesColumnWidth, entry.Games.ToString(), false));

            row.OnMouseOver += (_, _) =>
            {
                row.BackgroundColor = new Color(73, 94, 171) * 0.9f;
                row.BorderColor = new Color(89, 116, 213);
            };

            row.OnMouseOut += (_, _) =>
            {
                row.BackgroundColor = new Color(63, 82, 151) * 0.35f;
                row.BorderColor = new Color(89, 116, 213) * 0.25f;
            };

            list.Add(row);
        }
    }

    private static UIElement MakeColumn(LeaderboardRow row, float leftPixels, float widthPixels, string text, bool leftAligned)
    {
        UIElement column = new();
        column.Left.Set(leftPixels - PanelPadding, 0f);
        column.Width.Set(widthPixels, 0f);
        column.Height.Set(0f, 1f);

        UIText label = new(text)
        {
            TextColor = Color.White,
            VAlign = 0.5f
        };

        if (leftAligned)
        {
            label.Left.Set(10f, 0f);
            label.HAlign = 0f;
            label.TextOriginX = 0f;
        }
        else
        {
            label.HAlign = 0.5f;
        }

        column.Append(label);
        row.Texts.Add(label);
        return column;
    }

    public override void ScrollWheel(UIScrollWheelEvent evt)
    {
        base.ScrollWheel(evt);

        if (scrollbar != null)
            scrollbar.ViewPosition -= evt.ScrollWheelValue;
    }

    private void ToggleSort(SortColumn column)
    {
        if (sortColumn == column)
            sortAscending = !sortAscending;
        else
        {
            sortColumn = column;
            sortAscending = true;
        }

        RefreshList();
    }
}
