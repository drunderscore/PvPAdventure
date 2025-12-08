using System;
using System.Collections.Generic;
using System.IO;
using SubworldLibrary;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Core.SubworldArenas;

public class SWSystem : ModSystem
{
    public static readonly int[] Counts = new int[4];
    private static readonly Dictionary<int, int> playerArena = new();

    public override void OnWorldUnload()
    {
        Array.Clear(Counts, 0, Counts.Length);
        playerArena.Clear();
    }

    public static void RequestJoin(int index)
    {
        if (index < 0 || index > 3)
            return;

        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            EnterSubworld(index);
            return;
        }

        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
            packet.Write((byte)AdventurePacketIdentifier.SubworldJoin);
            packet.Write((byte)index);
            packet.Send();
        }
    }

    private static void EnterSubworld(int index)
    {
        switch (index)
        {
            case 0:
                SubworldSystem.Enter<SW1>();
                break;
            case 1:
                SubworldSystem.Enter<SW2>();
                break;
            case 2:
                SubworldSystem.Enter<SW3>();
                break;
            case 3:
                SubworldSystem.Enter<SW4>();
                break;
        }
    }

    public static void HandleJoinFromClient(int index, int whoAmI)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        if (index < 0 || index > 3)
            return;

        playerArena[whoAmI] = index;
        RecalcCounts();
        SendCounts();

        EnterSubworld(index);
    }

    private static void RecalcCounts()
    {
        Array.Clear(Counts, 0, Counts.Length);
        foreach (var kv in playerArena)
        {
            int idx = kv.Value;
            if (idx >= 0 && idx < 4)
                Counts[idx]++;
        }
    }

    private static void SendCounts()
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.SubworldCounts);
        for (int i = 0; i < 4; i++)
            packet.Write(Counts[i]);
        packet.Send();
    }

    public static void HandleCountsPacket(BinaryReader reader)
    {
        for (int i = 0; i < 4; i++)
            Counts[i] = reader.ReadInt32();

        SubworldUISystem.OnCountsUpdated();
    }
}
