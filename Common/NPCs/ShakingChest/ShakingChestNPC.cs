using PvPAdventure.Common.GameTimer;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.NPCs;

public class ShakingChestNPC : GlobalNPC
{
    private static int TargetType => NPCID.BoundTownSlimeOld;
    public const string ShopName = "Shop";

    private static readonly NPCShop _shop = new NPCShop(NPCID.BoundTownSlimeOld, ShopName)
        .Add(new Item(ItemID.Wood) { shopCustomPrice = Item.buyPrice(silver: 2) })
        .Add(new Item(ItemID.MiningPotion) { shopCustomPrice = Item.buyPrice(gold: 4) })
        .Add(new Item(ItemID.Torch) { shopCustomPrice = Item.buyPrice(silver: 1) })
        .Add(new Item(ItemID.WoodenBoomerang) { shopCustomPrice = Item.buyPrice(silver: 300) })
        .Add(new Item(ItemID.Umbrella) { shopCustomPrice = Item.buyPrice(silver: 250) })
        .Add(new Item(ItemID.WandofSparking) { shopCustomPrice = Item.buyPrice(silver: 540) })
        .Add(new Item(ItemID.Blowpipe) { shopCustomPrice = Item.buyPrice(silver: 150) })
        .Add(new Item(ItemID.Seed) { shopCustomPrice = Item.buyPrice(silver: 20) })
        .Add(new Item(ItemID.BlandWhip) { shopCustomPrice = Item.buyPrice(silver: 450) })
        .Add(new Item(ItemID.BabyBirdStaff) { shopCustomPrice = Item.buyPrice(silver: 275) })
        .Add(new Item(ItemID.Shackle) { shopCustomPrice = Item.buyPrice(silver: 40) })
        .Add(new Item(ItemID.ClimbingClaws) { shopCustomPrice = Item.buyPrice(silver: 60) })
        .Add(new Item(ItemID.Flipper) { shopCustomPrice = Item.buyPrice(silver: 80) })
        .Add(new Item(ItemID.CopperWatch) { shopCustomPrice = Item.buyPrice(silver: 50) })
        .Add(new Item(ItemID.BandofStarpower) { shopCustomPrice = Item.buyPrice(silver: 50) })
        .Add(new Item(ItemID.FallenStar) { shopCustomPrice = Item.buyPrice(silver: 20) })
        .Add(new Item(ItemID.NaturesGift) { shopCustomPrice = Item.buyPrice(silver: 33) })
        .Add(new Item(ItemID.IronBar) { shopCustomPrice = Item.buyPrice(silver: 75) })
        .Add(new Item(ItemID.ThrowingKnife) { shopCustomPrice = Item.buyPrice(silver: 15) })
        .Add(new Item(ItemID.Shuriken) { shopCustomPrice = Item.buyPrice(silver: 12) })
        .Add(new Item(ItemID.LesserHealingPotion) { shopCustomPrice = Item.buyPrice(silver: 125) })
        .Add(new Item(ItemID.ShinePotion) { shopCustomPrice = Item.buyPrice(silver: 125) })
        .Add(new Item(ItemID.NightOwlPotion) { shopCustomPrice = Item.buyPrice(silver: 160) })
        .Add(new Item(ItemID.SwiftnessPotion) { shopCustomPrice = Item.buyPrice(silver: 200) })
        .Add(new Item(ItemID.BuilderPotion) { shopCustomPrice = Item.buyPrice(silver: 250) })
        .Add(new Item(ItemID.Cobweb) { shopCustomPrice = Item.buyPrice(silver: 3) })
        .Add(new Item(ItemID.Grenade) { shopCustomPrice = Item.buyPrice(silver: 200) })
        .Add(new Item(ItemID.MolluskWhistle) { shopCustomPrice = Item.buyPrice(silver: 1002) })
        .Add(new Item(ItemID.Aglet) { shopCustomPrice = Item.buyPrice(silver: 100) })
        .Add(new Item(ItemID.Trident) { shopCustomPrice = Item.buyPrice(silver: 350) })
        .Add(new Item(ItemID.ObsidianBathtub) { shopCustomPrice = Item.buyPrice(silver: 1000) })
        .Add(new Item(ItemID.Bomb) { shopCustomPrice = Item.buyPrice(silver: 25) });


    public override void SetStaticDefaults()
    {
        new NPCShop(TargetType, ShopName).Register();
    }

    public override void SetDefaults(NPC npc)
    {
        if (npc.type != TargetType) return;

        npc.townNPC = true;
        npc.friendly = true;
        npc.dontTakeDamage = true;
        npc.immortal = true;
        npc.homeless = true;
        npc.aiStyle = NPCAIStyleID.Passive;
    }

    public override bool? CanChat(NPC npc)
    {
        if (npc.type == TargetType)
            return true;
        return null;
    }

    public override void GetChat(NPC npc, ref string chat)
    {
        if (npc.type != TargetType) return;
        chat = "...";
    }

    public override bool PreChatButtonClicked(NPC npc, bool firstButton)
    {
        if (npc.type != TargetType || !firstButton) return true;

        Main.playerInventory = true;
        Main.stackSplit = 9999;
        Main.npcChatText = "";
        Main.SetNPCShopIndex(1);
        Main.instance.shop[Main.npcShop].SetupShop(ShopName, npc);

        Main.LocalPlayer.currentShoppingSettings.PriceAdjustment = 1.0;

        SoundEngine.PlaySound(SoundID.MenuTick);

        return false;
    }

    public override void ModifyActiveShop(NPC npc, string shopName, Item[] items)
    {
        if (npc.type != TargetType || shopName != ShopName) return;

        _shop.FillShop(items, npc, out _);
    }

    public override void PostAI(NPC npc)
    {
        if (npc.type != TargetType) return;
        if (Main.netMode == NetmodeID.MultiplayerClient) return;

        if (ModContent.GetInstance<GameManager>().CurrentPhase == GameManager.Phase.Playing)
        {
            npc.active = false;
            npc.life = 0;

            if (Main.dedServ)
                NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
        }
    }
}