using MonoMod.Cil;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat.PvP;

internal class PvPBeetleArmorPlayer : ModPlayer
{
    public override void Load()
    {
        // Remove logic for handling Beetle Might buffs.
        IL_Player.UpdateBuffs += EditPlayerUpdateBuffs;
        // Simplify logic for handling Beetle Scale Mail set bonus to do the bare minimum required.
        IL_Player.UpdateArmorSets += EditPlayerUpdateArmorSets;
    }

    public override void SetStaticDefaults()
    {
        // Beetle Might buffs last forever until death.
        BuffID.Sets.TimeLeftDoesNotDecrease[BuffID.BeetleMight1] = true;
        BuffID.Sets.TimeLeftDoesNotDecrease[BuffID.BeetleMight2] = true;
        BuffID.Sets.TimeLeftDoesNotDecrease[BuffID.BeetleMight3] = true;
    }

    public override void PostUpdateEquips()
    {
        //if (Player.beetleOffense)
        //{
        //    Player.GetDamage<MeleeDamageClass>() += 0;
        //    Player.GetAttackSpeed<MeleeDamageClass>() += 0;
        //}
        //else
        //{
        //    Player.ClearBuff(BuffID.BeetleMight1);
        //    Player.ClearBuff(BuffID.BeetleMight2);
        //    Player.ClearBuff(BuffID.BeetleMight3);
        //}

        if (Player.HasBuff(BuffID.BeetleMight3))
        {
            // we apply the glowing eye effect from Yoraiz0rsSpell item
            Player.yoraiz0rEye = 33;
        }
    }

    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        //if (Main.netMode == NetmodeID.MultiplayerClient)
            //return;

        int attackerIdx = damageSource.SourcePlayerIndex;
        if ((uint)attackerIdx >= (uint)Main.maxPlayers)
            return;

        if (attackerIdx == Player.whoAmI)
            return;

        Player attacker = Main.player[attackerIdx];
        if (!attacker.active)
            return;

        if (!attacker.beetleOffense)
            return;

        int tier = 0;

        if (attacker.HasBuff(BuffID.BeetleMight3))
            tier = 3;
        else if (attacker.HasBuff(BuffID.BeetleMight2))
            tier = 2;
        else if (attacker.HasBuff(BuffID.BeetleMight1))
            tier = 1;

        if (tier >= 3)
            return;

        attacker.ClearBuff(BuffID.BeetleMight1);
        attacker.ClearBuff(BuffID.BeetleMight2);
        attacker.ClearBuff(BuffID.BeetleMight3);

        int buffType = BuffID.BeetleMight1 + tier;
        attacker.AddBuff(buffType, 2);
    }

    private void EditPlayerUpdateBuffs(ILContext il)
    {
        var cursor = new ILCursor(il);

        ILLabel label = null;
        // First, find a load of Player.buffType that is somewhere followed by a load of BuffID.BeetleMight1 and a blt
        // instruction...
        cursor.GotoNext(i =>
            i.MatchLdfld<Player>("buffType") && i.Next.Next.Next.MatchLdcI4(98) &&
            i.Next.Next.Next.Next.MatchBlt(out label));

        // ...and go back to the "this" load...
        cursor.Index -= 1;
        // ...while ensuring that instructions removed and emitted are labeled correctly...
        cursor.MoveAfterLabels();
        // ...to emit a branch to the fail case.
        cursor.EmitBr(label);
    }

    private void EditPlayerUpdateArmorSets(ILContext il)
    {
        var cursor = new ILCursor(il);

        // Find a load to a string...
        cursor.GotoNext(i => i.MatchLdstr("ArmorSetBonus.BeetleDamage"));
        // ...and go back to the branch instruction...
        cursor.Index -= 4;

        // ...to grab it's label...
        var label = (ILLabel)cursor.Next!.Operand;
        cursor.Index += 1;

        // ...and prepare a delegate call, doing the bare minimum for set bonus functionality...
        cursor.EmitLdarg0();
        cursor.EmitDelegate((Player self) =>
        {
            self.setBonus = Language.GetTextValue("ArmorSetBonus.BeetleDamage");
            self.beetleOffense = true;
        });
        // ...then branch away so we skip the original code.
        cursor.EmitBr(label);
    }
}

