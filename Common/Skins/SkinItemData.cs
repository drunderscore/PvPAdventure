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

    public SkinIdentity Identity = default;
    public bool HasSkin => Identity.IsValid;

    public override GlobalItem Clone(Item item, Item itemClone)
    {
        var clone = (SkinItemData)base.Clone(item, itemClone);
        clone.Identity = Identity;
        return clone;
    }

    public override void SaveData(Item item, TagCompound tag)
    {
        if (HasSkin)
        {
            tag["pvpadv_skin_proto"] = Identity.Prototype;
            tag["pvpadv_skin_name"] = Identity.Name;
        }
    }

    public override void LoadData(Item item, TagCompound tag)
    {
        string proto = tag.ContainsKey("pvpadv_skin_proto") ? tag.GetString("pvpadv_skin_proto") : "";
        string name = tag.ContainsKey("pvpadv_skin_name") ? tag.GetString("pvpadv_skin_name") : "";
        Identity = new SkinIdentity(proto, name);
    }

    public override void NetSend(Item item, BinaryWriter writer)
    {
        writer.Write((byte)1); // version
        writer.Write(HasSkin);
        if (HasSkin)
        {
            writer.Write(Identity.Prototype);
            writer.Write(Identity.Name);
        }
    }

    public override void NetReceive(Item item, BinaryReader reader)
    {
        reader.ReadByte(); // version
        if (reader.ReadBoolean())
        {
            Identity = new SkinIdentity(reader.ReadString(), reader.ReadString());
        }
        else
        {
            Identity = default;
        }
    }
}