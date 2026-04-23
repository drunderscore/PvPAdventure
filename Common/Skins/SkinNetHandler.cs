using PvPAdventure.Common.MainMenu.Shop;
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

    private static readonly Dictionary<int, ProductKey> _lastSent = [];

    public static void SendSelectedSkin(int itemType, ProductKey key, bool force = false)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        if (!force && _lastSent.TryGetValue(itemType, out ProductKey last) && last == key)
            return;

        _lastSent[itemType] = key;

        ModPacket p = ModContent.GetInstance<PvPAdventure>().GetPacket();
        p.Write((byte)AdventurePacketIdentifier.Skins);
        p.Write((byte)Msg.SetSelectedSkin);
        p.Write(itemType);
        p.Write(key.Prototype ?? "");
        p.Write(key.Name ?? "");
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
        string prototype = reader.ReadString();
        string name = reader.ReadString();

        ProductKey key = new(prototype, name);
        Player player = Main.player[whoAmI];

        if (player is null || !player.active)
            return;

        if (key.IsValid && (!SkinRegistry.TryGetByKey(key, out ShopProduct def) || def.ItemType != itemType))
            return;

        bool changed = false;

        foreach (Item item in player.inventory)
        {
            if (item is null || item.IsAir || item.type != itemType)
                continue;

            if (!item.TryGetGlobalItem(out SkinItemData data) || data.Key == key)
                continue;

            data.Key = key;
            changed = true;
        }

        if (changed)
        {
            NetMessage.SendData(MessageID.SyncPlayer, -1, -1, null, player.whoAmI);
            NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, player.whoAmI);
        }
    }
}