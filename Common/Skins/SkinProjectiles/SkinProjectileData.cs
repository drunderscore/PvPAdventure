using PvPAdventure.Common.MainMenu.Shop;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Common.Skins.SkinProjectiles;

internal sealed class SkinProjectileData : GlobalProjectile
{
    public override bool InstancePerEntity => true;

    public SkinIdentity Identity = default;

    public override void OnSpawn(Projectile projectile, IEntitySource source)
    {
        Item item = null;

        if (source is EntitySource_ItemUse s1)
            item = s1.Item;
        else if (source is EntitySource_ItemUse_WithAmmo s2)
            item = s2.Item;

        if (item is null || item.IsAir)
            return;

        if (!item.TryGetGlobalItem(out SkinItemData itemData))
            return;

        if (!itemData.HasSkin)
            return;

        Identity = itemData.Identity;

        if (Main.netMode == NetmodeID.Server)
            projectile.netUpdate = true;

        Log.Debug($"[SkinProj] item={item.type} proj={projectile.type} frameCount={Main.projFrames[projectile.type]} skin={Identity.Prototype}:{Identity.Name}");
    }

    public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter writer)
    {
        writer.Write((byte)1);

        bool has = Identity.IsValid;
        writer.Write(has);

        if (has)
        {
            writer.Write(Identity.Prototype);
            writer.Write(Identity.Name);
        }
    }

    public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader reader)
    {
        _ = reader.ReadByte();

        bool has = reader.ReadBoolean();
        Identity = has ? new SkinIdentity(reader.ReadString(), reader.ReadString()) : default;
    }
}