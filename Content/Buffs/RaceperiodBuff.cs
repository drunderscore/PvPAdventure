using PvPAdventure.Common.Game;
using PvPAdventure.Common.Players;
using PvPAdventure.Content.Buffs;
using PvPAdventure.Content.Mounts;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
#if DEBUG
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Core.Utilities;
#endif
/// <summary>
/// This is the buff that gets applied in spawn that controls the Raceperiod System at the start of each game.
/// </summary>
namespace PvPAdventure.Content.Buffs
{
    public class RacePeriodBuff : ModBuff
    {
        public override string Texture => "PvPAdventure/Assets/Buff/RacePeriodBuff";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (!player.GetModPlayer<PlayerInSpawnPlayer>().MountCancelled)
                player.GetDamage(DamageClass.Generic) *= -999f;
        }
    }
}

namespace PvPAdventure.Common.Players
{
    public class PlayerInSpawnPlayer : ModPlayer
    {
#if DEBUG
        private static bool _bunnyModeEnabled = true;
#endif

        private bool _wasMounted;
        private bool _mountCancelled;
        public bool MountCancelled => _mountCancelled;

        private bool _wasWaiting = true;
        private bool _gameHasStarted;

        private bool _wasJumpPressed;
        private bool _wasAirborne;
        private int _airJumpCount;
        private bool _isGrounded;
        private int _jumpCooldown;

        private const int JumpCooldownFrames = 40;
        private const float JumpHeightMultiplier = 2.5f;

        private bool IsGameWaiting =>
            ModContent.GetInstance<GameManager>()?.CurrentPhase == GameManager.Phase.Waiting;

        private bool InSpawn()
        {
            int playerTileX = (int)(Player.position.X / 16f);
            int playerTileY = (int)(Player.position.Y / 16f);
            return Math.Abs(playerTileX - Main.spawnTileX) <= 25
                && Math.Abs(playerTileY - Main.spawnTileY) <= 25;
        }

        private bool EffectsActive =>
            BunnyModeEnabled && Player.HasBuff(ModContent.BuffType<RacePeriodBuff>()) && !_mountCancelled;

        private static bool BunnyModeEnabled
        {
            get
            {
#if DEBUG
                return _bunnyModeEnabled;
#else
                return true;
#endif
            }
        }

        public override void HideDrawLayers(PlayerDrawSet drawInfo)
        {
            if (!BunnyModeEnabled || !Player.HasBuff(ModContent.BuffType<RacePeriodBuff>()))
                return;

            foreach (PlayerDrawLayer layer in PlayerDrawLayerLoader.Layers)
            {
                if (layer.Name == "MountBack" || layer.Name == "MountFront")
                    continue;
                layer.Hide();
            }
        }

        public override bool ImmuneTo(PlayerDeathReason damageSource, int cooldownCounter, bool pvp)
        {
            return Player.HasBuff(ModContent.BuffType<RacePeriodBuff>());
        }

        public override void PreUpdate()
        {
            _isGrounded = Player.velocity.Y == 0f;
            bool jumpJustPressed = Player.controlJump && !_wasJumpPressed;

            if (_wasAirborne && _isGrounded)
                _airJumpCount = 0;

            if (_jumpCooldown > 0) _jumpCooldown--;

            bool cooldownGate = _airJumpCount == 0 || _jumpCooldown == 0;
            bool wantsAirJump = EffectsActive && !_isGrounded && jumpJustPressed && cooldownGate;

            if (wantsAirJump)
            {
                if (_airJumpCount == 0)
                {
                    Player.velocity.Y = -Player.jumpSpeed;
                    Player.jump = Player.jumpHeight;
                }
                else
                {
                    Player.velocity.X *= 0.66f;
                    Player.velocity.Y = -Player.jumpSpeed * JumpHeightMultiplier;
                    Player.jump = 0;
                    SpawnJumpPoof();
                    if (_airJumpCount > 1)
                        _jumpCooldown = JumpCooldownFrames;
                }
                _airJumpCount++;
            }

            _wasJumpPressed = Player.controlJump;
            _wasAirborne = !_isGrounded;
        }

        private void SpawnJumpPoof()
        {
            for (int i = 0; i < 6; i++)
            {
                Dust dust = Dust.NewDustDirect(
                    Player.position, Player.width, Player.height,
                    DustID.Cloud, 0f, 0f, 100, default, 1.5f);
                dust.velocity *= 0.4f;
                dust.velocity.Y -= Main.rand.NextFloat(0.5f, 1.5f);
                dust.noGravity = false;
                dust.fadeIn = 0.5f;
            }
        }

