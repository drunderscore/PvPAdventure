using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Common.Skins;

/// <summary>
/// Stores the chosen skin id per Item instance. Safe on dedicated servers (no Texture2D).
/// </summary>
internal sealed class SkinItemData : GlobalItem
{
    public override bool InstancePerEntity => true;

    public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        => SkinRegistry.IsSkinnableItemType(entity.type);

    public string SkinId = "";
    public bool HasSkin => !string.IsNullOrEmpty(SkinId);

    public override GlobalItem Clone(Item item, Item itemClone)
    {
        var clone = (SkinItemData)base.Clone(item, itemClone);
        clone.SkinId = SkinId;
        return clone;
    }

    public override void SaveData(Item item, TagCompound tag)
    {
        if (HasSkin) 
            tag["pvpadv_skin"] = SkinId;
    }

    public override void LoadData(Item item, TagCompound tag)
    {
        SkinId = tag.ContainsKey("pvpadv_skin") ? tag.GetString("pvpadv_skin") : "";
    }

    public override void NetSend(Item item, BinaryWriter writer)
    {
        writer.Write((byte)1); // version
        writer.Write(HasSkin);
        if (HasSkin) writer.Write(SkinId);
    }

    public override void NetReceive(Item item, BinaryReader reader)
    {
        reader.ReadByte(); // version
        SkinId = reader.ReadBoolean() ? reader.ReadString() : "";
    }
}