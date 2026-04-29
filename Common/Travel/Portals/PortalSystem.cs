using Microsoft.Xna.Framework;
using PvPAdventure.Content.Portals;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Travel.Portals;

public static class PortalSystem
{
    public static void StartPortalCreation(Player player)
    {
        if (player?.active != true || Main.netMode == NetmodeID.MultiplayerClient)
            return;

        RemoveCreationProjectiles(player.whoAmI);

        Vector2 worldPos = PortalCreatorItem.GetPortalWorldPosition(player);
        int creationFrames = PortalCreatorItem.GetCreationTimeFrames();
        int ownerTeam = player.team;

        int index = Projectile.NewProjectile(
            player.GetSource_ItemUse(player.HeldItem),
            worldPos,
            Vector2.Zero,
            ModContent.ProjectileType<PortalCreationProjectile>(),
            0,
            0f,
            player.whoAmI,
            creationFrames,
            ownerTeam
        );

        if (index < 0 || index >= Main.maxProjectiles || Main.projectile[index].ModProjectile is not PortalCreationProjectile creation)
            return;

        creation.Initialize(worldPos, creationFrames, ownerTeam);
        //Log.Chat($"Portal creation at {worldPos}, team={ownerTeam}, frames={creationFrames}");

        if (Main.netMode == NetmodeID.Server)
            NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, index);
    }

    public static bool CreateOrReplacePortal(Player owner, Vector2 worldPos)
    {
        if (owner?.active != true || Main.netMode == NetmodeID.MultiplayerClient)
            return false;

        RemovePortals(owner.whoAmI);

        int index = NPC.NewNPC(owner.GetSource_Misc("PortalCreation"), (int)worldPos.X, (int)worldPos.Y, ModContent.NPCType<PortalNPC>());

        if (index < 0 || index >= Main.maxNPCs || Main.npc[index].ModNPC is not PortalNPC portal)
            return false;

        portal.Initialize(owner, worldPos);
        Log.Chat($"Portal created at {worldPos}");

        if (Main.netMode == NetmodeID.Server)
            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, index);

        return true;
    }

    public static void ClearPortal(int ownerIndex)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        RemovePortals(ownerIndex);
        ClearCreationProjectiles(ownerIndex);
    }

    public static void ClearCreationProjectiles(int ownerIndex)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        RemoveCreationProjectiles(ownerIndex);
    }

    public static IEnumerable<PortalNPC> ActivePortals()
    {
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];

            if (npc?.active == true && npc.ModNPC is PortalNPC portal)
                yield return portal;
        }
    }

    public static bool IsFriendlyPortal(Player player, PortalNPC portal)
    {
        if (player?.active != true || portal == null)
            return false;

        if (portal.OwnerIndex == player.whoAmI)
            return true;

        return player.team > 0 && player.team == portal.OwnerTeam;
    }

    private static void RemovePortals(int ownerIndex)
    {
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];

            if (npc?.active != true || npc.ModNPC is not PortalNPC portal || portal.OwnerIndex != ownerIndex)
                continue;

            npc.active = false;
            npc.life = 0;

            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, i);
        }
    }

    private static void RemoveCreationProjectiles(int ownerIndex)
    {
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile projectile = Main.projectile[i];

            if (projectile?.active != true || projectile.owner != ownerIndex || projectile.ModProjectile is not PortalCreationProjectile)
                continue;

            projectile.Kill();

            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.KillProjectile, -1, -1, null, projectile.identity, projectile.owner);
        }
    }
}
