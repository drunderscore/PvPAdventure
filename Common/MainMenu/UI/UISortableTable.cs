using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.Common.MainMenu.UI;

internal sealed class SortableTableColumn<TItem>
{
    public string HeaderText { get; }
    public Asset<Texture2D> HeaderTexture { get; }
    public Asset<Texture2D> HeaderHoverTexture { get; }
    public float Width { get; }
    public bool LeftAligned { get; }
    public float TextInset { get; }
    public Func<TItem, string> CellText { get; }
    public Func<IEnumerable<TItem>, bool, IEnumerable<TItem>>? SortItems { get; }

    public SortableTableColumn(
        string headerText,
        Asset<Texture2D> headerTexture,
        Asset<Texture2D> headerHoverTexture,
        float width,
        Func<TItem, string> cellText,
        Func<IEnumerable<TItem>, bool, IEnumerable<TItem>>? sortItems = null,
        bool leftAligned = false,
        float textInset = 10f)
    {
        HeaderText = headerText;
        HeaderTexture = headerTexture;
        HeaderHoverTexture = headerHoverTexture;
        Width = width;
        CellText = cellText;
        SortItems = sortItems;
        LeftAligned = leftAligned;
        TextInset = textInset;
    }
}

internal sealed class UISortableTable<TItem> : UIElement
{
    private readonly List<SortableTableColumn<TItem>> columns;
    private readonly float rowHeight;
    private readonly float headerHeight;
    private readonly float panelPadding;
    private readonly UIElement headerBar;

    private TItem[] items = Array.Empty<TItem>();
    private int sortColumnIndex;
    private bool sortAscending = true;

    public UIList List { get; }
    public UIScrollbar Scrollbar { get; }

    public Func<TItem, bool>? IsSelected { get; set; }
    public Action<UIPanel, TItem, bool, bool>? ApplyRowStyle { get; set; }
    public Action<TItem>? OnRowClicked { get; set; }
    public Action<TItem>? OnRowDoubleClicked { get; set; }

    public UISortableTable(
        IEnumerable<SortableTableColumn<TItem>> columns,
        float rowHeight = 30f,
        float headerHeight = 28f,
        float panelPadding = 12f,
        float scrollbarWidth = 20f,
        float listPadding = 2f)
    {
        this.columns = columns.ToList();
        this.rowHeight = rowHeight;
        this.headerHeight = headerHeight;
        this.panelPadding = panelPadding;

        Width = StyleDimension.Fill;
        Height = StyleDimension.Fill;

        float totalWidth = this.columns.Sum(x => x.Width);

        headerBar = new UIElement();
        headerBar.Width.Set(totalWidth, 0f);
        headerBar.Height.Set(32f, 0f);
        headerBar.Top.Set(6f, 0f);
        Append(headerBar);

        float left = 0f;
        for (int i = 0; i < this.columns.Count; i++)
        {
            SortableTableColumn<TItem> column = this.columns[i];
            UIImageButton header = new(column.HeaderTexture)
            {
                Left = { Pixels = left },
                Top = { Pixels = 0f },
                Width = { Pixels = column.Width },
                Height = { Pixels = headerHeight }
            };
            header.SetHoverImage(column.HeaderHoverTexture);
            header.SetVisibility(1f, 1f);

            int columnIndex = i;
            if (column.SortItems != null)
            {
                header.OnLeftClick += (_, _) => ToggleSort(columnIndex);
                header.OnUpdate += _ =>
                {
                    if (!header.IsMouseHovering)
                        return;

                    string dir = sortColumnIndex == columnIndex
                        ? sortAscending ? "Ascending" : "Descending"
                        : "Not active";
                    Main.instance.MouseText($"Sort by {column.HeaderText} ({dir})");
                };
            }

            UIText label = new(column.HeaderText, 1f)
            {
                IgnoresMouseInteraction = true,
                HAlign = 0.5f,
                VAlign = 0.48f
            };
            header.Append(label);
            headerBar.Append(header);

            left += column.Width;
        }

        UIElement contentRoot = new UIElement();
        contentRoot.Width.Set(-(scrollbarWidth + 2f), 1f);
        contentRoot.Height.Set(-(headerHeight + 10f), 1f);
        contentRoot.Top.Set(headerHeight + 8f, 0f);
        Append(contentRoot);

        Scrollbar = new UIScrollbar();
        Scrollbar.HAlign = 1f;
        Scrollbar.Top.Set(headerHeight + 8f, 0f);
        Scrollbar.Width.Set(scrollbarWidth, 0f);
        Scrollbar.Height.Set(-(headerHeight + 10f), 1f);
        Append(Scrollbar);

        List = new UIList();
        List.Width.Set(0f, 1f);
        List.Height.Set(0f, 1f);
        List.ListPadding = listPadding;
        List.ManualSortMethod += _ => { };
        List.SetScrollbar(Scrollbar);
        contentRoot.Append(List);
    }

