using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Common.NPCs;

internal static class ShakingChestLoadouts
{
    internal const int SlotCount = 3;

    private static readonly TagCompound[] Slots = new TagCompound[SlotCount];
    private static readonly string Folder = Path.Combine(Main.SavePath, "PvPAdventure");
    private static readonly string FilePath = Path.Combine(Folder, "StartingLoadouts.nbt");
    private static bool _loaded;

    internal static bool HasSlot(int slot)
    {
        Load();
        return Valid(slot) && Slots[slot] != null;
    }

    internal static bool Save(int slot, Player player)
    {
        Load();
        if (!Valid(slot))
            return false;

        Slots[slot] = new TagCompound
        {
            ["Inventory"] = Save(player.inventory),
            ["Armor"] = Save(player.armor),
            ["Dye"] = Save(player.dye),
            ["MiscEquips"] = Save(player.miscEquips),
            ["MiscDyes"] = Save(player.miscDyes)
        };

        try
        {
            Directory.CreateDirectory(Folder);
            TagIO.ToFile(new TagCompound
            {
                ["Loadouts"] = Slots
                    .Select((value, slot) => (value, slot))
                    .Where(entry => entry.value != null)
                    .Select(entry =>
                    {
                        entry.value["Slot"] = entry.slot;
                        return entry.value;
                    })
                    .ToList()
            }, FilePath);
            return true;
        }
        catch (Exception exception)
        {
            Log.Error($"Could not save shaking chest loadouts: {exception}");
            return false;
        }
    }

    internal static bool Apply(int slot, Player player)
    {
        Load();
        if (!Valid(slot) || Slots[slot] == null)
            return false;

        Load(Slots[slot], "Inventory", player.inventory);
        Load(Slots[slot], "Armor", player.armor);
        Load(Slots[slot], "Dye", player.dye);
        Load(Slots[slot], "MiscEquips", player.miscEquips);
        Load(Slots[slot], "MiscDyes", player.miscDyes);
        player.itemAnimation = 0;
        player.itemTime = 0;
        Recipe.FindRecipes();
        Sync(player);
        return true;
    }

    internal static void Sync(Player player)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        Sync(player, player.inventory, PlayerItemSlotID.Inventory0);
        Sync(player, player.armor, PlayerItemSlotID.Armor0);
        Sync(player, player.dye, PlayerItemSlotID.Dye0);
        Sync(player, player.miscEquips, PlayerItemSlotID.Misc0);
        Sync(player, player.miscDyes, PlayerItemSlotID.MiscDye0);
    }

    private static void Load()
    {
        if (_loaded || Main.dedServ)
            return;

        _loaded = true;
        if (!File.Exists(FilePath))
            return;

        try
        {
            IList<TagCompound> saved = TagIO.FromFile(FilePath).GetList<TagCompound>("Loadouts");
            for (int i = 0; i < saved.Count; i++)
            {
                int slot = saved[i].ContainsKey("Slot") ? saved[i].GetInt("Slot") : i;
                if (Valid(slot))
                    Slots[slot] = saved[i];
            }
        }
        catch (Exception exception)
        {
            Log.Error($"Could not load shaking chest loadouts: {exception}");
        }
    }

    private static List<TagCompound> Save(Item[] items) => items.Select(ItemIO.Save).ToList();

    private static void Load(TagCompound loadout, string key, Item[] target)
    {
        IList<TagCompound> saved = loadout.GetList<TagCompound>(key);
        for (int i = 0; i < target.Length; i++)
            target[i] = i < saved.Count ? ItemIO.Load(saved[i]) : new Item();
    }

    private static void Sync(Player player, Item[] items, int firstSlot)
    {
        for (int i = 0; i < items.Length; i++)
            NetMessage.SendData(MessageID.SyncEquipment, number: player.whoAmI, number2: firstSlot + i);
    }

    private static bool Valid(int slot) => slot is >= 0 and < SlotCount;
}
