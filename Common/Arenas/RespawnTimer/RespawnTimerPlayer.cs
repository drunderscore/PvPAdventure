using PvPAdventure.Core.Config;
using SubworldLibrary;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Arenas.RespawnTimer;

public sealed class RespawnTimerPlayer : ModPlayer
{
    public int CustomRespawnTicks { get; private set; }

    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        if (!SubworldSystem.IsActive<ArenasSubworld>())
        {
            CustomRespawnTicks = 0;
            return;
        }

        ArenasConfig config = ModContent.GetInstance<ArenasConfig>();
        if (!config.EnableCustomRespawnTimer)
        {
            CustomRespawnTicks = 0;
            return;
        }

        CustomRespawnTicks = Math.Max(1, config.RespawnTimeSeconds * 60);
        Player.respawnTimer = CustomRespawnTicks;
    }

    public override void UpdateDead()
    {
        if (Player.respawnTimer <= 0)
            CustomRespawnTicks = 0;
    }

    public override void OnRespawn()
    {
        CustomRespawnTicks = 0;
    }
}
