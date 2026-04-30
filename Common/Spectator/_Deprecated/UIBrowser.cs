//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;
//using PvPAdventure.Core.Utilities;
//using PvPAdventure.UI;
//using ReLogic.Content;
//using System;
//using System.Collections.Generic;
//using Terraria;
//using Terraria.GameContent.UI.Elements;
//using Terraria.ModLoader.UI;
//using Terraria.ModLoader.UI.Elements;
//using Terraria.UI;

//namespace PvPAdventure.Common.Spectator.UI;

//internal sealed class UIBrowser : UIElement
//{
//    private readonly Action<List<UIBrowserEntry>> populateEntries;
//    private readonly Func<List<UIBrowserSort>> getSorts;
//    private readonly Func<int, string> getHintText;
//    private readonly Func<List<UIBrowserFilter>> getFilters;

//    private UIGrid grid;
//    private UIScrollbar scrollbar;

//    // Header row
//    private UIElement headerRow;
//    private UISearchbox searchbox;
//    private UIBrowserButton viewToggleButton;
//    private UIBrowserSizeSlider sizeSlider;
//    private UIBrowserButton sortButton;
//    private UIBrowserButton refreshButton;

//    // Content
//    private readonly List<UIBrowserButton> filterButtons = [];
//    private readonly List<UIBrowserEntry> entries = [];
//    private readonly List<UIBrowserSort> sorts = [];
//    private readonly List<UIBrowserFilter> filters = [];

//    private bool listMode = true;
//    private int sortIndex;
//    private int listEntrySize = 100;
//    private int gridEntrySize = 64;

//    public int ListMinEntrySize { get; set; } = 64;
//    public int ListMaxEntrySize { get; set; } = 128;
//    public int GridMinEntrySize { get; set; } = 40;
//    public int GridMaxEntrySize { get; set; } = 130;

//    public int MinEntrySize => listMode ? ListMinEntrySize : GridMinEntrySize;
//    public int MaxEntrySize => listMode ? ListMaxEntrySize : GridMaxEntrySize;

//    private string CurrentSortText => sorts.Count > 0 ? sorts[sortIndex].Name : "Sort";

//    private int CurrentEntrySize
//    {
//        get => listMode ? listEntrySize : gridEntrySize;
//        set
//        {
//            if (listMode)
//                listEntrySize = Math.Clamp(value, ListMinEntrySize, ListMaxEntrySize);
//            else
//                gridEntrySize = Math.Clamp(value, GridMinEntrySize, GridMaxEntrySize);
//        }
//    }

//    public UIBrowser(Action<List<UIBrowserEntry>> populateEntries, Func<List<UIBrowserSort>> getSorts = null, Func<List<UIBrowserFilter>> getFilters = null, Func<int, string> getHintText = null)
//    {
//        this.populateEntries = populateEntries;
//        this.getSorts = getSorts;
//        this.getFilters = getFilters;
//        this.getHintText = getHintText;

//        Width.Set(0f, 1f);
//        Height.Set(0f, 1f);
//        SetPadding(0f);

//        listEntrySize = Math.Clamp(listEntrySize, ListMinEntrySize, ListMaxEntrySize);
//        gridEntrySize = Math.Clamp(gridEntrySize, GridMinEntrySize, GridMaxEntrySize);

//        Rebuild();
//    }

//    public void Rebuild()
//    {
//        string oldSearch = searchbox?.currentString ?? string.Empty;
//        HashSet<string> oldSelectedFilters = [];

//        foreach (UIBrowserFilter filter in filters)
//        {
//            if (filter.Selected)
//                oldSelectedFilters.Add(filter.Name);
//        }

//        RemoveAllChildren();
//        filterButtons.Clear();

//        headerRow = new UIElement();
//        headerRow.Left.Set(8f, 0f);
//        headerRow.Top.Set(8f, 0f);
//        headerRow.Width.Set(-16f, 1f);
//        headerRow.Height.Set(32f, 0f);
//        headerRow.SetPadding(0f);
//        Append(headerRow);

//        searchbox = new UISearchbox(string.Empty, oldSearch);
//        searchbox.OnTextChanged += RefreshEntries;
//        searchbox.Left.Set(0f, 0f);
//        searchbox.Top.Set(0f, 0f);
//        searchbox.Width.Set(220f, 0f);
//        searchbox.Height.Set(32f, 0f);
//        headerRow.Append(searchbox);

//        refreshButton = new UIBrowserButton(Ass.Icon_Reset, static () => "Refresh");
//        refreshButton.Left.Set(232f, 0f);
//        refreshButton.Top.Set(0f, 0f);
//        refreshButton.OnLeftClick += (_, _) => Rebuild();
//        headerRow.Append(refreshButton);

