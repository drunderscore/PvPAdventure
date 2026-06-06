using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Config;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace PvPAdventure.Common.NPCs;

internal sealed class BossExpertiseNPC : GlobalNPC
{
    private static readonly FieldInfo ExpertModeOverrideField =
        typeof(Main).GetField("_overrideForExpertMode", BindingFlags.Static | BindingFlags.NonPublic);

    private static readonly FieldInfo MasterModeOverrideField =
        typeof(Main).GetField("_overrideForMasterMode", BindingFlags.Static | BindingFlags.NonPublic);

    private static readonly Stack<ServerConfig.BossExpertiseMode> DifficultyRestoreStack = [];

    public override bool PreAI(NPC npc)
    {
        TryPushDifficulty(npc);
        return true;
    }

    public override void PostAI(NPC npc) => TryPopDifficulty(npc);

    public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        TryPushDifficulty(npc);
        return true;
    }

    public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) => TryPopDifficulty(npc);

    internal static void RestoreAllDifficultyOverrides()
    {
        while (DifficultyRestoreStack.Count > 0)
            SetDifficulty(DifficultyRestoreStack.Pop());
    }

    private static bool TryGetConfiguredDifficulty(NPC npc, out ServerConfig.BossExpertiseMode difficulty)
    {
        if (TryGetConfiguredDifficulty(npc.type, out difficulty))
            return true;

        if (npc.realLife >= 0 && npc.realLife < Main.maxNPCs)
        {
            NPC owner = Main.npc[npc.realLife];
            return owner.active && TryGetConfiguredDifficulty(owner.type, out difficulty);
        }

        difficulty = default;
        return false;
    }

    private static bool TryGetConfiguredDifficulty(int npcType, out ServerConfig.BossExpertiseMode difficulty)
    {
        ModContent.GetInstance<ServerConfig>().BossExpertise.TryGetValue(
            new NPCDefinition(npcType),
            out ServerConfig.BossExpertiseEntry entry);

        difficulty = entry?.Difficulty ?? default;
        return entry != null;
    }

    private static void TryPushDifficulty(NPC npc)
    {
        if (TryGetConfiguredDifficulty(npc, out ServerConfig.BossExpertiseMode difficulty))
            PushDifficulty(difficulty);
    }

    private static void TryPopDifficulty(NPC npc)
    {
        if (TryGetConfiguredDifficulty(npc, out _))
            PopDifficulty();
    }

    private static void PushDifficulty(ServerConfig.BossExpertiseMode difficulty)
    {
        DifficultyRestoreStack.Push(GetCurrentDifficulty());
        SetDifficulty(difficulty);
    }

    private static void PopDifficulty()
    {
        if (DifficultyRestoreStack.Count == 0)
            return;

        SetDifficulty(DifficultyRestoreStack.Pop());
    }

    private static ServerConfig.BossExpertiseMode GetCurrentDifficulty() =>
        Main.masterMode ? ServerConfig.BossExpertiseMode.Master :
        Main.expertMode ? ServerConfig.BossExpertiseMode.Expert :
        ServerConfig.BossExpertiseMode.Classic;

    private static void SetDifficulty(ServerConfig.BossExpertiseMode difficulty)
    {
        ExpertModeOverrideField?.SetValue(null, difficulty != ServerConfig.BossExpertiseMode.Classic);
        MasterModeOverrideField?.SetValue(null, difficulty == ServerConfig.BossExpertiseMode.Master);
    }

}

internal sealed class BossExpertiseSystem : ModSystem
{
    public override void PostSetupContent()
    {
        ModContent.GetInstance<ServerConfig>().EnsureBossExpertiseDefaults();
    }

    public override void PreSaveAndQuit()
    {
        BossExpertiseNPC.RestoreAllDifficultyOverrides();
    }

    public override void Unload()
    {
        BossExpertiseNPC.RestoreAllDifficultyOverrides();
    }
}
