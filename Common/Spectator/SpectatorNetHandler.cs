using PvPAdventure.Core.Net;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spectator;

internal static class SpectatorNetHandler
{
    internal enum SpectatorOperation : byte
    {
        RequestFullSync,
        FullSync,
        RequestSetMode,
        SetMode
    }

    public static void Receive(BinaryReader reader, int sender)
    {
        SpectatorOperation op = (SpectatorOperation)reader.ReadByte();

        switch (op)
        {
            case SpectatorOperation.RequestFullSync:
                HandleRequestFullSync(sender);
                break;

            case SpectatorOperation.FullSync:
                HandleFullSync(reader);
                break;

            case SpectatorOperation.RequestSetMode:
                HandleRequestSetMode(reader, sender);
                break;

            case SpectatorOperation.SetMode:
                HandleSetMode(reader);
                break;
        }
    }

    public static void SendRequestFullSync()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.Spectator);
        packet.Write((byte)SpectatorOperation.RequestFullSync);
        packet.Send();
    }

    public static void SendRequestSetMode(int slot, PlayerMode mode)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.Spectator);
        packet.Write((byte)SpectatorOperation.RequestSetMode);
        packet.Write(slot);
        packet.Write((byte)mode);
        packet.Send();
    }

    public static void SendFullSync(int toClient)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];

            if (!player.active)
                continue;

            if (!SpectatorSystem.Modes.ContainsKey(i))
                SpectatorSystem.Modes[i] = SpectatorSystem.GetJoinDefaultMode();
        }

        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.Spectator);
        packet.Write((byte)SpectatorOperation.FullSync);
        packet.Write(SpectatorSystem.Modes.Count);

        foreach ((int slot, PlayerMode mode) in SpectatorSystem.Modes)
        {
            packet.Write(slot);
            packet.Write((byte)mode);
        }

        packet.Send(toClient);
    }

    public static void SendMode(int slot, PlayerMode mode, int toClient = -1)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.Spectator);
        packet.Write((byte)SpectatorOperation.SetMode);
        packet.Write(slot);
        packet.Write((byte)mode);
        packet.Send(toClient);
    }

    private static void HandleRequestFullSync(int sender)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        SendFullSync(sender);
    }

    private static void HandleFullSync(BinaryReader reader)
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        SpectatorSystem.Modes.Clear();
        Player local = Main.LocalPlayer;

        int count = reader.ReadInt32();

        for (int i = 0; i < count; i++)
        {
            int slot = reader.ReadInt32();
            PlayerMode mode = (PlayerMode)reader.ReadByte();
            SpectatorSystem.SetModeLocal(slot, mode);

            if (local?.active == true && local.whoAmI == slot)
                local.GetModPlayer<SpectatorPlayer>().HandleInitialModeMessage(mode);
        }
    }

    private static void HandleRequestSetMode(BinaryReader reader, int sender)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        int slot = reader.ReadInt32();
        PlayerMode mode = (PlayerMode)reader.ReadByte();

        if (slot < 0 || slot >= Main.maxPlayers)
            return;

        if (slot != sender && !HasDragonLensAdminPermission(sender))
            return;

        SpectatorSystem.SetModeServer(slot, mode);
    }

    private static void HandleSetMode(BinaryReader reader)
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        int slot = reader.ReadInt32();
        PlayerMode mode = (PlayerMode)reader.ReadByte();
        SpectatorSystem.SetModeLocal(slot, mode);

        Player local = Main.LocalPlayer;
        if (local?.active == true && local.whoAmI == slot)
            local.GetModPlayer<SpectatorPlayer>().HandleInitialModeMessage(mode);
    }

    private static bool HasDragonLensAdminPermission(int sender)
    {
        if (sender < 0 || sender >= Main.maxPlayers || !ModLoader.HasMod("DragonLens"))
            return false;

        return HasDragonLensAdminPermission_DragonLens(sender);
    }

    [JITWhenModsEnabled("DragonLens")]
    private static bool HasDragonLensAdminPermission_DragonLens(int sender)
    {
        Player player = Main.player[sender];
        return player?.active == true && DragonLens.Core.Systems.PermissionHandler.CanUseTools(player);
    }
}
