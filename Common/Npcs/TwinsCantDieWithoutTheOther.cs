using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Npcs;
/// <summary>
/// - Makes Spazmatism and Retinazer only able to go below 1 hp and die if the other twin is at 1hp <br/>
/// - This effectively means that both twins need to be killed all of the way in order for them to die <br/>
/// - This is somewhat scuffed, because it creates a large amount of gore from constantly killing the other twin while it is at 1hp, despite it not dying <br/>
/// - It also has some bad interactions with teambosslife, at some point this system should be integrated directly into teambosslife <br/>
/// </summary>
public class TillDeathDoUsPart : GlobalNPC
{
    public override bool PreKill(NPC npc)
    {
        if (npc.type == NPCID.Spazmatism || npc.type == NPCID.Retinazer)
        {
            NPC otherTwin = FindOtherTwin(npc);
            if (otherTwin != null && otherTwin.life > 1)
            {
                npc.life = 1;
                return false;
            }
            return true;
        }
        return true;
    }

    public override bool CheckDead(NPC npc)
    {
        if (npc.type == NPCID.Spazmatism || npc.type == NPCID.Retinazer)
        {
            NPC otherTwin = FindOtherTwin(npc);
            if (otherTwin != null && otherTwin.life > 1)
            {
                npc.life = 1;
                return false;
            }
        }
        return true;
    }

    public override void OnKill(NPC npc)
    {
        if (npc.type == NPCID.Spazmatism || npc.type == NPCID.Retinazer)
        {
            NPC otherTwin = FindOtherTwin(npc);
            if (otherTwin != null && otherTwin.active && otherTwin.life <= 1)
            {

                int killingPlayer = npc.lastInteraction;

                if (killingPlayer != 255 && killingPlayer < Main.maxPlayers && Main.player[killingPlayer].active)
                {
                    Player player = Main.player[killingPlayer];
                    // Terrible, garbage, frankly awful solution because it was easier than properly assigning kill credit, so we spawn a projectile to kill the other one

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int projectile = Projectile.NewProjectile(
                            npc.GetSource_Death(),
                            otherTwin.Center.X,
                            otherTwin.Center.Y,
                            0f,
                            0f,
                            ProjectileID.DD2ExplosiveTrapT3Explosion,
                            200,
                            0f,
                            killingPlayer
                        );


                        if (projectile >= 0 && projectile < Main.maxProjectiles)
                        {
                            Projectile proj = Main.projectile[projectile];

                            proj.penetrate = 1;
                            proj.timeLeft = 120;
                        }


                        if (Main.netMode == NetmodeID.Server && projectile >= 0)
                        {
                            NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, projectile);
                        }
                    }
                }
            }
        }
    }

    private NPC FindOtherTwin(NPC currentTwin)
    {
        int targetType = currentTwin.type == NPCID.Spazmatism ? NPCID.Retinazer : NPCID.Spazmatism;
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (npc.active && npc.type == targetType)
            {
                return npc;
            }
        }
        return null;
    }
}
