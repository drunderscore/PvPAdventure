using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Rest;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using PvPAdventure.System;
using PvPAdventure.System.Client;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.UI.Chat;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;
using Mono.Cecil.Cil;
using static PvPAdventure.AdventurePlayer;
using PvPAdventure.Content.Items;

namespace PvPAdventure;

public class AdventurePlayer : ModPlayer
{
    public RestSelfUser DiscordUser => _discordClient?.CurrentUser;
    public DamageInfo RecentDamageFromPlayer { get; private set; }
    public int Kills { get; private set; }
    public int Deaths { get; private set; }

    private bool deathProcessedThisLife = false;

    private readonly int[] _playerMeleeInvincibleTime = new int[Main.maxPlayers];

    public HashSet<int> ItemPickups { get; private set; } = new();

    private const int TimeBetweenPingPongs = 3 * 60;

    private static readonly HashSet<short> BossNpcsForImmunityCooldown =
    [
        NPCID.QueenSlimeMinionBlue,
        NPCID.QueenSlimeMinionPink,
        NPCID.QueenSlimeMinionPurple,
        NPCID.WallofFleshEye,
        NPCID.TheHungry,
        NPCID.TheHungryII,
        NPCID.LeechHead,
        NPCID.LeechBody,
        NPCID.LeechTail,
        NPCID.Probe,
        NPCID.PlanterasHook,
        NPCID.PlanterasTentacle,
        NPCID.Spore,
        NPCID.PrimeCannon,
        NPCID.PrimeSaw,
        NPCID.PrimeVice,
        NPCID.PrimeLaser,
        NPCID.GolemHead,
        NPCID.GolemFistLeft,
        NPCID.GolemFistRight,
        NPCID.GolemHeadFree,
        NPCID.Sharkron,
        NPCID.Sharkron2,
        NPCID.DetonatingBubble
    ];

    // Intentionally zero-initialize this so we get a ping/pong ASAP.
    private int _nextPingPongTime;
    private int _pingPongCanary;
    private Stopwatch _pingPongStopwatch;
    public TimeSpan? Latency { get; private set; }
    public int[] PvPImmuneTime { get; } = new int[Main.maxPlayers];

    public int[] GroupImmuneTime { get; } = new int[100];

    private DiscordRestClient _discordClient;

    public sealed class DamageInfo(byte who, int ticksRemaining)
    {
        public byte Who { get; } = who;
        public int TicksRemaining { get; set; } = ticksRemaining;
    }

    public sealed class Statistics(byte player, int kills, int deaths) : IPacket<Statistics>
    {
        public byte Player { get; } = player;
        public int Kills { get; } = kills;
        public int Deaths { get; } = deaths;

        public static Statistics Deserialize(BinaryReader reader)
        {
            var player = reader.ReadByte();
            var kills = reader.ReadInt32();
            var deaths = reader.ReadInt32();
            return new(player, kills, deaths);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Player);
            writer.Write(Kills);
            writer.Write(Deaths);
        }

