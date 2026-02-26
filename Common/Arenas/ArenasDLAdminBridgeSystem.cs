using DragonLens.Core.Systems;
using SubworldLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Arenas;

[JITWhenModsEnabled("DragonLens")]
[ExtendsFromMod("DragonLens")]
internal sealed class ArenasDragonLensAdminBridgeSystem : ModSystem, ICopyWorldData
{
    private const string WorldIdKey = "!PvPAdventure.DragonLensWorldId";
    private const string AdminsKey = "!PvPAdventure.DragonLensAdmins";

    private readonly bool[] adminAckSent = new bool[Main.maxPlayers];
    private int lastVisualHash;

    // Runs inside SubworldLibrary's copy pipeline, right before spawning the subserver.
    public void CopyMainWorldData()
    {
        if (Main.netMode != NetmodeID.Server)
        {
            return;
        }

        if (!ModLoader.TryGetMod("DragonLens", out _))
        {
            return;
        }

        string worldId = PermissionHandler.worldID ?? string.Empty;

        string[] adminIds =
            PermissionHandler.admins == null
                ? []
                : PermissionHandler.admins
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Distinct()
                    .ToArray();

        SubworldSystem.CopyWorldData(WorldIdKey, worldId);
        SubworldSystem.CopyWorldData(AdminsKey, adminIds);

        DebugDumpAdmins("before subworld", worldId, adminIds);
    }

    // Runs in the *subserver process* before the subworld generates/loads.
    public void ReadCopiedMainWorldData()
    {
        if (Main.netMode != NetmodeID.Server)
        {
            return;
        }

        if (!ModLoader.TryGetMod("DragonLens", out _))
        {
            return;
        }

        // Only meaningful in a subworld.
        if (!SubworldSystem.AnyActive())
        {
            return;
        }

        string copiedWorldId = SubworldSystem.ReadCopiedWorldData<string>(WorldIdKey);
        string[] copiedAdmins = SubworldSystem.ReadCopiedWorldData<string[]>(AdminsKey) ?? [];

        Log.Debug($"[Arenas/DL] Applying copied admin state to subserver. worldID={copiedWorldId} admins={copiedAdmins.Length}");

        PermissionHandler.worldID = copiedWorldId;

        if (PermissionHandler.admins == null)
        {
            PermissionHandler.admins = [];
        }

        PermissionHandler.admins.Clear();
        PermissionHandler.admins.AddRange(copiedAdmins);

        if (PermissionHandler.visualAdmins == null)
        {
            PermissionHandler.visualAdmins = [];
        }

        PermissionHandler.visualAdmins.Clear();

        Array.Fill(adminAckSent, false);
        lastVisualHash = 0;

        DebugDumpAdmins("after subworld", PermissionHandler.worldID, copiedAdmins);
    }

    public override void PostUpdateEverything()
    {
        if (Main.netMode != NetmodeID.Server)
        {
            return;
        }

        if (!SubworldSystem.AnyActive())
        {
            return;
        }

        if (!ModLoader.TryGetMod("DragonLens", out Mod dragonLens))
        {
            return;
        }

        SyncVisualAdminsAndNotifyClients(dragonLens);
    }

    private void SyncVisualAdminsAndNotifyClients(Mod dragonLens)
    {
        if (PermissionHandler.admins == null)
        {
            return;
        }

        var adminSet = new HashSet<string>(PermissionHandler.admins.Where(n => !string.IsNullOrEmpty(n)));
        List<int> newVisualAdmins = [];

        bool anyNewAck = false;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];
            if (player == null || !player.active)
            {
                adminAckSent[i] = false;
                continue;
            }

            PermissionPlayer mp = player.GetModPlayer<PermissionPlayer>();
            if (mp == null || string.IsNullOrEmpty(mp.currentServerID))
            {
                continue;
            }

            if (!adminSet.Contains(mp.currentServerID))
            {
                continue;
            }

            newVisualAdmins.Add(i);

            // Ensure the client gets the "you are admin" signal at least once in this subserver session.
            if (!adminAckSent[i])
            {
                SendAdminGrantedToClient(dragonLens, i);
                adminAckSent[i] = true;
                anyNewAck = true;

                Log.Debug($"[Arenas/DL] Admin ack -> '{player.name}' slot={i} id={mp.currentServerID}");
            }
        }

        int newHash = ComputeIntListHash(newVisualAdmins);
        if (!anyNewAck && newHash == lastVisualHash)
        {
            return;
        }

        lastVisualHash = newHash;

        PermissionHandler.visualAdmins.Clear();
        PermissionHandler.visualAdmins.AddRange(newVisualAdmins);

        BroadcastVisualAdmins(dragonLens, newVisualAdmins);

        Log.Debug($"[Arenas/DL] Visual admin sync -> {newVisualAdmins.Count} admins");
    }

    private static void SendAdminGrantedToClient(Mod dragonLens, int toClient)
    {
        ModPacket packet = dragonLens.GetPacket();
        packet.Write("AdminUpdate");
        packet.Write(0); // client-side op0 does NOT consume further payload
        packet.Send(toClient);
    }

    private static void BroadcastVisualAdmins(Mod dragonLens, List<int> visualAdmins)
    {
        ModPacket packet = dragonLens.GetPacket();
        packet.Write("AdminUpdate");
        packet.Write(2);
        packet.Write(visualAdmins.Count);

        for (int i = 0; i < visualAdmins.Count; i++)
        {
            packet.Write(visualAdmins[i]);
        }

        packet.Send();
    }

    private static int ComputeIntListHash(List<int> list)
    {
        unchecked
        {
            int hash = 17;
            for (int i = 0; i < list.Count; i++)
            {
                hash = (hash * 31) + list[i];
            }

            return hash;
        }
    }

    private static void DebugDumpAdmins(string phase, string worldId, string[] adminIds)
    {
        var adminSet = new HashSet<string>(adminIds.Where(n => !string.IsNullOrEmpty(n)));

        List<string> names = [];

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player p = Main.player[i];
            if (p == null || !p.active)
            {
                continue;
            }

            PermissionPlayer mp = p.GetModPlayer<PermissionPlayer>();
            if (mp == null || string.IsNullOrEmpty(mp.currentServerID))
            {
                continue;
            }

            if (adminSet.Contains(mp.currentServerID))
            {
                names.Add(p.name);
            }
        }

        string nameList = names.Count == 0 ? "(no active admin players matched)" : string.Join(", ", names);

        // Matches your requested "before subworld" style, but with useful real values.
        Log.Debug($"DL Admin count {phase}: {adminSet.Count} {nameList} (worldID={worldId})");
    }
}