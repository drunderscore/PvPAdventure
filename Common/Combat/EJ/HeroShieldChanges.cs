using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PvPFramework.Common.World.Outlines.ItemOutlines;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat.EJ;

/// <summary>
/// When wearing the Hero Shield, 10 no-gravity hearts are created upon the death of the wearer that only players on the same team as the wearer can pick up, using the BossDropItem system
/// </summary>

public class HeroShieldDeathHeartsPlayer : ModPlayer
{
    private const int HeartCount = 10;
    private const float ScatterSpeed = 4f;

    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        if (!IsHeroShieldEquipped())
            return;

        Team team = (Team)Player.team;

        for (int i = 0; i < HeartCount; i++)
        {
            int idx = Item.NewItem(Player.GetSource_Death(), Player.Hitbox, ItemID.Heart, 1, noBroadcast: false, prefixGiven: -1);
            if (idx < 0 || idx >= Main.maxItems)
                continue;

            Item item = Main.item[idx];
            item.GetGlobalItem<BossDropItem>()._team = team;

            float angle = MathHelper.TwoPi * i / HeartCount;
            item.velocity = angle.ToRotationVector2() * Main.rand.NextFloat(1.5f, ScatterSpeed);
            item.velocity.Y -= Main.rand.NextFloat(1f, 3f);
        }
    }

    private bool IsHeroShieldEquipped()
    {
        for (int i = 3; i < 10; i++)
        {
            if (Player.armor[i].type == ItemID.HeroShield && (i < 7 || !Player.hideVisibleAccessory[i - 3]))
                return true;
        }
        return false;
    }
}

internal sealed class TeamHeartPickupSystem : ModSystem
{
    private const int GrabDelayTicks = 45;
    private static readonly Vector2 HiddenPosition = new(-10000f, -10000f);

    public override void Load() => On_Player.GrabItems += SuppressWrongTeamHearts;
    public override void Unload() => On_Player.GrabItems -= SuppressWrongTeamHearts;

    private void SuppressWrongTeamHearts(On_Player.orig_GrabItems orig, Player self, int i)
    {
        Team playerTeam = (Team)self.team;
        List<(int index, Vector2 originalPos)> hidden = null;

        for (int j = 0; j < Main.maxItems; j++)
        {
            Item item = Main.item[j];
            if (!item.active || item.type != ItemID.Heart)
                continue;

            Team? itemTeam = item.GetGlobalItem<BossDropItem>()._team;
            if (!itemTeam.HasValue || itemTeam.Value == Team.None)
                continue;

            bool wrongTeam = itemTeam.Value != playerTeam;
            bool tooFresh = item.timeSinceItemSpawned < GrabDelayTicks;
            if (!wrongTeam && !tooFresh)
                continue;

            (hidden ??= new List<(int, Vector2)>()).Add((j, item.position));
            item.position = HiddenPosition;
        }

        orig(self, i);

        if (hidden != null)
        {
            foreach (var (j, originalPos) in hidden)
                Main.item[j].position = originalPos;
        }
    }
}
public class HeroShieldHeartEffects : GlobalItem
{
    private const int LifetimeTicks = 18000;

    public override void Update(Item item, ref float gravity, ref float maxFallSpeed)
    {
        if (!IsOurHeart(item))
            return;

        gravity = 0f;
        maxFallSpeed = 0f;
    }

    public override void PostUpdate(Item item)
    {
        if (!IsOurHeart(item))
            return;
        item.velocity *= 0.9f;

        if (item.timeSinceItemSpawned >= LifetimeTicks)
        {
            item.active = false;
            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.SyncItem, number: item.whoAmI);
        }
    }

    private static bool IsOurHeart(Item item)
    {
        if (item.type != ItemID.Heart)
            return false;

        Team? team = item.GetGlobalItem<BossDropItem>()._team;
        return team.HasValue && team.Value != Team.None;
    }
}