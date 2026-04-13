using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
/// <summary>
/// Makes players always respawn with a 20 second timer, regardless of how they died.
/// </summary>
namespace PvPAdventure.Common.Combat.EJ;

internal class FixedRespawnTimer : ModPlayer
{
    private bool hasSetRespawnTimer = false;
    public override void UpdateDead()
    {
        if (Player.dead)
        {
            // Set respawn time when player dies
            if (!hasSetRespawnTimer)
            {
                Player.respawnTimer = 20 * 60; // 20 seconds
                hasSetRespawnTimer = true;
            }
        }
    }

    public override void OnRespawn()
    {
        // Reset the bool when the player respawns
        hasSetRespawnTimer = false;
    }
}
