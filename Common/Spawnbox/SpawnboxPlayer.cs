using Microsoft.Xna.Framework;
using PvPAdventure.Common.Items;
using PvPAdventure.Core.Utilities;
using System;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using static PvPAdventure.Common.Spawnbox.RegionManager;

namespace PvPAdventure.Common.Spawnbox;

internal class SpawnboxPlayer : ModPlayer
{
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

    public override void PostUpdateMiscEffects()
    {
        // Apply player in spawn buff.
        int playerTileX = (int)(Player.position.X / 16f);
        int playerTileY = (int)(Player.position.Y / 16f);

        int spawnTileX = Main.spawnTileX;
        int spawnTileY = Main.spawnTileY;

        int distanceX = Math.Abs(playerTileX - spawnTileX);
        int distanceY = Math.Abs(playerTileY - spawnTileY);

        if (distanceX <= 25 && distanceY <= 25)
        {
            Player.AddBuff(ModContent.BuffType<Content.Buffs.PlayerInSpawn>(), 2);
        }
        else
        {
        }
    }

    //public override bool CanUseItem(Item item)
    //{
    //    // Prevent a recall from being started at all for these conditions.
    //    if (ItemBalance.RecallItems[item.type])
    //    {
    //        if (CanRecall())
    //            return true;

    //        if (!Main.dedServ && Player.whoAmI == Main.myPlayer)
    //            PopupText.NewText(new AdvancedPopupRequest
    //            {
    //                Color = Color.Crimson,
    //                Text = Language.GetTextValue("Mods.PvPAdventure.Player.CannotRecall"),
    //                Velocity = new(0.0f, -4.0f),
    //                DurationInFrames = 60 * 2
    //            }, Player.Top);

    //        return false;
    //    }

    //    return true;
    //}

    public override bool CanHitPvp(Item item, Player target)
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

    public override void PostHurt(Player.HurtInfo info)
    {
        //if (ItemBalance.RecallItems[Player.inventory[Player.selectedItem].type])
        //{
        //    Player.SetItemAnimation(0);
        //    Player.SetItemTime(0);
        //}
    }

    private static bool CanModifyAtTileTarget()
    {
        RegionManager Regions = ModContent.GetInstance<RegionManager>();
        var region = Regions.GetRegionContaining(new(Player.tileTargetX, Player.tileTargetY));
        return region == null || region.CanModifyTiles;
    }

    private void OnPlayerPlaceThing_Tiles(On_Player.orig_PlaceThing_Tiles orig, Player self)
    {
        // debug: always allow
#if DEBUG
        //orig(self);
#endif

        if (CanModifyAtTileTarget())
            orig(self);
    }

    private void OnPlayerItemCheck_UseMiningTools(On_Player.orig_ItemCheck_UseMiningTools orig, Player self, Item sitem)
    {
        // debug: always allow
#if DEBUG
        //orig(self, sitem);
#endif

        if (CanModifyAtTileTarget())
            orig(self, sitem);
    }

    private void OnPlayerPlaceThing_Walls(On_Player.orig_PlaceThing_Walls orig, Player self)
    {
        if (CanModifyAtTileTarget())
            orig(self);
    }

    private void OnPlayerItemCheck_UseTeleportRod(On_Player.orig_ItemCheck_UseTeleportRod orig, Player self, Item sitem)
    {
        if (CanModifyAtTileTarget())
            orig(self, sitem);
    }

    private void OnPlayerItemCheck_UseWiringTools(On_Player.orig_ItemCheck_UseWiringTools orig, Player self, Item sitem)
    {
        if (CanModifyAtTileTarget())
            orig(self, sitem);
    }

    private void OnPlayerItemCheck_CutTiles(On_Player.orig_ItemCheck_CutTiles orig, Player self, Item sitem,
        Rectangle itemrectangle, bool[] shouldignore)
    {
        var region = ModContent.GetInstance<RegionManager>().GetRegionIntersecting(itemrectangle.ToTileRectangle());

        if (region == null || region.CanModifyTiles)
            orig(self, sitem, itemrectangle, shouldignore);
    }
}
