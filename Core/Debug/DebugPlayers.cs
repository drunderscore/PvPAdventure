using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Common.Spectator.SpectatorMode;
using PvPAdventure.Core.Utilities;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Debug;

#if DEBUG
[Autoload(Side = ModSide.Client)]
internal sealed class DebugPlayers : ModSystem
{
    private const float WalkSpeed = 1.65f;
    private const float RunSpeed = 4.2f;
    private const float PatrolDistance = 900f;
    private const float JumpHeight = 76f;
    private const float FlyHeight = 130f;
    private const float FlyBobHeight = 24f;
    private const int MinBehaviorIntervalTicks = 60;
    private const int MaxBehaviorIntervalTicks = 600;
    private const int JumpDurationTicks = 36;
    private const int MinFlyDurationTicks = 120;
    private const int MaxFlyDurationTicks = 240;
    private const int AttackSweepTicks = 180;

    private static readonly Dictionary<int, DebugPlayerState> debugPlayers = [];
    private static int nextDebugPlayerNumber = 1;

    private static readonly int[] Weapons =
    [
        ItemID.WoodenSword,
        ItemID.EnchantedSword,
        ItemID.Minishark,
        ItemID.MoltenFury,
        ItemID.DemonScythe,
        ItemID.NightsEdge,
        ItemID.Flamelash,
        ItemID.DaoofPow,
        ItemID.Megashark,
        ItemID.TrueNightsEdge,
        ItemID.TerraBlade,
        ItemID.RazorbladeTyphoon,
        ItemID.LastPrism,
        ItemID.SDMG
    ];

    private static readonly int[] Armor =
    [
        ItemID.GoldHelmet,
        ItemID.GoldChainmail,
        ItemID.GoldGreaves,
        ItemID.MoltenHelmet,
        ItemID.MoltenBreastplate,
        ItemID.MoltenGreaves,
        ItemID.MythrilHelmet,
        ItemID.MythrilChainmail,
        ItemID.MythrilGreaves,
        ItemID.HallowedHelmet,
        ItemID.HallowedPlateMail,
        ItemID.HallowedGreaves,
        ItemID.ChlorophyteHelmet,
        ItemID.ChlorophytePlateMail,
        ItemID.ChlorophyteGreaves
    ];

    private static readonly int[] Accessories =
    [
        ItemID.HermesBoots,
        ItemID.CloudinaBottle,
        ItemID.CobaltShield,
        ItemID.ObsidianShield,
        ItemID.FeralClaws,
        ItemID.BandofRegeneration,
        ItemID.CharmofMyths,
        ItemID.FrostsparkBoots,
        ItemID.AnkhShield,
        ItemID.AvengerEmblem,
        ItemID.WormScarf,
        ItemID.MasterNinjaGear
    ];

    private static readonly int[] Wings =
    [
        ItemID.AngelWings,
        ItemID.DemonWings,
        ItemID.LeafWings,
        ItemID.FrozenWings,
        ItemID.FishronWings
    ];

    public static bool IsDebugPlayer(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= Main.maxPlayers)
            return false;

