using PvPAdventure.Common.GameTimer;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using PvPAdventure.Content.Portals;

namespace PvPAdventure.Common.NPCs;

public class ShakingChestNPC : GlobalNPC
{
    private static int TargetType => NPCID.BoundTownSlimeOld;
    public const string ShopName = "Shop";

    public static readonly NPCShop Shop = new NPCShop(NPCID.BoundTownSlimeOld, ShopName)
        .Add(new Item(ItemID.Wood) { shopCustomPrice = Item.buyPrice(silver: 2) })
        .Add(new Item(ItemID.MiningPotion) { shopCustomPrice = Item.buyPrice(gold: 5) })
        .Add(new Item(ItemID.Torch) { shopCustomPrice = Item.buyPrice(silver: 1) })
        .Add(new Item(ItemID.WoodenBoomerang) { shopCustomPrice = Item.buyPrice(silver: 300) })
        .Add(new Item(ItemID.Umbrella) { shopCustomPrice = Item.buyPrice(silver: 180) })
        .Add(new Item(ItemID.Blowpipe) { shopCustomPrice = Item.buyPrice(silver: 150) })
        .Add(new Item(ItemID.Seed) { shopCustomPrice = Item.buyPrice(silver: 2) })
        .Add(new Item(ItemID.BlandWhip) { shopCustomPrice = Item.buyPrice(silver: 400) })
        .Add(new Item(ItemID.BabyBirdStaff) { shopCustomPrice = Item.buyPrice(silver: 200) })
        .Add(new Item(ItemID.Shackle) { shopCustomPrice = Item.buyPrice(silver: 40) })
        .Add(new Item(ItemID.ClimbingClaws) { shopCustomPrice = Item.buyPrice(silver: 60) })
        .Add(new Item(ItemID.Flipper) { shopCustomPrice = Item.buyPrice(silver: 80) })
        .Add(new Item(ItemID.CopperWatch) { shopCustomPrice = Item.buyPrice(silver: 50) })
        .Add(new Item(ItemID.BandofStarpower) { shopCustomPrice = Item.buyPrice(silver: 50) })
        .Add(new Item(ItemID.NaturesGift) { shopCustomPrice = Item.buyPrice(silver: 33) })
        .Add(new Item(ItemID.IronBar) { shopCustomPrice = Item.buyPrice(silver: 75) })
        .Add(new Item(ItemID.IronskinPotion) { shopCustomPrice = Item.buyPrice(silver: 250) })
        .Add(new Item(ItemID.ThrowingKnife) { shopCustomPrice = Item.buyPrice(silver: 15) })
        .Add(new Item(ItemID.Shuriken) { shopCustomPrice = Item.buyPrice(silver: 12) })
        .Add(new Item(ItemID.LesserHealingPotion) { shopCustomPrice = Item.buyPrice(silver: 125) })
        .Add(new Item(ItemID.ShinePotion) { shopCustomPrice = Item.buyPrice(silver: 125) })
        .Add(new Item(ItemID.NightOwlPotion) { shopCustomPrice = Item.buyPrice(silver: 160) })
        .Add(new Item(ItemID.SwiftnessPotion) { shopCustomPrice = Item.buyPrice(silver: 200) })
        .Add(new Item(ItemID.BuilderPotion) { shopCustomPrice = Item.buyPrice(silver: 250) })
        .Add(new Item(ItemID.Cobweb) { shopCustomPrice = Item.buyPrice(silver: 3) })
        .Add(new Item(ItemID.Grenade) { shopCustomPrice = Item.buyPrice(silver: 100) })
        .Add(new Item(ItemID.Aglet) { shopCustomPrice = Item.buyPrice(silver: 100) })
        .Add(new Item(ItemID.Trident) { shopCustomPrice = Item.buyPrice(silver: 350) })
        .Add(new Item(ItemID.Toolbox) { shopCustomPrice = Item.buyPrice(silver: 500) })
        .Add(new Item(ItemID.PortableStool) { shopCustomPrice = Item.buyPrice(silver: 100) })
        .Add(new Item(ItemID.Bottle) { shopCustomPrice = Item.buyPrice(silver: 10) })
        .Add(new Item(ItemID.Mushroom) { shopCustomPrice = Item.buyPrice(silver: 25) })
        .Add(new Item(ItemID.WandofSparking) { shopCustomPrice = Item.buyPrice(silver: 775) })
        .Add(new Item(ItemID.HunterPotion) { shopCustomPrice = Item.buyPrice(silver: 175) })
        .Add(new Item(ItemID.Rope) { shopCustomPrice = Item.buyPrice(copper: 50) })
        .Add(new Item(ItemID.WaterWalkingBoots) { shopCustomPrice = Item.buyPrice(silver: 150) })
        .Add(new Item(ItemID.MoonLordLegs) { shopCustomPrice = Item.buyPrice(silver: 1000) })
        .Add(new Item(ItemID.Bomb) { shopCustomPrice = Item.buyPrice(silver: 25) })
        .Add(new Item(ItemID.Worm) { shopCustomPrice = Item.buyPrice(silver: 15) })
        .Add(new Item(ItemID.EnchantedNightcrawler) { shopCustomPrice = Item.buyPrice(silver: 50) })
        .Add(new Item(ItemID.GoldWorm) { shopCustomPrice = Item.buyPrice(silver: 1000) })
        .Add(new Item(ItemID.Glowstick) { shopCustomPrice = Item.buyPrice(silver: 10) })
        .Add(new Item(ItemID.Dynamite) { shopCustomPrice = Item.buyPrice(silver: 300) })
        .Add(new Item(ItemID.GillsPotion) { shopCustomPrice = Item.buyPrice(silver: 100) })
        .Add(new Item(ItemID.TrapsightPotion) { shopCustomPrice = Item.buyPrice(silver: 100) })
        .Add(new Item(ItemID.FlipperPotion) { shopCustomPrice = Item.buyPrice(silver: 30) })
        .Add(new Item(ItemID.Dynamite) { shopCustomPrice = Item.buyPrice(silver: 300) })
        .Add(new Item(ItemID.WhoopieCushion) { shopCustomPrice = Item.buyPrice(silver: 400) })
        .Add(new Item(ItemID.Radar) { shopCustomPrice = Item.buyPrice(silver: 100) })
        .Add(new Item(ItemID.FlareGun) { shopCustomPrice = Item.buyPrice(silver: 200) })
        .Add(new Item(ItemID.BlueFlare) { shopCustomPrice = Item.buyPrice(silver: 8) })
        .Add(new Item(ItemID.FloatingTube) { shopCustomPrice = Item.buyPrice(silver: 100) });

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
        if (npc.type == TargetType) return true;
        return null;
    }

    public override void GetChat(NPC npc, ref string chat)
    {
        if (npc.type != TargetType) return;
        chat = "...";
    }

    public override void OnChatButtonClicked(NPC npc, bool firstButton)
    {
        if (npc.type != TargetType) return;

        if (firstButton)
        {
            Main.playerInventory = true;
            Main.stackSplit = 9999;
            Main.npcChatText = "";
            Main.SetNPCShopIndex(1);

            var tempItems = new Item[Shop.Entries.Count + 1];
            Shop.FillShop(tempItems, npc, out _);

            ShopPager.Reset();
            ShopPager.OpenShop(tempItems, npc);

            Main.LocalPlayer.currentShoppingSettings.PriceAdjustment = 1.0;
            SoundEngine.PlaySound(SoundID.MenuTick);
        }
        else
        {
            RefundPlayer(Main.LocalPlayer);
            SoundEngine.PlaySound(SoundID.MenuTick);
        }
    }

    public static void RefundPlayer(Player player)
    {
        for (int i = 0; i < player.inventory.Length; i++)
            player.inventory[i] = new Item();

        for (int i = 0; i < player.armor.Length; i++)
            player.armor[i] = new Item();

        for (int i = 0; i < player.dye.Length; i++)
            player.dye[i] = new Item();

        for (int i = 0; i < player.miscEquips.Length; i++)
            player.miscEquips[i] = new Item();

        for (int i = 0; i < player.miscDyes.Length; i++)
            player.miscDyes[i] = new Item();

        player.trashItem = new Item();
        Main.mouseItem = new Item();

        // Clear all buffs
        for (int i = 0; i < Player.MaxBuffs; i++)
        {
            player.buffType[i] = 0;
            player.buffTime[i] = 0;
        }

        player.inventory[0] = new Item(ItemID.CopperShortsword);
        player.inventory[1] = new Item(ItemID.CopperPickaxe);
        player.inventory[2] = new Item(ItemID.CopperAxe);

        Item coins = new Item(ItemID.GoldCoin);
        coins.stack = 10;
        player.inventory[3] = coins;

        player.inventory[4] = new Item(ItemID.Bed);
        player.inventory[5] = new Item(ModContent.ItemType<PortalCreatorItem>());
    }

    public override void ModifyActiveShop(NPC npc, string shopName, Item[] items)
    {
        if (npc.type != TargetType || shopName != ShopName) return;
        Shop.FillShop(items, npc, out _);
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