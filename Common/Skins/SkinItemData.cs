using PvPAdventure.Common.MainMenu.Shop;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Common.Skins;

internal sealed class SkinItemData : GlobalItem
{
    public override bool InstancePerEntity => true;

    public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        => SkinRegistry.IsSkinnableItemType(entity.type);

    public ProductKey Key = default;
    public bool HasSkin => Key.IsValid;

    public override GlobalItem Clone(Item item, Item itemClone)
    {
        var clone = (SkinItemData)base.Clone(item, itemClone);
        clone.Key = Key;
        return clone;
    }

    public override void SaveData(Item item, TagCompound tag)
    {
        if (!HasSkin)
            return;

        tag["pvpadv_skin_proto"] = Key.Prototype;
        tag["pvpadv_skin_name"] = Key.Name;
    }

    public override void LoadData(Item item, TagCompound tag)
    {
        string proto = tag.ContainsKey("pvpadv_skin_proto") ? tag.GetString("pvpadv_skin_proto") : "";
        string name = tag.ContainsKey("pvpadv_skin_name") ? tag.GetString("pvpadv_skin_name") : "";
        Key = new ProductKey(proto, name);
    }

    public override void NetSend(Item item, BinaryWriter writer)
    {
        writer.Write((byte)1);
        writer.Write(HasSkin);

        if (!HasSkin)
            return;

        writer.Write(Key.Prototype);
        writer.Write(Key.Name);
    }

    public override void NetReceive(Item item, BinaryReader reader)
    {
        reader.ReadByte();

        Key = reader.ReadBoolean() ? new ProductKey(reader.ReadString(), reader.ReadString()) : default;
    }
}