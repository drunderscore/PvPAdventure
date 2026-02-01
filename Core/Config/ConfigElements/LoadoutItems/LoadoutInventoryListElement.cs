using PvPAdventure.Common.Arenas;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria.Localization;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace PvPAdventure.Core.Config.ConfigElements.LoadoutItems;

internal sealed class LoadoutInventoryListElement : ListElement
{
    private PropertyFieldWrapper _valueMember;
    private Type _wrapperType;
    private int _lastCount = -1;

    private static readonly FieldInfo ObjectElementExpandedField =
        typeof(ObjectElement).GetField("expanded", BindingFlags.Instance | BindingFlags.NonPublic);

    private sealed class EntryWrapper<T>
    {
        private readonly IList _list;
        private readonly int _index;

        public EntryWrapper(IList list, int index)
        {
            _list = list;
            _index = index;
        }

        [Expand(false)]
        public T Value
        {
            get => _index >= 0 && _index < _list.Count ? (T)_list[_index] : default;
            set
            {
                if (_index >= 0 && _index < _list.Count)
                    _list[_index] = value;
            }
        }
    }
    public override void OnBind()
    {
        base.OnBind();

        _lastCount = (Data as IList)?.Count ?? -1;

        TextDisplayFunction = () =>
        {
            const int maxItemsShown = 12;
            string summary = BuildInventorySummary(Data as IList, maxItemsShown);

            string baseLabel = Label ?? MemberInfo?.Name ?? "Inventory";
            return string.IsNullOrWhiteSpace(summary) ? baseLabel : $"{baseLabel} {summary}";
        };

        EnsureValueMember();
    }

    protected override void SetupList()
    {
        DataList.Clear();
        int top = 0;

        if (Data is not IList list)
            return;

        bool expandLast = _lastCount >= 0 && list.Count > _lastCount;
        _lastCount = list.Count;

        EnsureNonNullEntries(list);
        EnsureValueMember();

        for (int i = 0; i < list.Count; i++)
        {
            int index = i;
            object wrapper = Activator.CreateInstance(_wrapperType, list, index);

            var tuple = UIModConfig.WrapIt(DataList, ref top, _valueMember, wrapper, index);

            tuple.Item2.Left.Pixels += 24f;
            tuple.Item2.Width.Pixels -= 30f;

            if (expandLast && index == list.Count - 1)
                ForceExpandObjectElement(tuple.Item2);

            if (tuple.Item2 is ConfigElement rowCe)
            {
                rowCe.TextDisplayFunction = () =>
                {
                    if (index < 0 || index >= list.Count || list[index] is not LoadoutItem li || li == null)
                        return $"{index + 1}: Missing";

                    int type = li.Item?.Type ?? 0;
                    int stack = li.Stack < 1 ? 1 : li.Stack;

                    if (type <= 0)
                        return $"{index + 1}: None";

                    string tag = stack == 1 ? $"[i:{type}]" : $"[i/s{stack}:{type}]";
                    return $"{index + 1}: {tag} {li.Item.DisplayName}";
                };
            }

            var delete = new UIModConfigHoverImage(DeleteTexture, Language.GetTextValue("tModLoader.ModConfigRemove"))
            {
                VAlign = 0.5f
            };

            delete.OnLeftClick += (_, _) =>
            {
                ((IList)Data).RemoveAt(index);
                SetupList();
                Interface.modConfig.SetPendingChanges();
            };

            tuple.Item1.Append(delete);
        }
    }

    private static void ForceExpandObjectElement(UIElement rowElement)
    {
        ObjectElement oe = rowElement as ObjectElement;
        if (oe == null)
            oe = FindFirstObjectElement(rowElement);

        if (oe == null || ObjectElementExpandedField == null)
            return;

        ObjectElementExpandedField.SetValue(oe, true);
        oe.Recalculate();
    }

    private static ObjectElement FindFirstObjectElement(UIElement element)
    {
        for (int i = 0; i < element.Elements.Count; i++)
        {
            UIElement child = element.Elements[i];
            if (child is ObjectElement oe)
                return oe;

            ObjectElement nested = FindFirstObjectElement(child);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private void EnsureValueMember()
    {
        if (_valueMember != null)
            return;

        Type elementType = listType ?? MemberInfo.Type.GetGenericArguments()[0];
        _wrapperType = typeof(EntryWrapper<>).MakeGenericType(elementType);

        IList dummyList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
        object dummyWrapper = Activator.CreateInstance(_wrapperType, dummyList, 0);

        _valueMember = ConfigManager.GetFieldsAndProperties(dummyWrapper).First(p => p.Name == "Value");
    }

    private static void EnsureNonNullEntries(IList list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] is LoadoutItem)
                continue;

            list[i] = new LoadoutItem();
        }
    }

    private static string BuildInventorySummary(IList list, int maxShown)
    {
        if (list == null || list.Count == 0)
            return "";

        List<string> shown = [];
        int validCount = 0;

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] is not LoadoutItem li || li == null)
                continue;

            int type = li.Item?.Type ?? 0;
            if (type <= 0)
                continue;

            validCount++;

            if (shown.Count >= maxShown)
                continue;

            int stack = li.Stack < 1 ? 1 : li.Stack;
            shown.Add(stack == 1 ? $"[i:{type}]" : $"[i/s{stack}:{type}]");
        }

        if (shown.Count == 0)
            return "";

        int hidden = Math.Max(0, validCount - shown.Count);
        return hidden > 0 ? string.Join(" ", shown) + $" +{hidden}" : string.Join(" ", shown);
    }
}
