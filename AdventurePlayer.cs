using System;
using System.IO;
using System.Threading.Tasks;
using Discord.Rest;
using Microsoft.Xna.Framework;
using PvPAdventure.System;
using PvPAdventure.System.Client;
using PvPAdventure.System.Client.Interface;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PvPAdventure;

public class AdventurePlayer : ModPlayer
{
    public RestSelfUser DiscordUser => _discordClient?.CurrentUser;
    public DamageInfo RecentDamageFromPlayer { get; private set; }
    public int Kills { get; private set; }
    public int Deaths { get; private set; }
    private readonly int[] _playerMeleeInvincibleTime = new int[Main.maxPlayers];

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
    }

    private void OnPlayerPlaceThing_Tiles(On_Player.orig_PlaceThing_Tiles orig, Player self)
    {
        if (Player.tileTargetX < 3200)
            orig(self);
    }

    private void OnPlayerPlaceThing_Walls(On_Player.orig_PlaceThing_Walls orig, Player self)
    {
        if (Player.tileTargetX < 3200)
            orig(self);
    }

    private void OnPlayerItemCheck_UseMiningTools(On_Player.orig_ItemCheck_UseMiningTools orig, Player self, Item sitem)
    {
        if (Player.tileTargetX < 3200)
            orig(self, sitem);
    }

    private void OnPlayerItemCheck_UseTeleportRod(On_Player.orig_ItemCheck_UseTeleportRod orig, Player self, Item sitem)
    {
        if (Player.tileTargetX < 3200)
            orig(self, sitem);
    }

    private void OnPlayerItemCheck_UseWiringTools(On_Player.orig_ItemCheck_UseWiringTools orig, Player self, Item sitem)
    {
        if (Player.tileTargetX < 3200)
            orig(self, sitem);
    }

    private void OnPlayerItemCheck_CutTiles(On_Player.orig_ItemCheck_CutTiles orig, Player self, Item sitem,
        Rectangle itemrectangle, bool[] shouldignore)
    {
        if (Player.tileTargetX < 3200)
            orig(self, sitem, itemrectangle, shouldignore);
    }

    public override bool CanHitPvp(Item item, Player target)
    {
        if (_playerMeleeInvincibleTime[target.whoAmI] > 0)
            return false;

        _playerMeleeInvincibleTime[target.whoAmI] = 8;

        return true;
    }

    public override void PreUpdate()
    {
        for (var i = 0; i < _playerMeleeInvincibleTime.Length; i++)
        {
            if (_playerMeleeInvincibleTime[i] > 0)
                _playerMeleeInvincibleTime[i]--;
        }

        if (RecentDamageFromPlayer != null && --RecentDamageFromPlayer.TicksRemaining <= 0)
        {
            Mod.Logger.Info($"Recent damage for {this} expired (was from {RecentDamageFromPlayer.Who})");
            RecentDamageFromPlayer = null;
        }
    }

    private bool CanRecall()
    {
        return Player.lifeRegen >= 0.0 && !Player.controlLeft && !Player.controlRight && !Player.controlUp &&
               !Player.controlDown && Player.velocity == Vector2.Zero;
    }

    public override void PostUpdate()
    {
        // FIXME: this might be better off in preupdate
        if (AdventureItem.RecallItems[Player.inventory[Player.selectedItem].type] && !CanRecall())
        {
            Player.SetItemAnimation(0);
            Player.SetItemTime(0);
        }
    }

    public override bool CanUseItem(Item item)
    {
        // Prevent a recall from being started at all for these conditions.
        if (AdventureItem.RecallItems[item.type])
        {
            if (CanRecall())
                return true;

            if (Main.netMode != NetmodeID.Server && Player.whoAmI == Main.myPlayer)
                PopupText.NewText(new AdvancedPopupRequest
                {
                    Color = Color.Crimson,
                    Text = "Cannot recall!",
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

        // FIXME: Configurable number here! 15 seconds right now.
        RecentDamageFromPlayer = new((byte)damagerPlayer.whoAmI, 15 * 60);
    }

    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        try
        {
            Player killer = null;

            // This was a PvP death, so we should have a SourcePlayerIndex -- this is only a sanity check.
            if (pvp && damageSource.SourcePlayerIndex != -1)
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

            // FIXME: probably active check
            if (killer != null)
            {
                // Nothing should happen for suicide
                if (killer.whoAmI == Player.whoAmI)
                    return;

                ModContent.GetInstance<PointsManager>().AwardPlayerKillToTeam((Team)killer.team, Player);
                killer.GetModPlayer<AdventurePlayer>().Kills += 1;
                killer.GetModPlayer<AdventurePlayer>().SyncStatistics();

                Deaths += 1;
                SyncStatistics();
            }
        }
        finally
        {
            // PvP or not, reset whom we last took damage from.
            RecentDamageFromPlayer = null;
        }
    }

    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        var scoreboard = ModContent.GetInstance<Scoreboard>();

        if (ModContent.GetInstance<Keybinds>().Scoreboard.JustPressed)
        {
            scoreboard.Visible = true;
            Main.InGameUI.SetState(ModContent.GetInstance<Scoreboard>().UiScoreboard);
            // Main.InGameUI.SetState(ModContent.GetInstance<BountyManager>().UiBountyShop);
            // ModContent.GetInstance<ObjectiveNotice>()
            // .AddPlayerDeathNotice(Main.player[0], Main.player[0], Main.player[0].HeldItem);
            // ModContent.GetInstance<ObjectiveNotice>().AddBossDeathNotice((Team)Main.player[0].team,
            // Main.player[0].HeldItem, Main.npc.First(npc => npc.active));
            // ModContent.GetInstance<ObjectiveNotice>().AddClaimReceivedNotice((Team)Main.player[0].team);
        }
        else if (ModContent.GetInstance<Keybinds>().Scoreboard.JustReleased)
        {
            scoreboard.Visible = false;
            Main.InGameUI.SetState(null);
        }
    }

    private void SyncStatistics(int to = -1, int ignore = -1)
    {
        var packet = Mod.GetPacket();
        // FIXME: no magic
        packet.Write((byte)1);
        new Statistics((byte)Player.whoAmI, Kills, Deaths).Serialize(packet);
        packet.Send(to, ignore);
    }

    public override void SaveData(TagCompound tag)
    {
        tag["kills"] = Kills;
        tag["deaths"] = Deaths;
    }

    public override void LoadData(TagCompound tag)
    {
        Kills = tag.Get<int>("kills");
        Deaths = tag.Get<int>("deaths");
    }

    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
    {
        SyncStatistics(toWho, fromWho);
    }

    public override string ToString()
    {
        return $"{Player.whoAmI}/{Player.name}/{DiscordUser?.Id}";
    }
}