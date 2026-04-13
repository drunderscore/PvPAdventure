using Mono.Cecil.Cil;
using MonoMod.Cil;
using PvPAdventure.Core.Config;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat.PvP;

internal class PvPBeetleArmorPlayer : ModPlayer
{
    public float BeetleEnergy = 0f;
    private int _lastSentTier = -1;

    public const byte PacketType = 11;

    private static ServerConfig.OtherConfig.BeetleScaleMailConfig Cfg =>
        ModContent.GetInstance<ServerConfig>().Other.BeetleScaleMail;

    public override void Load()
    {
        IL_Player.UpdateBuffs += EditPlayerUpdateBuffs;
        IL_Player.UpdateArmorSets += EditPlayerUpdateArmorSets;
    }

    public override void SetStaticDefaults()
    {
        BuffID.Sets.TimeLeftDoesNotDecrease[BuffID.BeetleMight1] = true;
        BuffID.Sets.TimeLeftDoesNotDecrease[BuffID.BeetleMight2] = true;
        BuffID.Sets.TimeLeftDoesNotDecrease[BuffID.BeetleMight3] = true;
    }

    public override void PostUpdateEquips()
    {
        if (Player.HasBuff(BuffID.BeetleMight3))
            Player.yoraiz0rEye = 33;
    }

    public override void PostUpdate()
    {
        if (Player.whoAmI != Main.myPlayer)
            return;

        if (!Player.beetleOffense)
        {
            if (BeetleEnergy != 0f || _lastSentTier != 0)
            {
                BeetleEnergy = 0f;
                SendTierToServer(0);
            }
            return;
        }

        BeetleEnergy = System.Math.Max(0f, BeetleEnergy - Cfg.EnergyDecayPerTick);

        int desiredTier = BeetleEnergy switch
        {
            var e when e >= Cfg.Tier3Threshold => 3,
            var e when e >= Cfg.Tier2Threshold => 2,
            var e when e >= Cfg.Tier1Threshold => 1,
            _ => 0
        };

        if (desiredTier != _lastSentTier)
            SendTierToServer(desiredTier);
    }

    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        BeetleEnergy = 0f;
        if (Player.whoAmI == Main.myPlayer)
            SendTierToServer(0);
    }

    public void AddEnergy(int damageDealt)
    {
        BeetleEnergy = System.Math.Min(Cfg.EnergyMax, BeetleEnergy + damageDealt * Cfg.EnergyMultiplier);
    }

    private void SendTierToServer(int tier)
    {
        _lastSentTier = tier;
        ApplyTier(Player, tier);

        if (Main.netMode == NetmodeID.SinglePlayer)
            return;

        ModPacket packet = Mod.GetPacket();
        packet.Write(PacketType);
        packet.Write((byte)Player.whoAmI);
        packet.Write((byte)tier);
        packet.Send();
    }

    public static void ReceivePacket(System.IO.BinaryReader reader, int sender)
    {
        byte playerIndex = reader.ReadByte();
        byte tier = reader.ReadByte();

        if (playerIndex >= Main.maxPlayers)
            return;

        Player player = Main.player[playerIndex];
        ApplyTier(player, tier);

        if (Main.netMode == NetmodeID.Server)
        {
            ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
            packet.Write(PacketType);
            packet.Write(playerIndex);
            packet.Write(tier);
            packet.Send(ignoreClient: sender);
        }
    }

    private static void ApplyTier(Player player, int tier)
    {
        player.ClearBuff(BuffID.BeetleMight1);
        player.ClearBuff(BuffID.BeetleMight2);
        player.ClearBuff(BuffID.BeetleMight3);
        if (tier >= 1)
            player.AddBuff(BuffID.BeetleMight1 + tier - 1, 2);
    }

    private void EditPlayerUpdateBuffs(ILContext il)
    {
        var cursor = new ILCursor(il);

        ILLabel label = null;
        cursor.GotoNext(i =>
            i.MatchLdfld<Player>("buffType") && i.Next.Next.Next.MatchLdcI4(98) &&
            i.Next.Next.Next.Next.MatchBlt(out label));

        cursor.Index -= 1;
        cursor.MoveAfterLabels();
        cursor.EmitBr(label);
    }

    private void EditPlayerUpdateArmorSets(ILContext il)
    {
        var cursor = new ILCursor(il);

        cursor.GotoNext(i => i.MatchLdstr("ArmorSetBonus.BeetleDamage"));
        cursor.Index -= 4;

        var label = (ILLabel)cursor.Next!.Operand;
        cursor.Index += 1;

        cursor.EmitLdarg0();
        cursor.EmitDelegate((Player self) =>
        {
            self.setBonus = Language.GetTextValue("ArmorSetBonus.BeetleDamage");
            self.beetleOffense = true;
        });
        cursor.EmitBr(label);
    }
}

public class PvPBeetleArmorEnergyGainProj : ModPlayer
{
    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        if (!modifiers.PvP)
            return;

        int attackerIndex = modifiers.DamageSource.SourcePlayerIndex;
        if (attackerIndex < 0 || attackerIndex >= Main.maxPlayers)
            return;

        Player attacker = Main.player[attackerIndex];
        if (!attacker.active || attacker == Player)
            return;

        bool isMelee = false;
        var sourceItem = modifiers.DamageSource.SourceItem;
        if (sourceItem != null && !sourceItem.IsAir)
            isMelee = sourceItem.DamageType.CountsAsClass(DamageClass.Melee);
        else if (modifiers.DamageSource.SourceProjectileType != ProjectileID.None)
            isMelee = ContentSamples.ProjectilesByType[modifiers.DamageSource.SourceProjectileType]
                          .DamageType.CountsAsClass(DamageClass.Melee);

        if (!isMelee)
            return;

        modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) =>
        {
            attacker.GetModPlayer<PvPBeetleArmorPlayer>().AddEnergy(info.Damage);
        };
    }
}