        public void Apply(AdventurePlayer adventurePlayer)
        {
            adventurePlayer.Kills = Kills;
            adventurePlayer.Deaths = Deaths;
        }
    }

    public sealed class ItemPickup(int[] items) : IPacket<ItemPickup>
    {
        public int[] Items { get; } = items;

        public static ItemPickup Deserialize(BinaryReader reader)
        {
            var length = reader.ReadInt32();
            var items = new int[length];
            for (var i = 0; i < items.Length; i++)
                items[i] = reader.ReadInt32();

            return new(items);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Items.Length);

            foreach (var item in Items)
                writer.Write(item);
        }

        public void Apply(AdventurePlayer adventurePlayer)
        {
            adventurePlayer.ItemPickups.UnionWith(items);
        }
    }

    public sealed class PingPong(int canary) : IPacket<PingPong>
    {
        public int Canary { get; set; } = canary;

        public static PingPong Deserialize(BinaryReader reader)
        {
            return new(reader.ReadInt32());
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Canary);
        }
    }

    // This mod packet is required as opposed to MessageID.PlayerTeam, because the latter would be rejected during early
    // connection, which is important for us.
    public sealed class Team(byte player, Terraria.Enums.Team team) : IPacket<Team>
    {
        public byte Player { get; set; } = player;
        public Terraria.Enums.Team Value { get; set; } = team;

        public static Team Deserialize(BinaryReader reader)
        {
            return new(reader.ReadByte(), (Terraria.Enums.Team)reader.ReadInt32());
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Player);
            writer.Write((int)Value);
        }
    }

    public override void Load()
    {
        // NOTE: Cannot hook Player.PlaceThing, it seems to never invoke my callback.
        //        See: https://discord.com/channels/103110554649894912/534215632795729922/1320255884747608104
        On_Player.PlaceThing_Tiles += OnPlayerPlaceThing_Tiles;
        On_Player.PlaceThing_Walls += OnPlayerPlaceThing_Walls;
        On_Player.ItemCheck_UseMiningTools += OnPlayerItemCheck_UseMiningTools;
        On_Player.ItemCheck_UseTeleportRod += OnPlayerItemCheck_UseTeleportRod;
        On_Player.ItemCheck_UseWiringTools += OnPlayerItemCheck_UseWiringTools;
        On_Player.ItemCheck_CutTiles += OnPlayerItemCheck_CutTiles;

        On_Player.HasUnityPotion += OnPlayerHasUnityPotion;

        IL_Player.KillMe += EditPlayerKillMe;
        // Always consider the respawn time for non-pvp deaths.
        On_Player.GetRespawnTime += OnPlayerGetRespawnTime;
        On_Player.Spawn += OnPlayerSpawn;

        // Allow player hurt sound to be silenced or not, without regards to the networked value or mutating it.
        IL_Player.Hurt_HurtInfo_bool += EditPlayerHurt;

        // Modify the damage dealt by the entire wall from the Wall of Flesh to use ImmunityCooldownID.Bosses
        IL_Player.WOFTongue += EditPlayerWOFTongue;

        // Remove logic for handling Beetle Might buffs.
        IL_Player.UpdateBuffs += EditPlayerUpdateBuffs;
        // Simplify logic for handling Beetle Scale Mail set bonus to do the bare minimum required.
        IL_Player.UpdateArmorSets += EditPlayerUpdateArmorSets;
        // Prevent social armor slots from being drawn.
        IL_Player.PlayerFrame += EditPlayerFrame;
    }

    public override void SetStaticDefaults()
    {
        // Beetle Might buffs last forever until death.
        BuffID.Sets.TimeLeftDoesNotDecrease[BuffID.BeetleMight1] = true;
        BuffID.Sets.TimeLeftDoesNotDecrease[BuffID.BeetleMight2] = true;
        BuffID.Sets.TimeLeftDoesNotDecrease[BuffID.BeetleMight3] = true;

        Main.persistentBuff[BuffID.WeaponImbueVenom] = false;
        Main.persistentBuff[BuffID.WeaponImbueCursedFlames] = false;
        Main.persistentBuff[BuffID.WeaponImbueFire] = false;
        Main.persistentBuff[BuffID.WeaponImbueGold] = false;
        Main.persistentBuff[BuffID.WeaponImbueIchor] = false;
        Main.persistentBuff[BuffID.WeaponImbueNanites] = false;
        Main.persistentBuff[BuffID.WeaponImbueConfetti] = false;
        Main.persistentBuff[BuffID.WeaponImbuePoison] = false;
    }

    public override void Unload()
    {
        Main.persistentBuff[BuffID.WeaponImbueVenom] = true;
        Main.persistentBuff[BuffID.WeaponImbueCursedFlames] = true;
        Main.persistentBuff[BuffID.WeaponImbueFire] = true;
        Main.persistentBuff[BuffID.WeaponImbueGold] = true;
        Main.persistentBuff[BuffID.WeaponImbueIchor] = true;
        Main.persistentBuff[BuffID.WeaponImbueNanites] = true;
        Main.persistentBuff[BuffID.WeaponImbueConfetti] = true;
        Main.persistentBuff[BuffID.WeaponImbuePoison] = true;
    }

    private void OnPlayerPlaceThing_Tiles(On_Player.orig_PlaceThing_Tiles orig, Player self)
    {
        var region = ModContent.GetInstance<RegionManager>()
            .GetRegionContaining(new(Player.tileTargetX, Player.tileTargetY));

        if (region == null || region.CanModifyTiles)
            orig(self);
    }

    private void OnPlayerPlaceThing_Walls(On_Player.orig_PlaceThing_Walls orig, Player self)
    {
        var region = ModContent.GetInstance<RegionManager>()
            .GetRegionContaining(new(Player.tileTargetX, Player.tileTargetY));

        if (region == null || region.CanModifyTiles)
            orig(self);
    }

    private void OnPlayerItemCheck_UseMiningTools(On_Player.orig_ItemCheck_UseMiningTools orig, Player self, Item sitem)
    {
        var region = ModContent.GetInstance<RegionManager>()
            .GetRegionContaining(new(Player.tileTargetX, Player.tileTargetY));

        if (region == null || region.CanModifyTiles)
            orig(self, sitem);
    }

    private void OnPlayerItemCheck_UseTeleportRod(On_Player.orig_ItemCheck_UseTeleportRod orig, Player self, Item sitem)
    {
        var region = ModContent.GetInstance<RegionManager>()
            .GetRegionContaining(new(Player.tileTargetX, Player.tileTargetY));

        if (region == null || region.CanModifyTiles)
            orig(self, sitem);
    }

    private void OnPlayerItemCheck_UseWiringTools(On_Player.orig_ItemCheck_UseWiringTools orig, Player self, Item sitem)
    {
        var region = ModContent.GetInstance<RegionManager>()
            .GetRegionContaining(new(Player.tileTargetX, Player.tileTargetY));

        if (region == null || region.CanModifyTiles)
            orig(self, sitem);
    }

    private void OnPlayerItemCheck_CutTiles(On_Player.orig_ItemCheck_CutTiles orig, Player self, Item sitem,
        Rectangle itemrectangle, bool[] shouldignore)
    {
        var region = ModContent.GetInstance<RegionManager>().GetRegionIntersecting(itemrectangle.ToTileRectangle());

        if (region == null || region.CanModifyTiles)
            orig(self, sitem, itemrectangle, shouldignore);
    }

    private bool OnPlayerHasUnityPotion(On_Player.orig_HasUnityPotion orig, Player self)
    {
        var region = ModContent.GetInstance<RegionManager>().GetRegionIntersecting(self.Hitbox.ToTileRectangle());

        // By default, you cannot wormhole.
        if (region == null || !region.CanUseWormhole)
            return false;

        // This is now a possibility from our multiplayer pause.
        if (Main.gamePaused)
            return false;

        return orig(self);
    }

    private void OnPlayerSpawn(On_Player.orig_Spawn orig, Player self, PlayerSpawnContext context)
    {
        // Don't count this as a PvP death.
        self.pvpDeath = false;
        orig(self, context);
        // Remove immune and immune time applied during spawn.
        var adventureConfig = ModContent.GetInstance<AdventureServerConfig>();
        self.immuneTime = adventureConfig.SpawnImmuneFrames;
        self.immune = self.immuneTime > 0;
    }

    private void EditPlayerKillMe(ILContext il)
    {
        var cursor = new ILCursor(il);
        // Find the call to DropCoins...
        cursor.GotoNext(i => i.MatchCall<Player>("DropCoins"))
            // ...but go backwards to find the load to the 'pvp' parameter of the KillMe method
            .GotoPrev(i => i.MatchLdarg(4))
            // ...and remove the load and subsequent branch.
            .RemoveRange(2);
    }

    private void EditPlayerHurt(ILContext il)
    {
        var cursor = new ILCursor(il);

        // Find the load of Player.HurtInfo.SoundDisabled...
        cursor.GotoNext(i => i.MatchLdfld<Player.HurtInfo>("SoundDisabled"))
            // ...and remove it...
            .Remove()
            // ...emitting a load of argument 0 (this)...
            .EmitLdarg0()
            // ...and a delegate, whose return value will take the place of the above-removed load.
            .EmitDelegate((Player.HurtInfo hurtInfo, Player target) =>
                ShouldSilenceHurtSound(target, hurtInfo) ?? hurtInfo.SoundDisabled);
    }

    private void EditPlayerWOFTongue(ILContext il)
    {
        var cursor = new ILCursor(il);

        // Find the call to Player.Hurt...
        cursor.GotoNext(i => i.MatchCall<Player>("Hurt"));

        // ...and go back to the cooldownCounter parameter...
        cursor.Index -= 5;

        // ...to remove it...
        cursor.Remove()
            // ...and replace it with a constant.
            .EmitLdcI4(ImmunityCooldownID.Bosses);
    }

    private void EditPlayerUpdateBuffs(ILContext il)
    {
        var cursor = new ILCursor(il);

        ILLabel label = null;
        // First, find a load of Player.buffType that is somewhere followed by a load of BuffID.BeetleMight1 and a blt
        // instruction...
        cursor.GotoNext(i =>
            i.MatchLdfld<Player>("buffType") && i.Next.Next.Next.MatchLdcI4(98) &&
            i.Next.Next.Next.Next.MatchBlt(out label));

        // ...and go back to the "this" load...
        cursor.Index -= 1;
        // ...while ensuring that instructions removed and emitted are labeled correctly...
        cursor.MoveAfterLabels();
        // ...to emit a branch to the fail case.
        cursor.EmitBr(label);
    }

    private void EditPlayerUpdateArmorSets(ILContext il)
    {
        var cursor = new ILCursor(il);

        // Find a load to a string...
        cursor.GotoNext(i => i.MatchLdstr("ArmorSetBonus.BeetleDamage"));
        // ...and go back to the branch instruction...
        cursor.Index -= 4;

        // ...to grab it's label...
        var label = (ILLabel)cursor.Next!.Operand;
        cursor.Index += 1;

        // ...and prepare a delegate call, doing the bare minimum for set bonus functionality...
        cursor.EmitLdarg0();
        cursor.EmitDelegate((Player self) =>
        {
            self.setBonus = Language.GetTextValue("ArmorSetBonus.BeetleDamage");
            self.beetleOffense = true;
        });
        // ...then branch away so we skip the original code.
        cursor.EmitBr(label);
    }

    private int OnPlayerGetRespawnTime(On_Player.orig_GetRespawnTime orig, Player self, bool pvp) => orig(self, false);

    public override bool CanHitPvp(Item item, Player target)
    {
        var myRegion = ModContent.GetInstance<RegionManager>().GetRegionIntersecting(Player.Hitbox.ToTileRectangle());

        if (myRegion != null && !myRegion.AllowCombat)
            return false;

        var targetRegion = ModContent.GetInstance<RegionManager>()
            .GetRegionIntersecting(target.Hitbox.ToTileRectangle());

        if (targetRegion != null && !targetRegion.AllowCombat)
            return false;

        if (_playerMeleeInvincibleTime[target.whoAmI] > 0)
            return false;

        _playerMeleeInvincibleTime[target.whoAmI] =
            ModContent.GetInstance<AdventureServerConfig>().Combat.MeleeInvincibilityFrames;

        return true;
    }

    public override bool CanHitPvpWithProj(Projectile proj, Player target)
    {
        var myRegion = ModContent.GetInstance<RegionManager>().GetRegionIntersecting(Player.Hitbox.ToTileRectangle());

        if (myRegion != null && !myRegion.AllowCombat)
            return false;

        var targetRegion = ModContent.GetInstance<RegionManager>()
            .GetRegionIntersecting(target.Hitbox.ToTileRectangle());

        if (targetRegion != null && !targetRegion.AllowCombat)
            return false;

        return true;
    }

    public override bool CanBeHitByNPC(NPC npc, ref int cooldownSlot)
    {
        if (npc.boss || AdventureNpc.IsPartOfEaterOfWorlds((short)npc.type) ||
            AdventureNpc.IsPartOfTheDestroyer((short)npc.type) || BossNpcsForImmunityCooldown.Contains((short)npc.type))
            cooldownSlot = ImmunityCooldownID.Bosses;

        return true;
    }

    public override void ResetEffects()
    {
        // FIXME: This does not truly belong here.
        Player.hostile = true;
    }

    public override void PreUpdate()
    {
        for (var i = 0; i < _playerMeleeInvincibleTime.Length; i++)
        {
            if (_playerMeleeInvincibleTime[i] > 0)
                _playerMeleeInvincibleTime[i]--;
        }

        if (Main.dedServ && --_nextPingPongTime <= 0)
        {
            _nextPingPongTime = TimeBetweenPingPongs;
            SendPingPong();
        }

        if (RecentDamageFromPlayer != null && --RecentDamageFromPlayer.TicksRemaining <= 0)
        {
            Mod.Logger.Info($"Recent damage for {this} expired (was from {RecentDamageFromPlayer.Who})");
            RecentDamageFromPlayer = null;
        }

        if (AdventureItem.RecallItems[Player.inventory[Player.selectedItem].type] && !CanRecall())
        {
            Player.SetItemAnimation(0);
            Player.SetItemTime(0);
        }

        for (var i = 0; i < PvPImmuneTime.Length; i++)
        {
            if (PvPImmuneTime[i] > 0)
                PvPImmuneTime[i]--;
        }
       
        for (var i = 0; i < GroupImmuneTime.Length; i++)
        {
            if (GroupImmuneTime[i] > 0)
                GroupImmuneTime[i]--;
        }
    
        bool hasSpectreSet = IsSpectreSetEquipped();
        int currentHead = Player.armor[0].type;
        bool headChanged = IsSpectreHead(currentHead) && currentHead != lastSpectreHead;

        if ((hasSpectreSet && !hadSpectreSetLastFrame) || headChanged)
        {
            Player.AddBuff(ModContent.BuffType<Attuning>(), 3600); // 60 seconds
        }

        hadSpectreSetLastFrame = hasSpectreSet;
        if (IsSpectreHead(currentHead))
        {
            lastSpectreHead = currentHead;
        }
    }
    public override void PostUpdate()
    {
        if (Player.HasBuff(ModContent.BuffType<Attuning>()))
        {
            Player.ghostHurt = false;
            Player.ghostHeal = false;
        }
    }
    private bool CanRecall()
    {
        var region = ModContent.GetInstance<RegionManager>().GetRegionIntersecting(Player.Hitbox.ToTileRectangle());

        return Player.lifeRegen >= 0.0 && !Player.controlLeft && !Player.controlRight && !Player.controlUp &&
               !Player.controlDown && Player.velocity == Vector2.Zero && (region == null || region.CanRecall);
    }

    public override bool CanUseItem(Item item)
    {
        // Prevent a recall from being started at all for these conditions.
        if (AdventureItem.RecallItems[item.type])
        {
            if (CanRecall())
                return true;

            if (!Main.dedServ && Player.whoAmI == Main.myPlayer)
                PopupText.NewText(new AdvancedPopupRequest
                {
                    Color = Color.Crimson,
                    Text = Language.GetTextValue("Mods.PvPAdventure.Player.CannotRecall"),
                    Velocity = new(0.0f, -4.0f),
                    DurationInFrames = 60 * 2
                }, Player.Top);

            return false;
        }

        return true;
    }

    public async void SetDiscordToken(string token, Action<bool> onFinish)
    {
        if (_discordClient != null)
            throw new Exception("Cannot set Discord token for player after it has already been set.");

        // FIXME: How should we dispose of this?
        _discordClient = new DiscordRestClient();

        // FIXME: Could this ever be invoked multiple times? I don't think so, because it's the rest client, so we would have to manually
        //        logout and log back in...
        _discordClient.LoggedIn += () =>
        {
            // Good chance we are not on the main thread anymore, so let's get back there
            Main.QueueMainThreadAction(() => { onFinish(true); });

            return Task.CompletedTask;
        };

        try
        {
            await _discordClient.LoginAsync(Discord.TokenType.Bearer, token);
        }
        catch (Exception e)
        {
            Mod.Logger.Info($"Player {this} failed to login with token \"{token}\"", e);
            Main.QueueMainThreadAction(() => { onFinish(false); });
        }
    }

    public override void PostHurt(Player.HurtInfo info)
    {
        if (AdventureItem.RecallItems[Player.inventory[Player.selectedItem].type])
        {
            Player.SetItemAnimation(0);
            Player.SetItemTime(0);
        }

        // Don't need the client to have this information right now, and I can't be sure it's accurate.
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        if (!info.PvP)
            return;

        if (info.DamageSource.SourcePlayerIndex == -1)
        {
            Mod.Logger.Warn($"PostHurt for {this} indicated PvP, but source player was -1");
            return;
        }

        var damagerPlayer = Main.player[info.DamageSource.SourcePlayerIndex];
        if (!damagerPlayer.active)
        {
            Mod.Logger.Warn($"PostHurt for {this} sourced from inactive player");
            return;
        }

        // Hurting ourselves doesn't change our recent damage
        if (info.DamageSource.SourcePlayerIndex == Player.whoAmI)
            return;

        RecentDamageFromPlayer = new((byte)damagerPlayer.whoAmI,
            ModContent.GetInstance<AdventureServerConfig>().Combat.RecentDamagePreservationFrames);
    }

    public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust,
        ref PlayerDeathReason damageSource)
    {
        // Only silence death sound on clients that we hurt that aren't ourselves
        if (!Main.dedServ && pvp && Player.whoAmI != Main.myPlayer && damageSource.SourcePlayerIndex == Main.myPlayer)
        {
            var marker = ModContent.GetInstance<AdventureClientConfig>().SoundEffect.PlayerKillMarker;
            if (marker != null && marker.SilenceVanilla)
                playSound = false;
        }

        return true;
    }

    private bool hadShinyStoneLastFrame;
    private int lastSpectreHead = 0;
    private bool hadSpectreSetLastFrame;

    private bool IsSpectreSetEquipped()
    {
        int head = Player.armor[0].type;
        int body = Player.armor[1].type;
        int legs = Player.armor[2].type;

        bool hasSpectreHead = IsSpectreHead(head);
        bool hasSpectreBody = body == ItemID.SpectreRobe;
        bool hasSpectreLegs = legs == ItemID.SpectrePants;

        return hasSpectreHead && hasSpectreBody && hasSpectreLegs;
    }

    private bool IsSpectreHead(int headType)
    {
        return headType == ItemID.SpectreHood || headType == ItemID.SpectreMask;
    }
    private bool hadPhilostoneLastFrame;
    public override void PostUpdateEquips()
    {
        // Check if Shiny Stone is equipped
        bool hasShinyStone = IsShinyStoneEquipped();

        // Apply debuff when first equipped or after respawn
        if (hasShinyStone && !hadShinyStoneLastFrame)
        {
            Player.AddBuff(ModContent.BuffType<ShinyStoneHotswap>(), 3600); // 60 seconds
        }

        // Disable Shiny Stone effects while debuffed
        if (Player.HasBuff(ModContent.BuffType<ShinyStoneHotswap>()))
        {
            Player.shinyStone = false;
        }
        bool hasPhilostone = IsPhilostoneEquipped();
        hadShinyStoneLastFrame = hasShinyStone;

        if (hasPhilostone && !hadPhilostoneLastFrame)
        {
            Player.AddBuff(ModContent.BuffType<UncouthandBoring>(), 3600); // 60 seconds
        }

        hadPhilostoneLastFrame = hasPhilostone;


        if (Player.beetleOffense)
        {
            Player.GetDamage<MeleeDamageClass>() += 0;
            Player.GetAttackSpeed<MeleeDamageClass>() += 0;
        }
        else
        {
            // If we don't have the beetle offense set bonus, remove all possible buffs.
            Player.ClearBuff(BuffID.BeetleMight1);
            Player.ClearBuff(BuffID.BeetleMight2);
            Player.ClearBuff(BuffID.BeetleMight3);
        }

        if (Player.HasBuff(BuffID.BeetleMight3))
        {
            // we apply the glowing eye effect from Yoraiz0rsSpell item
            Player.yoraiz0rEye = 33;
        }

        if (Player.hasPaladinShield)
        {
            Player.buffImmune[BuffID.PaladinsShield] = true;
        }
    }

    private bool IsShinyStoneEquipped()
    {
        for (int i = 3; i < 10; i++) // Check all accessory slots
        {
            if (Player.armor[i].type == ItemID.ShinyStone &&
               (i < 7 || !Player.hideVisibleAccessory[i - 3]))
            {
                return true;
            }
        }
        return false;
    }
    private bool IsPhilostoneEquipped()
    {
        for (int i = 3; i < 10; i++) // Check all accessory slots
        {
            if (Player.armor[i].type == ItemID.PhilosophersStone || (Player.armor[i].type == ItemID.CharmofMyths) &&
               (i < 7 || !Player.hideVisibleAccessory[i - 3]))
            {
                return true;
            }
        }
        return false;
    }


    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        // Only play kill markers on clients that we hurt that aren't ourselves
        if (!Main.dedServ && pvp && damageSource.SourcePlayerIndex == Main.myPlayer && Player.whoAmI != Main.myPlayer)
            PlayKillMarker((int)damage);
        // Only for non-suicide PvP deaths, apply Beetle Might as needed to the attacker.
        if (pvp && damageSource.SourcePlayerIndex != Player.whoAmI)
        {
            var attacker = Main.player[damageSource.SourcePlayerIndex];
            if (attacker.beetleOffense && attacker.beetleOrbs < 3)
            {
                // First, make sure to clear any previous buff if applicable.
                if (attacker.beetleOrbs > 0)
                    attacker.ClearBuff(BuffID.BeetleMight1 + attacker.beetleOrbs - 1);
                attacker.AddBuff(BuffID.BeetleMight1 + attacker.beetleOrbs, 5);
            }
        }
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;
        // Remove the Dungeon Guardian when it kills a player.
        if (damageSource.SourceNPCIndex != -1)
        {
            var npc = Main.npc[damageSource.SourceNPCIndex];
            if (npc?.type == NPCID.DungeonGuardian)
            {
                npc.life = 0;
                npc.netSkip = -1;
                if (Main.dedServ)
                    NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
            }
        }

        try
        {
            if (deathProcessedThisLife)
            {
                Mod.Logger.Warn($"Death already processed for {Player.name} this life, skipping duplicate K/D increment");
                return;
            }
            deathProcessedThisLife = true;

            Player killer = null;
            if (pvp && damageSource.SourcePlayerIndex != -1 && damageSource.SourcePlayerIndex != Player.whoAmI)
            {
                killer = Main.player[damageSource.SourcePlayerIndex];
            }
            else
            {
                if (pvp && damageSource.SourcePlayerIndex == -1)
                    Mod.Logger.Warn($"PvP kill without a valid SourcePlayerIndex ({this} killed)");
                if (RecentDamageFromPlayer != null)
                    killer = Main.player[RecentDamageFromPlayer.Who];
            }
            // Nothing should happen for suicide
            if (killer == null || !killer.active || killer.whoAmI == Player.whoAmI)
                return;

            ModContent.GetInstance<PointsManager>().AwardPlayerKillToTeam(killer, Player);

            // Increment and sync
            var killerAdventurePlayer = killer.GetModPlayer<AdventurePlayer>();
            killerAdventurePlayer.Kills += 1;
            killerAdventurePlayer.SyncStatistics();
            Deaths += 1;
            SyncStatistics();

            damageSource.SourceCustomReason =
                $"[c/{Main.teamColor[killer.team].Hex3()}:{killer.name}] {ItemTagHandler.GenerateTag(damageSource.SourceItem ?? new Item(ItemID.Skull))} [c/{Main.teamColor[Player.team].Hex3()}:{Player.name}]";
        }
        finally
        {
            // PvP or not, reset whom we last took damage from.
            RecentDamageFromPlayer = null;
            // Remove recent damage for ALL players we've attacked after we die.
            // These are indirect post-mortem kills, which we don't want.
            // FIXME: We would still like to attribute this to the next recent damager, which would require a stack of
            //        recent damage.
            foreach (var player in Main.ActivePlayers)
            {
                var adventurePlayer = player.GetModPlayer<AdventurePlayer>();
                if (adventurePlayer.RecentDamageFromPlayer?.Who == Player.whoAmI)
                    adventurePlayer.RecentDamageFromPlayer = null;
            }
        }
    }

    public override void OnRespawn()
    {
        deathProcessedThisLife = false;
    }
    
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        var pointsManager = ModContent.GetInstance<PointsManager>();
        var keybinds = ModContent.GetInstance<Keybinds>();

        if (keybinds.Scoreboard.JustPressed)
        {
            pointsManager.BossCompletion.Active = true;
            Main.InGameUI.SetState(pointsManager.UiScoreboard);
        }
        else if (keybinds.Scoreboard.JustReleased)
        {
            pointsManager.BossCompletion.Active = false;
            Main.InGameUI.SetState(null);
        }

        if (keybinds.BountyShop.JustPressed)
        {
            var bountyShop = ModContent.GetInstance<BountyManager>().UiBountyShop;

            if (Main.InGameUI.CurrentState == bountyShop)
                Main.InGameUI.SetState(null);
            else
                Main.InGameUI.SetState(bountyShop);
        }

        if (keybinds.AllChat.JustPressed)
            ModContent.GetInstance<TeamChatManager>().OpenAllChat();
    }

    private void SyncStatistics(int to = -1, int ignore = -1)
    {
        var packet = Mod.GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.PlayerStatistics);
        new Statistics((byte)Player.whoAmI, Kills, Deaths).Serialize(packet);
        packet.Send(to, ignore);
    }

    private void SyncSingleItemPickup(int item, int to = -1, int ignore = -1)
    {
        var packet = Mod.GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.PlayerItemPickup);
        new ItemPickup([item]).Serialize(packet);
        packet.Send(to, ignore);
    }

    private void SyncItemPickups(int to = -1, int ignore = -1)
    {
        var packet = Mod.GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.PlayerItemPickup);
        new ItemPickup(ItemPickups.ToArray()).Serialize(packet);
        packet.Send(to, ignore);
    }

    public override void SaveData(TagCompound tag)
    {
        tag["kills"] = Kills;
        tag["deaths"] = Deaths;
        tag["itemPickups"] = ItemPickups.ToArray();
        tag["team"] = Player.team;
    }

    public override void LoadData(TagCompound tag)
    {
        Kills = tag.Get<int>("kills");
        Deaths = tag.Get<int>("deaths");
        ItemPickups = tag.Get<int[]>("itemPickups").ToHashSet();
        Player.team = tag.Get<int>("team");
    }

    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
    {
        SyncStatistics(toWho, fromWho);

        if (newPlayer)
        {
            // Sync all of our pickups at once when we join
            if (!Main.dedServ)
                SyncItemPickups(toWho, fromWho);

            var packet = Mod.GetPacket();
            packet.Write((byte)AdventurePacketIdentifier.PlayerTeam);
            new Team((byte)Player.whoAmI, (Terraria.Enums.Team)Player.team).Serialize(packet);
            packet.Send(toWho, fromWho);
        }
    }

    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) =>
        {
            var adventureConfig = ModContent.GetInstance<AdventureServerConfig>();
            info.Damage = Math.Max(info.Damage, adventureConfig.MinimumDamageReceivedByPlayers);
            if (info.PvP)
                info.Damage = Math.Max(info.Damage, adventureConfig.MinimumDamageReceivedByPlayersFromPlayer);
        };

        if (!modifiers.PvP)
            return;

        var adventureConfig = ModContent.GetInstance<AdventureServerConfig>();
        var playerDamageBalance = adventureConfig.Combat.PlayerDamageBalance;
        var sourcePlayer = Main.player[modifiers.DamageSource.SourcePlayerIndex];
        var tileDistance = Player.Distance(sourcePlayer.position) / 16.0f;
        var hasIncurredFalloff = false;

        // Track base armor penetration
        float baseArmorPen = 0f;
        bool isMagicDamage = false;

        var sourceItem = modifiers.DamageSource.SourceItem;
        if (sourceItem != null && !sourceItem.IsAir)
        {
            var itemDefinition = new ItemDefinition(sourceItem.type);
            if (playerDamageBalance.ItemDamageMultipliers.TryGetValue(itemDefinition, out var multiplier))
                modifiers.IncomingDamageMultiplier *= multiplier;
            if (playerDamageBalance.ItemFalloff.TryGetValue(itemDefinition, out var falloff) &&
                falloff != null)
            {
                modifiers.IncomingDamageMultiplier *= falloff.CalculateMultiplier(tileDistance);
                hasIncurredFalloff = true;
            }
            if (playerDamageBalance.ItemArmorPenetration.TryGetValue(itemDefinition, out var armorPen))
            {
                baseArmorPen = Math.Clamp(armorPen, 0f, 1f);
            }

            // Check if this is a magic weapon
            if (sourceItem.CountsAsClass(DamageClass.Magic))
                isMagicDamage = true;
        }

        if (modifiers.DamageSource.SourceProjectileType != ProjectileID.None)
        {
            var projectileDefinition = new ProjectileDefinition(modifiers.DamageSource.SourceProjectileType);
            if (playerDamageBalance.ProjectileDamageMultipliers.TryGetValue(projectileDefinition, out var multiplier))
                modifiers.IncomingDamageMultiplier *= multiplier;
            if (playerDamageBalance.ProjectileFalloff.TryGetValue(projectileDefinition, out var falloff) &&
                falloff != null)
            {
                modifiers.IncomingDamageMultiplier *= falloff.CalculateMultiplier(tileDistance);
                hasIncurredFalloff = true;
            }
            if (playerDamageBalance.ProjectileArmorPenetration.TryGetValue(projectileDefinition, out var armorPen))
            {
                baseArmorPen = Math.Clamp(armorPen, 0f, 1f);
            }

            // Check if the projectile is magic damage
            if (modifiers.DamageSource.SourceProjectileLocalIndex >= 0)
            {
                Projectile proj = Main.projectile[modifiers.DamageSource.SourceProjectileLocalIndex];
                if (proj != null && proj.active && proj.CountsAsClass(DamageClass.Magic))
                    isMagicDamage = true;
            }
        }

        if (!hasIncurredFalloff && playerDamageBalance.DefaultFalloff != null)
            modifiers.IncomingDamageMultiplier *= playerDamageBalance.DefaultFalloff.CalculateMultiplier(tileDistance);

        // Apply Spectre Hood armor penetration bonus for magic damage
        float finalArmorPen = baseArmorPen;
        if (isMagicDamage && sourcePlayer.ghostHeal)
        {
            // Formula: increase by configured percentage of remaining penetration
            float ghostHealPenBonus = adventureConfig.Combat.GhostHealArmorPenetration;
            finalArmorPen = baseArmorPen + (1f - baseArmorPen) * ghostHealPenBonus;
        }

        // Apply final armor penetration
        if (finalArmorPen > 0f)
        {
            modifiers.ScalingArmorPenetration += finalArmorPen;
        }
    }

    public override void OnHurt(Player.HurtInfo info)
    {
        if (!info.PvP)
            return;

        var adventureConfig = ModContent.GetInstance<AdventureServerConfig>();

        // Only play hit markers on clients that we hurt that aren't ourselves
        if (!Main.dedServ && Player.whoAmI != Main.myPlayer && info.DamageSource.SourcePlayerIndex == Main.myPlayer)
            PlayHitMarker(info.Damage);

        if (info.DamageSource.SourceProjectileType != 0 &&
           adventureConfig.Combat.ProjectileDamageImmunityGroup.TryGetValue(
               new(info.DamageSource.SourceProjectileType), out var immunityGroup))
        {
            GroupImmuneTime[immunityGroup.Id] = immunityGroup.Frames;
        }
        else if (adventureConfig.Combat.MeleeInvincibilityFrames == 0 &&
                 info.CooldownCounter == CombatManager.PvPImmunityCooldownId)
        {
            PvPImmuneTime[info.DamageSource.SourcePlayerIndex] = adventureConfig.Combat.StandardInvincibilityFrames;
        }
    }

    public override bool OnPickup(Item item)
    {
        // FIXME: This could work for non-modded items, but I'm not so sure the item type ordinals are determinant.
        //         We _can_ work under the assumption this one player will be played within one world with the same mods
        //         always, but I'm not sure even that is good enough -- so let's just ignore them for now.
        if (item.ModItem == null)
        {
            if (ItemPickups.Add(item.type) && Main.netMode == NetmodeID.MultiplayerClient)
                SyncSingleItemPickup(item.type);
        }

        return true;
    }

    public override bool? CanAutoReuseItem(Item item)
    {
        if (ModContent.GetInstance<AdventureServerConfig>().PreventAutoReuse.Contains(new(item.type)))
            return false;

        return null;
    }

    public override void UpdateBadLifeRegen()
    {
        if (Player.HasBuff(BuffID.CursedInferno))
        {
            Player.lifeRegenTime = 0.0f;
            // Reduce damage by 12 flat, from 24.
            Player.lifeRegen += 12;
        }

        if (Player.HasBuff(BuffID.Venom))
        {
            Player.lifeRegenTime = 0.0f;
            // Reduce damage by 18 flat, from 30.
            Player.lifeRegen += 18;
        }
    }



    private void SendPingPong()
    {
        _pingPongStopwatch = Stopwatch.StartNew();

        var packet = Mod.GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.PingPong);
        new PingPong(_pingPongCanary).Serialize(packet);
        packet.Send(Player.whoAmI);
    }

    public void OnPingPongReceived(PingPong pingPong)
    {
        if (_pingPongStopwatch == null)
            return;

        if (pingPong.Canary != _pingPongCanary)
            return;

        _pingPongStopwatch.Stop();
        Latency = _pingPongStopwatch.Elapsed / 2;
        _pingPongStopwatch = null;
        _pingPongCanary++;
    }

    public class TikiArmorPlayer : ModPlayer
    {
        public bool hasTikiSet = false;

        public override void PostUpdateEquips()
        {
            // Check if wearing full Tiki Armor
            if (Player.armor[0].type == ItemID.TikiMask &&
                Player.armor[1].type == ItemID.TikiShirt &&
                Player.armor[2].type == ItemID.TikiPants)
            {
                hasTikiSet = true;
            }
            else
            {
                hasTikiSet = false;
            }
        }
    }

    public class WhipRangePlayer : ModPlayer
    {
        public bool largeWhipIncrease = false;

        public override void PostUpdateEquips()
        {
            largeWhipIncrease = false;

            for (int i = 3; i < 8 + Player.GetAmountOfExtraAccessorySlotsToShow(); i++)
            {
                if ((Player.armor[0].type == ItemID.TikiMask && Player.armor[1].type == ItemID.TikiShirt &&Player.armor[2].type == ItemID.TikiPants) || (Player.armor[0].type == ItemID.ObsidianHelm && Player.armor[1].type == ItemID.ObsidianShirt && Player.armor[2].type == ItemID.ObsidianPants) || (Player.armor[0].type == ItemID.BeeHeadgear && Player.armor[1].type == ItemID.BeeBreastplate && Player.armor[2].type == ItemID.BeeGreaves) || (Player.armor[0].type == ItemID.SpiderMask && Player.armor[1].type == ItemID.SpiderBreastplate && Player.armor[2].type == ItemID.SpiderGreaves))
                {
                    largeWhipIncrease = true;
                    break;
                }
            }
            if (!largeWhipIncrease)
            {
                Player.whipRangeMultiplier -= 0.65f;
            }
        }
    }

    private void EditPlayerFrame(ILContext il)
    {
        var cursor = new ILCursor(il);

        RemoveSocialArmor(10);
        RemoveSocialArmor(11);
        RemoveSocialArmor(12);

        return;

        void RemoveSocialArmor(int slot)
        {
            cursor.Index = 0;

            cursor.GotoNext(i => i.MatchLdfld<Player>("armor") && i.Next.MatchLdcI4(slot));
            cursor.Index -= 1;
            cursor.RemoveRange(14);
        }
    }

    private static bool? ShouldSilenceHurtSound(Player target, Player.HurtInfo info)
    {
        // Only silence hurt sound on clients that we hurt that aren't ourselves
        if (!Main.dedServ && info.PvP && target.whoAmI != Main.myPlayer &&
            info.DamageSource.SourcePlayerIndex == Main.myPlayer)
        {
            var marker = ModContent.GetInstance<AdventureClientConfig>().SoundEffect.PlayerHitMarker;
            if (marker != null && marker.SilenceVanilla)
                return true;
        }

        return null;
    }

    private static void PlayHitMarker(int damage)
    {
        var marker = ModContent.GetInstance<AdventureClientConfig>().SoundEffect.PlayerHitMarker;
        if (marker != null)
            SoundEngine.PlaySound(marker.Create(damage));
    }

    private static void PlayKillMarker(int damage)
    {
        var marker = ModContent.GetInstance<AdventureClientConfig>().SoundEffect.PlayerKillMarker;
        if (marker != null)
            SoundEngine.PlaySound(marker.Create(damage));
    }

    public class PingCommand : ModCommand
    {
        public override void Action(CommandCaller caller, string input, string[] args)
        {
            foreach (var player in Main.ActivePlayers)
            {
                var ping = player.GetModPlayer<AdventurePlayer>().Latency;
                if (ping != null)
                    caller.Reply($"{player.name}: {ping.Value.TotalMilliseconds}ms");
            }
        }

        public override string Command => "ping";
        public override CommandType Type => CommandType.Console;
    }

    public override string ToString()
    {
        return $"{Player.whoAmI}/{Player.name}/{DiscordUser?.Id}";
    }
}

