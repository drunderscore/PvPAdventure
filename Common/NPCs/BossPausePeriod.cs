using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

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

        // Only the server/singleplayer should tick the timer and decide when to unpause.
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        _pauseTimer--;

        if (_pauseTimer <= 0)
        {
            RemovePause(npc);

            // Sync the unpaused state to all clients immediately.
            NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
        }
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
    public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        bitWriter.WriteBit(_isPaused);
        binaryWriter.Write(_pauseTimer);
        binaryWriter.Write(_originalAiStyle);
    }

    public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
    {
        bool wasPaused = _isPaused;

        _isPaused = bitReader.ReadBit();
        _pauseTimer = binaryReader.ReadInt32();
        _originalAiStyle = binaryReader.ReadInt32();

        if (_isPaused && !wasPaused)
            ApplyPause(npc);
        else if (!_isPaused && wasPaused)
            RemovePause(npc);
    }
}