using Terraria;
using Terraria.ID;
using Terraria.Enums;
using Terraria.Audio;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;
using Terraria.WorldBuilding;
using Terraria.IO;
using System.Collections.Generic;
using Terraria.GameContent.Generation;

namespace PvPAdventure;

public class LockedLihzahrdChest : ModTile
{
    public override string Texture => "PvPAdventure/Assets/Tiles/LockedLihzahrdChest";

    public override void Load()
    {
        IL_Player.TileInteractionsUse += EditPlayerTileInteractionsUse;
        On_WorldGen.templePart2 += AddExtraChestsDuringTempleGen;
    }

    public override void Unload()
    {
        IL_Player.TileInteractionsUse -= EditPlayerTileInteractionsUse;
        On_WorldGen.templePart2 -= AddExtraChestsDuringTempleGen;
    }

    private void AddExtraChestsDuringTempleGen(On_WorldGen.orig_templePart2 orig)
    {
        // First run the original temple generation (which places vanilla chests)
        orig();

        // Then add our extra chests
        AddExtraTempleChests();
    }

    private void AddExtraTempleChests()
    {
        if (GenVars.tLeft == 0 || GenVars.tRight == 0)
        {
            ModContent.GetInstance<PvPAdventure>().Logger.Info("Temple bounds not set, skipping extra chests");
            return;
        }

        int tLeft = GenVars.tLeft;
        int tRight = GenVars.tRight;
        int tTop = GenVars.tTop;
        int tBottom = GenVars.tBottom;
        int tRooms = GenVars.tRooms;

        ModContent.GetInstance<PvPAdventure>().Logger.Info($"Adding extra chests to temple: {tLeft},{tTop} to {tRight},{tBottom}, rooms: {tRooms}");

        int extraChests = tRooms * 30; // Reasonable amount
        int chestsPlaced = 0;

        // Place locked chests directly, just like the vanilla temple generation does
        for (int i = 0; i < extraChests; i++)
        {
            int attempts = 0;
            bool placed = false;

            while (!placed && attempts < 1000)
            {
                int x = WorldGen.genRand.Next(tLeft + 5, tRight - 5);
                int y = WorldGen.genRand.Next(tTop + 5, tBottom - 5);

                if (WorldGen.InWorld(x, y) && WorldGen.InWorld(x + 1, y + 1))
                {
                    Tile tile = Main.tile[x, y];
                    Tile tileRight = Main.tile[x + 1, y];
                    Tile tileBelow = Main.tile[x, y + 1];
                    Tile tileBelowRight = Main.tile[x + 1, y + 1];

                    // Check if location is suitable: temple wall, empty space, solid floor below
                    if (tile.WallType == 87 && !tile.HasTile &&
                        tileRight.WallType == 87 && !tileRight.HasTile &&
                        tileBelow.HasTile && Main.tileSolid[tileBelow.TileType] &&
                        tileBelowRight.HasTile && Main.tileSolid[tileBelowRight.TileType])
                    {
                        // Check for nearby chests
                        bool chestNearby = false;
                        for (int checkX = x - 3; checkX <= x + 3; checkX++)
                        {
                            for (int checkY = y - 3; checkY <= y + 3; checkY++)
                            {
                                if (WorldGen.InWorld(checkX, checkY))
                                {
                                    Tile checkTile = Main.tile[checkX, checkY];
                                    if (checkTile.HasTile && (checkTile.TileType == TileID.Containers ||
                                        checkTile.TileType == ModContent.TileType<LockedLihzahrdChest>()))
                                    {
                                        chestNearby = true;
                                        break;
                                    }
                                }
                            }
                            if (chestNearby) break;
                        }

                        if (!chestNearby)
                        {
                            // Place locked chest directly using WorldGen.PlaceChest
                            int chestIndex = WorldGen.PlaceChest(x, y, (ushort)ModContent.TileType<LockedLihzahrdChest>(), false, 0);

                            if (chestIndex != -1)
                            {
                                // Manually populate with temple loot based on vanilla temple chest contents
                                Chest chest = Main.chest[chestIndex];
                                if (chest != null)
                                {
                                    int slot = 0;

                                    // Always include Lihzahrd Power Cell
                                    chest.item[slot].SetDefaults(ItemID.LihzahrdPowerCell);
                                    chest.item[slot].stack = 3;
                                    slot++;

                                    // Always include Lihzahrd Furnace
                                    chest.item[slot].SetDefaults(ItemID.LihzahrdFurnace);
                                    chest.item[slot].stack = 12;
                                    slot++;

                                    // 33% chance for Solar Tablet, otherwise Solar Tablet Fragments
                                    if (WorldGen.genRand.NextBool(3))
                                    {
                                        chest.item[slot].SetDefaults(ItemID.SolarTablet);
                                        chest.item[slot].stack = 1;
                                    }
                                    else
                                    {
                                        chest.item[slot].SetDefaults(ItemID.LunarTabletFragment);
                                        chest.item[slot].stack = WorldGen.genRand.Next(4, 9);
                                    }
                                    slot++;

                                    // Add some typical Gold Chest loot (coins, potions, etc)
                                    if (WorldGen.genRand.NextBool(2))
                                    {
                                        chest.item[slot].SetDefaults(ItemID.GoldCoin);
                                        chest.item[slot].stack = WorldGen.genRand.Next(3, 8);
                                        slot++;
                                    }

                                    if (WorldGen.genRand.NextBool(2))
                                    {
                                        chest.item[slot].SetDefaults(ItemID.HealingPotion);
                                        chest.item[slot].stack = WorldGen.genRand.Next(2, 5);
                                        slot++;
                                    }

                                    if (WorldGen.genRand.NextBool(3))
                                    {
                                        chest.item[slot].SetDefaults(ItemID.Torch);
                                        chest.item[slot].stack = WorldGen.genRand.Next(15, 30);
                                        slot++;
                                    }

                                    placed = true;
                                    chestsPlaced++;
                                }
                            }
                        }
                    }
                }
                attempts++;
            }
        }

        ModContent.GetInstance<PvPAdventure>().Logger.Info($"Placed {chestsPlaced} extra locked temple chests");
    }