public class TurtleDashPlayer : ModPlayer
{
    private bool isWearingFullTurtleArmor = false;
    private int dashingTimer = 0;

    // Detect if player is in a dash state
    public bool IsInADashState => (Player.dashDelay == -1 || dashingTimer > 0) && Player.grapCount <= 0;

    public override void PostUpdateEquips()
    {

        isWearingFullTurtleArmor = Player.armor[0].type == ItemID.TurtleHelmet &&
                                   Player.armor[1].type == ItemID.TurtleScaleMail &&
                                   Player.armor[2].type == ItemID.TurtleLeggings;

        if (isWearingFullTurtleArmor)
        {
            Player.AddBuff(ModContent.BuffType<BROISACHOJ>(), 1 * 60 * 60);
        }
    }

    public override void PostUpdate()
    {

        if (Player.dashDelay == -1)
        {
            dashingTimer = 10;
        }
        else if (dashingTimer > 0)
        {
            dashingTimer--;
        }

        // Apply dash speed reduction if player has the debuff and is dashing
        if (Player.HasBuff(ModContent.BuffType<BROISACHOJ>()) && IsInADashState)
        {
            float dashSpeedReduction = Player.velocity.X * 0.05f;
            Player.velocity.X -= dashSpeedReduction;
        }
        if (Player.HasBuff(BuffID.BabyEater) && IsInADashState)
        {
            float dashSpeedReduction = Player.velocity.X * -0.03f;
            Player.velocity.X -= dashSpeedReduction;
            //Dont think I didnt notice this.
        }
        //thanks mr fargo
    }

}
public class NewIchorPlayer : ModPlayer
{
    public bool hasDefenseReduction = false;

