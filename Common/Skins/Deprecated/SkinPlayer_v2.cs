//using PvPAdventure.Common.MainMenu.Profile;
//using PvPAdventure.Common.Skins.Net;
//using System.Collections.Generic;
//using Terraria;
//using Terraria.ID;
//using Terraria.ModLoader;

//namespace PvPAdventure.Common.Skins;

///// <summary>
///// Per-player skin selections. The local player rebuilds selections from <see cref="ProfileStorage"/>
///// each frame, while remote players receive theirs via <see cref="SkinNetHandler"/>.
///// This runs on both client and server so the server can relay skin data.
///// </summary>
//internal sealed class SkinPlayer : ModPlayer
//{
//    /// <summary>
//    /// Maps ItemType → SkinDefinition for this player.
//    /// On the local client this is rebuilt from ProfileStorage every frame.
//    /// On the server and other clients this is populated via network messages.
//    /// </summary>
//    private readonly Dictionary<int, SkinDefinition> _selected = [];

//    /// <summary>
//    /// Tracks what we last sent to the server so we only send diffs.
//    /// Only used on the local client in multiplayer.
//    /// </summary>
//    private readonly Dictionary<int, string> _lastSynced = [];

//    /// <summary>
//    /// Try to get the active skin for a given item type on this player.
//    /// Works for any player (local or remote).
//    /// </summary>
//    public bool TryGetSkin(int itemType, out SkinDefinition def)
//        => _selected.TryGetValue(itemType, out def);

//    /// <summary>
//    /// Returns all active skin selections for this player.
//    /// Used by SkinNetHandler to send full state on player join.
//    /// </summary>
//    public IReadOnlyDictionary<int, SkinDefinition> GetAllSkins() => _selected;

//    public override void PostUpdate()
//    {
//        // Only the local player rebuilds from ProfileStorage
//        if (Player.whoAmI != Main.myPlayer)
//            return;

//        // Server doesn't have ProfileStorage
//        if (Main.netMode == NetmodeID.Server)
//            return;

//        ProfileStorage.EnsureLoaded();
//        RebuildLocalSelections();
//        SyncIfChanged();
//    }

//    /// <summary>
//    /// Called by <see cref="SkinNetHandler"/> when receiving a skin update for a remote player
//    /// (or on the server to store the authoritative state).
//    /// </summary>
//    internal void ApplyRemoteSkin(int itemType, string skinId)
//    {
//        if (string.IsNullOrEmpty(skinId))
//        {
//            bool removed = _selected.Remove(itemType);
//            Log.Debug($"[SkinPlayer] ApplyRemoteSkin player={Player.whoAmI} itemType={itemType} skinId=(empty) removed={removed}");
//            return;
//        }

//        if (SkinIndex.TryGetById(skinId, out SkinDefinition def) && def.ItemType == itemType)
//        {
//            _selected[itemType] = def;
//            Log.Debug($"[SkinPlayer] ApplyRemoteSkin player={Player.whoAmI} itemType={itemType} skinId={skinId} name={def.Name} APPLIED");
//        }
//        else
//        {
//            Log.Debug($"[SkinPlayer] ApplyRemoteSkin player={Player.whoAmI} itemType={itemType} skinId={skinId} REJECTED (invalid id or mismatched type)");
//        }
//    }

//    /// <summary>
//    /// Clear all skins. Called on player disconnect / initialize.
//    /// </summary>
//    internal void ClearAll()
//    {
//        _selected.Clear();
//        _lastSynced.Clear();
//        Log.Debug($"[SkinPlayer] ClearAll player={Player.whoAmI}");
//    }

//    public override void Initialize()
//    {
//        _selected.Clear();
//        _lastSynced.Clear();
//    }

//    public override void OnEnterWorld()
//    {
//        if (Player.whoAmI != Main.myPlayer)
//            return;

//        // Force a full resync on world enter
//        _lastSynced.Clear();
//        Log.Debug("[SkinPlayer] OnEnterWorld — cleared lastSynced for full resync");
//    }

//    /// <summary>
//    /// Rebuild _selected from ProfileStorage (local player only).
//    /// </summary>
//    private void RebuildLocalSelections()
//    {
//        _selected.Clear();

//        foreach (string skinId in ProfileStorage.Skins)
//        {
//            if (SkinIndex.TryGetById(skinId, out SkinDefinition def))
//            {
//                _selected[def.ItemType] = def;
//            }
//        }
//    }

//    /// <summary>
//    /// In multiplayer, send any changes to the server.
//    /// Only runs on the local client.
//    /// </summary>
//    private void SyncIfChanged()
//    {
//        if (Main.netMode != NetmodeID.MultiplayerClient)
//            return;

//        // Send new or changed skins
//        foreach (var (itemType, def) in _selected)
//        {
//            if (_lastSynced.TryGetValue(itemType, out string lastId) && lastId == def.Id)
//                continue;

//            _lastSynced[itemType] = def.Id;
//            Log.Debug($"[SkinPlayer] SyncIfChanged SENDING SetSkin itemType={itemType} skinId={def.Id} name={def.Name}");
//            SkinNetHandler.SendSetSkin(itemType, def.Id);
//        }

//        // Send removals for skins that were unequipped
//        List<int> removed = null;

//        foreach (var (itemType, _) in _lastSynced)
//        {
//            if (_selected.ContainsKey(itemType))
//                continue;

//            (removed ??= []).Add(itemType);
//            Log.Debug($"[SkinPlayer] SyncIfChanged SENDING RemoveSkin itemType={itemType}");
//            SkinNetHandler.SendSetSkin(itemType, "");
//        }

//        if (removed != null)
//        {
//            foreach (int key in removed)
//                _lastSynced.Remove(key);
//        }
//    }
//}
