using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace PvPAdventure.Core.Config.ConfigElements;

internal class InvasionDictionaryElement : DictionaryElement
{
    protected override void SetupList()
    {
        base.DataList.Clear();
        int top = 0;
        dataWrapperList = [];
        Type type = typeof(DictionaryElementWrapper<,>).MakeGenericType(keyType, valueType);
        if (base.Data == null)
        {
            return;
        }

        ICollection keys = ((IDictionary)base.Data).Keys;
        ICollection values = ((IDictionary)base.Data).Values;
        IEnumerator enumerator = keys.GetEnumerator();
        IEnumerator enumerator2 = values.GetEnumerator();
        int num = 0;
        while (enumerator.MoveNext())
        {
            enumerator2.MoveNext();
            object keyObj = enumerator.Current;
            IDictionaryElementWrapper item = (IDictionaryElementWrapper)Activator.CreateInstance(type, keyObj, enumerator2.Current, (IDictionary)base.Data);
            dataWrapperList.Add(item);
            _ = base.MemberInfo.Type.GetGenericArguments()[0];
            PropertyFieldWrapper memberInfo = ConfigManager.GetFieldsAndProperties(this).ToList()[0];
            Tuple<UIElement, UIElement> tuple = UIModConfig.WrapIt(base.DataList, ref top, memberInfo, this, 0, dataWrapperList, type, num);
            tuple.Item2.Left.Pixels += 24f;
            tuple.Item2.Width.Pixels -= 24f;

            // Collapse!!! Both these probably are overkill but it works, so, oh well.
            //CollapseEntry(tuple.Item2);
            //expanded = false;

            //if (TryCreateKeyIcon(keyObj, out UIElement icon))
            //{
            //    icon.Left.Set(24f, 0f);
            //    icon.VAlign = 0.5f;
            //    tuple.Item1.Append(icon);

            //    tuple.Item2.Left.Pixels += 20f;
            //    tuple.Item2.Width.Pixels -= 20f;
            //}

            // --- Add to the text, displaying 1: <extra text> instead of just the raw key ---
            int displayIndex = num + 1;
            object capturedKey = keyObj;

            if (tuple.Item2 is ConfigElement configElement)
            {
                configElement.TextDisplayFunction = () => $"{displayIndex}: {FormatKeyWithItemTag(capturedKey)}";
            }

            UIModConfigHoverImage uIModConfigHoverImage = new(base.DeleteTexture, Language.GetTextValue("tModLoader.ModConfigRemove"))
            {
                VAlign = 0.5f
            };
            object o = keyObj;
            uIModConfigHoverImage.OnLeftClick += delegate
            {
                ((IDictionary)base.Data).Remove(o);
                SetupList();
                Interface.modConfig.SetPendingChanges();
            };
            tuple.Item1.Append(uIModConfigHoverImage);
            num++;
        }
    }

    private static string FormatKeyWithItemTag(object keyObj)
    {
        if (keyObj is int invasionId)
        {
            if (InvasionNames.TryGetValue(invasionId, out var name))
                return name;

            return $"Unknown Invasion ({invasionId})";
        }

        return keyObj?.ToString() ?? "null";
    }

    // InvasionID's 0-4.
    private static readonly Dictionary<int, string> InvasionNames = new()
    {
        { InvasionID.None, "None" },
        { InvasionID.GoblinArmy, "Goblin Army" },
        { InvasionID.SnowLegion, "Snow Legion" },
        { InvasionID.PirateInvasion, "Pirate Invasion" },
        { InvasionID.MartianMadness, "Martian Madness" }
    };
}
