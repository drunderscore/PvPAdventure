using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.UI;

internal abstract class UIBrowserPanel : UIDraggablePanel
{
    #region Fields
    protected UIGrid grid;
    protected UIScrollbar scrollbar;
    private UIBrowserViewToggle viewToggle;
    protected UIText searchLabel;
    protected UIElement headerRow;
    protected UISearchbox searchbox;
    protected UIBrowserSizeSlider sizeSlider;

    // Sorting
    private UIBrowserSortButton sortButton;
    private readonly List<UIBrowserSort> sorts = [];
    private int sortIndex;
    protected virtual List<UIBrowserSort> GetSorts() => [];
    private string CurrentSortText => sorts.Count > 0 ? $"Sort: {sorts[sortIndex].Name}" : "Sort";


    protected readonly List<UIBrowserEntry> entries = [];

    protected bool listMode = true;

    protected int listEntrySize = 100;
    protected int gridEntrySize = 64;

    public virtual int ListMinEntrySize => 64;
    public virtual int ListMaxEntrySize => 128;

    public virtual int GridMinEntrySize => 40;
    public virtual int GridMaxEntrySize => 130;

    protected int CurrentEntrySize
    {
        get => listMode ? listEntrySize : gridEntrySize;
        set
        {
            if (listMode)
                listEntrySize = Math.Clamp(value, ListMinEntrySize, ListMaxEntrySize);
            else
                gridEntrySize = Math.Clamp(value, GridMinEntrySize, GridMaxEntrySize);
        }
    }

    protected int CurrentMinEntrySize => listMode ? ListMinEntrySize : GridMinEntrySize;
    protected int CurrentMaxEntrySize => listMode ? ListMaxEntrySize : GridMaxEntrySize;

    public int MinEntrySize => CurrentMinEntrySize;
    public int MaxEntrySize => CurrentMaxEntrySize;

    protected UIBrowserPanel(string title) : base(title)
    {
        Width.Set(520f, 0f);
        Height.Set(540f, 0f);

        listEntrySize = Math.Clamp(listEntrySize, ListMinEntrySize, ListMaxEntrySize);
        gridEntrySize = Math.Clamp(gridEntrySize, GridMinEntrySize, GridMaxEntrySize);

        Rebuild();
    }

    protected override bool ShowResizeButton => true;
    protected override void OnClosePanelLeftClick() => Remove();
    protected override void OnRefreshPanelLeftClick() => Rebuild();
    protected virtual void OnViewModeChanged() { }

    protected abstract void PopulateEntries();
    #endregion