    public override void ResetEffects()
    {
        hasDefenseReduction = false;
    }

    public override void PostUpdateBuffs()
    {
        // Convert vanilla Ichor to our custom debuff
        if (Player.HasBuff(BuffID.Ichor))
        {
            // Get the remaining time of the vanilla Ichor debuff
            int ichorBuffIndex = Player.FindBuffIndex(BuffID.Ichor);
            if (ichorBuffIndex != -1)
            {
                int remainingTime = Player.buffTime[ichorBuffIndex];

                // Remove vanilla Ichor
                Player.DelBuff(ichorBuffIndex);

                // Add our custom debuff with the same duration
                Player.AddBuff(ModContent.BuffType<NewIchorPlayerDebuff>(), remainingTime);
            }
        }

        // Check if player has our custom debuff
        if (Player.HasBuff(ModContent.BuffType<NewIchorPlayerDebuff>()))
        {
            hasDefenseReduction = true;
        }
    }

    public override void PostUpdateEquips()
    {

        if (hasDefenseReduction)
        {
            // Calculate 33% reduction (rounded down)
            int originalDefense = Player.statDefense;
            int reduction = (int)(originalDefense * 0.33f);
            Player.statDefense -= reduction;
            Player.ichor = true;
        }
    }
}
public class BitingEmbracePlayer : ModPlayer
{
    public int bitingEmbraceApplierIndex = -1;

    public override void PostHurt(Player.HurtInfo info)
    {
        if (info.DamageSource.SourceProjectileType == ProjectileID.CoolWhip)
        {
            int duration = 300;
            int applierIndex = -1;

            if (info.DamageSource.SourcePlayerIndex >= 0 && info.DamageSource.SourcePlayerIndex < Main.maxPlayers)
            {
                Player attacker = Main.player[info.DamageSource.SourcePlayerIndex];
                if (attacker != null && attacker.active)
                {
                    TikiArmorPlayer tikiPlayer = attacker.GetModPlayer<TikiArmorPlayer>();
                    if (tikiPlayer.hasTikiSet)
                    {
                        duration = (int)(duration * 3.5f);
                    }
                    applierIndex = info.DamageSource.SourcePlayerIndex;
                }
            }

            bitingEmbraceApplierIndex = applierIndex;
            Player.AddBuff(ModContent.BuffType<BitingEmbrace>(), duration);
            Player.AddBuff(BuffID.Frostburn2, duration);
        }
    }

