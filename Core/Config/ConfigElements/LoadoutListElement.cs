using PvPAdventure.Common.Arenas;
using System;
using System.Collections;
using System.Linq;
using Terraria.Localization;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace PvPAdventure.Core.Config.ConfigElements;

internal sealed class LoadoutListElement : ListElement
{
    private PropertyFieldWrapper _valueMember;

    private sealed class EntryWrapper<T>(IList list, int index)
    {
        public T Value
        {
            get => index >= 0 && index < list.Count ? (T)list[index] : default;
            set { if (index >= 0 && index < list.Count) list[index] = value; }
        }
    }

    public override void OnBind()
    {
        base.OnBind();
        EnsureValueMember();
    }

    protected override void SetupList()
    {
        DataList.Clear();
        int top = 0;

        if (Data is not IList list)
            return;

        EnsureValueMember();
        EnsureNonNullLoadouts(list);

        Type elementType = listType ?? MemberInfo.Type.GetGenericArguments()[0];
        Type wrapperType = typeof(EntryWrapper<>).MakeGenericType(elementType);

        for (int i = 0; i < list.Count; i++)
        {
            int index = i;
            object wrapper = Activator.CreateInstance(wrapperType, list, index);

            // IMPORTANT: Use the overload where list/arrayType/index are NOT supplied,
            // so WrapIt binds directly to wrapper.Value (no recursion, no dummy-value bug).
            var tuple = UIModConfig.WrapIt(DataList, ref top, _valueMember, wrapper, index);

            tuple.Item2.Left.Pixels += 24f;
            tuple.Item2.Width.Pixels -= 30f;

            if (tuple.Item2 is ConfigElement ce)
                ce.TextDisplayFunction = () => FormatLoadoutRow(list, index);

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

    private static void EnsureNonNullLoadouts(IList list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] is not Loadout l || l == null)
                list[i] = new Loadout();

            l = (Loadout)list[i];
            l.Armor ??= new Armor();
            l.Accessories ??= new Accessories();
            l.Inventory ??= new System.Collections.Generic.List<LoadoutItem>();
        }
    }

    private static string FormatLoadoutRow(IList list, int index)
    {
        int displayIndex = index + 1;

        if (index < 0 || index >= list.Count || list[index] is not Loadout l || l == null)
            return $"{displayIndex}: Missing";

        string name = string.IsNullOrWhiteSpace(l.Name) ? "Unnamed Loadout" : l.Name;

        string armor = JoinTags("Armor:", Tag(l.Armor.Head), Tag(l.Armor.Body), Tag(l.Armor.Legs));
        string acc = JoinTags("Acc:", Tag(l.Accessories.Accessory1), Tag(l.Accessories.Accessory2), Tag(l.Accessories.Accessory3), Tag(l.Accessories.Accessory4), Tag(l.Accessories.Accessory5));
        string inv = InventoryTags(l);

        string misc = JoinTags("",
            string.IsNullOrEmpty(Tag(l.GrapplingHook)) ? "" : "Hook: " + Tag(l.GrapplingHook),
            string.IsNullOrEmpty(Tag(l.Mount)) ? "" : "Mount: " + Tag(l.Mount)
        );

        string line = $"{displayIndex}: {name}";
        if (!string.IsNullOrEmpty(armor)) line += "   " + armor;
        if (!string.IsNullOrEmpty(acc)) line += "   " + acc;
        if (!string.IsNullOrEmpty(inv)) line += "   " + inv;
        if (!string.IsNullOrEmpty(misc)) line += "   " + misc;

        return line;
    }

    private static string Tag(ItemDefinition d)
    {
        int type = d?.Type ?? 0;
        return type > 0 ? $"[i:{type}]" : "";
    }

    private static string JoinTags(string prefix, params string[] parts)
    {
        string s = string.Join(" ", parts.Where(p => !string.IsNullOrEmpty(p)));
        if (string.IsNullOrEmpty(s))
            return "";

        return string.IsNullOrEmpty(prefix) ? s : (prefix + " " + s);
    }

    private static string InventoryTags(Loadout l)
    {
        if (l.Inventory == null || l.Inventory.Count == 0)
            return "";

        const int maxShown = 12;

        var shown = l.Inventory
            .Where(li => (li?.Item?.Type ?? 0) > 0)
            .Select(li =>
            {
                int type = li.Item.Type;
                int stack = li.Stack < 1 ? 1 : li.Stack;
                return stack == 1 ? $"[i:{type}]" : $"[i:{type}]x{stack}";
            })
            .Take(maxShown)
            .ToList();

        if (shown.Count == 0)
            return "";

        int hidden = Math.Max(0, l.Inventory.Count - maxShown);
        return "Inv: " + string.Join(" ", shown) + (hidden > 0 ? $" +{hidden}" : "");
    }
}
