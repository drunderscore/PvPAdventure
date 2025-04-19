using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord.Rest;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
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


namespace PvPAdventure;

public class AdventurePlayer : ModPlayer
{
    public RestSelfUser DiscordUser => _discordClient?.CurrentUser;
    public DamageInfo RecentDamageFromPlayer { get; private set; }
    public int Kills { get; private set; }
    public int Deaths { get; private set; }

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
        NPCID.GolemHeadFree
    ];

    // Intentionally zero-initialize this so we get a ping/pong ASAP.
    private int _nextPingPongTime;
    private int _pingPongCanary;
    private Stopwatch _pingPongStopwatch;
    public TimeSpan? Latency { get; private set; }
    public int PvPImmuneTime { get; private set; }

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
        var adventureConfig = ModContent.GetInstance<AdventureConfig>();
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
            ModContent.GetInstance<AdventureConfig>().Combat.MeleeInvincibilityFrames;

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

        if (PvPImmuneTime > 0)
            PvPImmuneTime--;
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
            ModContent.GetInstance<AdventureConfig>().Combat.RecentDamagePreservationFrames);
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

    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        // Only play kill markers on clients that we hurt that aren't ourselves
        if (!Main.dedServ && pvp && damageSource.SourcePlayerIndex == Main.myPlayer && Player.whoAmI != Main.myPlayer)
            PlayKillMarker((int)damage);

        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        try
        {
            Player killer = null;

            // If you killed yourself, we should delegate to the recent damage.
            if (pvp && damageSource.SourcePlayerIndex != -1 && damageSource.SourcePlayerIndex != Player.whoAmI)
            {
                killer = Main.player[damageSource.SourcePlayerIndex];
            }
            else
            {
                // We checked this earlier, but let's check again for logging purposes.
                if (pvp && damageSource.SourcePlayerIndex == -1)
                    Mod.Logger.Warn($"PvP kill without a valid SourcePlayerIndex ({this} killed)");

                if (RecentDamageFromPlayer != null)
                    killer = Main.player[RecentDamageFromPlayer.Who];
            }

            // Nothing should happen for suicide
            if (killer == null || !killer.active || killer.whoAmI == Player.whoAmI)
                return;

            ModContent.GetInstance<PointsManager>().AwardPlayerKillToTeam(killer, Player);
            killer.GetModPlayer<AdventurePlayer>().Kills += 1;
            killer.GetModPlayer<AdventurePlayer>().SyncStatistics();

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
        if (!modifiers.PvP)
            return;

        var adventureConfig = ModContent.GetInstance<AdventureConfig>();
        var playerDamageBalance = adventureConfig.Combat.PlayerDamageBalance;

        var sourcePlayer = Main.player[modifiers.DamageSource.SourcePlayerIndex];
        var tileDistance = Player.Distance(sourcePlayer.position) / 16.0f;

        var hasIncurredFalloff = false;

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
        }

        if (!hasIncurredFalloff && playerDamageBalance.DefaultFalloff != null)
            modifiers.IncomingDamageMultiplier *= playerDamageBalance.DefaultFalloff.CalculateMultiplier(tileDistance);
    }

    public override void OnHurt(Player.HurtInfo info)
    {
        if (!info.PvP)
            return;

        var adventureConfig = ModContent.GetInstance<AdventureConfig>();

        // Only play hit markers on clients that we hurt that aren't ourselves
        if (!Main.dedServ && Player.whoAmI != Main.myPlayer && info.DamageSource.SourcePlayerIndex == Main.myPlayer)
            PlayHitMarker(info.Damage);

        if (info.CooldownCounter == CombatManager.PvPImmunityCooldownId &&
            adventureConfig.Combat.MeleeInvincibilityFrames == 0)
            PvPImmuneTime = adventureConfig.Combat.StandardInvincibilityFrames;
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
        if (ModContent.GetInstance<AdventureConfig>().PreventAutoReuse.Contains(new(item.type)))
            return false;

        return null;
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
                    caller.Reply($"{player.name}: {ping.Value.Milliseconds}ms");
            }
        }

        public override string Command => "ping";
        public override CommandType Type => CommandType.Console;
    }

    public override string ToString()
    {
        return $"{Player.whoAmI}/{Player.name}/{DiscordUser?.Id}";
    }

    private bool hadShinyStoneLastFrame;

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

        hadShinyStoneLastFrame = hasShinyStone;

        // Check if wearing full Tiki Armor
        if (Player.armor[0].type == ItemID.TikiMask &&
            Player.armor[1].type == ItemID.TikiShirt &&
            Player.armor[2].type == ItemID.TikiPants)
        {
            Player.maxMinions += 1;
            Player.noKnockback = true;
        }
    
    }

    public override void OnRespawn()
    {
        // Re-apply debuff if equipped during respawn
        if (IsShinyStoneEquipped())
        {
            Player.AddBuff(ModContent.BuffType<ShinyStoneHotswap>(), 900);
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
    //used for ForceNoAutoFire 
    public bool PreviousUseItem { get; set; }
    public override void PostUpdate()
    {
        // Save the previous state of "UseItem" (left-click)
        PreviousUseItem = Player.controlUseItem;
    }
}
