using System;
using System.IO;
using Terraria;
using Terraria.Enums;
using Terraria.ID;

namespace PvPAdventure.Common.Combat.TeamBoss;

public static class TeamBossNetHandler
{
    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        var npcIndex = reader.ReadInt16();
        var team = reader.ReadByte();

        if (npcIndex < 0 || npcIndex >= Main.maxNPCs)
            return;

        if (team >= Enum.GetValues<Team>().Length)
            return;

        var npc = Main.npc[npcIndex];
        npc.GetGlobalNPC<TeamBossNPC>().MarkNextStrikeForTeam(npc, (Team)team);
    }
}
