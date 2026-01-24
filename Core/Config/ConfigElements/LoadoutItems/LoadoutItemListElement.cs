using PvPAdventure.Common.Arenas;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Terraria.Localization;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace PvPAdventure.Core.Config.ConfigElements.LoadoutItems;

internal sealed class LoadoutItemListElement : ListElement
{
    private PropertyFieldWrapper _valueMember;
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

        // IMPORTANT: collapses each list entry (ObjectElement) by default
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
        EnsureValueMember();

        _lastCount = (Data as IList)?.Count ?? -1;

        // Header summary for the whole option
        TextDisplayFunction = () =>
        {
            if (Data is not IList list || list.Count == 0)
                return "Starting Items:";

            return $"Starting Items: {string.Join(" ", Enumerable.Range(0, list.Count).Select(i => Format(list, i, false)).Where(s => s != "Empty" && s != "Missing"))}";
        };
    }

    protected override void SetupList()
    {
        DataList.Clear();
        int top = 0;

        if (Data is not IList list)
            return;

        bool expandLast = _lastCount >= 0 && list.Count > _lastCount;
        _lastCount = list.Count;

        EnsureValueMember();

        Type elementType = listType ?? MemberInfo.Type.GetGenericArguments()[0];
        Type wrapperType = typeof(EntryWrapper<>).MakeGenericType(elementType);

        for (int i = 0; i < list.Count; i++)
        {
            int index = i;
            object wrapper = Activator.CreateInstance(wrapperType, list, index);

            var tuple = UIModConfig.WrapIt(DataList, ref top, _valueMember, wrapper, index);

            tuple.Item2.Left.Pixels += 24f;
            tuple.Item2.Width.Pixels -= 30f;

            if (expandLast && index == list.Count - 1)
                ForceExpandObjectElement(tuple.Item2);

            if (tuple.Item2 is ConfigElement ce)
                ce.TextDisplayFunction = () => $"{index + 1}: {Format(list, index, true)}";

            var delete = new UIModConfigHoverImage(DeleteTexture, Language.GetTextValue("tModLoader.ModConfigRemove"))
            {
                VAlign = 0.5f
            };

            delete.OnLeftClick += (_, _) =>
            {
                ((IList)Data).RemoveAt(index);
                _lastCount = ((IList)Data).Count;
                SetupList();
                Interface.modConfig.SetPendingChanges();
            };

            tuple.Item1.Append(delete);
        }
    }

    private static string Format(IList list, int index, bool showDisplayName = false)
    {
        if (index < 0 || index >= list.Count || list[index] is not LoadoutItem li)
            return "Missing";

        int type = li.Item?.Type ?? 0;
        if (type <= 0)
            return "None";

        int max = LoadoutItem.GetMaxStack(type);
        int stack = Math.Clamp(li.Stack, 1, max);

        if (showDisplayName)
            return stack == 1 ? $"[i:{type}] {li.Item.DisplayName}" : $"[i/s{stack}:{type}] {li.Item.DisplayName}";

        return stack == 1 ? $"[i:{type}]" : $"[i/s{stack}:{type}]";
    }

    private void EnsureValueMember()
    {
        if (_valueMember != null)
            return;

        Type elementType = listType ?? MemberInfo.Type.GetGenericArguments()[0];
        Type wrapperType = typeof(EntryWrapper<>).MakeGenericType(elementType);

        IList dummyList = (IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(elementType));
        object dummyWrapper = Activator.CreateInstance(wrapperType, dummyList, 0);

        _valueMember = ConfigManager.GetFieldsAndProperties(dummyWrapper).First(p => p.Name == "Value");
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
}
