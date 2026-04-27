using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Spectator.Drawers;
using PvPAdventure.Common.Spectator.Trackers;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using static PvPAdventure.Common.Spectator.Drawers.BiomeHelper;

namespace PvPAdventure.Common.Spectator.UI.Tabs.Players;

internal static class PlayerStats
{
    public static readonly PlayerStatDefinition PlayerName = new(
        "PlayerName",
        "Player",
        Ass.Icon_PlayerHead,
        player => player.name,
        player => $"Player: {player.name}");

    public static readonly PlayerStatDefinition Life = new(
        "Life",
        "Health",
        TextureAssets.Heart,
        player => $"{player.statLife}/{player.statLifeMax2}");

    public static readonly PlayerStatDefinition Mana = new(
        "Mana",
        "Mana",
        TextureAssets.Mana,
        player => $"{player.statMana}/{player.statManaMax2}");

    public static readonly PlayerStatDefinition Defense = new(
        "Defense",
        "Defense",
        TextureAssets.Extra[ExtrasID.DefenseShield],
        player => $"{player.statDefense} defense");

    public static readonly PlayerStatDefinition HeldItem = new(
        "HeldItem",
        "Held Item",
        GetHeldItemIcon,
        GetHeldItemText,
        getIconFrame: GetHeldItemFrame);

    public static readonly PlayerStatDefinition Biome = new(
        "BiomeName",
        "Biome",
        GetBiomeIcon,
        GetBiomeText,
        getIconFrame: GetBiomeIconFrame);

    public static readonly PlayerStatDefinition MovementSpeed = new(
        "MovementSpeed",
        "Movement Speed",
        TextureAssets.Item[ItemID.Stopwatch],
        GetMovementSpeed);

    public static readonly PlayerStatDefinition Distance = new(
        "Distance",
        "Distance",
        Ass.Distance,
        player => $"{Math.Round(Vector2.Distance(Main.LocalPlayer.Center, player.Center) / 16f)} tiles");

    public static readonly PlayerStatDefinition SessionTime = new(
        "SessionTime",
        "Session",
        Ass.Time,
        player => SessionTracker.GetSessionDuration(player.whoAmI));

    public static readonly PlayerStatDefinition Ping = new(
        "Ping",
        "Ping",
        Ass.Ping,
        player => $"{GetPlayerPingMs(player)} ms");

    public static readonly PlayerStatDefinition InventoryItemCount = new(
        "InventoryItemCount",
        "Inventory",
        Ass.InventoryCount,
        player => $"{CountInventoryItems(player)} items");

    public static readonly PlayerStatDefinition CoinCount = new(
        "CoinCount",
        "Coins",
        GetHighestCoinIcon,
        player => FormatTotalCoins(CountTotalCoins(player), out _));

    public static readonly PlayerStatDefinition AmmoCount = new(
        "AmmoCount",
        "Ammo",
        GetMostStackedAmmoIcon,
        GetMostStackedAmmoText,
        getIconFrame: GetMostStackedAmmoFrame);

    //public static readonly PlayerStatDefinition NearbyEnemies = new(
    //    "NearbyEnemies",
    //    "Nearby Enemies",
    //    GetNearestHostileNPCIcon,
    //    GetNearestHostileNPCText,
    //    getIconFrame: GetNearestHostileNPCFrame);

    //public static readonly PlayerStatDefinition LastEnemyHit = new(
    //    "LastEnemyHit",
    //    "Last Enemy Hit",
    //    GetLastEnemyHitIcon,
    //    GetLastEnemyHitText,
    //    getIconFrame: GetLastEnemyHitFrame);

    //public static readonly PlayerStatDefinition LastPlayerHit = new(
    //    "LastPlayerHit",
    //    "Last Player Hit",
    //    Ass.PvP,
    //    GetLastPlayerHitText);

    //public static readonly PlayerStatDefinition MinionCount = new(
    //    "MinionCount",
    //    "Minions",
    //    GetLatestSummonStaffIcon,
    //    player => $"{CountPlayerMinions(player)}/{player.maxMinions} minions",
    //    getIconFrame: GetLatestSummonStaffFrame);

    //public static readonly PlayerStatDefinition BossDamage = new(
    //    "BossDamage",
    //    "Boss Damage",
    //    Ass.BossDamage,
    //    GetBossDamageText);

