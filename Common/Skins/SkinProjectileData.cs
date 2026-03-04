using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Common.Skins;

internal sealed class SkinProjectileData : GlobalProjectile
{
    public override bool InstancePerEntity => true;

    public string SkinId = "";

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

        if (string.IsNullOrEmpty(itemData.SkinId))
            return;

        SkinId = itemData.SkinId;

        if (Main.netMode == NetmodeID.Server)
            projectile.netUpdate = true;

        Log.Debug($"[SkinProj] item={item.type} proj={projectile.type} frameCount={Main.projFrames[projectile.type]} skin={SkinId}");
    }

    public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter writer)
    {
        writer.Write((byte)1);

        bool has = !string.IsNullOrEmpty(SkinId);
        writer.Write(has);

        if (has)
            writer.Write(SkinId);
    }

    public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader reader)
    {
        _ = reader.ReadByte();

        bool has = reader.ReadBoolean();
        SkinId = has ? reader.ReadString() : "";
    }
}