    #region Methods
    private void Rebuild()
    {
        headerRow?.Remove();
        scrollbar?.Remove();
        grid?.Remove();
        viewToggle?.Remove();

        headerRow = new UIElement();
        headerRow.Left.Set(8f, 0f);
        headerRow.Top.Set(8f, 0f);
        headerRow.Width.Set(-16f, 1f);
        headerRow.Height.Set(32f, 0f);
        headerRow.SetPadding(0f);
        ContentPanel.Append(headerRow);

        searchbox = new UISearchbox("");
        searchbox.OnTextChanged += RefreshEntries;
        searchbox.Left.Set(0f, 0f);
        searchbox.Top.Set(0f, 0f);
        searchbox.Width.Set(220f, 0f);
        searchbox.Height.Set(32f, 0f);
        headerRow.Append(searchbox);

        viewToggle = new UIBrowserViewToggle(listMode ? Ass.List : Ass.Grid, true, () => listMode);
        viewToggle.Left.Set(240f, 0f);
        viewToggle.Top.Set(0f, 0f);
        viewToggle.SetVisibility(1f, 1f);
        viewToggle.OnLeftClick += (_, _) =>
        {
            listMode = !listMode;
            CurrentEntrySize = CurrentEntrySize;
            viewToggle.SetImageWithoutSettingSize(listMode ? Ass.List : Ass.Grid);

            if (sizeSlider != null)
                sizeSlider.Ratio = (CurrentEntrySize - CurrentMinEntrySize) / (float)(CurrentMaxEntrySize - CurrentMinEntrySize);

            OnViewModeChanged();
            RefreshEntries();
        };
        headerRow.Append(viewToggle);

        sizeSlider = new(this);
        sizeSlider.Left.Set(290f, 0f);
        sizeSlider.Top.Set(8f, 0f);
        sizeSlider.Width.Set(104f, 0f);
        sizeSlider.Height.Set(16f, 0f);
        sizeSlider.Ratio = (CurrentEntrySize - CurrentMinEntrySize) / (float)(CurrentMaxEntrySize - CurrentMinEntrySize);
        sizeSlider.OnDrag += SetEntrySizeFromSlider;
        headerRow.Append(sizeSlider);

        // Sorts
        sorts.Clear();
        sorts.AddRange(GetSorts());

        if (sorts.Count > 0)
            sortIndex = Math.Clamp(sortIndex, 0, sorts.Count - 1);
        else
            sortIndex = 0;

        sortButton = new UIBrowserSortButton(Ass.Sort, true, () => CurrentSortText);
        sortButton.Left.Set(402f, 0f);
        sortButton.Top.Set(0f, 0f);
        sortButton.SetVisibility(1f, 1f);
        sortButton.OnLeftClick += (_, _) =>
        {
            if (sorts.Count == 0)
                return;

            sortIndex = (sortIndex + 1) % sorts.Count;
            RefreshEntries();
        };
        headerRow.Append(sortButton);

        // Scrollbar
        scrollbar = new UIScrollbar();
        scrollbar.Left.Set(-30f, 1f);
        scrollbar.Top.Set(48f, 0f);
        scrollbar.Height.Set(-80f, 1f);
        ContentPanel.Append(scrollbar);

        grid = new UIGrid();
        grid.Left.Set(10f, 0f);
        grid.Top.Set(50f, 0f);
        grid.Width.Set(-50f, 1f);
        grid.Height.Set(-80f, 1f);
        grid.ListPadding = 4f;
        grid.SetScrollbar(scrollbar);
        grid.OverflowHidden = true;
        ContentPanel.Append(grid);

        entries.Clear();
        PopulateEntries();

        RefreshEntries();
        Recalculate();
    }

    public void RefreshEntries()
    {
        if (grid is null)
            return;

        grid.Clear();

        string search = searchbox?.currentString?.Trim() ?? string.Empty;

        List<UIBrowserEntry> visibleEntries = [];
        foreach (UIBrowserEntry entry in entries)
        {
            bool visible = string.IsNullOrWhiteSpace(search) || entry.SearchText.Contains(search, StringComparison.OrdinalIgnoreCase);
            if (!visible)
                continue;

            entry.SetListMode(listMode);
            entry.SetEntrySize(CurrentEntrySize);
            visibleEntries.Add(entry);
        }

        if (sorts.Count > 0)
            visibleEntries.Sort(sorts[sortIndex].Compare);

        grid.AddRange(visibleEntries);
        grid.Recalculate();
        Recalculate();
    }

    protected void AddEntry(UIBrowserEntry entry)
    {
        entries.Add(entry);
    }

    public void SetEntrySizeFromSlider(float progress)
    {
        progress = MathHelper.Clamp(progress, 0f, 1f);
        CurrentEntrySize = (int)MathHelper.Lerp(CurrentMinEntrySize, CurrentMaxEntrySize, progress);
        RefreshEntries();
    }

    public string GetSearchValue() => searchbox?.currentString ?? string.Empty;

    public override void Draw(SpriteBatch sb)
    {
        base.Draw(sb);

#if DEBUG
        //DebugDrawer.DrawElement(sb, headerRow);
        //DebugDrawer.DrawElement(sb, searchbox);
        //DebugDrawer.DrawElement(sb, viewToggle);
        //DebugDrawer.DrawElement(sb, sizeSlider);
        //DebugDrawer.DrawElement(sb, grid);
        //DebugDrawer.DrawElement(sb, scrollbar);
#endif
    }