    /// <summary>
    /// A list of all player stats to include.
    /// </summary>
    public static readonly IReadOnlyList<PlayerStatDefinition> All =
    [
        PlayerName,
        Life,
        Mana,
        Defense,
        HeldItem,
        Biome,
        //Position,
        //Team,
        MovementSpeed,
        Distance,
        SessionTime,
        Ping,
        InventoryItemCount,
        //CoinCount,
        //AmmoCount,
        //MinionCount,
        //NearbyEnemies,
        //LastEnemyHit,
        //LastPlayerHit,
        //DeathCount,
        //BossDamage
    ];

    private static Asset<Texture2D> GetBiomeIcon(Player player)
    {
        PlayerBiomeVisual biome = BiomeHelper.GetBiomeVisual(player);

        if (BiomeHelper.TryGetBestiaryIconDrawData(biome.BestiaryBiome, out Asset<Texture2D> texture, out _))
            return texture;

        return Ass.Biome;
    }

    private static Rectangle? GetBiomeIconFrame(Player player)
    {
        PlayerBiomeVisual biome = BiomeHelper.GetBiomeVisual(player);

        if (BiomeHelper.TryGetBestiaryIconDrawData(biome.BestiaryBiome, out _, out Rectangle source))
            return source;

        return null;
    }

    private static Asset<Texture2D> GetHeldItemIcon(Player player)
    {
        Item item = player.HeldItem;

        if (item == null || item.IsAir || item.type <= 0 || item.type >= TextureAssets.Item.Length)
            return Ass.HeldItem;

        Main.instance.LoadItem(item.type);
        return TextureAssets.Item[item.type];
    }

    private static Rectangle? GetHeldItemFrame(Player player)
    {
        Item item = player.HeldItem;

        if (item == null || item.IsAir || item.type <= 0 || item.type >= TextureAssets.Item.Length)
            return null;

        Main.instance.LoadItem(item.type);
        Texture2D texture = TextureAssets.Item[item.type].Value;

        return Main.itemAnimations[item.type]?.GetFrame(texture) ?? texture.Frame();
    }

    private static string GetHeldItemText(Player player)
    {
        Item item = player.HeldItem;
        return item == null || item.IsAir ? "Empty" : item.Name;
    }

    public static string GetBiomeText(Player player)
    {
        PlayerBiomeVisual biome = BiomeHelper.GetBiomeVisual(player);
        string biomeName = Language.GetTextValue(biome.BestiaryBiome.GetDisplayNameKey());

        if (biome.BestiaryBiome == BiomeHelper.ShimmerBiome)
            biomeName = "Aether";

        return biomeName;
    }

    private static int GetPlayerPingMs(Player player)
    {
        int ping = PingTracker.GetPing(player.whoAmI);
        return ping < 0 ? 0 : ping;
    }

    private static int CountInventoryItems(Player player)
    {
        int count = 0;
        for (int i = 0; i < player.inventory.Length; i++)
        {
            Item item = player.inventory[i];
            if (item != null && !item.IsAir)
                count += item.stack;
        }

        return count;
    }

    private static long CountTotalCoins(Player player)
    {
        long total = 0;
        for (int i = 0; i < player.inventory.Length; i++)
        {
            Item item = player.inventory[i];
            if (item == null || item.IsAir || item.stack <= 0)
                continue;

            if (item.type == ItemID.CopperCoin)
                total += item.stack;
            else if (item.type == ItemID.SilverCoin)
                total += item.stack * 100L;
            else if (item.type == ItemID.GoldCoin)
                total += item.stack * 10000L;
            else if (item.type == ItemID.PlatinumCoin)
                total += item.stack * 1000000L;
        }

        return total;
    }

    private static string FormatTotalCoins(long totalCopper, out Color color)
    {
        color = Color.White;

        long platinum = totalCopper / 1000000L;
        totalCopper %= 1000000L;
        long gold = totalCopper / 10000L;
        totalCopper %= 10000L;
        long silver = totalCopper / 100L;
        long copper = totalCopper % 100L;

        if (platinum > 9999)
            return "9999+ plat";
        if (platinum > 0)
            return $"{platinum} plat {gold} gold";
        if (gold > 0)
            return $"{gold} gold {silver} silver";
        if (silver > 0)
            return $"{silver} silver {copper} copper";
        if (copper <= 0)
            return "No coins";

        return $"{copper} copper";
    }

    private static int CountPlayerMinions(Player player)
    {
        int total = 0;
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile projectile = Main.projectile[i];
            if (projectile.active && projectile.owner == player.whoAmI && projectile.minion)
                total++;
        }

