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

internal sealed class LoadoutListElement : ListElement
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
        EnsureValueMember();
        _lastCount = (Data as IList)?.Count ?? -1;
    }

    protected override void SetupList()
    {
        DataList.Clear();
        int top = 0;

        if (Data is not IList list)
            return;

        bool expandLast = _lastCount >= 0 && list.Count > _lastCount;
        _lastCount = list.Count;

        EnsureNonNullLoadouts(list);
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
                    if (index < 0 || index >= list.Count || list[index] is not Loadout l || l == null)
                        return $"{index + 1}: Missing";

                    string name = string.IsNullOrWhiteSpace(l.Name) ? "" : l.Name;

                    // Order: armor -> accessories -> hook -> mount -> inventory
                    string armor = Join(Tag(l.Armor?.Head), Tag(l.Armor?.Body), Tag(l.Armor?.Legs));
                    string acc = Join(
                        Tag(l.Accessories?.Accessory1),
                        Tag(l.Accessories?.Accessory2),
                        Tag(l.Accessories?.Accessory3),
                        Tag(l.Accessories?.Accessory4),
                        Tag(l.Accessories?.Accessory5));

                    string hook = Tag(l.Equipment?.GrapplingHook);
                    string mount = Tag(l.Equipment?.Mount);
                    string inv = InventoryTags(l);

                    return Join($"{index + 1}:", name, armor, acc, hook, mount, inv);
                };
            }

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

    private static void EnsureNonNullLoadouts(IList list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            Loadout l = list[i] as Loadout;
            if (l == null)
            {
                l = new Loadout();
                list[i] = l;
            }

            l.Armor ??= new Armor();
            l.Accessories ??= new Accessories();
            l.Equipment ??= new Equipment();
            l.Inventory ??= new List<LoadoutItem>();
        }
    }

    private static IEnumerable<ConfigElement> EnumerateConfigElements(UIElement element)
    {
        if (element is ConfigElement ce)
            yield return ce;

        for (int i = 0; i < element.Elements.Count; i++)
        {
            foreach (ConfigElement child in EnumerateConfigElements(element.Elements[i]))
                yield return child;
        }
    }

    private static string Tag(ItemDefinition d)
    {
        return TagUtilities.Tag(d);
    }

    private static string InventoryTags(Loadout l)
    {
        List<LoadoutItem> inv = l.Inventory;
        if (inv == null || inv.Count == 0)
            return "";

        const int maxShown = 4;

        List<string> shown = new();
        for (int i = 0; i < inv.Count && shown.Count < maxShown; i++)
        {
            LoadoutItem li = inv[i];
            int type = li?.Item?.Type ?? 0;
            if (type <= 0)
                continue;

            int stack = li.Stack < 1 ? 1 : li.Stack;

            // Example: [i/s10:29] 
            // This is a custom Terraria tag, read the wiki for more info.
            // /s is for stack!
            shown.Add(stack == 1 ? $"[i:{type}]" : $"[i/s{stack}:{type}]");
        }

        if (shown.Count == 0)
            return "";

        int hidden = Math.Max(0, inv.Count - maxShown);
        return hidden > 0 ? string.Join(" ", shown) + $"+{hidden}" : string.Join(" ", shown);
    }

    private static string Join(params string[] parts) =>
        TagUtilities.Join(parts);

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
