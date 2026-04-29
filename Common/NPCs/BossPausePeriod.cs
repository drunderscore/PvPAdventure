using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.NPCs;
/// <summary>
/// Makes bosses not do anything for a period after they spawn by replacing their AI with the LunaticDevote AI and preventing them from getting hit
/// </summary>
internal class BossPausePeriod : GlobalNPC
{
    public override bool InstancePerEntity => true;

    private static readonly Dictionary<int, int> PauseDurations = new()
    {
        { NPCID.Plantera,          30 * 60 }, 
        { NPCID.CultistBoss,       30 * 60 },
        { NPCID.SkeletronHead,       30 * 60 },
    };

    private int _pauseTimer;  
    private int _originalAiStyle; 
    private bool _isPaused;

    public override void OnSpawn(NPC npc, Terraria.DataStructures.IEntitySource source)
    {
        if (!PauseDurations.TryGetValue(npc.type, out int duration))
            return;

        _pauseTimer = duration;
        _originalAiStyle = npc.aiStyle;
        _isPaused = true;

        ApplyPause(npc);
    }

    public override void AI(NPC npc)
    {
        if (!_isPaused)
            return;

        _pauseTimer--;

        if (_pauseTimer <= 0)
            RemovePause(npc);
    }

    public override bool? CanBeHitByItem(NPC npc, Player player, Item item)
        => _isPaused ? false : null;

    public override bool? CanBeHitByProjectile(NPC npc, Projectile projectile)
        => _isPaused ? false : null;

    private void ApplyPause(NPC npc)
    {
        npc.aiStyle = NPCAIStyleID.LunaticDevote;
        npc.dontTakeDamage = true;
    }

    private void RemovePause(NPC npc)
    {
        npc.aiStyle = _originalAiStyle;
        npc.dontTakeDamage = false;
        _isPaused = false;
    }
}