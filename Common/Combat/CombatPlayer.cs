using MonoMod.Cil;
using PvPAdventure.Common.Spawnbox;
using PvPAdventure.Common.Statistics;
using PvPAdventure.Core.Config;
using PvPAdventure.Core.Utilities;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace PvPAdventure.Common.Combat;

// Everything combat related, player-side.
internal class CombatPlayer : ModPlayer
{
    private readonly int[] _playerMeleeInvincibleTime = new int[Main.maxPlayers];
    private int _currentMeleeUseId = 0;
    private int _lastItemAnimation = 0;
    private readonly Dictionary<(int attackerWho, int useId), int> _meleeImmuneBySwing = new();
    private static readonly HashSet<short> BossNpcsForImmunityCooldown =
    [
        NPCID.QueenSlimeMinionBlue,
        NPCID.QueenSlimeMinionPink,
        NPCID.QueenSlimeMinionPurple,
        NPCID.WallofFleshEye,
        NPCID.TheHungry,
        NPCID.TheHungryII,
        NPCID.LeechHead,
        NPCID.LeechBody,
        NPCID.LeechTail,
        NPCID.Probe,
        NPCID.PlanterasHook,
        NPCID.PlanterasTentacle,
        NPCID.Spore,
        NPCID.PrimeCannon,
        NPCID.PrimeSaw,
        NPCID.PrimeVice,
        NPCID.PrimeLaser,
        NPCID.GolemHead,
        NPCID.GolemFistLeft,
        NPCID.GolemFistRight,
        NPCID.GolemHeadFree,
        NPCID.Sharkron,
        NPCID.Sharkron2,
        NPCID.DetonatingBubble
    ];

    public int[] PvPImmuneTime { get; } = new int[Main.maxPlayers];

    public int[] GroupImmuneTime { get; } = new int[100];

    public override void Load()
    {
        On_Player.HasUnityPotion += OnPlayerHasUnityPotion;

        IL_Player.KillMe += EditPlayerKillMe;
        // Always consider the respawn time for non-pvp deaths.
        On_Player.GetRespawnTime += OnPlayerGetRespawnTime;
        On_Player.Spawn += OnPlayerSpawn;

        // Allow player hurt sound to be silenced or not, without regards to the networked value or mutating it.
        IL_Player.Hurt_HurtInfo_bool += EditPlayerHurt;

        // Modify the damage dealt by the entire wall from the Wall of Flesh to use ImmunityCooldownID.Bosses
        IL_Player.WOFTongue += EditPlayerWOFTongue;

        // Remove logic for handling Beetle Might buffs.
        IL_Player.UpdateBuffs += EditPlayerUpdateBuffs;
        // Simplify logic for handling Beetle Scale Mail set bonus to do the bare minimum required.
        IL_Player.UpdateArmorSets += EditPlayerUpdateArmorSets;
    }
    private bool OnPlayerHasUnityPotion(On_Player.orig_HasUnityPotion orig, Player self)
    {
        var region = ModContent.GetInstance<RegionManager>().GetRegionIntersecting(self.Hitbox.ToTileRectangle());

        // By default, you cannot wormhole.
        if (region == null || !region.CanUseWormhole)
            return false;

        // This is now a possibility from our multiplayer pause.
        if (Main.gamePaused)
            return false;

        return orig(self);
    }
    private int OnPlayerGetRespawnTime(On_Player.orig_GetRespawnTime orig, Player self, bool pvp) => orig(self, false);

    private void OnPlayerSpawn(On_Player.orig_Spawn orig, Player self, PlayerSpawnContext context)
    {
        // Don't count this as a PvP death.
        self.pvpDeath = false;
        orig(self, context);
        // Remove immune and immune time applied during spawn.
        var adventureConfig = ModContent.GetInstance<ServerConfig>();
        self.immuneTime = adventureConfig.SpawnImmuneFrames;
        self.immune = self.immuneTime > 0;
    }

    private void EditPlayerKillMe(ILContext il)
    {
        var cursor = new ILCursor(il);
        // Find the call to DropCoins...
        cursor.GotoNext(i => i.MatchCall<Player>("DropCoins"))
            // ...but go backwards to find the load to the 'pvp' parameter of the KillMe method
            .GotoPrev(i => i.MatchLdarg(4))
            // ...and remove the load and subsequent branch.
            .RemoveRange(2);
    }

