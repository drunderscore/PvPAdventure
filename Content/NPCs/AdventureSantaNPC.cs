using PvPAdventure.Content.Items;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace PvPAdventure.Content.NPCs;

internal class AdventureSantaNPC : ModNPC
{
    //public override string Texture => $"Terraria/Images/NPC_{NPCID.SantaClaus}";
    public override string Texture => $"PvPAdventure/Assets/NPC/AdventureSanta";

    public override void SetStaticDefaults()
    {
        // Match vanilla animation frame count
        Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.SantaClaus];

        // Copy common town NPC static sets Santa uses
        NPCID.Sets.ExtraFramesCount[Type] = NPCID.Sets.ExtraFramesCount[NPCID.SantaClaus];
        NPCID.Sets.AttackFrameCount[Type] = NPCID.Sets.AttackFrameCount[NPCID.SantaClaus];
        NPCID.Sets.DangerDetectRange[Type] = NPCID.Sets.DangerDetectRange[NPCID.SantaClaus];
        NPCID.Sets.AttackType[Type] = NPCID.Sets.AttackType[NPCID.SantaClaus];
        NPCID.Sets.AttackTime[Type] = NPCID.Sets.AttackTime[NPCID.SantaClaus];
        NPCID.Sets.AttackAverageChance[Type] = NPCID.Sets.AttackAverageChance[NPCID.SantaClaus];
        NPCID.Sets.HatOffsetY[Type] = NPCID.Sets.HatOffsetY[NPCID.SantaClaus];
    }

    public override void SetDefaults()
    {
        // Copies stats, aiStyle, townNPC flags, sounds, etc.
        NPC.CloneDefaults(NPCID.SantaClaus);

        // Ensures the AI/animation logic follows Santa
        AIType = NPCID.SantaClaus;
        AnimationType = NPCID.SantaClaus;

        // Ensure shop is an option
        NPC.townNPC = true;
        NPC.friendly = true;
    }

    // Only for debugging purposes; prevent spawning in normal gameplay
    public override bool CanTownNPCSpawn(int numTownNPCs) => false;

    public override List<string> SetNPCNameList()
    {
        return ["Adventure Santa"];
    }

    public override void OnChatButtonClicked(bool firstButton, ref string shopName)
    {
        if (firstButton)
        {
            shopName = "Shop";
        }
    }

    public override void SetChatButtons(ref string button, ref string button2)
    {
        button = "Shop";
        button2 = null;
    }

    public override void AddShops()
    {
        var shop = new NPCShop(Type);

        shop.Add(ModContent.ItemType<AdventureBag>());
        shop.Register();
    }

    public override string GetChat()
    {
        WeightedRandom<string> chat = new();

        // playful / PvP-adventure lines
        chat.Add("Ho ho ho! Nothing like a good duel to warm the spirit.");
        chat.Add("PvP builds character… and rivalries.");
        chat.Add("I’ve seen heroes fall faster than snow in a Blizzard.");
        chat.Add("Careful out there—adventure rarely plays fair.");

        // seasoned adventurer wisdom
        chat.Add("Every great adventure starts with poor preparation.");
        chat.Add("You learn more from defeat than from loot.");
        chat.Add("A good team wins fights. A great team survives them.");
        chat.Add("Terraria rewards the bold… and punishes the careless.");

        // Santa-flavored flavor text
        chat.Add("I don’t just deliver gifts—I deliver second chances.");
        chat.Add("Even Santa needs good gear in this world.");
        chat.Add("Naughty or nice? In PvP, everyone bleeds the same.");
        chat.Add("Ho ho ho… that build could use some work.");

        return chat;
    }
}
