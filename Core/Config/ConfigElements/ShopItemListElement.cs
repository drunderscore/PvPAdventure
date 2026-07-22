using System;
using System.Collections;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader.Config.UI;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace PvPAdventure.Core.Config.ConfigElements;

internal sealed class ShopItemListElement : ListElement
{
    protected override void SetupList()
    {
        DataList.Clear();
        int top = 0;

        if (Data is not IList items)
            return;

        Type itemType = MemberInfo.Type.GetGenericArguments()[0];
        for (int i = 0; i < items.Count; i++)
        {
            int index = i;
            ServerConfig.ShopItem shopItem = items[index] as ServerConfig.ShopItem;
            Tuple<UIElement, UIElement> wrapped = UIModConfig.WrapIt(
                DataList, ref top, MemberInfo, Item, 0, Data, itemType, index);

            wrapped.Item2.Left.Pixels += 24f;
            wrapped.Item2.Width.Pixels -= 30f;
            Collapse(wrapped.Item2);

            if (wrapped.Item2 is ConfigElement element)
                element.TextDisplayFunction = () => $"{index + 1}: {Display(shopItem)}";

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
