using PvPAdventure.Core.Config;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Content.NPCs;

public abstract class BoundNPC : ModNPC
{
    public override string Name => $"Bound.{GetType().Name}";
    public override string Texture => $"PvPAdventure/Assets/NPC/Bound/{GetType().Name}";
    public abstract int TransformInto { get; }

    public override void SetDefaults()
    {
        NPC.friendly = true;
        NPC.width = 18;
        NPC.height = 34;
        NPC.aiStyle = NPCAIStyleID.FaceClosestPlayer;
        NPC.damage = 10;
        NPC.defense = 15;
        NPC.lifeMax = 250;
        NPC.HitSound = SoundID.NPCHit1;
        NPC.DeathSound = SoundID.NPCDeath1;
        NPC.knockBackResist = 0.5f;
        NPC.rarity = 1;
    }

    public override bool CanChat() => true;

    public override string GetChat()
    {
        var ourChatKey = $"Mods.PvPAdventure.NPCs.Bound.{GetType().Name}.Chat";
        return Language.GetTextValue(Language.Exists(ourChatKey) ? ourChatKey : "Mods.PvPAdventure.NPCs.Bound.Chat");
    }

    public override void AI()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        foreach (var player in Main.ActivePlayers)
        {
            if (player.talkNPC == NPC.whoAmI)
            {
                Transform(player.whoAmI);
                break;
            }
        }
    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        // Don't spawn inside of water.
        if (spawnInfo.Water)
            return 0.0f;

        // Don't spawn if there is already one of me, or one of who I transform into.
        if (NPC.AnyNPCs(NPC.type) || NPC.AnyNPCs(TransformInto))
            return 0.0f;

        return ModContent.GetInstance<ServerConfig>().BoundSpawnChance;
    }

    protected virtual void Transform(int whoAmI)
    {
        NPC.AI_000_TransformBoundNPC(whoAmI, TransformInto);
    }
}