//        viewToggleButton = new UIBrowserButton(listMode ? Ass.List : Ass.Grid, () => listMode ? "List mode\nClick to switch to grid mode" : "Grid mode\nClick to switch to list mode");
//        viewToggleButton.Left.Set(268f, 0f);
//        viewToggleButton.Top.Set(0f, 0f);
//        viewToggleButton.OnLeftClick += (_, _) =>
//        {
//            listMode = !listMode;
//            CurrentEntrySize = CurrentEntrySize;
//            viewToggleButton.SetImage(listMode ? Ass.List : Ass.Grid);

//            if (sizeSlider is not null)
//                sizeSlider.Ratio = (CurrentEntrySize - MinEntrySize) / (float)(MaxEntrySize - MinEntrySize);

//            RefreshEntries();
//        };
//        headerRow.Append(viewToggleButton);

//        sizeSlider = new UIBrowserSizeSlider(this);
//        sizeSlider.Left.Set(312f, 0f);
//        sizeSlider.Top.Set(8f, 0f);
//        sizeSlider.Width.Set(104f, 0f);
//        sizeSlider.Height.Set(16f, 0f);
//        sizeSlider.Ratio = (CurrentEntrySize - MinEntrySize) / (float)(MaxEntrySize - MinEntrySize);
//        sizeSlider.OnDrag += SetEntrySizeFromSlider;
//        headerRow.Append(sizeSlider);

//        sorts.Clear();
//        sorts.AddRange(getSorts?.Invoke() ?? []);
//        sortIndex = sorts.Count > 0 ? Math.Clamp(sortIndex, 0, sorts.Count - 1) : 0;

//        if (sorts.Count > 0)
//        {
//            sortButton = new UIBrowserButton(Ass.Sort, () => CurrentSortText);
//            sortButton.Left.Set(424f, 0f);
//            sortButton.Top.Set(0f, 0f);
//            sortButton.OnLeftClick += (_, _) =>
//            {
//                sortIndex = (sortIndex + 1) % sorts.Count;
//                RefreshEntries();
//            };
//            headerRow.Append(sortButton);
//        }

//        filters.Clear();
//        filters.AddRange(getFilters?.Invoke() ?? []);

//        for (int i = 0; i < filters.Count; i++)
//        {
//            UIBrowserFilter filter = filters[i];
//            filter.Selected = oldSelectedFilters.Contains(filter.Name);

//            UIBrowserButton button = new(filter.Icon, () => filter.Tooltip, () => filter.Selected);
//            button.Left.Set(GetFilterButtonLeft(i), 0f);
//            button.Top.Set(0f, 0f);
//            button.OnLeftClick += (_, _) =>
//            {
//                filter.Selected = !filter.Selected;
//                RefreshEntries();
//            };

//            filterButtons.Add(button);
//            headerRow.Append(button);
//        }

//        scrollbar = new UIScrollbar();
//        scrollbar.Left.Set(-30f, 1f);
//        scrollbar.Top.Set(48f, 0f);
//        scrollbar.Height.Set(-80f, 1f);
//        Append(scrollbar);

//        grid = new UIGrid();
//        grid.Left.Set(10f, 0f);
//        grid.Top.Set(50f, 0f);
//        grid.Width.Set(-50f, 1f);
//        grid.Height.Set(-80f, 1f);
//        grid.ListPadding = 4f;
//        grid.SetScrollbar(scrollbar);
//        grid.OverflowHidden = true;
//        Append(grid);

//        entries.Clear();
//        populateEntries?.Invoke(entries);

//        RefreshEntries();
//        Recalculate();
//    }

//    public void RefreshEntries()
//    {
//        if (grid is null)
//            return;

//        grid.Clear();

//        string search = searchbox?.currentString?.Trim() ?? string.Empty;
//        List<UIBrowserEntry> visibleEntries = [];
//        List<UIBrowserFilter> selectedFilters = [];

//        foreach (UIBrowserFilter filter in filters)
//        {
//            if (filter.Selected)
//                selectedFilters.Add(filter);
//        }

//        foreach (UIBrowserEntry entry in entries)
//        {
//            bool visible = string.IsNullOrWhiteSpace(search) || entry.SearchText?.Contains(search, StringComparison.OrdinalIgnoreCase) == true;

//            if (!visible)
//                continue;

//            if (selectedFilters.Count > 0 && !MatchesAnyFilter(entry, selectedFilters))
//                continue;

//            entry.SetListMode(listMode);
//            entry.SetEntrySize(CurrentEntrySize);
//            visibleEntries.Add(entry);
//        }

//        if (sorts.Count > 0)
//            visibleEntries.Sort(sorts[sortIndex].Compare);

//        searchbox?.SetHintText(getHintText?.Invoke(visibleEntries.Count) ?? string.Empty);

//        grid.AddRange(visibleEntries);
//        grid.Recalculate();
//        Recalculate();
//    }