    private void EditPlayerWOFTongue(ILContext il)
    {
        var cursor = new ILCursor(il);

        // Find the call to Player.Hurt...
        cursor.GotoNext(i => i.MatchCall<Player>("Hurt"));

        // ...and go back to the cooldownCounter parameter...
        cursor.Index -= 5;

        // ...to remove it...
        cursor.Remove()
            // ...and replace it with a constant.
            .EmitLdcI4(ImmunityCooldownID.Bosses);
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

    public override void SetStaticDefaults()
    {
        // Beetle Might buffs last forever until death.
        BuffID.Sets.TimeLeftDoesNotDecrease[BuffID.BeetleMight1] = true;
        BuffID.Sets.TimeLeftDoesNotDecrease[BuffID.BeetleMight2] = true;
        BuffID.Sets.TimeLeftDoesNotDecrease[BuffID.BeetleMight3] = true;

        Main.persistentBuff[BuffID.WeaponImbueVenom] = false;
        Main.persistentBuff[BuffID.WeaponImbueCursedFlames] = false;
        Main.persistentBuff[BuffID.WeaponImbueFire] = false;
        Main.persistentBuff[BuffID.WeaponImbueGold] = false;
        Main.persistentBuff[BuffID.WeaponImbueIchor] = false;
        Main.persistentBuff[BuffID.WeaponImbueNanites] = false;
        Main.persistentBuff[BuffID.WeaponImbueConfetti] = false;
        Main.persistentBuff[BuffID.WeaponImbuePoison] = false;
    }

    public override void Unload()
    {
        Main.persistentBuff[BuffID.WeaponImbueVenom] = true;
        Main.persistentBuff[BuffID.WeaponImbueCursedFlames] = true;
        Main.persistentBuff[BuffID.WeaponImbueFire] = true;
        Main.persistentBuff[BuffID.WeaponImbueGold] = true;
        Main.persistentBuff[BuffID.WeaponImbueIchor] = true;
        Main.persistentBuff[BuffID.WeaponImbueNanites] = true;
        Main.persistentBuff[BuffID.WeaponImbueConfetti] = true;
        Main.persistentBuff[BuffID.WeaponImbuePoison] = true;
    }
    public override bool CanHitPvp(Item item, Player target)
    {
        var myRegion = ModContent.GetInstance<RegionManager>().GetRegionIntersecting(Player.Hitbox.ToTileRectangle());

        if (myRegion != null && !myRegion.AllowCombat)
            return false;

        var targetRegion = ModContent.GetInstance<RegionManager>()
            .GetRegionIntersecting(target.Hitbox.ToTileRectangle());

        if (targetRegion != null && !targetRegion.AllowCombat)
            return false;

        // Detect new swing RIGHT HERE before collision check
        if (Player.itemAnimation > 0 && _lastItemAnimation == 0)
        {
            _currentMeleeUseId++;
            _lastItemAnimation = Player.itemAnimation;
        }

        var targetAdventurePlayer = target.GetModPlayer<CombatPlayer>();

        // Check if target is immune to THIS specific swing
        if (targetAdventurePlayer._meleeImmuneBySwing.TryGetValue((Player.whoAmI, _currentMeleeUseId), out var remainingTime) && remainingTime > 0)
            return false;

        _playerMeleeInvincibleTime[target.whoAmI] =
            ModContent.GetInstance<ServerConfig>().WeaponBalance.ImmunityFrames.PerPlayerGlobal;
        var immunityFrames = Player.itemAnimation + 2;
        targetAdventurePlayer._meleeImmuneBySwing[(Player.whoAmI, _currentMeleeUseId)] = immunityFrames;

        return true;
    }
    public override bool CanHitPvpWithProj(Projectile proj, Player target)
    {
        var myRegion = ModContent.GetInstance<RegionManager>().GetRegionIntersecting(Player.Hitbox.ToTileRectangle());

        if (myRegion != null && !myRegion.AllowCombat)
            return false;

        var targetRegion = ModContent.GetInstance<RegionManager>()
            .GetRegionIntersecting(target.Hitbox.ToTileRectangle());

        if (targetRegion != null && !targetRegion.AllowCombat)
            return false;

        return true;
    }

    public override bool CanBeHitByNPC(NPC npc, ref int cooldownSlot)
    {
        if (npc.boss || StatisticsNPC.IsPartOfEaterOfWorlds((short)npc.type) ||
            StatisticsNPC.IsPartOfTheDestroyer((short)npc.type) || BossNpcsForImmunityCooldown.Contains((short)npc.type))
            cooldownSlot = ImmunityCooldownID.Bosses;

        return true;
    }

    public override void ResetEffects()
    {
        // FIXME: This does not truly belong here.
        Player.hostile = true;
    }
    public override void PreUpdate()
    {
        for (var i = 0; i < _playerMeleeInvincibleTime.Length; i++)
        {
            if (_playerMeleeInvincibleTime[i] > 0)
                _playerMeleeInvincibleTime[i]--;
        }

        for (var i = 0; i < PvPImmuneTime.Length; i++)
        {
            if (PvPImmuneTime[i] > 0)
                PvPImmuneTime[i]--;
        }

        for (var i = 0; i < GroupImmuneTime.Length; i++)
        {
            if (GroupImmuneTime[i] > 0)
                GroupImmuneTime[i]--;
        }
    }

    public override void OnHurt(Player.HurtInfo info)
    {
        if (!info.PvP)
            return;

        var adventureConfig = ModContent.GetInstance<ServerConfig>();

        // Only play hit markers on clients that we hurt that aren't ourselves
        if (!Main.dedServ && Player.whoAmI != Main.myPlayer && info.DamageSource.SourcePlayerIndex == Main.myPlayer)
            PlayHitMarker(info.Damage);

        // Apply minimum damage received by players. This is done here to ensure it applies to all damage sources.
        if (info.CooldownCounter == CombatManager.PvPImmunityCooldownId &&
            adventureConfig.WeaponBalance.ImmunityFrames.TrueMelee == 0)
            PvPImmuneTime[info.DamageSource.SourcePlayerIndex] = adventureConfig.WeaponBalance.ImmunityFrames.PerPlayerGlobal;
    }
    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) =>
        {
            var adventureConfig = ModContent.GetInstance<ServerConfig>();
            info.Damage = Math.Max(info.Damage, adventureConfig.MinimumDamageReceivedByPlayers);

            if (info.PvP)
                info.Damage = Math.Max(info.Damage, adventureConfig.MinimumDamageReceivedByPlayersFromPlayer);
        };

