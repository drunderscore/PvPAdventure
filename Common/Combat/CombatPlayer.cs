 using MonoMod.Cil;
using PvPAdventure.Common.NPCs;
using PvPAdventure.Common.Spawnbox;
using PvPAdventure.Core.Config;
using PvPAdventure.Core.Utilities;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace PvPAdventure.Common.Combat;

/// <summary>
/// 
/// </summary>
internal class CombatPlayer : ModPlayer
{
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
        // Forcibly treat deaths as non-PvP for coin drop logic.
        IL_Player.KillMe += EditPlayerKillMe;

        // Always consider the respawn time for non-pvp deaths.
        On_Player.GetRespawnTime += OnPlayerGetRespawnTime;
        On_Player.Spawn += OnPlayerSpawn;

        // Modify the damage dealt by the entire wall from the Wall of Flesh to use ImmunityCooldownID.Bosses
        IL_Player.WOFTongue += EditPlayerWOFTongue;
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

    public override void SetStaticDefaults()
    {
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

    public override bool CanBeHitByNPC(NPC npc, ref int cooldownSlot)
    {
        if (npc.boss || BossPartUtilities.IsPartOfEaterOfWorlds((short)npc.type) ||
            BossPartUtilities.IsPartOfTheDestroyer((short)npc.type) || BossNpcsForImmunityCooldown.Contains((short)npc.type))
            cooldownSlot = ImmunityCooldownID.Bosses;

        return true;
    }

    public override void ResetEffects()
    {
        // FIXME: This does not truly belong here.
        // This sets PvP enabled for the player every tick.
        Player.hostile = true;
    }
    public override void PreUpdate()
    {
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

        var config = ModContent.GetInstance<ServerConfig>();

        // 1) Per-projectile immunity groups
        if (info.DamageSource.SourceProjectileType != ProjectileID.None &&
            config.Immunity.ProjectileDamageImmunityGroup.TryGetValue(
                new ProjectileDefinition(info.DamageSource.SourceProjectileType),
                out var immunityGroup))
        {
            int id = immunityGroup.Id;
            if ((uint)id < (uint)GroupImmuneTime.Length)
                GroupImmuneTime[id] = immunityGroup.Frames;

            return;
        }

        // 2) Per-attacker global PvP immunity (applies to both melee and projectile unless group overrides)
        if (info.CooldownCounter == CombatManager.PvPImmunityCooldownId)
        {
            int attacker = info.DamageSource.SourcePlayerIndex;
            if ((uint)attacker < (uint)PvPImmuneTime.Length)
            {
                int frames = config.Immunity.PerPlayerGlobal;

                if (frames > 0)
                    PvPImmuneTime[attacker] = frames;
            }
        }
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
    
    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

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
    
    
}
