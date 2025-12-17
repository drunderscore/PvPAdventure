using System;
using System.Collections.Generic;
using System.IO;
using MonoMod.Cil;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace PvPAdventure.System;

[Autoload(Side = ModSide.Both)]
public class CombatManager : ModSystem
{
    private const bool PreventPersonalCombatModifications = false;

    private static readonly HashSet<short> BossProjectiles =
    [
        ProjectileID.DeerclopsIceSpike,
        ProjectileID.DeerclopsRangedProjectile,
        ProjectileID.InsanityShadowHostile,
        ProjectileID.QueenBeeStinger,
        ProjectileID.Skull,
        ProjectileID.EyeLaser,
        ProjectileID.QueenSlimeSmash,
        ProjectileID.QueenSlimeGelAttack,
        ProjectileID.QueenSlimeMinionBlueSpike,
        ProjectileID.QueenSlimeMinionPinkBall,
        ProjectileID.CursedFlameHostile,
        ProjectileID.EyeFire,
        ProjectileID.DeathLaser,
        ProjectileID.BombSkeletronPrime,
        ProjectileID.SeedPlantera,
        ProjectileID.PoisonSeedPlantera,
        ProjectileID.ThornBall,
        ProjectileID.EyeBeam,
        ProjectileID.Fireball,
        ProjectileID.CultistBossIceMist,
        ProjectileID.CultistBossLightningOrb,
        ProjectileID.CultistBossLightningOrbArc,
        ProjectileID.CultistBossFireBall,
        ProjectileID.CultistBossFireBallClone,
        ProjectileID.Sharknado,
        ProjectileID.Cthulunado,
        // This is also shot by Gastropods, but that's acceptable fallout.
        ProjectileID.PinkLaser,
        ProjectileID.BombSkeletronPrime,
    ];

    public const int PvPImmunityCooldownId = -100;
    public const int PaladinsShieldReflectImmunityCooldownId = -101;
    public const int GroupCooldownId = -1000;
    public const int MaximumNumberOfGroupCooldownId = 100;

    public override void Load()
    {
        // Do not draw the PvP or team icons -- the server has full control over your PvP and team.
        // TODO: In the future, the server should send a packet relaying if the player can toggle hostile and which teams they may join.
        //       For now, let's just totally disable it.
        if (PreventPersonalCombatModifications && !Main.dedServ)
            On_Main.DrawPVPIcons += _ => { };

        // Re-network player hurt packets when dealing with PvP (part of our ModPlayer.ModifyHurt PvP fixes).
        // Remove player i-frames to allow ours to function.
        On_Player.Hurt_HurtInfo_bool += OnPlayerHurt;
        // Remove random damage variation.
        On_Main.DamageVar_float_int_float += OnMainDamageVar;
        // Stub this method, as our previous Player.Hurt hook does the job of this (part of our ModPlayer.ModifyHurt PvP
        // fixes).
        On_NetMessage.SendPlayerHurt_int_PlayerDeathReason_int_int_bool_bool_int_int_int +=
            (_, _, _, _, _, _, _, _, _, _) => { };

        // Override the cooldown counter/immunity cooldown ID and "proceed" flag for PvP damage.
        IL_Player.Hurt_PlayerDeathReason_int_int_refHurtInfo_bool_bool_int_bool_float_float_float += EditPlayerHurt2;
        // Don't presumptuously check Player.immune for projectile PvP damage -- player hurt will do it for you.
        // Don't apply statuses conditionally on the value of Player.immune.
        // Override the cooldown counter/immunity cooldown ID for boss projectiles.
        IL_Projectile.Damage += EditProjectileDamage;
        // Don't presumptuously check Player.immune for melee PvP damage -- player hurt will do it for you.
        IL_Player.ItemCheck_MeleeHitPVP += EditPlayerItemCheck_MeleeHitPVP;

        // Remove Main.myPlayer check when determining Paladin's Shield damage reduction.
        IL_Player.ApplyVanillaHurtEffectModifiers += EditPlayerApplyVanillaHurtEffectModifiers;
        // Use a different immunity cooldown ID when applying Paladin's Shield reflect damage, to remove i-frames.
        IL_Player.OnHurt_Part2 += EditPlayerOnHurt_Part2;
        // Remove player immunity check, so it doesn't influence whether Paladin's Shield damage reduction occurs.
        IL_Player.TeammateHasPalidinShieldAndCanTakeDamage += EditPlayerTeammateHasPalidinShieldAndCanTakeDamage;
        // Don't network player stealth.
        IL_Player.OnHurt_Part1 += EditPlayerOnHurt_Part1;
    }