        if (!modifiers.PvP)
            return;

        if (modifiers.DamageSource.SourcePlayerIndex < 0)
            return;

        var sourcePlayer = Main.player[modifiers.DamageSource.SourcePlayerIndex];
        if (!sourcePlayer.active)
            return;

        var adventureConfig = ModContent.GetInstance<ServerConfig>();
        var damageConfig = adventureConfig.WeaponBalance.Damage;
        var falloffConfig = adventureConfig.WeaponBalance.Falloff;

        var tileDistance = Player.Distance(sourcePlayer.position) / 16f;
        var hasIncurredFalloff = false;

        var sourceItem = modifiers.DamageSource.SourceItem;
        if (sourceItem != null && !sourceItem.IsAir)
        {
            var itemDef = new ItemDefinition(sourceItem.type);

            if (damageConfig.ItemDamage.TryGetValue(itemDef, out var multiplier))
                modifiers.IncomingDamageMultiplier *= multiplier;

            if (falloffConfig.PerItem.TryGetValue(itemDef, out var falloff) && falloff != null)
            {
                modifiers.IncomingDamageMultiplier *= falloff.CalculateMultiplier(tileDistance);
                hasIncurredFalloff = true;
            }
        }

        if (modifiers.DamageSource.SourceProjectileType != ProjectileID.None)
        {
            var projDef = new ProjectileDefinition(modifiers.DamageSource.SourceProjectileType);

            if (damageConfig.ProjectileDamage.TryGetValue(projDef, out var multiplier))
                modifiers.IncomingDamageMultiplier *= multiplier;

            if (falloffConfig.PerProjectile.TryGetValue(projDef, out var falloff) && falloff != null)
            {
                modifiers.IncomingDamageMultiplier *= falloff.CalculateMultiplier(tileDistance);
                hasIncurredFalloff = true;
            }
        }