    public override void PostUpdateBuffs()
    {
        if (Player.HasBuff<BitingEmbrace>() && bitingEmbraceApplierIndex >= 0 && bitingEmbraceApplierIndex < Main.maxPlayers)
        {
            Player applier = Main.player[bitingEmbraceApplierIndex];
            if (applier == null || !applier.active || applier.dead)
            {
                int buffIndex = Player.FindBuffIndex(ModContent.BuffType<BitingEmbrace>());
                if (buffIndex >= 0)
                {
                    Player.buffTime[buffIndex] = 0;
                }
                Player.ClearBuff(BuffID.Frostburn2);
                bitingEmbraceApplierIndex = -1;
                return;
            }
        }

        if (Player.HasBuff<BitingEmbrace>())
        {
            float pulseTime = Main.GameUpdateCount % 60f / 60f;
            float pulseScale = 1f + (float)Math.Sin(pulseTime * MathHelper.TwoPi) * 0.2f;

            for (int i = 0; i < 4; i++)
            {
                float angle = (MathHelper.TwoPi / 4f) * i + (Main.GameUpdateCount * 0.07f);
                float distance = 21f * pulseScale;

                Vector2 offset = new Vector2(
                    (float)Math.Cos(angle) * distance,
                    (float)Math.Sin(angle) * distance
                );
                Vector2 dustPosition = Player.Center + offset;

                Dust dust = Dust.NewDustPerfect(dustPosition, DustID.IceTorch, Vector2.Zero, 100, Color.Teal, 1.5f);
                dust.noGravity = true;
                dust.fadeIn = 1f;
                dust.noLight = false;
            }

            if (Main.rand.NextBool(3))
            {
                for (int i = 0; i < 2; i++)
                {
                    float lineAngle = i * MathHelper.PiOver2;
                    float lineDistance = Main.rand.NextFloat(10f, 40f);

                    Vector2 lineOffset = new Vector2(
                        (float)Math.Cos(lineAngle) * lineDistance,
                        (float)Math.Sin(lineAngle) * lineDistance
                    );
                    Vector2 dustPos = Player.Center + lineOffset;

                    Dust lineDust = Dust.NewDustPerfect(dustPos, DustID.Torch, Vector2.Zero, 100, Color.Teal, 1f);
                    lineDust.noGravity = true;
                    lineDust.fadeIn = 0.5f;
                }
            }
        }
        else
        {
            bitingEmbraceApplierIndex = -1;
        }
    }

    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        if (Player.HasBuff<BitingEmbrace>())
        {
            bool isDebuffApplier = modifiers.DamageSource.SourcePlayerIndex == bitingEmbraceApplierIndex;

            bool isSummon = false;
            if (modifiers.DamageSource.SourceProjectileLocalIndex >= 0)
            {
                Projectile proj = Main.projectile[modifiers.DamageSource.SourceProjectileLocalIndex];
                if (proj != null && proj.active && (proj.CountsAsClass(DamageClass.SummonMeleeSpeed)))
                {
                    isSummon = true;
                }
            }
            else if (modifiers.DamageSource.SourceProjectileType > 0)
            {
                int projType = modifiers.DamageSource.SourceProjectileType;
                if (projType == ProjectileID.BlandWhip || projType == ProjectileID.FireWhip ||
                    projType == ProjectileID.SwordWhip || projType == ProjectileID.MaceWhip ||
                    projType == ProjectileID.ScytheWhip || projType == ProjectileID.ThornWhip ||
                    projType == ProjectileID.BoneWhip || projType == ProjectileID.RainbowWhip ||
                    projType == ProjectileID.CoolWhip)
                {
                    isSummon = true;
                }
            }

            if (!isSummon && !isDebuffApplier)
            {
                modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) => {
                    info.Damage += 7;
                };
            }
        }
    }
}

public class PressurePointsPlayer : ModPlayer
{
    public int pressurePointsApplierIndex = -1;

    public override void PostHurt(Player.HurtInfo info)
    {
        if (info.DamageSource.SourceProjectileType == ProjectileID.ThornWhip)
        {
            int duration = 300;
            int applierIndex = -1;

            if (info.DamageSource.SourcePlayerIndex >= 0 && info.DamageSource.SourcePlayerIndex < Main.maxPlayers)
            {
                Player attacker = Main.player[info.DamageSource.SourcePlayerIndex];
                if (attacker != null && attacker.active)
                {
                    TikiArmorPlayer tikiPlayer = attacker.GetModPlayer<TikiArmorPlayer>();
                    if (tikiPlayer.hasTikiSet)
                    {
                        duration = (int)(duration * 3.5f);
                    }
                    applierIndex = info.DamageSource.SourcePlayerIndex;
                }
            }

            pressurePointsApplierIndex = applierIndex;
            Player.AddBuff(ModContent.BuffType<PressurePoints>(), duration);
        }
    }

    public override void PostUpdateBuffs()
    {
        if (Player.HasBuff<PressurePoints>() && pressurePointsApplierIndex >= 0 && pressurePointsApplierIndex < Main.maxPlayers)
        {
            Player applier = Main.player[pressurePointsApplierIndex];
            if (applier == null || !applier.active || applier.dead)
            {
                int buffIndex = Player.FindBuffIndex(ModContent.BuffType<PressurePoints>());
                if (buffIndex >= 0)
                {
                    Player.buffTime[buffIndex] = 0;
                }
                pressurePointsApplierIndex = -1;
                return;
            }
        }

        if (Player.HasBuff<PressurePoints>())
        {
            float pulseTime = Main.GameUpdateCount % 60f / 60f;
            float pulseScale = 1f + (float)Math.Sin(pulseTime * MathHelper.TwoPi) * 0.2f;

            for (int i = 0; i < 4; i++)
            {
                float angle = (MathHelper.TwoPi / 4f) * i + (Main.GameUpdateCount * 0.05f);
                float distance = 12f * pulseScale;

                Vector2 offset = new Vector2(
                    (float)Math.Cos(angle) * distance,
                    (float)Math.Sin(angle) * distance
                );
                Vector2 dustPosition = Player.Center + offset;

                Dust dust = Dust.NewDustPerfect(dustPosition, DustID.CursedTorch, Vector2.Zero, 100, Color.LimeGreen, 1.5f);
                dust.noGravity = true;
                dust.fadeIn = 1f;
                dust.noLight = false;
            }

            if (Main.rand.NextBool(3))
            {
                for (int i = 0; i < 2; i++)
                {
                    float lineAngle = i * MathHelper.PiOver2;
                    float lineDistance = Main.rand.NextFloat(10f, 40f);

                    Vector2 lineOffset = new Vector2(
                        (float)Math.Cos(lineAngle) * lineDistance,
                        (float)Math.Sin(lineAngle) * lineDistance
                    );
                    Vector2 dustPos = Player.Center + lineOffset;

                    Dust lineDust = Dust.NewDustPerfect(dustPos, DustID.Torch, Vector2.Zero, 100, Color.LimeGreen, 1f);
                    lineDust.noGravity = true;
                    lineDust.fadeIn = 0.5f;
                }
            }
        }
        else
        {
            pressurePointsApplierIndex = -1;
        }
    }

    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        if (Player.HasBuff<PressurePoints>())
        {
            bool isDebuffApplier = modifiers.DamageSource.SourcePlayerIndex == pressurePointsApplierIndex;

            bool isSummon = false;

            if (modifiers.DamageSource.SourceProjectileLocalIndex >= 0)
            {
                Projectile proj = Main.projectile[modifiers.DamageSource.SourceProjectileLocalIndex];
                if (proj != null && proj.active && (proj.CountsAsClass(DamageClass.SummonMeleeSpeed)))
                {
                    isSummon = true;
                }
            }
            else if (modifiers.DamageSource.SourceProjectileType > 0)
            {
                int projType = modifiers.DamageSource.SourceProjectileType;
                if (projType == ProjectileID.BlandWhip || projType == ProjectileID.FireWhip ||
                    projType == ProjectileID.SwordWhip || projType == ProjectileID.MaceWhip ||
                    projType == ProjectileID.ScytheWhip || projType == ProjectileID.ThornWhip ||
                    projType == ProjectileID.BoneWhip || projType == ProjectileID.RainbowWhip ||
                    projType == ProjectileID.CoolWhip)
                {
                    isSummon = true;
                }
            }

            if (!isSummon && !isDebuffApplier)
            {
                modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) => {
                    info.Damage += 6;
                };
            }
        }
    }
}

public class BrittleBonesPlayer : ModPlayer
{
    public int brittleBonesApplierIndex = -1;

    public override void PostHurt(Player.HurtInfo info)
    {
        if (info.DamageSource.SourceProjectileType == ProjectileID.BoneWhip)
        {
            int duration = 300;
            int applierIndex = -1;

            if (info.DamageSource.SourcePlayerIndex >= 0 && info.DamageSource.SourcePlayerIndex < Main.maxPlayers)
            {
                Player attacker = Main.player[info.DamageSource.SourcePlayerIndex];
                if (attacker != null && attacker.active)
                {
                    TikiArmorPlayer tikiPlayer = attacker.GetModPlayer<TikiArmorPlayer>();
                    if (tikiPlayer.hasTikiSet)
                    {
                        duration = (int)(duration * 3.5f);
                    }
                    applierIndex = info.DamageSource.SourcePlayerIndex;
                }
            }

            brittleBonesApplierIndex = applierIndex;
            Player.AddBuff(ModContent.BuffType<BrittleBones>(), duration);
        }
    }

    public override void PostUpdateBuffs()
    {
        if (Player.HasBuff<BrittleBones>() && brittleBonesApplierIndex >= 0 && brittleBonesApplierIndex < Main.maxPlayers)
        {
            Player applier = Main.player[brittleBonesApplierIndex];
            if (applier == null || !applier.active || applier.dead)
            {
                int buffIndex = Player.FindBuffIndex(ModContent.BuffType<BrittleBones>());
                if (buffIndex >= 0)
                {
                    Player.buffTime[buffIndex] = 0;
                }
                brittleBonesApplierIndex = -1;
                return;
            }
        }

        if (Player.HasBuff<BrittleBones>())
        {
            float pulseTime = Main.GameUpdateCount % 60f / 60f;
            float pulseScale = 1f + (float)Math.Sin(pulseTime * MathHelper.TwoPi) * 0.2f;

            for (int i = 0; i < 4; i++)
            {
                float angle = (MathHelper.TwoPi / 4f) * i + (Main.GameUpdateCount * 0.06f);
                float distance = 19f * pulseScale;

                Vector2 offset = new Vector2(
                    (float)Math.Cos(angle) * distance,
                    (float)Math.Sin(angle) * distance
                );
                Vector2 dustPosition = Player.Center + offset;

                Dust dust = Dust.NewDustPerfect(dustPosition, DustID.BoneTorch, Vector2.Zero, 100, Color.DarkGray, 1.5f);
                dust.noGravity = true;
                dust.fadeIn = 1f;
                dust.noLight = false;
            }

            if (Main.rand.NextBool(3))
            {
                for (int i = 0; i < 2; i++)
                {
                    float lineAngle = i * MathHelper.PiOver2;
                    float lineDistance = Main.rand.NextFloat(10f, 40f);

                    Vector2 lineOffset = new Vector2(
                        (float)Math.Cos(lineAngle) * lineDistance,
                        (float)Math.Sin(lineAngle) * lineDistance
                    );
                    Vector2 dustPos = Player.Center + lineOffset;

                    Dust lineDust = Dust.NewDustPerfect(dustPos, DustID.Torch, Vector2.Zero, 100, Color.DarkGray, 1f);
                    lineDust.noGravity = true;
                    lineDust.fadeIn = 0.5f;
                }
            }
        }
        else
        {
            brittleBonesApplierIndex = -1;
        }
    }

    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        if (Player.HasBuff<BrittleBones>())
        {
            bool isDebuffApplier = modifiers.DamageSource.SourcePlayerIndex == brittleBonesApplierIndex;

            bool isSummon = false;

            if (modifiers.DamageSource.SourceProjectileLocalIndex >= 0)
            {
                Projectile proj = Main.projectile[modifiers.DamageSource.SourceProjectileLocalIndex];
                if (proj != null && proj.active && (proj.CountsAsClass(DamageClass.SummonMeleeSpeed)))
                {
                    isSummon = true;
                }
            }
            else if (modifiers.DamageSource.SourceProjectileType > 0)
            {
                int projType = modifiers.DamageSource.SourceProjectileType;
                if (projType == ProjectileID.BlandWhip || projType == ProjectileID.FireWhip ||
                    projType == ProjectileID.SwordWhip || projType == ProjectileID.MaceWhip ||
                    projType == ProjectileID.ScytheWhip || projType == ProjectileID.ThornWhip ||
                    projType == ProjectileID.BoneWhip || projType == ProjectileID.RainbowWhip ||
                    projType == ProjectileID.CoolWhip)
                {
                    isSummon = true;
                }
            }

            if (!isSummon && !isDebuffApplier)
            {
                modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) => {
                    info.Damage += 7;
                };
            }
        }
    }
}

public class MarkedPlayer : ModPlayer
{
    public int markedApplierIndex = -1;

