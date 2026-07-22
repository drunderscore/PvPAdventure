using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace PvPAdventure.Core.Config.ConfigElements;

internal sealed class ShopItemListElement : ListElement
{
    private PropertyFieldWrapper valueMember;
    private Type wrapperType;

    // Wrap each list slot in a plain property that does not carry ShopItems' CustomModConfigItem
    // attribute. Passing the original ShopItems member back to WrapIt recursively constructs this
    // same ShopItemListElement until the process exhausts its stack.
    private sealed class EntryWrapper<T>
    {
        private readonly IList list;
        private readonly int index;

        public EntryWrapper(IList list, int index)
        {
            this.list = list;
            this.index = index;
        }

        [Expand(false)]
        public T Value
        {
            get => index >= 0 && index < list.Count ? (T)list[index] : default;
            set
            {
                if (index >= 0 && index < list.Count)
                    list[index] = value;
            }
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

        if (Data is not IList items)
            return;

        EnsureValueMember();

        for (int i = 0; i < items.Count; i++)
        {
            int index = i;
            object wrapper = Activator.CreateInstance(wrapperType, items, index);
            Tuple<UIElement, UIElement> wrapped = UIModConfig.WrapIt(DataList, ref top, valueMember, wrapper, index);

            wrapped.Item2.Left.Pixels += 24f;
            wrapped.Item2.Width.Pixels -= 30f;
            Collapse(wrapped.Item2);

            if (wrapped.Item2 is ConfigElement element)
                element.TextDisplayFunction = () => $"{index + 1}: {Display(index < items.Count ? items[index] as ServerConfig.ShopItem : null)}";

            UIModConfigHoverImage delete = new(
                DeleteTexture,
                Language.GetTextValue("tModLoader.ModConfigRemove"))
            {
                VAlign = 0.5f
            };

            delete.OnLeftClick += (_, _) =>
            {
                items.RemoveAt(index);
                SetupList();
                Interface.modConfig.SetPendingChanges();
            };
            wrapped.Item1.Append(delete);
        }
    }

    private void EnsureValueMember()
    {
        if (valueMember != null)
            return;

        Type itemType = listType ?? MemberInfo.Type.GetGenericArguments()[0];
        wrapperType = typeof(EntryWrapper<>).MakeGenericType(itemType);

        IList dummyList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType));
        object dummyWrapper = Activator.CreateInstance(wrapperType, dummyList, 0);
        valueMember = ConfigManager.GetFieldsAndProperties(dummyWrapper)
            .First(member => member.Name == nameof(EntryWrapper<object>.Value));
    }

    private static string Display(ServerConfig.ShopItem entry)
    {
        int type = entry?.Item?.Type ?? ItemID.None;
        string item = type > ItemID.None
            ? $"[i:{type}] {Lang.GetItemNameValue(type)}"
            : "No item";
        string price = entry?.Price > 0 ? Main.ValueToCoins(entry.Price) : "Free";
        return $"{item} — {price}";
    }

    private static void Collapse(UIElement element)
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type type = element.GetType();
        type.GetField("expanded", Flags)?.SetValue(element, false);
        type.GetField("pendingChanges", Flags)?.SetValue(element, true);
    }
}
