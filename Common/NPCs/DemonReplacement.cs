using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.NPCs;

// Replaces Demons with Voodoo Demons after any mechanical boss has been defeated
public class DemonReplacement : GlobalNPC
{
    public override void OnSpawn(NPC npc, IEntitySource source)
    {
        bool anyMechBossDefeated = NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3;

        if (anyMechBossDefeated && npc.type == NPCID.Demon)
        {
            Vector2 position = npc.position;
            Vector2 velocity = npc.velocity;
            int target = npc.target;
            int direction = npc.direction;
            int spriteDirection = npc.spriteDirection;
            float rotation = npc.rotation;

            npc.SetDefaults(NPCID.VoodooDemon);

            // Restore the state
            npc.position = position;
            npc.velocity = velocity;
            npc.target = target;
            npc.direction = direction;
            npc.spriteDirection = spriteDirection;
            npc.rotation = rotation;

            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
            }
        }
    }
}