    public override void PostHurt(Player.HurtInfo info)
    {
        if (info.DamageSource.SourceProjectileType == ProjectileID.SwordWhip)
        {
            int duration = 300;
            int applierIndex = -1;

            if (info.DamageSource.SourcePlayerIndex >= 0 && info.DamageSource.SourcePlayerIndex < Main.maxPlayers)
            {
                Player attacker = Main.player[info.DamageSource.SourcePlayerIndex];
                if (attacker != null && attacker.active)
                {
                    TikiArmorPlayer tikiPlayer = attacker.GetModPlayer<TikiArmorPlayer>();
                    if (tikiPlayer.hasTikiSet)
                    {
                        duration = (int)(duration * 3.5f);
                    }
                    applierIndex = info.DamageSource.SourcePlayerIndex;
                }
            }

            markedApplierIndex = applierIndex;
            Player.AddBuff(ModContent.BuffType<Marked>(), duration);
        }
    }

    public override void PostUpdateBuffs()
    {
        if (Player.HasBuff<Marked>() && markedApplierIndex >= 0 && markedApplierIndex < Main.maxPlayers)
        {
            Player applier = Main.player[markedApplierIndex];
            if (applier == null || !applier.active || applier.dead)
            {
                int buffIndex = Player.FindBuffIndex(ModContent.BuffType<Marked>());
                if (buffIndex >= 0)
                {
                    Player.buffTime[buffIndex] = 0;
                }
                markedApplierIndex = -1;
                return;
            }
        }

        if (Player.HasBuff<Marked>())
        {
            float pulseTime = Main.GameUpdateCount % 60f / 60f;
            float pulseScale = 1f + (float)Math.Sin(pulseTime * MathHelper.TwoPi) * 0.2f;

            for (int i = 0; i < 4; i++)
            {
                float angle = (MathHelper.TwoPi / 4f) * i + (Main.GameUpdateCount * 0.08f);
                float distance = 26f * pulseScale;

                Vector2 offset = new Vector2(
                    (float)Math.Cos(angle) * distance,
                    (float)Math.Sin(angle) * distance
                );
                Vector2 dustPosition = Player.Center + offset;

                Dust dust = Dust.NewDustPerfect(dustPosition, DustID.Blood, Vector2.Zero, 100, Color.Red, 1.5f);
                dust.noGravity = true;
                dust.fadeIn = 1f;
                dust.noLight = false;
            }

            if (Main.rand.NextBool(3))
            {
                for (int i = 0; i < 2; i++)
                {
                    float lineAngle = i * MathHelper.PiOver2;
                    float lineDistance = Main.rand.NextFloat(10f, 40f);

                    Vector2 lineOffset = new Vector2(
                        (float)Math.Cos(lineAngle) * lineDistance,
                        (float)Math.Sin(lineAngle) * lineDistance
                    );
                    Vector2 dustPos = Player.Center + lineOffset;

                    Dust lineDust = Dust.NewDustPerfect(dustPos, DustID.Torch, Vector2.Zero, 100, Color.DarkRed, 1f);
                    lineDust.noGravity = true;
                    lineDust.fadeIn = 0.5f;
                }
            }
        }
        else
        {
            markedApplierIndex = -1;
        }
    }

    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        if (Player.HasBuff<Marked>())
        {
            bool isDebuffApplier = modifiers.DamageSource.SourcePlayerIndex == markedApplierIndex;

            bool isSummon = false;

            if (modifiers.DamageSource.SourceProjectileLocalIndex >= 0)
            {
                Projectile proj = Main.projectile[modifiers.DamageSource.SourceProjectileLocalIndex];
                if (proj != null && proj.active && ( proj.CountsAsClass(DamageClass.SummonMeleeSpeed)))
                {
                    isSummon = true;
                }
            }
            else if (modifiers.DamageSource.SourceProjectileType > 0)
            {
                int projType = modifiers.DamageSource.SourceProjectileType;
                if (projType == ProjectileID.BlandWhip || projType == ProjectileID.FireWhip ||
                    projType == ProjectileID.SwordWhip || projType == ProjectileID.MaceWhip ||
                    projType == ProjectileID.ScytheWhip || projType == ProjectileID.ThornWhip ||
                    projType == ProjectileID.BoneWhip || projType == ProjectileID.RainbowWhip ||
                    projType == ProjectileID.CoolWhip)
                {
                    isSummon = true;
                }
            }

            if (!isSummon && !isDebuffApplier)
            {
                modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) => {
                    info.Damage += 9;
                };
            }
        }
    }
}

public class AnathemaPlayer : ModPlayer
{
    public int anathemaApplierIndex = -1;
    public int dummyNPCIndex = -1;

    public override void PostHurt(Player.HurtInfo info)
    {
        if (info.DamageSource.SourceProjectileType == ProjectileID.RainbowWhip)
        {
            int duration = 300;
            int applierIndex = -1;

            if (info.DamageSource.SourcePlayerIndex >= 0 && info.DamageSource.SourcePlayerIndex < Main.maxPlayers)
            {
                Player attacker = Main.player[info.DamageSource.SourcePlayerIndex];
                if (attacker != null && attacker.active)
                {
                    TikiArmorPlayer tikiPlayer = attacker.GetModPlayer<TikiArmorPlayer>();
                    if (tikiPlayer.hasTikiSet)
                    {
                        duration = (int)(duration * 3.5f);
                    }
                    applierIndex = info.DamageSource.SourcePlayerIndex;
                }
            }

            anathemaApplierIndex = applierIndex;
            Player.AddBuff(ModContent.BuffType<Anathema>(), duration);
        }
    }

    public override void PostUpdateBuffs()
    {
        if (Player.HasBuff<Anathema>() && anathemaApplierIndex >= 0 && anathemaApplierIndex < Main.maxPlayers)
        {
            Player applier = Main.player[anathemaApplierIndex];
            if (applier == null || !applier.active || applier.dead)
            {
                int buffIndex = Player.FindBuffIndex(ModContent.BuffType<Anathema>());
                if (buffIndex >= 0)
                {
                    Player.buffTime[buffIndex] = 0;
                }
                anathemaApplierIndex = -1;
                return;
            }
        }

        if (Player.HasBuff<Anathema>())
        {
            bool needsSpawn = dummyNPCIndex < 0 ||
                              dummyNPCIndex >= Main.maxNPCs ||
                              !Main.npc[dummyNPCIndex].active ||
                              Main.npc[dummyNPCIndex].type != ModContent.NPCType<Target>();

            if (needsSpawn)
            {

                int npcIndex = NPC.NewNPC(Player.GetSource_FromThis(), (int)Player.Center.X, (int)Player.Center.Y, ModContent.NPCType<Target>());

                if (npcIndex >= 0 && npcIndex < Main.maxNPCs)
                {
                    Target targetNPC = Main.npc[npcIndex].ModNPC as Target;
                    if (targetNPC != null)
                    {
                        targetNPC.targetPlayerIndex = Player.whoAmI;
                    }
                    dummyNPCIndex = npcIndex;

                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npcIndex);
                    }
                }
                else
                {
                }
            }

            for (int i = 0; i < 3; i++)
            {
                float distance = Main.rand.NextFloat(40f, 80f);
                float angle = Main.rand.NextFloat(0f, MathHelper.TwoPi);

                Vector2 spawnOffset = new Vector2(
                    (float)Math.Cos(angle) * distance,
                    (float)Math.Sin(angle) * distance
                );
                Vector2 dustPosition = Player.Center + spawnOffset;

                Vector2 towardPlayer = Player.Center - dustPosition;
                towardPlayer.Normalize();
                Vector2 dustVelocity = towardPlayer * Main.rand.NextFloat(2f, 4f);

                int dustType;
                Color dustColor;

                if (Main.rand.NextBool())
                {
                    dustType = DustID.PlatinumCoin;
                    dustColor = Color.White;
                }
                else
                {
                    dustType = DustID.Smoke;
                    dustColor = Color.Black;
                }

                Dust dust = Dust.NewDustDirect(dustPosition, 0, 0, dustType, dustVelocity.X, dustVelocity.Y, 100, dustColor, Main.rand.NextFloat(0.6f, 1.2f));
                dust.noGravity = true;
                dust.fadeIn = 0.8f;
            }
        }
        else
        {
            anathemaApplierIndex = -1;

            // Remove dummy NPC when buff expires
            if (dummyNPCIndex >= 0 && dummyNPCIndex < Main.maxNPCs && Main.npc[dummyNPCIndex].active)
            {
                Main.npc[dummyNPCIndex].active = false;
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, dummyNPCIndex);
                }
            }
            dummyNPCIndex = -1;
        }
    }

    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        if (Player.HasBuff<Anathema>())
        {
            bool isDebuffApplier = modifiers.DamageSource.SourcePlayerIndex == anathemaApplierIndex;

            bool isSummon = false;

            if (modifiers.DamageSource.SourceProjectileLocalIndex >= 0)
            {
                Projectile proj = Main.projectile[modifiers.DamageSource.SourceProjectileLocalIndex];
                if (proj != null && proj.active && (proj.CountsAsClass(DamageClass.SummonMeleeSpeed)))
                {
                    isSummon = true;
                }
            }
            else if (modifiers.DamageSource.SourceProjectileType > 0)
            {
                int projType = modifiers.DamageSource.SourceProjectileType;
                if (projType == ProjectileID.BlandWhip || projType == ProjectileID.FireWhip ||
                    projType == ProjectileID.SwordWhip || projType == ProjectileID.MaceWhip ||
                    projType == ProjectileID.ScytheWhip || projType == ProjectileID.ThornWhip ||
                    projType == ProjectileID.BoneWhip || projType == ProjectileID.RainbowWhip ||
                    projType == ProjectileID.CoolWhip)
                {
                    isSummon = true;
                }
            }

            if (!isSummon && !isDebuffApplier)
            {
                modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) => {
                    info.Damage += 20;
                };
                modifiers.FinalDamage *= 1.1f;
            }
        }
    }
}

public class ShatteredArmorPlayer : ModPlayer
{
    public int shatteredArmorApplierIndex = -1;

    public override void PostHurt(Player.HurtInfo info)
    {
        if (info.DamageSource.SourceProjectileType == ProjectileID.MaceWhip)
        {
            int duration = 300;
            int applierIndex = -1;

            if (info.DamageSource.SourcePlayerIndex >= 0 && info.DamageSource.SourcePlayerIndex < Main.maxPlayers)
            {
                Player attacker = Main.player[info.DamageSource.SourcePlayerIndex];
                if (attacker != null && attacker.active)
                {
                    TikiArmorPlayer tikiPlayer = attacker.GetModPlayer<TikiArmorPlayer>();
                    if (tikiPlayer.hasTikiSet)
                    {
                        duration = (int)(duration * 3.5f);
                    }
                    applierIndex = info.DamageSource.SourcePlayerIndex;
                }
            }

            shatteredArmorApplierIndex = applierIndex;
            Player.AddBuff(ModContent.BuffType<ShatteredArmor>(), duration);
        }
    }