        if (!hasIncurredFalloff && falloffConfig.Default != null)
        {
            modifiers.IncomingDamageMultiplier *=
                falloffConfig.Default.CalculateMultiplier(tileDistance);
        }
    }

    public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust,
        ref PlayerDeathReason damageSource)
    {
        // Only silence death sound on clients that we hurt that aren't ourselves
        if (!Main.dedServ && pvp && Player.whoAmI != Main.myPlayer && damageSource.SourcePlayerIndex == Main.myPlayer)
        {
            var marker = ModContent.GetInstance<ClientConfig>().SoundEffect.PlayerKillMarker;
            if (marker != null && marker.SilenceVanilla)
                playSound = false;
        }

        return true;
    }
    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        // Only play kill markers on clients that we hurt that aren't ourselves
        if (!Main.dedServ && pvp && damageSource.SourcePlayerIndex == Main.myPlayer && Player.whoAmI != Main.myPlayer)
            PlayKillMarker((int)damage);

        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        // Only for non-suicide PvP deaths, apply Beetle Might as needed to the attacker.
        if (pvp && damageSource.SourcePlayerIndex != Player.whoAmI)
        {
            var attacker = Main.player[damageSource.SourcePlayerIndex];

            if (attacker.beetleOffense && attacker.beetleOrbs < 3)
            {
                // First, make sure to clear any previous buff if applicable.
                if (attacker.beetleOrbs > 0)
                    attacker.ClearBuff(BuffID.BeetleMight1 + attacker.beetleOrbs - 1);

                attacker.AddBuff(BuffID.BeetleMight1 + attacker.beetleOrbs, 5);
            }
        }

        // Remove the Dungeon Guardian when it kills a player.
        if (damageSource.SourceNPCIndex != -1)
        {
            var npc = Main.npc[damageSource.SourceNPCIndex];
            if (npc?.type == NPCID.DungeonGuardian)
            {
                npc.life = 0;
                npc.netSkip = -1;
                if (Main.dedServ)
                    NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
            }
        }
    }
    public override void UpdateBadLifeRegen()
    {
        if (Player.HasBuff(BuffID.CursedInferno))
        {
            Player.lifeRegenTime = 0.0f;
            // Reduce damage by 12 flat, from 24.
            Player.lifeRegen += 12;
        }

        if (Player.HasBuff(BuffID.Venom))
        {
            Player.lifeRegenTime = 0.0f;
            // Reduce damage by 18 flat, from 30.
            Player.lifeRegen += 18;
        }
    }
    public override void PostUpdateEquips()
    {
        if (!Player.beetleOffense)
        {
            // If we don't have the beetle offense set bonus, remove all possible buffs.
            Player.ClearBuff(BuffID.BeetleMight1);
            Player.ClearBuff(BuffID.BeetleMight2);
            Player.ClearBuff(BuffID.BeetleMight3);
        }
    }

    private static void PlayHitMarker(int damage)
    {
        var marker = ModContent.GetInstance<ClientConfig>().SoundEffect.PlayerHitMarker;
        if (marker != null)
            SoundEngine.PlaySound(marker.Create(damage));
    }
    private static void PlayKillMarker(int damage)
    {
        var marker = ModContent.GetInstance<ClientConfig>().SoundEffect.PlayerKillMarker;
        if (marker != null)
            SoundEngine.PlaySound(marker.Create(damage));
    }
    private void EditPlayerHurt(ILContext il)
    {
        var cursor = new ILCursor(il);

        // Find the load of Player.HurtInfo.SoundDisabled...
        cursor.GotoNext(i => i.MatchLdfld<Player.HurtInfo>("SoundDisabled"))
            // ...and remove it...
            .Remove()
            // ...emitting a load of argument 0 (this)...
            .EmitLdarg0()
            // ...and a delegate, whose return value will take the place of the above-removed load.
            .EmitDelegate((Player.HurtInfo hurtInfo, Player target) =>
                ShouldSilenceHurtSound(target, hurtInfo) ?? hurtInfo.SoundDisabled);
    }
    private static bool? ShouldSilenceHurtSound(Player target, Player.HurtInfo info)
    {
        // Only silence hurt sound on clients that we hurt that aren't ourselves
        if (!Main.dedServ && info.PvP && target.whoAmI != Main.myPlayer &&
            info.DamageSource.SourcePlayerIndex == Main.myPlayer)
        {
            var marker = ModContent.GetInstance<ClientConfig>().SoundEffect.PlayerHitMarker;
            if (marker != null && marker.SilenceVanilla)
                return true;
        }

        return null;
    }
}