    public override void SetStaticDefaults()
    {
        Main.tileSpelunker[Type] = true;
        Main.tileContainer[Type] = true;
        Main.tileFrameImportant[Type] = true;
        Main.tileNoAttach[Type] = true;
        Main.tileOreFinderPriority[Type] = 500;
        TileID.Sets.HasOutlines[Type] = true;
        TileID.Sets.BasicChest[Type] = true;
        TileID.Sets.DisableSmartCursor[Type] = true;
        TileID.Sets.AvoidedByNPCs[Type] = true;
        TileID.Sets.InteractibleByNPCs[Type] = true;
        TileID.Sets.IsAContainer[Type] = true;
        TileID.Sets.FriendlyFairyCanLureTo[Type] = true;
        TileID.Sets.GeneralPlacementTiles[Type] = false;

        DustType = DustID.t_Lihzahrd;
        AdjTiles = [TileID.Containers];
        RegisterItemDrop(ItemID.LihzahrdChest);

        AddMapEntry(new Color(174, 129, 92), this.GetLocalization("MapEntry"));

        TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
        TileObjectData.newTile.Origin = new Point16(0, 1);
        TileObjectData.newTile.CoordinateHeights = [16, 18];
        TileObjectData.newTile.HookCheckIfCanPlace = new PlacementHook(Chest.FindEmptyChest, -1, 0, true);
        TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(Chest.AfterPlacement_Hook, -1, 0, false);
        TileObjectData.newTile.AnchorInvalidTiles =
        [
            TileID.MagicalIceBlock,
            TileID.Boulder,
            TileID.BouncyBoulder,
            TileID.LifeCrystalBoulder,
            TileID.RollingCactus
        ];
        TileObjectData.newTile.StyleHorizontal = true;
        TileObjectData.newTile.LavaDeath = false;
        TileObjectData.newTile.AnchorBottom = new AnchorData(
            AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
        TileObjectData.addTile(Type);

        IL_WorldGen.templePart2 += EditWorldGentemplePart2;
    }

    private void EditWorldGentemplePart2(ILContext il)
    {
        var cursor = new ILCursor(il);

        // Find the call to WorldGen.AddBuriedChest...
        cursor.GotoNext(i => i.MatchCall<WorldGen>("AddBuriedChest"));
        // ...and go back to the Style argument...
        cursor.Index -= 3;
        // ...to remove it...
        cursor.Remove();
        // ...and replace it with 0.
        cursor.Emit(OpCodes.Ldc_I4_0);
        // Advance to the chestTileType argument...
        cursor.Index += 1;
        // ...to remove it...
        cursor.Remove();
        // ...and replace it with our tile.
        cursor.EmitLdcI4(ModContent.TileType<LockedLihzahrdChest>());
    }

    private void EditPlayerTileInteractionsUse(ILContext il)
    {
        var c = new ILCursor(il);
        c.GotoNext(i => i.MatchCall<WorldGen>("UnlockDoor"));
        c.GotoPrev(i => i.MatchDup());
        c.Index -= 4;
        c.RemoveRange(9);

        c.GotoNext(i => i.MatchCall<WorldGen>("UnlockDoor"));
        c.GotoNext(i => i.MatchCall<WorldGen>("UnlockDoor"));
        c.GotoPrev(i => i.MatchDup());
        c.Index -= 5;
        c.RemoveRange(10);
    }

    public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;
    public override bool IsLockedChest(int i, int j) => true;
    public override void KillMultiTile(int i, int j, int frameX, int frameY) => Chest.DestroyChest(i, j);

    public override bool RightClick(int i, int j)
    {
        var player = Main.LocalPlayer;
        Main.mouseRightRelease = false;

        if (!player.ConsumeItem(ItemID.TempleKey, includeVoidBag: true))
            return false;

        var left = i;
        var top = j;

        if (Main.tile[i, j].TileFrameX != 0)
            left--;

        if (Main.tile[i, j].TileFrameY != 0)
            top--;

        player.CloseSign();
        player.SetTalkNPC(-1);
        Main.npcChatCornerItem = ItemID.None;
        Main.npcChatText = "";
        SoundEngine.PlaySound(SoundID.Unlock, new Vector2(left, top).ToWorldCoordinates());

        for (var x = left; x <= left + 1; x++)
        {
            for (var y = top; y <= top + 1; y++)
            {
                for (var dustIndex = 0; dustIndex < 4; dustIndex++)
                    Dust.NewDust(new Vector2(x, y).ToWorldCoordinates(), 16, 16, DustID.Gold);

                Main.tile[x, y].TileType = TileID.Containers;

                if (x == left)
                    Main.tile[x, y].TileFrameX = 576;
                else if (x == left + 1)
                    Main.tile[x, y].TileFrameX = 594;

                if (top == y + 1)
                    Main.tile[x, y].TileFrameY = 18;
            }
        }

        if (Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.SendTileSquare(-1, left, top, 2, 2);

        return true;
    }

    public override void MouseOver(int i, int j)
    {
        var player = Main.LocalPlayer;

        player.cursorItemIconID = ItemID.TempleKey;
        player.cursorItemIconEnabled = true;
        player.cursorItemIconText = "";
        player.noThrow = 2;
    }

    public override void MouseOverFar(int i, int j)
    {
        var player = Main.LocalPlayer;
        MouseOver(i, j);

        if (player.cursorItemIconText == "")
        {
            player.cursorItemIconEnabled = false;
            player.cursorItemIconID = ItemID.None;
        }
    }
}