    public override void PostUpdateBuffs()
    {
        if (Player.HasBuff<ShatteredArmor>() && shatteredArmorApplierIndex >= 0 && shatteredArmorApplierIndex < Main.maxPlayers)
        {
            Player applier = Main.player[shatteredArmorApplierIndex];
            if (applier == null || !applier.active || applier.dead)
            {
                int buffIndex = Player.FindBuffIndex(ModContent.BuffType<ShatteredArmor>());
                if (buffIndex >= 0)
                {
                    Player.buffTime[buffIndex] = 0;
                }
                shatteredArmorApplierIndex = -1;
                return;
            }
        }

        if (Player.HasBuff<ShatteredArmor>())
        {
            for (int i = 0; i < 1; i++)
            {
                int dustType = DustID.BatScepter;
                Vector2 dustPosition = Player.position + new Vector2(Main.rand.Next(Player.width), Main.rand.Next(Player.height));
                Vector2 dustVelocity = Player.velocity * 0.3f + new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-1f, 1f));

                Dust dust = Dust.NewDustDirect(dustPosition, 0, 0, dustType, dustVelocity.X, dustVelocity.Y, 100, Color.Black, Main.rand.NextFloat(0.8f, 1.5f));
                dust.noGravity = Main.rand.NextBool(2);
                dust.fadeIn = 1.2f;
            }
        }
        else
        {
            shatteredArmorApplierIndex = -1;
        }
    }

    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        if (Player.HasBuff<ShatteredArmor>())
        {
            bool isDebuffApplier = modifiers.DamageSource.SourcePlayerIndex == shatteredArmorApplierIndex;

            bool isSummon = false;

            if (modifiers.DamageSource.SourceProjectileLocalIndex >= 0)
            {
                Projectile proj = Main.projectile[modifiers.DamageSource.SourceProjectileLocalIndex];
                if (proj != null && proj.active && (proj.CountsAsClass(DamageClass.SummonMeleeSpeed)))
                {
                    isSummon = true;
                }
            }
            else if (modifiers.DamageSource.SourceProjectileType > 0)
            {
                int projType = modifiers.DamageSource.SourceProjectileType;
                if (projType == ProjectileID.BlandWhip || projType == ProjectileID.FireWhip ||
                    projType == ProjectileID.SwordWhip || projType == ProjectileID.MaceWhip ||
                    projType == ProjectileID.ScytheWhip || projType == ProjectileID.ThornWhip ||
                    projType == ProjectileID.BoneWhip || projType == ProjectileID.RainbowWhip ||
                    projType == ProjectileID.CoolWhip)
                {
                    isSummon = true;
                }
            }

            if (!isSummon && !isDebuffApplier)
            {
                modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) => {
                    info.Damage += 8;
                };
                modifiers.FinalDamage *= 1.12f;
            }
        }
    }
}

public class HellhexPlayer : ModPlayer
{
    public bool hellhexTriggered = false;
    private int storedDamage = 0;
    public int hellhexApplierIndex = -1;
    private bool explosionSpawned = false;

    private bool IsSummonOrWhipDamage(Player.HurtInfo info)
    {
        if (info.DamageSource.SourceProjectileLocalIndex >= 0)
        {
            Projectile proj = Main.projectile[info.DamageSource.SourceProjectileLocalIndex];
            if (proj != null && proj.active && (proj.CountsAsClass(DamageClass.SummonMeleeSpeed)))
            {
                return true;
            }
        }

        if (info.DamageSource.SourceProjectileType > 0)
        {
            int projType = info.DamageSource.SourceProjectileType;
            if (projType == ProjectileID.BlandWhip || projType == ProjectileID.FireWhip ||
                projType == ProjectileID.SwordWhip || projType == ProjectileID.MaceWhip ||
                projType == ProjectileID.ScytheWhip || projType == ProjectileID.ThornWhip ||
                projType == ProjectileID.BoneWhip || projType == ProjectileID.RainbowWhip ||
                projType == ProjectileID.CoolWhip)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsSummonOrWhipDamage(ref Player.HurtModifiers modifiers)
    {
        if (modifiers.DamageSource.SourceProjectileLocalIndex >= 0)
        {
            Projectile proj = Main.projectile[modifiers.DamageSource.SourceProjectileLocalIndex];
            if (proj != null && proj.active && (proj.CountsAsClass(DamageClass.SummonMeleeSpeed)))
            {
                return true;
            }
        }

        if (modifiers.DamageSource.SourceProjectileType > 0)
        {
            int projType = modifiers.DamageSource.SourceProjectileType;
            if (projType == ProjectileID.BlandWhip || projType == ProjectileID.FireWhip ||
                projType == ProjectileID.SwordWhip || projType == ProjectileID.MaceWhip ||
                projType == ProjectileID.ScytheWhip || projType == ProjectileID.ThornWhip ||
                projType == ProjectileID.BoneWhip || projType == ProjectileID.RainbowWhip ||
                projType == ProjectileID.CoolWhip)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsSummonOrWhipDeath(PlayerDeathReason damageSource)
    {
        if (damageSource.SourceProjectileLocalIndex >= 0)
        {
            Projectile proj = Main.projectile[damageSource.SourceProjectileLocalIndex];
            if (proj != null && proj.active && (proj.CountsAsClass(DamageClass.SummonMeleeSpeed)))
            {
                return true;
            }
        }

        if (damageSource.SourceProjectileType > 0)
        {
            int projType = damageSource.SourceProjectileType;
            if (projType == ProjectileID.BlandWhip || projType == ProjectileID.FireWhip ||
                projType == ProjectileID.SwordWhip || projType == ProjectileID.MaceWhip ||
                projType == ProjectileID.ScytheWhip || projType == ProjectileID.ThornWhip ||
                projType == ProjectileID.BoneWhip || projType == ProjectileID.RainbowWhip ||
                projType == ProjectileID.CoolWhip)
            {
                return true;
            }
        }

        return false;
    }

    public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
    {
        if (Player.HasBuff<Hellhex>())
        {
            bool isSummon = IsSummonOrWhipDeath(damageSource);
            bool isDebuffApplier = damageSource.SourcePlayerIndex == hellhexApplierIndex;

            if (!isSummon && !isDebuffApplier && damage >= 30 && !explosionSpawned)
            {
                explosionSpawned = true;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int owner = hellhexApplierIndex >= 0 && hellhexApplierIndex < Main.maxPlayers ? hellhexApplierIndex : -1;
                    int explosionDamage = (int)(damage * 2.75f);

                    int proj = Projectile.NewProjectile(
                        Player.GetSource_Death(),
                        Player.Center,
                        Vector2.Zero,
                        ProjectileID.FireWhipProj,
                        explosionDamage,
                        0f,
                        owner,
                        0f,
                        0f
                    );

                    if (proj >= 0 && proj < Main.maxProjectiles)
                    {
                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, proj);
                        }
                    }
                }
            }
        }

        return true;
    }

    public override void PostHurt(Player.HurtInfo info)
    {
        if (info.DamageSource.SourceProjectileType == ProjectileID.FireWhip)
        {
            int duration = 450;
            int applierIndex = -1;

            if (info.DamageSource.SourcePlayerIndex >= 0 && info.DamageSource.SourcePlayerIndex < Main.maxPlayers)
            {
                Player attacker = Main.player[info.DamageSource.SourcePlayerIndex];
                if (attacker != null && attacker.active)
                {
                    TikiArmorPlayer tikiPlayer = attacker.GetModPlayer<TikiArmorPlayer>();
                    if (tikiPlayer.hasTikiSet)
                    {
                        duration = (int)(duration * 3.5f);
                    }

                    applierIndex = info.DamageSource.SourcePlayerIndex;
                }
            }

            hellhexApplierIndex = applierIndex;
            Player.AddBuff(ModContent.BuffType<Hellhex>(), duration);

            if (Main.netMode == NetmodeID.Server)
            {
                ModPacket packet = Mod.GetPacket();
                packet.Write((byte)0);
                packet.Write((byte)Player.whoAmI);
                packet.Write((byte)applierIndex);
                packet.Send();
            }

            return;
        }

        if (Player.HasBuff<Hellhex>() && !explosionSpawned)
        {
            bool isSummon = IsSummonOrWhipDamage(info);
            bool isDebuffApplier = info.DamageSource.SourcePlayerIndex == hellhexApplierIndex;

            if (!isSummon && !isDebuffApplier && info.Damage >= 30)
            {
                explosionSpawned = true;

                int buffIndex = Player.FindBuffIndex(ModContent.BuffType<Hellhex>());
                if (buffIndex >= 0)
                {
                    Player.buffTime[buffIndex] = 0;
                }

                Vector2 spawnPos = Player.Center;
                float scale = info.Damage / 100f;
                int owner = hellhexApplierIndex >= 0 && hellhexApplierIndex < Main.maxPlayers ? hellhexApplierIndex : -1;
                int explosionDamage = (int)(info.Damage * 2.75f);

                if (Main.netMode == NetmodeID.SinglePlayer ||
                    (info.DamageSource.SourcePlayerIndex >= 0 && Main.myPlayer == info.DamageSource.SourcePlayerIndex))
                {
                    int proj = Projectile.NewProjectile(
                        Player.GetSource_Buff(buffIndex),
                        spawnPos,
                        Vector2.Zero,
                        ProjectileID.FireWhipProj,
                        explosionDamage,
                        0f,
                        owner
                    );

                    if (proj >= 0 && proj < Main.maxProjectiles)
                    {
                        Main.projectile[proj].scale = scale;
                    }

                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        ModPacket packet = Mod.GetPacket();
                        packet.Write((byte)1);
                        packet.Write((byte)Player.whoAmI);
                        packet.Write(spawnPos.X);
                        packet.Write(spawnPos.Y);
                        packet.Write(explosionDamage);
                        packet.Write(scale);
                        packet.Write((sbyte)owner);
                        packet.Send();
                    }
                }
                else if (Main.netMode == NetmodeID.Server)
                {
                    int proj = Projectile.NewProjectile(
                        Player.GetSource_Buff(buffIndex),
                        spawnPos,
                        Vector2.Zero,
                        ProjectileID.FireWhipProj,
                        explosionDamage,
                        0f,
                        owner
                    );

                    if (proj >= 0 && proj < Main.maxProjectiles)
                    {
                        Main.projectile[proj].scale = scale;
                        NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, proj);
                    }
                }
            }
        }
    }

    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        if (Player.HasBuff<Hellhex>())
        {
            bool isSummon = IsSummonOrWhipDamage(ref modifiers);

            if (!isSummon)
            {
                hellhexTriggered = true;
            }
        }
    }

    public override void PostUpdateBuffs()
    {
        if (Player.HasBuff<Hellhex>() && hellhexApplierIndex >= 0 && hellhexApplierIndex < Main.maxPlayers)
        {
            Player applier = Main.player[hellhexApplierIndex];
            if (applier == null || !applier.active || applier.dead)
            {
                int buffIndex = Player.FindBuffIndex(ModContent.BuffType<Hellhex>());
                if (buffIndex >= 0)
                {
                    Player.buffTime[buffIndex] = 0;
                }
                hellhexApplierIndex = -1;
                hellhexTriggered = false;
                explosionSpawned = false;
                return;
            }
        }

        if (Player.HasBuff<Hellhex>())
        {
            for (int i = 0; i < 2; i++)
            {
                int dustType = DustID.Torch;
                Vector2 dustPosition = Player.position + new Vector2(Main.rand.Next(Player.width), Main.rand.Next(Player.height));
                Vector2 dustVelocity = new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-2f, 0f));

                Dust dust = Dust.NewDustDirect(dustPosition, 0, 0, dustType, dustVelocity.X, dustVelocity.Y, 100, default(Color), Main.rand.NextFloat(1f, 2f));
                dust.noGravity = true;
                dust.fadeIn = 1.3f;
            }

            if (Main.rand.NextBool(2))
            {
                int dustType = DustID.Smoke;
                Vector2 dustPosition = Player.position + new Vector2(Main.rand.Next(Player.width), Main.rand.Next(Player.height));
                Vector2 dustVelocity = new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-1.5f, 0f));

                Dust dust = Dust.NewDustDirect(dustPosition, 0, 0, dustType, dustVelocity.X, dustVelocity.Y, 100, Color.OrangeRed, Main.rand.NextFloat(0.8f, 1.5f));
                dust.noGravity = true;
            }
        }
        else
        {
            hellhexTriggered = false;
            hellhexApplierIndex = -1;
            explosionSpawned = false;
        }
    }
}
public class PvPAdventurePlayer : ModPlayer
{
    public bool hasReceivedStarterBag = false;

    public override void SaveData(TagCompound tag)
    {
        tag["hasReceivedStarterBag"] = hasReceivedStarterBag;
    }