        Player player = Main.player[playerIndex];
        return player?.active == true && debugPlayers.ContainsKey(playerIndex);
    }

    public static bool IsDebugPlayer(Player player)
    {
        return player?.active == true && debugPlayers.ContainsKey(player.whoAmI);
    }

    public static int Count => debugPlayers.Count;

    public override void UpdateUI(GameTime gameTime)
    {
        if (Main.dedServ || Main.netMode == NetmodeID.Server)
            return;

        if (Pressed(Keys.Add) || Pressed(Keys.NumPad1))
            AddDebugPlayer();
        else if (Pressed(Keys.Subtract) || Pressed(Keys.NumPad2))
            RemoveLastDebugPlayer();
    }

    public override void PostUpdatePlayers()
    {
        if (Main.dedServ || Main.netMode == NetmodeID.Server)
            return;

        UpdateDebugPlayerAI();
    }

    public override void OnWorldUnload()
    {
        ClearAll();
    }

    public override void Unload()
    {
        ClearAll();
    }

    private static bool Pressed(Keys key)
    {
        return Main.keyState.IsKeyDown(key) && !Main.oldKeyState.IsKeyDown(key);
    }

    private static void AddDebugPlayer()
    {
        if (!TryFindFreePlayerSlot(out int slot))
        {
            DebugLog.Chat("No free player slots for debug player.");
            return;
        }

        Player local = Main.LocalPlayer;
        Player player = local?.active == true ? local.SerializedClone() : new Player();

        player.whoAmI = slot;
        player.name = GetNextDebugPlayerName();
        player.active = true;
        player.dead = false;
        player.ghost = false;
        player.hostile = true;
        player.team = Main.rand.Next(1, 5);
        int maxLife = Main.rand.Next(100, 501);
        int maxMana = Main.rand.Next(20, 201);
        int defense = Main.rand.Next(0, 81);
        player.statLifeMax = maxLife;
        player.statLifeMax2 = maxLife;
        player.statLife = maxLife;
        player.statManaMax = maxMana;
        player.statManaMax2 = maxMana;
        player.statMana = maxMana;
        player.statDefense += defense - (int)player.statDefense;
        player.respawnTimer = 0;
        player.selectedItem = 0;
        player.SpawnX = local?.SpawnX ?? -1;
        player.SpawnY = local?.SpawnY ?? -1;

        RandomizeAppearance(player);
        RandomizeLoadout(player);
        PlaceNearLocalPlayer(player, slot);

        Main.player[slot] = player;
        SpectatorModeSystem.Modes[slot] = PlayerMode.Player;
        debugPlayers[slot] = CreateState(player, maxLife, maxMana, defense);

        DebugLog.Chat($"Added {player.name}, player count: {GetActivePlayerCount()}");
    }

    private static void RemoveLastDebugPlayer()
    {
        if (debugPlayers.Count == 0)
            return;

        int slot = -1;

        foreach (int candidate in debugPlayers.Keys)
            slot = Math.Max(slot, candidate);

        if (slot < 0)
            return;

        string name = Main.player[slot]?.name ?? $"DebugPlayer{slot}";
        debugPlayers.Remove(slot);
        Main.player[slot] = new Player { whoAmI = slot };
        SpectatorModeSystem.Modes.Remove(slot);

        DebugLog.Chat($"Removed {name}, player count: {GetActivePlayerCount()}");
    }

    private static void ClearAll()
    {
        foreach (int slot in debugPlayers.Keys)
        {
            if (slot >= 0 && slot < Main.maxPlayers)
            {
                Main.player[slot] = new Player { whoAmI = slot };
                SpectatorModeSystem.Modes.Remove(slot);
            }
        }

        debugPlayers.Clear();
    }

    private static bool TryFindFreePlayerSlot(out int slot)
    {
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            if (i != Main.myPlayer && Main.player[i]?.active != true)
            {
                slot = i;
                return true;
            }
        }

        slot = -1;
        return false;
    }

    private static string GetNextDebugPlayerName()
    {
        while (nextDebugPlayerNumber <= Main.maxPlayers * 2)
        {
            string name = $"DebugPlayer{nextDebugPlayerNumber++}";

            if (!PlayerNameExists(name))
                return name;
        }

        return $"DebugPlayer{Main.rand.Next(1000, 9999)}";
    }

    private static bool PlayerNameExists(string name)
    {
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            if (Main.player[i]?.active == true && Main.player[i].name == name)
                return true;
        }

        return false;
    }

    private static void RandomizeAppearance(Player player)
    {
        player.skinVariant = Main.rand.Next(0, 8);
        player.hair = Main.rand.Next(0, 165);
        player.hairDye = 0;
        player.hairColor = RandomBrightColor();
        player.skinColor = new Color(Main.rand.Next(160, 255), Main.rand.Next(100, 220), Main.rand.Next(80, 200));
        player.eyeColor = RandomBrightColor();
        player.shirtColor = RandomBrightColor();
        player.underShirtColor = RandomBrightColor();
        player.pantsColor = RandomBrightColor();
        player.shoeColor = RandomBrightColor();
    }

    private static Color RandomBrightColor()
    {
        return new Color(Main.rand.Next(40, 256), Main.rand.Next(40, 256), Main.rand.Next(40, 256));
    }

    private static void RandomizeLoadout(Player player)
    {
        ClearItems(player.inventory);
        ClearItems(player.armor);
        ClearItems(player.dye);
        ClearItems(player.miscEquips);
        ClearItems(player.miscDyes);

        SetRandomItem(player.inventory[0], Weapons);
        player.inventory[0].useTime = Main.rand.Next(8, 42);
        player.inventory[0].useAnimation = Math.Max(player.inventory[0].useTime, Main.rand.Next(12, 48));
        player.inventory[0].stack = Math.Max(1, player.inventory[0].stack);

        for (int i = 1; i < Math.Min(player.inventory.Length, 10); i++)
        {
            if (Main.rand.NextBool(3))
                SetRandomItem(player.inventory[i], Weapons);
        }

        for (int i = 0; i < Math.Min(3, player.armor.Length); i++)
            SetRandomItem(player.armor[i], Armor);

        if (player.armor.Length > 3)
            SetRandomItem(player.armor[3], Wings);

        for (int i = 4; i < Math.Min(10, player.armor.Length); i++)
        {
            if (Main.rand.NextBool())
                SetRandomItem(player.armor[i], Accessories);
        }
    }

    private static void ClearItems(Item[] items)
    {
        if (items == null)
            return;

        for (int i = 0; i < items.Length; i++)
            items[i] = new Item();
    }

    private static void SetRandomItem(Item item, int[] itemIds)
    {
        item.SetDefaults(itemIds[Main.rand.Next(itemIds.Length)]);
        item.Prefix(-1);
    }

    private static void PlaceNearLocalPlayer(Player player, int slot)
    {
        Player local = Main.LocalPlayer;

        Vector2 center = local?.active == true
            ? local.Center + new Vector2((slot % 8 - 4) * 64f, -16f)
            : new Vector2(Main.spawnTileX, Main.spawnTileY).ToWorldCoordinates();

        player.Center = ClampWorldPosition(center);
        player.fallStart = (int)(player.position.Y / 16f);
    }

    private static Vector2 ClampWorldPosition(Vector2 position)
    {
        float maxX = Math.Max(16f, Main.maxTilesX * 16f - 16f);
        float maxY = Math.Max(16f, Main.maxTilesY * 16f - 16f);
        position.X = MathHelper.Clamp(position.X, 16f, maxX);
        position.Y = MathHelper.Clamp(position.Y, 16f, maxY);
        return position;
    }

    private static void UpdateDebugPlayerAI()
    {
        List<int> slots = new(debugPlayers.Keys);
        List<int> remove = [];

        foreach (int slot in slots)
        {
            DebugPlayerState state = debugPlayers[slot];
            Player player = Main.player[slot];

            if (player?.active != true)
            {
                remove.Add(slot);
                continue;
            }

            DebugPlayerState nextState = UpdateMovementState(player, state);
            PrepareDebugPlayer(player, nextState);
            ApplyMovement(player, nextState);
            ApplyAttack(player, slot);

            debugPlayers[slot] = nextState;
        }

        foreach (int slot in remove)
        {
            debugPlayers.Remove(slot);
            SpectatorModeSystem.Modes.Remove(slot);
        }
    }

    private static DebugPlayerState CreateState(Player player, int maxLife, int maxMana, int defense)
    {
        return new DebugPlayerState(
            player.Center,
            Main.rand.NextBool() ? 1 : -1,
            RandomBehaviorInterval(),
            Main.rand.NextBool(),
            RandomBehaviorInterval(),
            0,
            RandomBehaviorInterval(),
            0,
            maxLife,
            maxMana,
            defense,
            player.position.Y);
    }

    private static DebugPlayerState UpdateMovementState(Player player, DebugPlayerState state)
    {
        int direction = state.Direction < 0 ? -1 : 1;
        int directionTicksLeft = Math.Max(0, state.DirectionTicksLeft - 1);
        bool running = state.Running;

        float distanceFromOrigin = player.Center.X - state.Origin.X;

        if (distanceFromOrigin > PatrolDistance)
        {
            direction = -1;
            directionTicksLeft = RandomBehaviorInterval();
            running = Main.rand.NextBool();
        }
        else if (distanceFromOrigin < -PatrolDistance)
        {
            direction = 1;
            directionTicksLeft = RandomBehaviorInterval();
            running = Main.rand.NextBool();
        }
        else if (directionTicksLeft <= 0)
        {
            direction *= -1;
            directionTicksLeft = RandomBehaviorInterval();
            running = Main.rand.NextBool();
        }

        int flyTicksLeft = Math.Max(0, state.FlyTicksLeft - 1);
        int flyFramesLeft = Math.Max(0, state.FlyFramesLeft - 1);

        if (flyTicksLeft <= 0 && flyFramesLeft <= 0)
        {
            flyTicksLeft = RandomBehaviorInterval();
            flyFramesLeft = RandomFlyDuration();
        }

        int jumpTicksLeft = Math.Max(0, state.JumpTicksLeft - 1);
        int jumpFramesLeft = Math.Max(0, state.JumpFramesLeft - 1);

        if (jumpTicksLeft <= 0 && jumpFramesLeft <= 0 && flyFramesLeft <= 0)
        {
            jumpTicksLeft = RandomBehaviorInterval();
            jumpFramesLeft = JumpDurationTicks;
        }

        if (flyFramesLeft > 0)
            jumpFramesLeft = 0;

        return state with
        {
            Direction = direction,
            DirectionTicksLeft = directionTicksLeft,
            Running = running,
            JumpTicksLeft = jumpTicksLeft,
            JumpFramesLeft = jumpFramesLeft,
            FlyTicksLeft = flyTicksLeft,
            FlyFramesLeft = flyFramesLeft
        };
    }

    private static void PrepareDebugPlayer(Player player, DebugPlayerState state)
    {
        player.active = true;
        player.dead = false;
        player.ghost = false;
        player.hostile = true;
        player.respawnTimer = 0;
        int previousMaxLife = player.statLifeMax2;
        int previousMaxMana = player.statManaMax2;
        player.statLifeMax = state.MaxLife;
        player.statLifeMax2 = state.MaxLife;
        player.statLife = previousMaxLife != state.MaxLife && player.statLife == previousMaxLife
            ? state.MaxLife
            : Math.Clamp(player.statLife, 1, player.statLifeMax2);
        player.statManaMax = state.MaxMana;
        player.statManaMax2 = state.MaxMana;
        player.statMana = previousMaxMana != state.MaxMana && player.statMana == previousMaxMana
            ? state.MaxMana
            : Math.Clamp(player.statMana, 0, player.statManaMax2);
        player.statDefense += state.Defense - (int)player.statDefense;
        player.selectedItem = 0;
    }

    private static void ApplyMovement(Player player, DebugPlayerState state)
    {
        int movementDirection = state.Direction < 0 ? -1 : 1;
        Vector2 oldPosition = player.position;
        float speed = state.Running ? RunSpeed : WalkSpeed;

        player.controlLeft = movementDirection < 0;
        player.controlRight = movementDirection > 0;
        player.controlJump = state.JumpFramesLeft > 0 || state.FlyFramesLeft > 0;
        player.controlUp = state.FlyFramesLeft > 0;
        player.controlDown = false;

        player.position.X += speed * movementDirection;
        ApplyVerticalMovement(player, state);
        player.Center = ClampWorldPosition(player.Center);
        player.velocity = player.position - oldPosition;
        player.fallStart = (int)(player.position.Y / 16f);
    }

    private static void ApplyVerticalMovement(Player player, DebugPlayerState state)
    {
        if (state.FlyFramesLeft > 0)
        {
            float bob = (float)Math.Sin(Main.GameUpdateCount / 18f) * FlyBobHeight;
            player.position.Y = state.GroundY - FlyHeight + bob;
            player.wingTime = Math.Max(player.wingTime, 30);
            return;
        }

        if (state.JumpFramesLeft > 0)
        {
            float progress = 1f - state.JumpFramesLeft / (float)JumpDurationTicks;
            float jumpOffset = (float)Math.Sin(progress * MathHelper.Pi) * JumpHeight;
            player.position.Y = state.GroundY - jumpOffset;
            return;
        }

        player.position.Y = MathHelper.Lerp(player.position.Y, state.GroundY, 0.35f);

        if (Math.Abs(player.position.Y - state.GroundY) < 1f)
            player.position.Y = state.GroundY;
    }

    private static void ApplyAttack(Player player, int slot)
    {
        player.controlUseItem = true;
        player.releaseUseItem = true;
        player.channel = false;

        Item heldItem = player.inventory[player.selectedItem];

        if (heldItem == null || heldItem.IsAir)
            return;

        player.itemTime = Math.Max(0, player.itemTime - 1);
        player.itemAnimation = Math.Max(0, player.itemAnimation - 1);

        int useTime = Math.Max(1, heldItem.useTime);
        int useAnimation = Math.Max(useTime, heldItem.useAnimation);

        if (player.itemTime <= 0)
        {
            player.itemTime = useTime;
            player.itemTimeMax = useTime;
        }

        if (player.itemAnimation <= 0)
        {
            player.itemAnimation = useAnimation;
            player.itemAnimationMax = useAnimation;
        }

        float sweepProgress = (Main.GameUpdateCount + slot * 17) % AttackSweepTicks / (float)(AttackSweepTicks - 1);
        float angle = MathHelper.Lerp(MathHelper.Pi, 0f, sweepProgress);
        Vector2 aim = new((float)Math.Cos(angle), -(float)Math.Sin(angle));
        int attackDirection = aim.X < -0.05f ? -1 : 1;

        player.direction = attackDirection;
        player.ChangeDir(attackDirection);
        player.itemRotation = (float)Math.Atan2(aim.Y, aim.X);

        if (attackDirection < 0)
            player.itemRotation += MathHelper.Pi;

        player.itemLocation = player.MountedCenter + aim * 18f;
    }

    private static int RandomBehaviorInterval()
    {
        return Main.rand.Next(MinBehaviorIntervalTicks, MaxBehaviorIntervalTicks + 1);
    }

    private static int RandomFlyDuration()
    {
        return Main.rand.Next(MinFlyDurationTicks, MaxFlyDurationTicks + 1);
    }

    private static int GetActivePlayerCount()
    {
        int count = 0;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            if (Main.player[i]?.active == true)
                count++;
        }

        return count;
    }

    private readonly record struct DebugPlayerState(
        Vector2 Origin,
        int Direction,
        int DirectionTicksLeft,
        bool Running,
        int JumpTicksLeft,
        int JumpFramesLeft,
        int FlyTicksLeft,
        int FlyFramesLeft,
        int MaxLife,
        int MaxMana,
        int Defense,
        float GroundY);
}
#endif