    public override bool HijackGetData(ref byte messageType, ref BinaryReader reader, int playerNumber)
    {
        if (PreventPersonalCombatModifications && Main.dedServ &&
            (messageType is MessageID.TogglePVP or MessageID.PlayerTeam))
            return true;

        if (Main.dedServ && messageType == MessageID.DamageNPC)
        {
            var previousPosition = reader.BaseStream.Position;

            try
            {
                var npc = Main.npc[reader.ReadInt16()];
                npc.GetGlobalNPC<AdventureNpc>().MarkNextStrikeForTeam(npc, (Team)Main.player[playerNumber].team);
            }
            finally
            {
                reader.BaseStream.Position = previousPosition;
            }
        }

        return false;
    }

    private void OnPlayerHurt(On_Player.orig_Hurt_HurtInfo_bool orig, Player self, Player.HurtInfo info, bool quiet)
    {
        // This logic is originally from Player.Hurt
        var pvp = info.PvP;
        var isPvpDamageDealtByMyself = pvp && info.DamageSource.SourcePlayerIndex == Main.myPlayer;

        // We additionally now check if this is PvP damage dealt by myself -- in which case I will send the player hurt
        // packet.
        if (Main.netMode == NetmodeID.MultiplayerClient &&
            ((self.whoAmI == Main.myPlayer && !pvp) || isPvpDamageDealtByMyself) && !quiet)
        {
            if (!isPvpDamageDealtByMyself)
            {
                if (info.Knockback != 0 && info.HitDirection != 0 && (!self.mount.Active || !self.mount.Cart))
                    NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, self.whoAmI);

                NetMessage.SendData(MessageID.PlayerLifeMana, -1, -1, null, self.whoAmI);
            }

            NetMessage.SendPlayerHurt(self.whoAmI, info);
        }

        // Don't try to send any packets -- I just did it for you.
        quiet = true;

