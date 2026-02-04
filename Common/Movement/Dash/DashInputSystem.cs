using PvPAdventure.Core.Config;
using PvPAdventure.Core.Input;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Movement.Dash;

/// <summary>
/// Enables custom dash keybind handling with support for multiplayer synchronization.
/// </remarks>
public class DashInputSystem : ModSystem
{
    internal enum DashPacketType : byte
    {
        Request = 0,
        Perform = 1
    }
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
        var config = ModContent.GetInstance<ClientConfig>();

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

                    var dashPlayer = player.GetModPlayer<DashInputPlayer>();
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

                    var dashPlayer = remotePlayer.GetModPlayer<DashInputPlayer>();
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