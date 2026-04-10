//using PvPAdventure.Common.MainMenu.Profile;
//using System.Collections.Generic;
//using Terraria;
//using Terraria.ID;
//using Terraria.ModLoader;

//namespace PvPAdventure.Common.Skins;

///// <summary>
///// Per-player skin selection tracking.
///// The local player rebuilds from <see cref="ProfileStorage"/> each frame.
///// Remote players receive selections via <see cref="SkinNetHandler"/>.
///// </summary>
//internal sealed class SkinPlayer : ModPlayer
//{
//    /// <summary>
//    /// Maps ItemType → <see cref="SkinDefinition"/> for this player's active skins.
//    /// </summary>
//    private readonly Dictionary<int, SkinDefinition> _selected = [];

//    /// <summary>
//    /// Tracks the last skin ID synced per item type, so we only send net messages on change.
//    /// </summary>
//    private readonly Dictionary<int, string> _lastSynced = [];

//    /// <summary>
//    /// Returns the selected skin for the given item type, if any.
//    /// </summary>
//    public bool TryGetSkin(int itemType, out SkinDefinition def)
//        => _selected.TryGetValue(itemType, out def);

//    public override void PostUpdate()
//    {
//        if (Player.whoAmI != Main.myPlayer)
//            return;

//        if (Main.netMode == NetmodeID.Server)
//            return;

//        ProfileStorage.EnsureLoaded();
//        RebuildLocalSelections();
//        SyncIfChanged();
//    }

//    /// <summary>
//    /// Rebuilds <see cref="_selected"/> from <see cref="ProfileStorage.Skins"/>.
//    /// Called every frame for the local player only.
//    /// </summary>
//    private void RebuildLocalSelections()
//    {
//        _selected.Clear();

//        foreach (string skinId in ProfileStorage.Skins)
//        {
//            if (SkinIndex.TryGetById(skinId, out SkinDefinition def))
//                _selected[def.ItemType] = def;
//        }
//    }

//    /// <summary>
//    /// Sends net messages when the local player's skin selections change.
//    /// Multiplayer client only.
//    /// </summary>
//    private void SyncIfChanged()
//    {
//        if (Main.netMode != NetmodeID.MultiplayerClient)
//            return;

//        // Detect newly selected or changed skins.
//        foreach (var (itemType, def) in _selected)
//        {
//            if (_lastSynced.TryGetValue(itemType, out string lastId) && lastId == def.Id)
//                continue;

//            _lastSynced[itemType] = def.Id;
//            SkinNetHandler.SendSkinSelection(itemType, def.Id);
//        }

//        // Detect removed skins.
//        List<int> removed = null;

//        foreach (var (itemType, _) in _lastSynced)
//        {
//            if (_selected.ContainsKey(itemType))
//                continue;

//            (removed ??= []).Add(itemType);
//            SkinNetHandler.SendSkinSelection(itemType, "");
//        }

//        if (removed != null)
//            foreach (int key in removed)
//                _lastSynced.Remove(key);
//    }

//    /// <summary>
//    /// Called by <see cref="SkinNetHandler"/> to apply a remote player's skin selection.
//    /// </summary>
//    internal void ApplyRemoteSkin(int itemType, string skinId)
//    {
//        if (string.IsNullOrEmpty(skinId))
//        {
//            _selected.Remove(itemType);
//            return;
//        }

//        if (SkinIndex.TryGetById(skinId, out SkinDefinition def) && def.ItemType == itemType)
//            _selected[itemType] = def;
//    }
//}