    private void UpdateHeaderLayout()
    {
        float width = ContentPanel.GetInnerDimensions().Width - 16;

        bool showViewToggle = width >= 300f;
        bool showSlider = width >= 450f;
        bool showSort = width >= 560f && sorts.Count > 0;

        if (showViewToggle)
        {
            if (viewToggle?.Parent is null)
                headerRow.Append(viewToggle);
        }
        else viewToggle?.Remove();

        if (showSlider)
        {
            if (sizeSlider?.Parent is null)
                headerRow.Append(sizeSlider);
        }
        else sizeSlider?.Remove();

        if (showSort)
        {
            if (sortButton?.Parent is null)
                headerRow.Append(sortButton);
        }
        else sortButton?.Remove();
    }

    private void UpdateScrollbarVisibility()
    {
        bool needsScroll = scrollbar.CanScroll;

        if (needsScroll && scrollbar.Parent is null)
            ContentPanel.Append(scrollbar);
        else if (!needsScroll)
            scrollbar?.Remove();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        UpdateHeaderLayout();
        UpdateScrollbarVisibility();

#if DEBUG
        if (Main.keyState.IsKeyDown(Keys.F5) && !Main.oldKeyState.IsKeyDown(Keys.F5))
        {
            Log.Chat("Rebuilding browser");
            Rebuild();
        }
#endif
    }

    #endregion
}

internal class UIBrowserEntry : UIElement
{
    public string SearchText;

    protected bool listMode = true;
    protected int entrySize = 80;

    public UIBrowserEntry()
    {
        listMode = true;
        Width.Set(0f, 1f);
        Height.Set(entrySize, 0f);
    }

    public virtual void SetListMode(bool value)
    {
        listMode = value;

        if (listMode)
        {
            Width.Set(0f, 1f);
            Height.Set(entrySize, 0f);
        }
        else
        {
            Width.Set(entrySize, 0f);
            Height.Set(entrySize, 0f);
        }

        Recalculate();
    }

    public virtual void SetEntrySize(int size)
    {
        entrySize = size;

        if (listMode)
        {
            Width.Set(0f, 1f);
            Height.Set(entrySize, 0f);
        }
        else
        {
            Width.Set(entrySize * 2.2f, 0f);
            Height.Set(entrySize * 1.4f, 0f);
        }

        Recalculate();
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

    }
}

internal sealed class UIBrowserSizeSlider : UISlider
{
    public UIBrowserSizeSlider(UIBrowserPanel owner)
    {
        OnDraw += _ =>
        {
            if (IsMouseHovering)
            {
                string tooltip = $"Button size: {(int)MathHelper.Lerp(owner.MinEntrySize, owner.MaxEntrySize, Ratio)}";
                UICommon.TooltipMouseText(tooltip);
                //Main.instance.MouseText(tooltip);
            }
        };
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
    }
}

internal sealed class UIBrowserViewToggle : UIColoredImageButton
{
    private readonly Func<bool> getListMode;

    public UIBrowserViewToggle(Asset<Texture2D> texture, bool isSmall, Func<bool> getListMode) : base(texture, isSmall)
    {
        this.getListMode = getListMode;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);

        if (IsMouseHovering)
            UICommon.TooltipMouseText(getListMode() ? "List mode\nClick to switch to grid mode" : "Grid mode\nClick to switch to list mode");
    }
}

internal sealed class UIBrowserSortButton : UIColoredImageButton
{
    private readonly Func<string> getText;

    public UIBrowserSortButton(Asset<Texture2D> texture, bool isSmall, Func<string> getText) : base(texture, isSmall)
    {
        this.getText = getText;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);

        if (IsMouseHovering)
            UICommon.TooltipMouseText($"{getText()}");
    }
}

internal sealed class UIBrowserSort
{
    public string Name { get; }
    public Comparison<UIBrowserEntry> Compare { get; }

    public UIBrowserSort(string name, Comparison<UIBrowserEntry> compare)
    {
        Name = name;
        Compare = compare;
    }
}