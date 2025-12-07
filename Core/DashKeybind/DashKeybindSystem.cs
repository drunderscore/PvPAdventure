using System;
using System.IO;
using Microsoft.Xna.Framework;
using PvPAdventure.System.Client;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Core.DashKeybind
{
    internal enum DashPacketType : byte
    {
        Request = 0,
        Perform = 1
    }

    public class DashKeybindSystem : ModSystem
    {
        public override void Load()
        {
            On_Player.DoCommonDashHandle += VanillaDashDetour;
            On_Player.DashMovement += CustomDashHandle;
        }
        public override void Unload()
        {
            On_Player.DoCommonDashHandle -= VanillaDashDetour;
            On_Player.DashMovement -= CustomDashHandle;
        }
        private static void VanillaDashDetour(On_Player.orig_DoCommonDashHandle orig, Player self, out int dir, out bool dashing, Player.DashStartAction dashStartAction)
        {
            var config = ModContent.GetInstance<AdventureClientConfig>();

            // Execute vanilla dash only if enabled in config
            if (config.IsVanillaDashEnabled)
            {
                orig.Invoke(self, out dir, out dashing, dashStartAction);
                return;
            }
            else
            {
                // Must set out parameters even if not dashing
                dir = 0;
                dashing = false;
            }
        }
        private static void CustomDashHandle(On_Player.orig_DashMovement orig, Player self)
        {
            if (self.whoAmI == Main.myPlayer && ModContent.GetInstance<Keybinds>().Dash.JustPressed)
            {
                //Log.Info($"Dash key pressed on player {self.whoAmI} (dashDelay={self.dashDelay}).");
            }

            orig(self);
        }

        internal static void HandlePacket(BinaryReader reader, int sender)
        {
            DashPacketType packetType = (DashPacketType)reader.ReadByte();

            switch (packetType)
            {
                case DashPacketType.Request:
                    {
                        if (Main.netMode != NetmodeID.Server)
                            break;

                        sbyte direction = reader.ReadSByte();
                        byte dashTypeHint = reader.ReadByte();

                        direction = ClampDirection(direction);
                        if (direction == 0)
                            direction = 1;

                        Player player = Main.player[sender];
                        if (!player.active || player.dead)
                            break;

                        var dashPlayer = player.GetModPlayer<DashKeybindPlayer>();
                        if (dashPlayer.PerformDash(direction, force: false, out byte dashTypeUsed, dashTypeHint > 0 ? dashTypeHint : (int?)null))
                        {
                            dashPlayer.BroadcastDash(direction, dashTypeUsed);
                        }
                        break;
                    }
                case DashPacketType.Perform:
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            break;

                        byte playerIndex = reader.ReadByte();
                        sbyte direction = reader.ReadSByte();
                        byte dashTypeUsed = reader.ReadByte();

                        if (playerIndex >= Main.maxPlayers)
                            break;

                        // <-- ignore self to prevent double application
                        //if (playerIndex == Main.myPlayer)
                            //break;

                        Player remotePlayer = Main.player[playerIndex];
                        if (!remotePlayer.active)
                            break;

                        direction = ClampDirection(direction);
                        if (direction == 0)
                            direction = 1;

                        var dashPlayer = remotePlayer.GetModPlayer<DashKeybindPlayer>();
                        dashPlayer.PerformDash(direction, force: true, out _, dashTypeUsed > 0 ? dashTypeUsed : (int?)null);
                        break;
                    }
            }
        }

        private static sbyte ClampDirection(int direction)
        {
            if (direction > 1)
                return 1;
            if (direction < -1)
                return -1;
            return (sbyte)direction;
        }
    }

    public class DashKeybindPlayer : ModPlayer
    {
        private bool restoreDashType;
        private int dashTypeToRestore;

        public override void ProcessTriggers(TriggersSet triggers)
        {
            if (ModContent.GetInstance<Keybinds>().Dash == null || !ModContent.GetInstance<Keybinds>().Dash.JustPressed)
                return;

            int inputDirection = NormalizeDirection(GetInputDirection());
            ModContent.GetInstance<PvPAdventure>().Logger.Info($"ProcessTriggers: player {Player.whoAmI} requesting dash dir={inputDirection} dashType={Player.dashType} delay={Player.dashDelay}");

            if (PerformDash(inputDirection, force: false, out byte dashTypeUsed))
            {
                ModContent.GetInstance<PvPAdventure>().Logger.Info($"Dash executed locally on {Player.whoAmI} using type {dashTypeUsed} (netMode={Main.netMode}).");
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    SendDashRequest(inputDirection, dashTypeUsed);
                }
                else if (Main.netMode == NetmodeID.Server)
                {
                    BroadcastDash(inputDirection, dashTypeUsed);
                }
            }
        }

        private int GetInputDirection()
        {
            if (Player.controlRight && !Player.controlLeft)
                return 1;
            if (Player.controlLeft && !Player.controlRight)
                return -1;

            if (Player.velocity.X > 0f)
                return 1;
            if (Player.velocity.X < 0f)
                return -1;

            return Player.direction;
        }

        private int NormalizeDirection(int direction)
        {
            if (direction > 0)
                return 1;
            if (direction < 0)
                return -1;
            return Player.direction == 0 ? 1 : Player.direction;
        }

        private bool CanDash()
        {
            if (Player.dashDelay != 0)
                return false;

            if (Player.mount.Active || Player.dead || Player.frozen || Player.webbed || Player.tongued || Player.stoned)
                return false;

            return true;
        }

        internal bool PerformDash(int direction, bool force, out byte dashTypeUsed, int? overrideDashType = null)
        {
            dashTypeUsed = 0;

            direction = NormalizeDirection(direction);

            if (!force && !CanDash())
                return false;

            int originalDashType = Player.dashType;
            int selectedDashType = overrideDashType.HasValue && overrideDashType.Value > 0
                ? overrideDashType.Value
                : originalDashType;

            if (selectedDashType <= 0)
            {
                ModContent.GetInstance<PvPAdventure>().Logger.Info($"PerformDash aborted: player {Player.whoAmI} has no dash-granting item (override={overrideDashType}).");
                return false;
            }

            if (selectedDashType < 1 || selectedDashType > 5)
                selectedDashType = Math.Clamp(selectedDashType, 1, 5);

            dashTypeUsed = (byte)selectedDashType;

            if (selectedDashType != originalDashType)
            {
                restoreDashType = true;
                dashTypeToRestore = originalDashType;
            }
            else
            {
                restoreDashType = false;
            }

            Player.dashType = selectedDashType;
            Player.dash = selectedDashType;
            Player.dashTime = 0;
            Player.dashDelay = -1;
            Player.direction = direction;
            Player.timeSinceLastDashStarted = 0;

            ApplyDashEffects(selectedDashType, direction);

            // Player.netUpdate = true;
            return true;
        }

        private void ApplyDashEffects(int dashType, int direction)
        {
            switch (dashType)
            {
                case 1:
                    ApplyTabiDash(direction);
                    break;
                case 2:
                case 4:
                    ApplyShieldDash(direction);
                    break;
                case 3:
                    ApplySolarDash(direction);
                    break;
                case 5:
                    ApplyCrystalDash(direction);
                    break;
                default:
                    ApplyShieldDash(direction);
                    break;
            }
        }

        private void ApplyTabiDash(int direction)
        {
            ApplyHorizontalVelocity(direction, 16.9f);

            if (Main.netMode == NetmodeID.Server)
                return;

            IEntitySource source = Player.GetSource_FromThis();

            for (int i = 0; i < 20; i++)
            {
                int dust = Dust.NewDust(new Vector2(Player.position.X, Player.position.Y), Player.width, Player.height, DustID.Smoke, 0f, 0f, 100, default, 2f);
                Main.dust[dust].position.X += Main.rand.Next(-5, 6);
                Main.dust[dust].position.Y += Main.rand.Next(-5, 6);
                Main.dust[dust].velocity *= 0.2f;
                Main.dust[dust].scale *= 1f + Main.rand.Next(20) * 0.01f;
            }

            int gore = Gore.NewGore(source, new Vector2(Player.position.X + Player.width / 2f - 24f, Player.position.Y + Player.height / 2f - 34f), Vector2.Zero, Main.rand.Next(61, 64));
            Main.gore[gore].velocity.X = Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[gore].velocity.Y = Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[gore].velocity *= 0.4f;

            gore = Gore.NewGore(source, new Vector2(Player.position.X + Player.width / 2f - 24f, Player.position.Y + Player.height / 2f - 14f), Vector2.Zero, Main.rand.Next(61, 64));
            Main.gore[gore].velocity.X = Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[gore].velocity.Y = Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[gore].velocity *= 0.4f;
        }

        private void ApplyShieldDash(int direction)
        {
            ApplyHorizontalVelocity(direction, 14.5f);
            Player.eocDash = 15;
        }

        private void ApplySolarDash(int direction)
        {
            ApplyHorizontalVelocity(direction, 21.9f);
            Player.solarDashing = true;
            Player.solarDashConsumedFlare = false;

            if (Main.netMode == NetmodeID.Server)
                return;

            for (int i = 0; i < 20; i++)
            {
                int dust = Dust.NewDust(new Vector2(Player.position.X, Player.position.Y), Player.width, Player.height, DustID.SolarFlare, 0f, 0f, 100, default, 2f);
                Main.dust[dust].position.X += Main.rand.Next(-5, 6);
                Main.dust[dust].position.Y += Main.rand.Next(-5, 6);
                Main.dust[dust].velocity *= 0.2f;
                Main.dust[dust].scale *= 1f + Main.rand.Next(20) * 0.01f;
                Main.dust[dust].shader = GameShaders.Armor.GetSecondaryShader(Player.ArmorSetDye(), Player);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].fadeIn = 0.5f;
            }
        }

        private void ApplyCrystalDash(int direction)
        {
            ApplyHorizontalVelocity(direction, 16.9f);

            if (Main.netMode == NetmodeID.Server)
                return;

            for (int i = 0; i < 20; i++)
            {
                short dustType = Main.rand.NextFromList((short)68, (short)69, (short)70);
                int dust = Dust.NewDust(new Vector2(Player.position.X, Player.position.Y), Player.width, Player.height, dustType, 0f, 0f, 100, default, 1.5f);
                Main.dust[dust].position.X += Main.rand.Next(-5, 6);
                Main.dust[dust].position.Y += Main.rand.Next(-5, 6);
                Main.dust[dust].velocity = Player.DirectionTo(Main.dust[dust].position) * 2f;
                Main.dust[dust].scale *= 1f + Main.rand.Next(20) * 0.01f;
                Main.dust[dust].fadeIn = 0.5f + Main.rand.Next(20) * 0.01f;
                Main.dust[dust].noGravity = true;
                Main.dust[dust].shader = GameShaders.Armor.GetSecondaryShader(Player.ArmorSetDye(), Player);
            }
        }

        private void ApplyHorizontalVelocity(int direction, float speed)
        {
            Player.velocity.X = speed * direction;

            Point tileAheadUpper = (Player.Center + new Vector2(direction * Player.width / 2f + 2f, Player.gravDir * (-Player.height) / 2f + Player.gravDir * 2f)).ToTileCoordinates();
            Point tileAheadMid = (Player.Center + new Vector2(direction * Player.width / 2f + 2f, 0f)).ToTileCoordinates();

            if (WorldGen.SolidOrSlopedTile(tileAheadUpper.X, tileAheadUpper.Y) || WorldGen.SolidOrSlopedTile(tileAheadMid.X, tileAheadMid.Y))
            {
                Player.velocity.X *= 0.5f;
            }
        }

        private void SendDashRequest(int direction, byte dashTypeUsed)
        {
            ModContent.GetInstance<PvPAdventure>().Logger.Info($"SendDashRequest: player {Player.whoAmI} dir={direction} type={dashTypeUsed}");
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)AdventurePacketIdentifier.Dash);
            packet.Write((byte)DashPacketType.Request);
            packet.Write((sbyte)direction);
            packet.Write(dashTypeUsed);
            packet.Send();
        }

        internal void BroadcastDash(int direction, byte dashTypeUsed)
        {
            ModContent.GetInstance<PvPAdventure>().Logger.Info($"BroadcastDash: player {Player.whoAmI} dir={direction} type={dashTypeUsed}");
            if (Main.netMode != NetmodeID.Server)
                return;

            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)AdventurePacketIdentifier.Dash);
            packet.Write((byte)DashPacketType.Perform);
            packet.Write((byte)Player.whoAmI);
            packet.Write((sbyte)direction);
            packet.Write(dashTypeUsed);

            // <-- exclude the origin so it won’t double-apply
            packet.Send(toClient: -1, ignoreClient: Player.whoAmI);

            // old 
            //packet.Send();
        }

        internal void PostDashMovementCleanup()
        {
            if (restoreDashType && Player.dashDelay >= 0)
            {
                Player.dashType = dashTypeToRestore;
                restoreDashType = false;
            }
        }
    }
}