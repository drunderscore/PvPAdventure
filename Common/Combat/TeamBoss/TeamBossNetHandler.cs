using System.IO;
using Terraria;
using Terraria.Enums;
using Terraria.ID;

namespace PvPAdventure.Common.Combat.TeamBoss;

public static class TeamBossNetHandler
{
    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        int npcIndex = reader.ReadInt16();
        Team team = (Team)reader.ReadByte();

        if ((uint)npcIndex >= Main.maxNPCs)
            return;

        if (!System.Enum.IsDefined(typeof(Team), team))
            return;

        NPC npc = Main.npc[npcIndex];
        if (npc == null || !npc.active)
            return;

        npc.GetGlobalNPC<TeamBossNPC>().MarkNextStrikeForTeam(npc, team);
    }
}