        orig(self, info, quiet);
    }

    private int OnMainDamageVar(On_Main.orig_DamageVar_float_int_float orig, float dmg, int percent, float luck)
    {
        return (int)Math.Round(dmg);
    }

    private void EditPlayerHurt2(ILContext il)
    {
        var cursor = new ILCursor(il);
        // Find where we load Player.immune...
        cursor.GotoNext(i => i.MatchLdfld<Player>("immune"))
            // ...where we store it off to a local...
            .GotoNext(i => i.MatchStloc0());

        // ...then after that...
        cursor.Index += 1;
        // ...prepare a delegate call.
        cursor.EmitLdarg0()
            .EmitLdarg1()
            .EmitLdarg(5)
            .EmitLdarga(7)
            .EmitLdloca(0)
            .EmitDelegate((Player self, PlayerDeathReason damageSource, bool pvp, ref int cooldownCounter,
                ref bool flag) =>
            {
                var adventurePlayer = self.GetModPlayer<AdventurePlayer>();

                if (pvp)
                {
                    if (damageSource.SourceProjectileType != 0)
                    {
                        var adventureConfig = ModContent.GetInstance<AdventureServerConfig>();
                        if (adventureConfig.Combat.ProjectileDamageImmunityGroup.TryGetValue(new ProjectileDefinition(
                                damageSource.SourceProjectileType), out var immunityGroup) &&
                            adventurePlayer.GroupImmuneTime[immunityGroup.Id] > 0)
                        {
                            cooldownCounter = GroupCooldownId - immunityGroup.Id;
                            flag = false;
                        }
                    }

                    if (flag)
                    {
                        // Overwrite the cooldown counter, so that if the hurt succeeds, no other counter gets modified.
                        cooldownCounter = PvPImmunityCooldownId;
                        // Set the flag deciding if this hurt should proceed.
                        flag = adventurePlayer.PvPImmuneTime[damageSource.SourcePlayerIndex] == 0;
                    }
                }
            });
    }

    private void EditProjectileDamage(ILContext il)
    {
        var cursor = new ILCursor(il);
        // Find the first load of Player.immune...
        cursor.GotoNext(i => i.MatchLdfld<Player>("immune"));
        // ...and go back to where the player instance is loaded...
        cursor.Index -= 1;
        // ...to remove it, the load, and the branch.
        cursor.RemoveRange(3);

        // Find the next load of Player.immune...
        cursor.GotoNext(i => i.MatchLdfld<Player>("immune"));
        // ...and remove it...
        cursor.Remove();
        // ...to replace it's loaded value with the result of our delegate.
        cursor
            .EmitLdarg0()
            // If we don't have a player owner somehow, allow it regardless.
            .EmitDelegate((Player self, Projectile projectile) => !projectile.TryGetOwner(out var owner) ||
                                                                  self.GetModPlayer<AdventurePlayer>()
                                                                      .PvPImmuneTime[owner.whoAmI] > 0);

        // Find the first call to ModProjectile.CooldownSlot (property)...
        cursor.GotoNext(i => i.MatchCallvirt<ModProjectile>("get_CooldownSlot"));
        // ...and skip its call and store...
        cursor.Index += 2;
        // ...and two more unrelated instructions, to pass a label for a branch earlier...
        cursor.Index += 2;
        // ...to prepare a delegate call.
        cursor.EmitLdarg0()
            .EmitLdloca(2)
            .EmitDelegate((Projectile projectile, ref int cooldownSlot) =>
            {
                if (BossProjectiles.Contains((short)projectile.type))
                    cooldownSlot = ImmunityCooldownID.Bosses;
            });
    }

    private void EditPlayerItemCheck_MeleeHitPVP(ILContext il)
    {
        var cursor = new ILCursor(il);
        // Find the first load of Player.immune...
        cursor.GotoNext(i => i.MatchLdfld<Player>("immune"));
        // ...and go back to where the player instance is loaded...
        cursor.Index -= 1;
        // ...to remove it, the load, and the branch.
        cursor.RemoveRange(3);
    }

    private void EditPlayerApplyVanillaHurtEffectModifiers(ILContext il)
    {
        var cursor = new ILCursor(il);

        // Find the load of Entity.whoAmI...
        cursor.GotoNext(i => i.MatchLdfld<Entity>("whoAmI"));
        // ...and go back to the "this" load...
        cursor.Index -= 1;
        // ...to remove it, Entity.whoAmI load, comparison load, and branch.
        cursor.RemoveRange(4);
    }

    private void EditPlayerOnHurt_Part2(ILContext il)
    {
        var cursor = new ILCursor(il);

        // Find the call to Player.Hurt...
        cursor.GotoNext(i => i.MatchCallvirt<Player>("Hurt"));
        // ...and go back to the cooldownCounter parameter...
        cursor.Index -= 5;
        // ...to remove it...
        cursor.Remove();
        // ...and replace it with a constant (functionally removing i-frames for this hurt).
        cursor.EmitLdcI4(PaladinsShieldReflectImmunityCooldownId);
    }

    private void EditPlayerTeammateHasPalidinShieldAndCanTakeDamage(ILContext il)
    {
        var cursor = new ILCursor(il);

        // Find the load of Player.immune...
        cursor.GotoNext(i => i.MatchLdfld<Player>("immune"));
        // ...and go back...
        cursor.Index -= 3;
        // ...to remove some loads and branches (functionally removing immunity influence).
        cursor.RemoveRange(5);
    }

    private void EditPlayerOnHurt_Part1(ILContext il)
    {
        var cursor = new ILCursor(il);

        // Find the first reference to NetMessage.SendData...
        cursor.GotoNext(i => i.MatchCall<NetMessage>("SendData"));
        // ...go back past it's invocation prologue and a conditional check...
        cursor.Index -= 15;
        // ...to remove it all.
        cursor.RemoveRange(16);
    }
}