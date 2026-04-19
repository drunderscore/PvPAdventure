using AssGen;
using Microsoft.Xna.Framework;
using PvPAdventure.Common.Spectator.Drawers;
using PvPAdventure.Common.Spectator.Trackers;
using PvPAdventure.Core.Utilities;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using static PvPAdventure.Common.Spectator.Drawers.BiomeHelper;

namespace PvPAdventure.Common.Spectator.UI.Players;

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
        Ass.HeldItem,
        GetHeldItemText);

    public static readonly PlayerStatDefinition Biome = new(
        "BiomeName",
        "Biome",
        Ass.Biome,
        GetBiomeText);

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
        TextureAssets.Item[ItemID.GoldCoin],
        player => FormatTotalCoins(CountTotalCoins(player), out _));

    public static readonly PlayerStatDefinition AmmoCount = new(
        "AmmoCount",
        "Ammo",
        TextureAssets.Item[ItemID.MusketBall],
        player => $"{CountAmmo(player)} ammo");

    public static readonly PlayerStatDefinition MinionCount = new(
        "MinionCount",
        "Minions",
        Ass.MinionCount,
        player => $"{CountPlayerMinions(player)}/{player.maxMinions} minions");

    public static readonly PlayerStatDefinition NearbyEnemies = new(
        "NearbyEnemies",
        "Nearby Enemies",
        TextureAssets.Item[ItemID.LifeformAnalyzer],
        player => $"{CountNearbyEnemies(player, 1200f)} nearby");

    public static readonly PlayerStatDefinition LastEnemyHit = new(
        "LastEnemyHit",
        "Last Enemy Hit",
        Ass.PvE,
        GetLastEnemyHitText);

    public static readonly PlayerStatDefinition LastPlayerHit = new(
        "LastPlayerHit",
        "Last Player Hit",
        Ass.PvP,
        GetLastPlayerHitText);

    public static readonly PlayerStatDefinition BossDamage = new(
        "BossDamage",
        "Boss Damage",
        Ass.BossDamage,
        GetBossDamageText);

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
        CoinCount,
        AmmoCount,
        MinionCount,
        NearbyEnemies,
        LastEnemyHit,
        LastPlayerHit,
        //DeathCount,
        BossDamage
    ];
    private static string GetTeamText(Player player) => player.team switch
    {
        1 => "Red",
        2 => "Green",
        3 => "Blue",
        4 => "Yellow",
        5 => "Pink",
        _ => "None"
    };

    private static string GetHeldItemText(Player player)
    {
        Item item = player.HeldItem;
        return item == null || item.IsAir ? "Empty" : item.Name;
    }

    private static string GetBiomeText(Player player)
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

        if (platinum > 0)
            return $"{platinum}p {gold}g {silver}s {copper}c";
        if (gold > 0)
            return $"{gold}g {silver}s {copper}c";
        if (silver > 0)
            return $"{silver}s {copper}c";

        return $"{copper}c";
    }

    private static int CountAmmo(Player player)
    {
        int total = 0;
        for (int i = 0; i < player.inventory.Length; i++)
        {
            Item item = player.inventory[i];
            if (item != null && !item.IsAir && item.ammo > 0)
                total += item.stack;
        }

        return total;
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

    private static int CountNearbyEnemies(Player player, float range)
    {
        int total = 0;
        float rangeSq = range * range;
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (npc == null || !npc.active || npc.friendly || npc.townNPC || npc.dontTakeDamage)
                continue;

            if (Vector2.DistanceSquared(player.Center, npc.Center) <= rangeSq)
                total++;
        }

        return total;
    }

    private static string GetLastEnemyHitText(Player player)
    {
        NPCHitTrackerPlayer tracker = player.GetModPlayer<NPCHitTrackerPlayer>();
        return string.IsNullOrWhiteSpace(tracker.LastEnemyHitName) ? "None" : tracker.LastEnemyHitName;
    }

    private static string GetLastPlayerHitText(Player player)
    {
        NPCHitTrackerPlayer tracker = player.GetModPlayer<NPCHitTrackerPlayer>();
        return string.IsNullOrWhiteSpace(tracker.LastPlayerHitName) ? "None" : tracker.LastPlayerHitName;
    }

    private static string GetBossDamageText(Player player)
    {
        long damage = player.GetModPlayer<NPCHitTrackerPlayer>().TotalBossDamage;
        return damage.ToString("N0");
    }

    // Taken from Main.DrawInfoAccs(), if (info == InfoDisplay.Stopwatch)...
    private static string GetMovementSpeed(Player player)
    {
        Vector2 vector = player.velocity + player.instantMovementAccumulatedThisFrame;
        if (Main.LocalPlayer.mount.Active && Main.player[Main.myPlayer].mount.IsConsideredASlimeMount && player.velocity.Y != 0f && !player.SlimeDontHyperJump)
        {
            vector.Y += Main.player[Main.myPlayer].velocity.Y;
        }
        int num15 = (int)(1f + vector.Length() * 6f);
        if (num15 > Main.player[Main.myPlayer].speedSlice.Length)
        {
            num15 = Main.player[Main.myPlayer].speedSlice.Length;
        }
        float num16 = 0f;
        for (int num17 = num15 - 1; num17 > 0; num17--)
        {
            Main.player[Main.myPlayer].speedSlice[num17] = Main.player[Main.myPlayer].speedSlice[num17 - 1];
        }
        Main.player[Main.myPlayer].speedSlice[0] = vector.Length();
        for (int m = 0; m < Main.player[Main.myPlayer].speedSlice.Length; m++)
        {
            if (m < num15)
            {
                num16 += Main.player[Main.myPlayer].speedSlice[m];
            }
            else
            {
                Main.player[Main.myPlayer].speedSlice[m] = num16 / (float)num15;
            }
        }
        num16 /= (float)num15;
        int num18 = 42240;
        int num19 = 216000;
        float num20 = num16 * (float)num19 / (float)num18;
        if (!Main.player[Main.myPlayer].merman && !Main.player[Main.myPlayer].ignoreWater)
        {
            if (Main.player[Main.myPlayer].honeyWet)
            {
                num20 /= 4f;
            }
            else if (Main.player[Main.myPlayer].wet)
            {
                num20 /= 2f;
            }
        }
        return Language.GetTextValue("GameUI.Speed", Math.Round(num20));
    }
}
