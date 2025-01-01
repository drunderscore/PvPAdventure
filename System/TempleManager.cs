using System;
using Terraria;
using Terraria.ID;
using MonoMod.Cil;
using Terraria.Enums;
using Terraria.Audio;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;

namespace PvPAdventure.System;

public class TempleManager : ModSystem
{
    public override void Load()
    {
        IL_Player.TileInteractionsUse += ILPlayerTileInteractionsUse;
        On_WorldGen.AddBuriedChest_int_int_int_bool_int_bool_ushort += OnWorldGenAddBuriedChest;
    }

    private void ILPlayerTileInteractionsUse(ILContext il)
    {
        try
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
        catch (Exception e)
        {
            MonoModHooks.DumpIL(ModContent.GetInstance<PvPAdventure>(), il);
        }
    }

    // FIXME: Replace this with an IL patch of WorldGen.templePart2.
    //        This assumes that every lihzahrd chest generated is going to be apart of temple.
    //        IL patching would ensure that only chests generated in temple are locked.
    private bool OnWorldGenAddBuriedChest(On_WorldGen.orig_AddBuriedChest_int_int_int_bool_int_bool_ushort orig, int i, int j, int contain, bool notNearOtherChests, int Style, bool trySlope, ushort chestTileType)
    {
        if (Style != 16)
            return orig(i, j, contain, notNearOtherChests, Style, trySlope, chestTileType);

        return orig(i, j, contain, notNearOtherChests, 0, trySlope, (ushort)ModContent.TileType<LockedLihzahrdChest>());
    }

    private class LockedLihzahrdChest : ModTile
    {
        public override string Texture => "PvpAdventure/Assets/Tiles/LockedLihzahrdChest";

        public override string HighlightTexture => "PvPAdventure/Assets/Tiles/LockedLihzahrdChest_Highlight";

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
            AdjTiles = new int[] { TileID.Containers };
            RegisterItemDrop(ItemID.LihzahrdChest);

            AddMapEntry(new Color(174, 129, 92), this.GetLocalization("MapEntry"));

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.Origin = new Point16(0, 1);
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 18 };
            TileObjectData.newTile.HookCheckIfCanPlace = new PlacementHook(Chest.FindEmptyChest, -1, 0, true);
            TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(Chest.AfterPlacement_Hook, -1, 0, false);
            TileObjectData.newTile.AnchorInvalidTiles = new int[]
            {
                TileID.MagicalIceBlock,
                TileID.Boulder,
                TileID.BouncyBoulder,
                TileID.LifeCrystalBoulder,
                TileID.RollingCactus
            };
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
            TileObjectData.addTile(Type);
        }

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings)
        {
            return true;
        }

        public override void KillMultiTile(int i, int j, int frameX, int frameY)
        {
            Chest.DestroyChest(i, j);
            
        }

        public override bool IsLockedChest(int i, int j)
        {
            return true;
        }

        public override bool RightClick(int i, int j)
        {
            Player player = Main.LocalPlayer;
            Main.mouseRightRelease = false;
            if (player.HasItemInInventoryOrOpenVoidBag(ItemID.TempleKey))
            {
                int left = i;
                int top = j;

                if (Main.tile[i, j].TileFrameX != 0)
                    left--;

                if (Main.tile[i, j].TileFrameY != 0)
                    top--;

                player.CloseSign();
                player.SetTalkNPC(-1);
                Main.npcChatCornerItem = ItemID.None;
                Main.npcChatText = "";
                SoundEngine.PlaySound(SoundID.Unlock, new Vector2(left * 16, top * 16));
                
                for (int x = left; x <= left + 1; x++)
                {
                    for (int y = top; y <= top + 1; y++)
                    {
                        for (int dustIndex = 0; dustIndex < 4; dustIndex++)
                            Dust.NewDust(new Vector2(x * 16, y * 16), 16, 16, DustID.Gold);

                        Main.tile[x, y].TileType = TileID.Containers;

                        if (x == left)
                            Main.tile[x, y].TileFrameX = 576;
                        else if (x == left + 1)
                            Main.tile[x, y].TileFrameX = 594;

                        if (top == y + 1)
                            Main.tile[x, y].TileFrameY = 18;
                    }
                }
                // TODO: It would be better to replace the netmessage with a custom packet to perfectly sync
                //       the chest unlock sound and dust emitted.
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    NetMessage.SendTileSquare(-1, left, top, 2, 2);
            }
            return true;
        }

        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            player.cursorItemIconID = ItemID.TempleKey;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconText = "";
            player.noThrow = 2;
        }

        public override void MouseOverFar(int i, int j)
        {
            Player player = Main.LocalPlayer;
            MouseOver(i, j);

            if (player.cursorItemIconText == "")
            {
                player.cursorItemIconEnabled = false;
                player.cursorItemIconID = ItemID.None;
            }
        }
    }
}