        return total;
    }

    //private static string GetLastEnemyHitText(Player player)
    //{
    //    NPCHitTrackerPlayer tracker = player.GetModPlayer<NPCHitTrackerPlayer>();
    //    return string.IsNullOrWhiteSpace(tracker.LastEnemyHitName) ? "None" : tracker.LastEnemyHitName;
    //}

    //private static string GetLastPlayerHitText(Player player)
    //{
    //    NPCHitTrackerPlayer tracker = player.GetModPlayer<NPCHitTrackerPlayer>();
    //    return string.IsNullOrWhiteSpace(tracker.LastPlayerHitName) ? "None" : tracker.LastPlayerHitName;
    //}

    //private static string GetBossDamageText(Player player)
    //{
    //    long damage = player.GetModPlayer<NPCHitTrackerPlayer>().TotalBossDamage;
    //    return damage.ToString("N0");
    //}

    private static string GetMovementSpeed(Player player)
    {
        Vector2 vector = player.velocity + player.instantMovementAccumulatedThisFrame;

        if (player.mount.Active && player.mount.IsConsideredASlimeMount && player.velocity.Y != 0f && !player.SlimeDontHyperJump)
            vector.Y += player.velocity.Y;

        const int TilesPerMile = 42240;
        const int TicksPerHour = 216000;
        float speed = vector.Length() * TicksPerHour / TilesPerMile;

        if (!player.merman && !player.ignoreWater)
        {
            if (player.honeyWet)
            {
                speed /= 4f;
            }
            else if (player.wet)
            {
                speed /= 2f;
            }
        }
        return Language.GetTextValue("GameUI.Speed", Math.Round(speed));
    }

    private static Asset<Texture2D> GetHighestCoinIcon(Player player)
    {
        return GetHighestCoinType(player) switch
        {
            ItemID.PlatinumCoin => TextureAssets.Item[ItemID.PlatinumCoin],
            ItemID.GoldCoin => TextureAssets.Item[ItemID.GoldCoin],
            ItemID.SilverCoin => TextureAssets.Item[ItemID.SilverCoin],
            ItemID.CopperCoin => TextureAssets.Item[ItemID.CopperCoin],
            _ => TextureAssets.Item[ItemID.CopperCoin]
        };
    }

    private static int GetHighestCoinType(Player player)
    {
        int highest = 0;

        for (int i = 0; i < player.inventory.Length; i++)
        {
            Item item = player.inventory[i];

            if (item == null || item.IsAir || item.stack <= 0)
                continue;

            if (item.type == ItemID.PlatinumCoin)
                return ItemID.PlatinumCoin;

            if (item.type == ItemID.GoldCoin)
                highest = ItemID.GoldCoin;
            else if (item.type == ItemID.SilverCoin && highest != ItemID.GoldCoin)
                highest = ItemID.SilverCoin;
            else if (item.type == ItemID.CopperCoin && highest == 0)
                highest = ItemID.CopperCoin;
        }

        return highest;
    }

    private static Item GetMostStackedAmmo(Player player)
    {
        Item best = null;
        int bestStack = 0;

        for (int i = 54; i < 58 && i < player.inventory.Length; i++)
        {
            Item item = player.inventory[i];

            if (item == null || item.IsAir || item.stack <= bestStack)
                continue;

            best = item;
            bestStack = item.stack;
        }

        return best;
    }

    private static Asset<Texture2D> GetMostStackedAmmoIcon(Player player)
    {
        Item item = GetMostStackedAmmo(player);

        if (item == null || item.IsAir || item.type <= 0 || item.type >= TextureAssets.Item.Length)
            return TextureAssets.Item[ItemID.MusketBall];

        Main.instance.LoadItem(item.type);
        return TextureAssets.Item[item.type];
    }

    private static Rectangle? GetMostStackedAmmoFrame(Player player)
    {
        Item item = GetMostStackedAmmo(player);

        if (item == null || item.IsAir || item.type <= 0 || item.type >= TextureAssets.Item.Length)
            return null;

        Main.instance.LoadItem(item.type);
        Texture2D texture = TextureAssets.Item[item.type].Value;
        return Main.itemAnimations[item.type]?.GetFrame(texture) ?? texture.Frame();
    }

    private static string GetMostStackedAmmoText(Player player)
    {
        Item item = GetMostStackedAmmo(player);
        return item == null || item.IsAir ? "No ammo" : $"{item.stack} {item.Name}";
    }

    private static NPC FindNearestHostileNPC(Player player, float range)
    {
        NPC nearest = null;
        float nearestDistanceSq = range * range;

        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];

            if (npc == null || !npc.active || npc.friendly || npc.townNPC || npc.dontTakeDamage || npc.lifeMax <= 5)
                continue;

            float distanceSq = Vector2.DistanceSquared(player.Center, npc.Center);

            if (distanceSq >= nearestDistanceSq)
                continue;

            nearest = npc;
            nearestDistanceSq = distanceSq;
        }

        return nearest;
    }

    private static Asset<Texture2D> GetNearestHostileNPCIcon(Player player)
    {
        NPC npc = FindNearestHostileNPC(player, 1200f);

        if (npc == null || npc.type <= 0 || npc.type >= TextureAssets.Npc.Length)
            return TextureAssets.Item[ItemID.LifeformAnalyzer];

        Main.instance.LoadNPC(npc.type);
        return TextureAssets.Npc[npc.type];
    }

    private static Rectangle? GetNearestHostileNPCFrame(Player player)
    {
        NPC npc = FindNearestHostileNPC(player, 1200f);

        if (npc == null || npc.type <= 0 || npc.type >= TextureAssets.Npc.Length)
            return null;

        return npc.frame;
    }

    private static string GetNearestHostileNPCText(Player player)
    {
        NPC npc = FindNearestHostileNPC(player, 1200f);
        return npc == null ? "None nearby" : npc.FullName;
    }

    //private static NPC FindLastEnemyHitNPC(Player player)
    //{
    //    NPCHitTrackerPlayer tracker = player.GetModPlayer<NPCHitTrackerPlayer>();

    //    if (string.IsNullOrWhiteSpace(tracker.LastEnemyHitName))
    //        return null;

    //    for (int i = 0; i < Main.maxNPCs; i++)
    //    {
    //        NPC npc = Main.npc[i];

    //        if (npc?.active == true && npc.FullName == tracker.LastEnemyHitName)
    //            return npc;
    //    }

    //    return null;
    //}

    //private static Asset<Texture2D> GetLastEnemyHitIcon(Player player)
    //{
    //    NPC npc = FindLastEnemyHitNPC(player);

    //    if (npc == null || npc.type <= 0 || npc.type >= TextureAssets.Npc.Length)
    //        return Ass.PvE;

    //    Main.instance.LoadNPC(npc.type);
    //    return TextureAssets.Npc[npc.type];
    //}

    //private static Rectangle? GetLastEnemyHitFrame(Player player)
    //{
    //    NPC npc = FindLastEnemyHitNPC(player);

    //    if (npc == null || npc.type <= 0 || npc.type >= TextureAssets.Npc.Length)
    //        return null;

    //    return npc.frame;
    //}

    //private static Player FindLastPlayerHit(Player player)
    //{
    //    NPCHitTrackerPlayer tracker = player.GetModPlayer<NPCHitTrackerPlayer>();

    //    if (string.IsNullOrWhiteSpace(tracker.LastPlayerHitName))
    //        return null;

    //    for (int i = 0; i < Main.maxPlayers; i++)
    //    {
    //        Player target = Main.player[i];

    //        if (target?.active == true && target.name == tracker.LastPlayerHitName)
    //            return target;
    //    }

    //    return null;
    //}

    //private static void DrawLastPlayerHitHead(SpriteBatch spriteBatch, Rectangle area, Player player)
    //{
    //    Player target = FindLastPlayerHit(player);

    //    if (target == null)
    //    {
    //        spriteBatch.Draw(Ass.PvP.Value, area, Color.White);
    //        return;
    //    }

    //    Vector2 position = area.Center.ToVector2();
    //    EntityDrawer.DrawPlayerHead(spriteBatch, target, position, 0.85f);
    //}

    //private static Asset<Texture2D> GetLatestSummonStaffIcon(Player player)
    //{
    //    if (player.GetModPlayer<SummonTrackerPlayer>().TryGetLatestSummonItem(out int itemType) &&
    //        itemType > 0 &&
    //        itemType < TextureAssets.Item.Length)
    //    {
    //        Main.instance.LoadItem(itemType);
    //        return TextureAssets.Item[itemType];
    //    }

    //    return Ass.MinionCount;
    //}

    //private static Rectangle? GetLatestSummonStaffFrame(Player player)
    //{
    //    if (!player.GetModPlayer<SummonTrackerPlayer>().TryGetLatestSummonItem(out int itemType) ||
    //        itemType <= 0 ||
    //        itemType >= TextureAssets.Item.Length)
    //    {
    //        return null;
    //    }

    //    Main.instance.LoadItem(itemType);
    //    Texture2D texture = TextureAssets.Item[itemType].Value;

    //    return Main.itemAnimations[itemType]?.GetFrame(texture) ?? texture.Frame();
    //}
}
