//using System.Collections.Generic;

//namespace PvPAdventure.Common.Skins;

//public static class ItemSkinIndex
//{
//    private static readonly Dictionary<string, ItemSkinDefinition> ById = BuildById();

//    public static bool TryGetById(string id, out ItemSkinDefinition def) => ById.TryGetValue(id, out def);

//    private static Dictionary<string, ItemSkinDefinition> BuildById()
//    {
//        Dictionary<string, ItemSkinDefinition> dict = [];

//        for (int i = 0; i < All.Length; i++)
//        {
//            ItemSkinDefinition def = All[i];

//            if (dict.ContainsKey(def.Id))
//            {
//                Log.Error($"Duplicate skin id '{def.Id}' in ItemSkinCatalog.");
//                continue;
//            }

//            dict.Add(def.Id, def);
//        }

//        return dict;
//    }
//}
