using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using PvPAdventure.Common.Game;
using PvPAdventure.Content.Portals;
using PvPAdventure.Core.Compat;
using PvPAdventure.Core.Config;
using PvPFramework.Common.Spawnbox;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.NPCs;

public sealed class ShakingChestNPC : GlobalNPC
{
    internal const int TargetType = NPCID.BoundTownSlimeOld;
    private const string ShopName = "Shop";
    private const float DisplayScale = 4f;
    private const int SpawnBoxPadding = 8;

    private static ServerConfig.ShakingChestConfig Config =>
        ModContent.GetInstance<ServerConfig>().ShakingChest;

    public override void SetStaticDefaults() =>
        new NPCShop(TargetType, ShopName).Register();

    public override void SetDefaults(NPC npc)
    {
        if (npc.type != TargetType)
            return;

        npc.townNPC = true;
        npc.friendly = true;
        npc.dontTakeDamage = true;
        npc.immortal = true;
        npc.homeless = true;
        npc.aiStyle = NPCAIStyleID.Slime;
        npc.scale = DisplayScale;
    }

    public override bool? CanChat(NPC npc) => npc.type == TargetType ? true : null;

    public override void GetChat(NPC npc, ref string chat)
    {
        if (npc.type == TargetType)
            chat = "...";
    }

    public override void OnChatButtonClicked(NPC npc, bool firstButton)
    {
        if (npc.type != TargetType || !firstButton)
            return;

        Main.playerInventory = true;
        Main.stackSplit = 9999;
        Main.npcChatText = "";
        Main.SetNPCShopIndex(1);
        ShakingChestUI.Open(CreateShopItems(), npc);
        Main.LocalPlayer.currentShoppingSettings.PriceAdjustment = 1d;
        SoundEngine.PlaySound(SoundID.MenuTick);
    }

    private static Item[] CreateShopItems() => Config.ShopItems
        .Select(entry => CreateItem(entry?.Item, 1, 0, entry?.Price ?? 0))
        .Where(item => !item.IsAir)
        .ToArray();

    public static void RefundPlayer(Player player)
    {
        Clear(player.inventory);
        Clear(player.armor);
        Clear(player.dye);
        Clear(player.miscEquips);
        Clear(player.miscDyes);
        player.trashItem = new Item();
        Main.mouseItem = new Item();

        for (int i = 0; i < Player.MaxBuffs; i++)
        {
            player.buffType[i] = 0;
            player.buffTime[i] = 0;
        }

        if (!ErkySSCCompat.TryApplyStartingItems(player))
        {
            ApplyFallbackStartingItems(player);
            AddStartingCoins(player);
        }

        Recipe.FindRecipes();
        ShakingChestLoadouts.Sync(player);
    }

    private static void ApplyFallbackStartingItems(Player player)
    {
        player.inventory[0] = new Item(ItemID.CopperShortsword);
        player.inventory[1] = new Item(ItemID.CopperPickaxe);
        player.inventory[2] = new Item(ItemID.CopperAxe);
        player.inventory[4] = new Item(ItemID.Bed);
        player.inventory[5] = new Item(ModContent.ItemType<PortalCreatorItem>());
    }

    private static void AddStartingCoins(Player player)
    {
        if (Config.StartingCoins <= 0)
            return;

        int slot = System.Array.FindIndex(player.inventory, item => item.IsAir);
        if (slot >= 0)
            player.inventory[slot] = new Item(ItemID.GoldCoin) { stack = Config.StartingCoins };
    }

    private static Item CreateItem(
        Terraria.ModLoader.Config.ItemDefinition definition,
        int stack,
        int prefix,
        int price = 0)
    {
        if (definition == null || definition.IsUnloaded || definition.Type <= ItemID.None)
            return new Item();

        return new Item(definition.Type, stack, prefix)
        {
            shopCustomPrice = price
        };
    }

    private static void Clear(IEnumerable<Item> items)
    {
        foreach (Item item in items)
            item?.TurnToAir();
    }

    public override void PostAI(NPC npc)
    {
        if (npc.type != TargetType || Main.netMode == NetmodeID.MultiplayerClient)
            return;

        if (ModContent.GetInstance<GameManager>().CurrentPhase == GameManager.Phase.Playing)
        {
            npc.active = false;
            npc.life = 0;
            if (Main.dedServ)
                NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
            return;
        }

        ConfineToSpawnBox(npc);
    }

    private static void ConfineToSpawnBox(NPC npc)
    {
        Rectangle tileArea = ModContent.GetInstance<SpawnBoxSystem>().TileArea;
        if (tileArea.IsEmpty)
            return;

        Rectangle area = SpawnBoxSystem.TileToWorld(tileArea);
        float minX = area.Left + SpawnBoxPadding;
        float maxX = area.Right - SpawnBoxPadding - npc.width;

        if (maxX < minX)
        {
            npc.Center = new Vector2(area.Center.X, npc.Center.Y);
            npc.velocity.X = 0f;
            return;
        }

        if (npc.position.X < minX)
        {
            npc.position.X = minX;
            npc.velocity.X = System.Math.Abs(npc.velocity.X);
            npc.direction = 1;
            npc.netUpdate = true;
        }
        else if (npc.position.X > maxX)
        {
            npc.position.X = maxX;
            npc.velocity.X = -System.Math.Abs(npc.velocity.X);
            npc.direction = -1;
            npc.netUpdate = true;
        }
    }

    public override void ModifyHoverBoundingBox(NPC npc, ref Rectangle boundingBox)
    {
        if (npc.type == TargetType)
            boundingBox = npc.Hitbox;
    }
}
