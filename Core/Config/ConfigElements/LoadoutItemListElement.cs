using PvPAdventure.Common.Arenas;
using System;
using System.Collections;
using System.Linq;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace PvPAdventure.Core.Config.ConfigElements;

internal sealed class LoadoutItemListElement : ListElement
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

        Type elementType = listType ?? MemberInfo.Type.GetGenericArguments()[0];
        Type wrapperType = typeof(EntryWrapper<>).MakeGenericType(elementType);

        for (int i = 0; i < list.Count; i++)
        {
            int index = i;
            object wrapper = Activator.CreateInstance(wrapperType, list, index);

            var tuple = UIModConfig.WrapIt(DataList, ref top, _valueMember, wrapper, index);

            tuple.Item2.Left.Pixels += 24f;
            tuple.Item2.Width.Pixels -= 30f;

            if (tuple.Item2 is ConfigElement ce)
                ce.TextDisplayFunction = () => $"{index + 1}: {Format(list, index)}";

            var delete = new UIModConfigHoverImage(DeleteTexture, Language.GetTextValue("tModLoader.ModConfigRemove")) { VAlign = 0.5f };
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

    private static string Format(IList list, int index)
    {
        if (index < 0 || index >= list.Count)
            return "Missing";

        if (list[index] is not LoadoutItem li)
            return list[index]?.ToString() ?? "Invalid Entry";

        int type = li.Item?.Type ?? 0;
        if (type <= 0)
            return "Empty";

        Item temp = new Item();
        temp.SetDefaults(type);

        int max = Math.Max(1, temp.maxStack);
        int stack = Math.Clamp(li.Stack, 1, max);

        return $"[i:{type}] {Lang.GetItemNameValue(type)} x{stack}";
    }
}
