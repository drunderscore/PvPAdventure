using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;


namespace PvPAdventure;

public abstract class BoundNpc : ModNPC
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

        return ModContent.GetInstance<AdventureConfig>().BoundSpawnChance;
    }

    protected virtual void Transform(int whoAmI)
    {
        NPC.AI_000_TransformBoundNPC(whoAmI, TransformInto);
    }

    public class Merchant : BoundNpc
    {
        public override int TransformInto => NPCID.Merchant;

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            // Don't spawn if we've already been unlocked.
            // Note this MUST come BEFORE NPC.SpawnAllowed_Merchant, as it short-circuits based on the value we check.
            if (NPC.unlockedMerchantSpawn)
                return 0.0f;

            // Don't spawn if we shouldn't.
            if (!NPC.SpawnAllowed_Merchant())
                return 0.0f;

            // Don't spawn if we aren't in the caverns layer.
            if (!spawnInfo.Player.ZoneRockLayerHeight)
                return 0.0f;

            // FIXME: What is this doing...? this is what bound goblin does!
            if (spawnInfo.SpawnTileY >= Main.maxTilesY - 210)
                return 0.0f;

            return base.SpawnChance(spawnInfo);
        }

        protected override void Transform(int whoAmI)
        {
            base.Transform(whoAmI);
            NPC.unlockedMerchantSpawn = true;
        }
    }

    public class ArmsDealer : BoundNpc
    {
        public override int TransformInto => NPCID.ArmsDealer;

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            // Don't spawn if we've already been unlocked.
            // Note this MUST come BEFORE NPC.SpawnAllowed_ArmsDealer, as it short-circuits based on the value we check.
            if (NPC.unlockedArmsDealerSpawn)
                return 0.0f;

            // Don't spawn if we shouldn't.
            if (!NPC.SpawnAllowed_ArmsDealer())
                return 0.0f;

            // Don't spawn if we aren't in the caverns layer.
            if (!spawnInfo.Player.ZoneRockLayerHeight)
                return 0.0f;

            // FIXME: What is this doing...? this is what bound goblin does!
            if (spawnInfo.SpawnTileY >= Main.maxTilesY - 210)
                return 0.0f;

            return base.SpawnChance(spawnInfo);
        }

        protected override void Transform(int whoAmI)
        {
            base.Transform(whoAmI);
            NPC.unlockedArmsDealerSpawn = true;
        }
    }

    // FIXME: Don't actually face towards with aiStyle 0 -- probably need an PreAI override
    public class Cyborg : BoundNpc
    {
        public override int TransformInto => NPCID.Cyborg;

        public override void SetDefaults()
        {
            base.SetDefaults();

            NPC.width = 34;
            NPC.height = 8;
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            // FIXME: We need to check if the Cyborg has already moved in once before.

            // Don't spawn if we shouldn't.
            if (!NPC.downedPlantBoss)
                return 0.0f;

            // Don't spawn if we aren't in the caverns layer.
            if (!spawnInfo.Player.ZoneRockLayerHeight)
                return 0.0f;

            // FIXME: What is this doing...? this is what bound goblin does!
            if (spawnInfo.SpawnTileY >= Main.maxTilesY - 210)
                return 0.0f;

            return base.SpawnChance(spawnInfo);
        }
    }

    public class WitchDoctor : BoundNpc
    {
        public override int TransformInto => NPCID.WitchDoctor;

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            // FIXME: We need to check if the Witch Doctor has already moved in once before.

            // Don't spawn if we shouldn't.
            if (!NPC.downedQueenBee)
                return 0.0f;

            // Don't spawn if we aren't in the caverns layer.
            if (!spawnInfo.Player.ZoneRockLayerHeight)
                return 0.0f;

            // FIXME: What is this doing...? this is what bound goblin does!
            if (spawnInfo.SpawnTileY >= Main.maxTilesY - 210)
                return 0.0f;

            return base.SpawnChance(spawnInfo);
        }
    }

    // FIXME: Don't actually face towards with aiStyle 0 -- probably need an PreAI override
    public class Steampunker : BoundNpc
    {
        public override int TransformInto => NPCID.Steampunker;

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            // FIXME: We need to check if the Steampunker has already moved in once before.

            // Don't spawn if we shouldn't.
            if (!NPC.downedMechBossAny)
                return 0.0f;

            // Don't spawn if we aren't in the caverns layer.
            if (!spawnInfo.Player.ZoneRockLayerHeight)
                return 0.0f;

            // FIXME: What is this doing...? this is what bound goblin does!
            if (spawnInfo.SpawnTileY >= Main.maxTilesY - 210)
                return 0.0f;

            return base.SpawnChance(spawnInfo);
        }
    }

    public class Truffle : BoundNpc
    {
        public override int TransformInto => NPCID.Truffle;

        public override void SetDefaults()
        {
            base.SetDefaults();

            NPC.width = 34;
            NPC.height = 8;
            NPC.dontTakeDamage = true;
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            // Don't spawn if we've already been unlocked.
            // Note this MUST come BEFORE NPC.SpawnAllowed_ArmsDealer, as it short-circuits based on the value we check.
            if (NPC.unlockedTruffleSpawn)
                return 0.0f;

            // Don't spawn if we shouldn't.
            if (!Main.hardMode)
                return 0.0f;

            // Don't spawn if we aren't in the mushroom biome.
            if (!spawnInfo.Player.ZoneGlowshroom)
                return 0.0f;

            // FIXME: What is this doing...? this is what bound goblin does!
            if (spawnInfo.SpawnTileY >= Main.maxTilesY - 210)
                return 0.0f;

            return base.SpawnChance(spawnInfo);
        }

        protected override void Transform(int whoAmI)
        {
            base.Transform(whoAmI);
            NPC.unlockedTruffleSpawn = true;
            NetMessage.SendData(MessageID.WorldData);
        }
    }
    public class Demolitionist : BoundNpc
    {
        public override int TransformInto => NPCID.Demolitionist;

        public override void SetDefaults()
        {
            base.SetDefaults();

            NPC.width = 26;
            NPC.height = 46;
            NPC.dontTakeDamage = false;
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            // Don't spawn if we shouldn't.
            if (!NPC.SpawnAllowed_Demolitionist())
                return 0.0f;

            // Don't spawn if we aren't in the caverns layer.
            if (!spawnInfo.Player.ZoneRockLayerHeight)
                return 0.0f;
            // FIXME: What is this doing...? this is what bound goblin does!
            if (spawnInfo.SpawnTileY >= Main.maxTilesY - 210)
                return 0.0f;

            return base.SpawnChance(spawnInfo);
        }

        protected override void Transform(int whoAmI)
        {
            base.Transform(whoAmI);
            NPC.unlockedDemolitionistSpawn = true;
            NetMessage.SendData(MessageID.WorldData);
        }
    }
}