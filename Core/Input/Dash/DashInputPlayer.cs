using Microsoft.Xna.Framework;
using PvPAdventure.Common.Debug;
using PvPAdventure.Core.Config;
using PvPAdventure.Core.Net;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using static PvPAdventure.Core.Input.Dash.DashInputSystem;

namespace PvPAdventure.Core.Input.Dash;

public class DashInputPlayer : ModPlayer
{
    private bool restoreDashType;
    private int dashTypeToRestore;

    public override void ProcessTriggers(TriggersSet triggers)
    {
        if (ModContent.GetInstance<Keybinds>().Dash == null || !ModContent.GetInstance<Keybinds>().Dash.JustPressed)
            return;

        int inputDirection = NormalizeDirection(GetInputDirection());
        Log.Chat($"{Player.whoAmI} requesting dash dir={inputDirection} dashType={Player.dashType} delay={Player.dashDelay}");

        if (PerformDash(inputDirection, force: false, out byte dashTypeUsed))
        {
            Log.Chat($"Dash executed locally on {Player.whoAmI} type {dashTypeUsed} netMode {Main.netMode}");
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
            Log.Chat($"PerformDash aborted: player {Player.whoAmI} has no dash-granting item (override={overrideDashType}).");

            //string key = "you have no dash item!";
            string key = "Mods.PvPAdventure.DashKeybind.NoDashItem";

            var config = ModContent.GetInstance<ClientConfig>();
            //if (config.ShowPopupMessages)
            {
                if (Main.myPlayer == Player.whoAmI)
                {
                    PopupText.NewText(new AdvancedPopupRequest
                    {
                        Color = Color.Crimson,
                        Text = Language.GetTextValue(key),
                        Velocity = new Vector2(0f, -4f),
                        DurationInFrames = 60
                    }, Main.LocalPlayer.Top + new Vector2(0f, -40f));
                }
            }
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
        Log.Chat($"Dashing with Tabi in direction: {direction} and velocity {16.9f}");

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
        Log.Chat($"Dashing with Shield in direction: {direction} and velocity {14.5f}");

        ApplyHorizontalVelocity(direction, 14.5f);
        Player.eocDash = 15;
    }

    private void ApplySolarDash(int direction)
    {
        Log.Chat($"Dashing with Solar in direction: {direction} and velocity {21.9f}");

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
        Log.Chat($"Dashing with Crystal in direction: {direction} and velocity {16.9f}");

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
        Log.Chat($"SendDashRequest: player {Player.whoAmI} dir={direction} type={dashTypeUsed}");
        ModPacket packet = Mod.GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.Dash);
        packet.Write((byte)DashPacketType.Request);
        packet.Write((sbyte)direction);
        packet.Write(dashTypeUsed);
        packet.Send();
    }

    internal void BroadcastDash(int direction, byte dashTypeUsed)
    {
        Log.Chat($"BroadcastDash: player {Player.whoAmI} dir={direction} type={dashTypeUsed}");
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

    //internal void PostDashMovementCleanup()
    //{
    //    if (restoreDashType && Player.dashDelay >= 0)
    //    {
    //        Player.dashType = dashTypeToRestore;
    //        restoreDashType = false;
    //    }
    //}
}