    public override void LoadData(TagCompound tag)
    {
        hasReceivedStarterBag = tag.GetBool("hasReceivedStarterBag");
    }

    public override void OnEnterWorld()
    {
        if (!hasReceivedStarterBag)
        {
            int itemType = ModContent.ItemType<AdventureBag>();
            var item = new Item();
            item.SetDefaults(itemType);
            Player.inventory[1] = item; // Adds to second inventory slot
            var beachBallItem = new Item();
            beachBallItem.SetDefaults(ItemID.BeachBall);
            Player.inventory[2] = beachBallItem; // Adds to third inventory slot

            hasReceivedStarterBag = true;
        }
    }
}


public class SpawnProtectionPlayer : ModPlayer
{
    public override void PostUpdateMiscEffects()
    {
        int playerTileX = (int)(Player.position.X / 16f);
        int playerTileY = (int)(Player.position.Y / 16f);

        int spawnTileX = Main.spawnTileX;
        int spawnTileY = Main.spawnTileY;

        int distanceX = Math.Abs(playerTileX - spawnTileX);
        int distanceY = Math.Abs(playerTileY - spawnTileY);

        if (distanceX <= 25 && distanceY <= 25)
        {
            Player.AddBuff(ModContent.BuffType<PlayerInSpawn>(), 2);
        }
    }
}

public class AetherLuckPlayer : ModPlayer
{
    public override void ModifyLuck(ref float luck)
    {
        if (Player.ZoneShimmer)
        {
            luck += 400; // 400 times the amount of normal luck
        }
    }
}
public class ShadowFlamePlayer : ModPlayer
{
    public override void PostHurt(Player.HurtInfo info)
    {
        int shadowflameDuration = 0;

        if (info.DamageSource.SourceProjectileType == ProjectileID.ShadowFlameArrow)
        {
            int maxDuration = 2 * 60;
            float damageRatio = Math.Min(info.Damage / 30f, 1f);
            shadowflameDuration = (int)(maxDuration * damageRatio);
        }
        else if (info.DamageSource.SourceProjectileType == ProjectileID.ShadowFlame)
        {
            shadowflameDuration = 60 * 10;
        }
        else if (info.DamageSource.SourceProjectileType == ProjectileID.ShadowFlameKnife)
        {
            int maxDuration = 60 * 2;
            float damageRatio = Math.Min(info.Damage / 25f, 1f);
            shadowflameDuration = (int)(maxDuration * damageRatio);
        }
        else if (info.DamageSource.SourceProjectileType == ProjectileID.DarkLance)
        {
            shadowflameDuration = 66 * 6; // 6.66 seconds
        }

        if (shadowflameDuration > 0)
        {
            Player.AddBuff(BuffID.ShadowFlame, shadowflameDuration);
        }
    }

    public override void UpdateBadLifeRegen()
    {
        if (Player.HasBuff(BuffID.ShadowFlame))
        {
            if (Player.lifeRegen > 0)
            {
                Player.lifeRegen = 0;
            }
            Player.lifeRegenTime = 0;

            Player.lifeRegen -= 24; // 12 dps
        }
    }

    public override void PostUpdateBuffs()
    {
        if (Player.HasBuff(BuffID.ShadowFlame))
        {
            for (int i = 0; i < 2; i++)
            {
                Vector2 dustPosition = Player.position + new Vector2(Main.rand.Next(Player.width), Main.rand.Next(Player.height));
                Vector2 dustVelocity = new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-2f, 0f));

                Dust dust = Dust.NewDustDirect(dustPosition, 0, 0, DustID.Shadowflame, dustVelocity.X, dustVelocity.Y, 100, default(Color), Main.rand.NextFloat(1f, 1.5f));
                dust.noGravity = true;
                dust.fadeIn = 1.2f;
            }

            if (Main.rand.NextBool(3))
            {
                Vector2 smokePos = Player.position + new Vector2(Main.rand.Next(Player.width), Main.rand.Next(Player.height));
                Dust smoke = Dust.NewDustDirect(smokePos, 0, 0, DustID.Smoke, 0, -1f, 100, Color.Purple, 0.8f);
                smoke.noGravity = true;
            }
        }
    }
}
public class QuiverNerfPlayer : ModPlayer
{
    public bool hasQuiver = false;

    public override void ResetEffects()
    {
        hasQuiver = false;
    }

    public override void PostUpdateEquips()
    {
        for (int i = 3; i < 8 + Player.GetAmountOfExtraAccessorySlotsToShow(); i++)
        {
            if (Player.armor[i].type == ItemID.MagicQuiver || Player.armor[i].type == ItemID.MoltenQuiver)
            {
                hasQuiver = true;
                break;
            }
        }
    }
}
public class HittheChytty : ModPlayer
{
    public override void OnRespawn()
    {
        bool hasCharmOfMyths = false;

        for (int i = 3; i < 8 + Player.GetAmountOfExtraAccessorySlotsToShow(); i++)
        {
            if (Player.armor[i].type == ItemID.CharmofMyths || Player.armor[i].type == ItemID.PhilosophersStone)
            {
                hasCharmOfMyths = true;
                break;
            }
        }

        if (hasCharmOfMyths && !Player.HasBuff<UncouthandBoring>())
        {
            Player.statLife = Player.statLifeMax2;
        }
    }
}

public class ShinyStoneHotswap : ModBuff
{
    public override string Texture => $"PvPAdventure/Assets/Buff/ShinyStoneHotswap";

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = true;
        Main.buffNoTimeDisplay[Type] = false;
    }
}
public class NewIchorPlayerDebuff : ModBuff
{
    public override string Texture => $"PvPAdventure/Assets/Buff/NewIchorPlayerDebuff";

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = false;
        Main.buffNoTimeDisplay[Type] = false;
        Main.persistentBuff[Type] = false;
    }
}
public class BROISACHOJ : ModBuff
{
    public override string Texture => $"PvPAdventure/Assets/Buff/BROISACHOJ";

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = false;
        Main.buffNoTimeDisplay[Type] = false;
        Main.persistentBuff[Type] = true;
    }
}

public class ConsumableShadowKeyPlayer : ModPlayer
{
    private Dictionary<Point, int> trackedLockedChests = new Dictionary<Point, int>();

    public override void PostUpdate()
    {
        // Only track and consume on the client that owns this player
        if (Player.whoAmI != Main.myPlayer)
            return;

        // Scan every frame for consistency
        int scanRange = 10;
        int playerTileX = (int)(Player.Center.X / 16);
        int playerTileY = (int)(Player.Center.Y / 16);

        HashSet<Point> foundLockedChests = new HashSet<Point>();

        for (int i = playerTileX - scanRange; i < playerTileX + scanRange; i++)
        {
            for (int j = playerTileY - scanRange; j < playerTileY + scanRange; j++)
            {
                if (!WorldGen.InWorld(i, j))
                    continue;

                Tile tile = Main.tile[i, j];

                if (tile != null && tile.TileType == TileID.Containers)
                {
                    int left = i - (tile.TileFrameX % 36) / 18;
                    int top = j - (tile.TileFrameY % 36) / 18;
                    Point chestPos = new Point(left, top);

                    if (foundLockedChests.Contains(chestPos))
                        continue;

                    Tile topLeftTile = Main.tile[left, top];
                    int frameX = topLeftTile.TileFrameX;

                    if (frameX == 144)
                    {
                        foundLockedChests.Add(chestPos);

                        if (!trackedLockedChests.ContainsKey(chestPos))
                        {
                            trackedLockedChests.Add(chestPos, (int)Main.GameUpdateCount);
                        }
                    }
                }
            }
        }

        // Check if any tracked chests are now unlocked
        List<Point> chestsToRemove = new List<Point>();
        foreach (var kvp in trackedLockedChests)
        {
            Point chestPos = kvp.Key;

            if (!foundLockedChests.Contains(chestPos))
            {
                if (WorldGen.InWorld(chestPos.X, chestPos.Y))
                {
                    float distance = Vector2.Distance(new Vector2(chestPos.X * 16, chestPos.Y * 16), Player.Center);

                    // Only consume key if chest is within interaction range
                    // Terraria's tile interaction range is about 6.5 tiles (104 pixels)
                    if (distance <= 104)
                    {
                        // Consume Shadow Key - only players within interaction range could have unlocked it
                        if (Player.HasItem(ItemID.ShadowKey))
                        {
                            Player.ConsumeItem(ItemID.ShadowKey);
                        }
                    }
                }

                chestsToRemove.Add(chestPos);
            }
        }

        // Remove processed chests from tracking
        foreach (Point pos in chestsToRemove)
        {
            trackedLockedChests.Remove(pos);
        }

        // Clean up chests that are far away
        List<Point> distantChests = new List<Point>();
        foreach (var kvp in trackedLockedChests)
        {
            float distance = Vector2.Distance(new Vector2(kvp.Key.X * 16, kvp.Key.Y * 16), Player.Center);
            if (distance > scanRange * 16 * 1.5f)
            {
                distantChests.Add(kvp.Key);
            }
        }
        foreach (Point pos in distantChests)
        {
            trackedLockedChests.Remove(pos);
        }
    }
}
public class PlayerInSpawn : ModBuff
{
    public override string Texture => $"PvPAdventure/Assets/Buff/PlayerInSpawn";

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = false;
        Main.buffNoTimeDisplay[Type] = true;
        Main.persistentBuff[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        player.GetDamage(DamageClass.Generic) *= -999f;

    }
}

public class ShatteredArmor : ModBuff
{
    public override string Texture => $"PvPAdventure/Assets/Buff/ShatteredArmor";

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = false;
        Main.buffNoTimeDisplay[Type] = false;
        Main.persistentBuff[Type] = false;
        BuffID.Sets.IsATagBuff[Type] = false;
    }
    public override void Update(Player player, ref int buffIndex)
    { }
}

public class Anathema : ModBuff
{
    public override string Texture => $"PvPAdventure/Assets/Buff/Anathema";

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = false;
        Main.buffNoTimeDisplay[Type] = false;
        Main.persistentBuff[Type] = false;
    }
}
public class Hellhex : ModBuff
{
    public override string Texture => $"PvPAdventure/Assets/Buff/Hellhex";

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = false;
        Main.buffNoTimeDisplay[Type] = false;
        Main.persistentBuff[Type] = false;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        // Visual effects and buff management handled in ModPlayer
    }
}
public class PressurePoints : ModBuff
{
    public override string Texture => $"PvPAdventure/Assets/Buff/PressurePoints";

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = false;
        Main.buffNoTimeDisplay[Type] = false;
        Main.persistentBuff[Type] = false;
    }
}
public class BrittleBones : ModBuff
{
    public override string Texture => $"PvPAdventure/Assets/Buff/BrittleBones";

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = false;
        Main.buffNoTimeDisplay[Type] = false;
        Main.persistentBuff[Type] = false;
    }
}
public class BitingEmbrace : ModBuff
{
    public override string Texture => $"PvPAdventure/Assets/Buff/BitingEmbrace";

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = false;
        Main.buffNoTimeDisplay[Type] = false;
        Main.persistentBuff[Type] = false;
    }
}
public class Marked : ModBuff
{
    public override string Texture => $"PvPAdventure/Assets/Buff/Marked";

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = false;
        Main.buffNoTimeDisplay[Type] = false;
        Main.persistentBuff[Type] = false;
    }
}
public class Attuning : ModBuff
{
    public override string Texture => $"PvPAdventure/Assets/Buff/Attuning";

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = false;
        Main.buffNoTimeDisplay[Type] = false;
        Main.persistentBuff[Type] = true;
    }
}
public class UncouthandBoring : ModBuff
{
    public override string Texture => $"PvPAdventure/Assets/Buff/uncouthandboring";

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = false;
        Main.buffNoTimeDisplay[Type] = false;
        Main.persistentBuff[Type] = true;
    }
}