    public void SetItems(IEnumerable<TItem> items)
    {
        this.items = items?.ToArray() ?? Array.Empty<TItem>();
        RefreshRows();
    }

    public void ClearRows()
    {
        List.Clear();
    }

    public void RefreshRows()
    {
        List.Clear();

        IEnumerable<TItem> sorted = items;
        if (sortColumnIndex >= 0 &&
            sortColumnIndex < columns.Count &&
            columns[sortColumnIndex].SortItems != null)
        {
            sorted = columns[sortColumnIndex].SortItems!(items, sortAscending);
        }

        foreach (TItem item in sorted)
        {
            UIPanel row = new();
            row.Width.Set(0f, 1f);
            row.Height.Set(rowHeight, 0f);
            row.SetPadding(0f);

            float left = 0f;
            for (int i = 0; i < columns.Count; i++)
            {
                SortableTableColumn<TItem> column = columns[i];
                row.Append(MakeColumn(left, column, item));
                left += column.Width;
            }

            ApplyRowStyleInternal(row, item, isHovered: false);

            row.OnMouseOver += (_, _) => ApplyRowStyleInternal(row, item, isHovered: true);
            row.OnMouseOut += (_, _) => ApplyRowStyleInternal(row, item, isHovered: false);

            if (OnRowClicked != null)
                row.OnLeftClick += (_, _) => OnRowClicked(item);

            if (OnRowDoubleClicked != null)
                row.OnLeftDoubleClick += (_, _) => OnRowDoubleClicked(item);

            List.Add(row);
        }
    }

    private UIElement MakeColumn(float leftPixels, SortableTableColumn<TItem> column, TItem item)
    {
        UIElement cell = new();
        cell.IgnoresMouseInteraction = true;
        cell.Left.Set(leftPixels - panelPadding, 0f);
        cell.Width.Set(column.Width, 0f);
        cell.Height.Set(0f, 1f);

        UIText label = new(column.CellText(item))
        {
            TextColor = Color.White,
            VAlign = 0.5f,
            IgnoresMouseInteraction = true
        };

        if (column.LeftAligned)
        {
            label.Left.Set(column.TextInset, 0f);
            label.HAlign = 0f;
            label.TextOriginX = 0f;
        }
        else
        {
            label.HAlign = 0.5f;
        }

        cell.Append(label);
        return cell;
    }

    private void ToggleSort(int columnIndex)
    {
        if (sortColumnIndex == columnIndex)
            sortAscending = !sortAscending;
        else
        {
            sortColumnIndex = columnIndex;
            sortAscending = true;
        }

        RefreshRows();
    }

    private void ApplyRowStyleInternal(UIPanel row, TItem item, bool isHovered)
    {
        bool isSelected = IsSelected?.Invoke(item) ?? false;

        if (ApplyRowStyle != null)
        {
            ApplyRowStyle(row, item, isSelected, isHovered);
            return;
        }

        if (isSelected)
        {
            row.BackgroundColor = new Color(73, 94, 171) * 0.95f;
            row.BorderColor = new Color(89, 116, 213);
            return;
        }

        if (isHovered)
        {
            row.BackgroundColor = new Color(73, 94, 171) * 0.9f;
            row.BorderColor = new Color(89, 116, 213);
            return;
        }

        row.BackgroundColor = new Color(63, 82, 151) * 0.35f;
        row.BorderColor = new Color(89, 116, 213) * 0.25f;
    }
}