        public override void PostUpdateRunSpeeds()
        {
            if (!EffectsActive)
                return;

            Mount.MountData bunny = Mount.mounts[MountID.Bunny];
            float baseSpeed = bunny.runSpeed * 2f * 22f / 40f;
            float fullSpeed = bunny.runSpeed * 2f;
            float fastAccel = bunny.acceleration;
            float slowAccel = bunny.acceleration / 5f;
            float currentAbsSpeed = Math.Abs(Player.velocity.X);

            if (_isGrounded)
            {
                bool pressingOpposite = (Player.controlRight && Player.velocity.X < 0f)
                                     || (Player.controlLeft && Player.velocity.X > 0f);
                Player.maxRunSpeed = fullSpeed;
                Player.runAcceleration = (pressingOpposite || currentAbsSpeed < baseSpeed)
                    ? fastAccel : slowAccel;
                Player.runSlowdown = 0.6f;
            }
            else if (_airJumpCount <= 1)
            {
                Player.maxRunSpeed = Math.Max(baseSpeed, currentAbsSpeed);
                Player.runAcceleration = fastAccel;
                Player.runSlowdown = 0f;
            }
            else
            {
                float floorCap = fullSpeed * 5f / 40f;
                Player.maxRunSpeed = Math.Max(floorCap, currentAbsSpeed);
                Player.runAcceleration = fastAccel;
            }
            Player.noFallDmg = true;
        }

        public override void PostUpdateMiscEffects()
        {
#if DEBUG
            if (Player.whoAmI == Main.myPlayer && KeyboardHelper.Pressed(Keys.F7))
            {
                _bunnyModeEnabled = !_bunnyModeEnabled;
                Main.NewText($"Bunny mode: {(_bunnyModeEnabled ? "On" : "Off")}");
            }
#endif

            int buffType = ModContent.BuffType<RacePeriodBuff>();
            int mountType = ModContent.MountType<RacePeriodMount>();
            bool inSpawn = InSpawn();
            bool isMounted = Player.mount.Active && Player.mount.Type == mountType;
            bool hasBuff = Player.HasBuff(buffType);
            bool isWaiting = IsGameWaiting;

            if (!BunnyModeEnabled)
            {
                if (hasBuff)
                    Player.ClearBuff(buffType);
                if (isMounted)
                    Player.mount.Dismount(Player);

                _wasMounted = false;
                return;
            }

            if (_wasWaiting && !isWaiting)
                _gameHasStarted = true;
            _wasWaiting = isWaiting;

            if (!isWaiting && !inSpawn && _wasMounted && !isMounted && !Player.controlHook)
                _mountCancelled = true;

            _wasMounted = isMounted;

            if (!isWaiting && isMounted &&
                (Player.chest != -1 || Player.position.Y / 16f > (float)Main.worldSurface))
                Player.mount.Dismount(Player);

            if (_mountCancelled)
            {
                if (!inSpawn)
                {
                    if (hasBuff) Player.ClearBuff(buffType);
                    if (isMounted) Player.mount.Dismount(Player);
                }
                else
                {
                    if (!isMounted)
                        Player.mount.SetMount(mountType, Player);

                    int buffIndex = Player.FindBuffIndex(buffType);
                    if (buffIndex != -1)
                        Player.buffTime[buffIndex] = int.MaxValue;
                    else
                        Player.AddBuff(buffType, int.MaxValue);
                }
                return;
            }
            if (inSpawn && isWaiting && !_gameHasStarted)
            {
                if (!isMounted)
                    Player.mount.SetMount(mountType, Player);

                int buffIndex = Player.FindBuffIndex(buffType);
                if (buffIndex != -1)
                    Player.buffTime[buffIndex] = int.MaxValue;
                else
                    Player.AddBuff(buffType, int.MaxValue);

                Player.AddBuff(BuffID.NoBuilding, 2);
            }
            else if (isWaiting && !inSpawn && isMounted)
            {
                Player.mount.Dismount(Player);
            }
        }
    }

    public class PlayerInSpawnItemBlock : GlobalItem
    {
        public override bool CanUseItem(Item item, Player player)
        {
            if (player.GetModPlayer<PlayerInSpawnPlayer>().MountCancelled)
                return base.CanUseItem(item, player);
            if (player.HasBuff(ModContent.BuffType<RacePeriodBuff>()))
                return false;
            return base.CanUseItem(item, player);
        }

        public override bool CanEquipAccessory(Item item, Player player, int slot, bool modded)
        {
            if (player.GetModPlayer<PlayerInSpawnPlayer>().MountCancelled)
                return base.CanEquipAccessory(item, player, slot, modded);
            if (player.HasBuff(ModContent.BuffType<RacePeriodBuff>()))
                return false;
            return base.CanEquipAccessory(item, player, slot, modded);
        }

        public override bool? CanBeChosenAsAmmo(Item ammo, Item weapon, Player player)
        {
            if (player.GetModPlayer<PlayerInSpawnPlayer>().MountCancelled)
                return null;
            if (player.HasBuff(ModContent.BuffType<RacePeriodBuff>()))
                return false;
            return null;
        }
    }

    public class PlayerInSpawnSpawnSuppressor : GlobalNPC
    {
        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            if (player.GetModPlayer<PlayerInSpawnPlayer>().MountCancelled)
                return;
            if (player.HasBuff(ModContent.BuffType<RacePeriodBuff>()))
                maxSpawns = 0;
        }
    }
}