//    private static bool MatchesAnyFilter(UIBrowserEntry entry, List<UIBrowserFilter> selectedFilters)
//    {
//        for (int i = 0; i < selectedFilters.Count; i++)
//        {
//            if (selectedFilters[i].Matches(entry))
//                return true;
//        }

//        return false;
//    }

//    private float GetFilterButtonLeft(int index)
//    {
//        return (sorts.Count > 0 ? 460f : 424f) + index * 36f;
//    }

//    private void SetEntrySizeFromSlider(float progress)
//    {
//        progress = MathHelper.Clamp(progress, 0f, 1f);
//        CurrentEntrySize = (int)MathHelper.Lerp(MinEntrySize, MaxEntrySize, progress);
//        RefreshEntries();
//    }

//    public override void Update(GameTime gameTime)
//    {
//        base.Update(gameTime);

//        if (IsMouseHovering)
//            Main.LocalPlayer.mouseInterface = true; // disble item use

//        UpdateHeaderLayout();
//        UpdateScrollbarVisibility();
//    }

//    private void UpdateHeaderLayout()
//    {
//        if (headerRow is null)
//            return;

//        float width = GetInnerDimensions().Width - 16f;

//        SetHeaderChild(refreshButton, width >= 264f);
//        SetHeaderChild(viewToggleButton, width >= 300f);
//        SetHeaderChild(sizeSlider, width >= 424f);
//        SetHeaderChild(sortButton, width >= 464f && sorts.Count > 0);

//        for (int i = 0; i < filterButtons.Count; i++)
//            SetHeaderChild(filterButtons[i], width >= GetFilterButtonLeft(i) + 32f);
//    }

//    private void SetHeaderChild(UIElement element, bool visible)
//    {
//        if (element is null)
//            return;

//        if (visible && element.Parent is null)
//            headerRow.Append(element);
//        else if (!visible)
//            element.Remove();
//    }

//    private void UpdateScrollbarVisibility()
//    {
//        if (scrollbar is null)
//            return;

//        bool needsScroll = scrollbar.CanScroll;

//        if (needsScroll && scrollbar.Parent is null)
//            Append(scrollbar);
//        else if (!needsScroll)
//            scrollbar.Remove();
//    }

//    private sealed class UIBrowserButton : UIColoredImageButton
//    {
//        private readonly Func<string> getTooltip;
//        private readonly Func<bool> isSelected;

//        public UIBrowserButton(Asset<Texture2D> texture, Func<string> getTooltip, Func<bool> isSelected = null) : base(texture, isSmall: true)
//        {
//            this.getTooltip = getTooltip;
//            this.isSelected = isSelected;

//            Width.Set(32f, 0f);
//            Height.Set(32f, 0f);
//            SetVisibility(1f, 1f);
//        }

//        protected override void DrawSelf(SpriteBatch spriteBatch)
//        {
//            SetSelected(isSelected?.Invoke() == true);
//            base.DrawSelf(spriteBatch);

//            if (IsMouseHovering)
//                UICommon.TooltipMouseText(getTooltip?.Invoke() ?? string.Empty);
//        }
//    }

//    private sealed class UIBrowserSizeSlider : UISlider
//    {
//        private readonly UIBrowser owner;

//        public UIBrowserSizeSlider(UIBrowser owner)
//        {
//            this.owner = owner;

//            OnDraw += _ =>
//            {
//                if (IsMouseHovering)
//                    UICommon.TooltipMouseText($"Size: {owner.CurrentEntrySize}");
//            };
//        }
//    }
//}
//public class UIBrowserEntry : UIElement
//{
//    public string SearchText;

//    protected bool listMode = true;
//    protected int entrySize = 80;

//    public UIBrowserEntry()
//    {
//        Width.Set(0f, 1f);
//        Height.Set(entrySize, 0f);
//    }

//    public virtual void SetListMode(bool value)
//    {
//        listMode = value;
//        Width.Set(listMode ? 0f : entrySize, listMode ? 1f : 0f);
//        Height.Set(entrySize, 0f);
//        Recalculate();
//    }

//    public virtual void SetEntrySize(int size)
//    {
//        entrySize = size;
//        Width.Set(listMode ? 0f : entrySize, listMode ? 1f : 0f);
//        Height.Set(entrySize, 0f);
//        Recalculate();
//    }
//}

//internal sealed class UIBrowserSort(string name, Comparison<UIBrowserEntry> compare)
//{
//    public string Name { get; } = name;
//    public Comparison<UIBrowserEntry> Compare { get; } = compare;
//}

//internal sealed class UIBrowserFilter(string name, string tooltip, Asset<Texture2D> icon, Predicate<UIBrowserEntry> matches)
//{
//    public string Name { get; } = name;
//    public string Tooltip { get; } = tooltip;
//    public Asset<Texture2D> Icon { get; } = icon;
//    public Predicate<UIBrowserEntry> Matches { get; } = matches;
//    public bool Selected { get; set; }
//}
