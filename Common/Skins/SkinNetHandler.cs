using PvPAdventure.Core.Net;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Skins;

internal static class SkinNetHandler
{
    private enum Msg : byte { SetSelectedSkin = 1 }

    private static readonly Dictionary<int, string> _lastSent = [];

    public static void SendSelectedSkin(int itemType, string skinId)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        skinId ??= "";

        if (_lastSent.TryGetValue(itemType, out string last) && last == skinId)
            return;

        _lastSent[itemType] = skinId;

        ModPacket p = ModContent.GetInstance<PvPAdventure>().GetPacket();
        p.Write((byte)AdventurePacketIdentifier.Skins);
        p.Write((byte)Msg.SetSelectedSkin);
        p.Write(itemType);
        p.Write(skinId);
        p.Send();
    }

    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        if ((Msg)reader.ReadByte() == Msg.SetSelectedSkin)
            HandleSetSelectedSkin(reader, whoAmI);
    }

    private static void HandleSetSelectedSkin(BinaryReader reader, int whoAmI)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        int itemType = reader.ReadInt32();
        string skinId = reader.ReadString();

        Player player = Main.player[whoAmI];
        if (player is null || !player.active)
            return;

        if (!string.IsNullOrEmpty(skinId) &&
            (!SkinRegistry.TryGetById(skinId, out SkinDefinition def) || def.ItemType != itemType))
            return;

        bool changed = false;
        foreach (Item item in player.inventory)
        {
            if (item is null || item.IsAir || item.type != itemType)
                continue;
            if (!item.TryGetGlobalItem(out SkinItemData data) || data.SkinId == skinId)
                continue;
            data.SkinId = skinId;
            changed = true;
        }

        if (changed)
            NetMessage.SendData(MessageID.SyncPlayer, -1, -1, null, player.whoAmI);
    }
}