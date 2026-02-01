using Steamworks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Social;
using Terraria.Utilities;

namespace PvPAdventure.Common.SSC;

// Modifies the load and save map methods to work with SSC.
// Map data is saved in Players/<steamId>/<worldId>.map file by default.
// Map data is loaded from Main.ActivePlayerFileData.
internal class MapIOHooks : ModSystem
{
    public override void Load()
    {
        On_WorldMap.Load += LoadMap;
        On_MapHelper.InternalSaveMap += SaveMap;
    }

    void LoadMap(On_WorldMap.orig_Load orig, WorldMap self)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            orig(self);
            return;
        }

        Lighting.Clear();

        // Custom SSC logic to get the correct map path
        string plrPath = Main.ActivePlayerFileData?.Path;
        bool isCloudSave = Main.ActivePlayerFileData?.IsCloudSave ?? false;

        // Debug print player
        Log.Debug($"Load player: '{plrPath}'");

        /// Continue with vanilla loading logic, but using our computed map path
        string text = plrPath.Substring(0, plrPath.Length - 4) + Path.DirectorySeparatorChar;

        if (Main.ActiveWorldFileData.UseGuidAsMapName)
        {
            string text2 = text;
            text = string.Concat(text, Main.ActiveWorldFileData.UniqueId, ".map");
            if (!FileUtilities.Exists(text, isCloudSave))
            {
                text = text2 + Main.worldID + ".map";
            }

            // Debug print map path
            string mapPath = text;
            bool exists = FileUtilities.Exists(mapPath, isCloudSave);
            long bytes = (!isCloudSave && exists) ? new FileInfo(mapPath).Length : -1;
            Log.Debug($"Load map: '{mapPath}' exists={exists} bytes={bytes}");
        }
        else
        {
            text = text + Main.worldID + ".map";
        }
        if (!FileUtilities.Exists(text, isCloudSave))
        {
            Main.MapFileMetadata = FileMetadata.FromCurrentSettings(FileType.Map);
            return;
        }
        using MemoryStream input = FileUtilities.ReadAllBytes(text, isCloudSave).ToMemoryStream();
        using BinaryReader binaryReader = new BinaryReader(input);
        try
        {
            int num = binaryReader.ReadInt32();
            if (num <= 279)
            {
                if (num <= 91)
                {
                    MapHelper.LoadMapVersion1(binaryReader, num);
                }
                else
                {
                    MapHelper.LoadMapVersion2(binaryReader, num);
                }
                MapIO.ReadModFile(text, isCloudSave);
                //this.ClearEdges();
                Main.Map.ClearEdges();
                Main.clearMap = true;
                Main.loadMap = true;
                Main.loadMapLock = true;
                Main.refreshMap = false;
                return;
            }
            throw new Exception($"Map release version too high ({num}), the map file '{text}' is either corrupted or from a future version of Terraria.");
        }
        catch (Exception value)
        {
            using (StreamWriter streamWriter = new StreamWriter("client-crashlog.txt", append: true))
            {
                streamWriter.WriteLine(DateTime.Now);
                streamWriter.WriteLine(value);
                streamWriter.WriteLine("");
            }
            if (!isCloudSave)
            {
                File.Copy(text, text + ".bad", overwrite: true);
            }
            Main.Map.Clear();
            //WorldMap.Clear();
            Main.MapFileMetadata = FileMetadata.FromCurrentSettings(FileType.Map);
        }
    }

    void SaveMap(On_MapHelper.orig_InternalSaveMap orig)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            orig();
            return;
        }

        bool isCloudSave = Main.ActivePlayerFileData.IsCloudSave;

        //string text = Main.playerPathName.Substring(0, Main.playerPathName.Length - 4); // -4 to strip the .plr file extension

        // Use our own steamid.ssc player path where we update our SSC character
        string plrPath = Main.ActivePlayerFileData.Path; // <steamId>.SSC
        string text = plrPath.Substring(0, plrPath.Length - 4);

        Log.Debug($"Save player: '{Main.ActivePlayerFileData.Path}'");

        if (!isCloudSave)
        {
            Utils.TryCreatingDirectory(text);
        }
        text += Path.DirectorySeparatorChar;
        text = ((!Main.ActiveWorldFileData.UseGuidAsMapName) ? (text + Main.worldID + ".map") : string.Concat(text, Main.ActiveWorldFileData.UniqueId, ".map"));
        new Stopwatch().Start();
        if (!Main.gameMenu)
        {
            MapHelper.noStatusText = true;
        }
        using (MemoryStream memoryStream = new MemoryStream(4000))
        {
            using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
            //DeflateStream deflateStream = new DeflateStream((Stream)memoryStream, (CompressionMode)0);
            DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress, leaveOpen: true);

            try
            {
                int num = 0;
                byte[] array = new byte[16384];
                binaryWriter.Write(Main.curRelease);
                Main.MapFileMetadata.IncrementAndWrite(binaryWriter);
                binaryWriter.Write(Main.worldName);
                binaryWriter.Write(Main.worldID);
                binaryWriter.Write(Main.maxTilesY);
                binaryWriter.Write(Main.maxTilesX);
                binaryWriter.Write((short)TileID.Count);
                binaryWriter.Write((short)WallID.Count);
                binaryWriter.Write((short)4);
                binaryWriter.Write((short)256);
                binaryWriter.Write((short)256);
                binaryWriter.Write((short)256);
                byte b = 1;
                byte b2 = 0;
                int i;
                for (i = 0; i < TileID.Count; i++)
                {
                    if (MapHelper.tileOptionCounts[i] != 1)
                    {
                        b2 |= b;
                    }
                    if (b == 128)
                    {
                        binaryWriter.Write(b2);
                        b2 = 0;
                        b = 1;
                    }
                    else
                    {
                        b <<= 1;
                    }
                }
                if (b != 1)
                {
                    binaryWriter.Write(b2);
                }
                i = 0;
                b = 1;
                b2 = 0;
                for (; i < WallID.Count; i++)
                {
                    if (MapHelper.wallOptionCounts[i] != 1)
                    {
                        b2 |= b;
                    }
                    if (b == 128)
                    {
                        binaryWriter.Write(b2);
                        b2 = 0;
                        b = 1;
                    }
                    else
                    {
                        b <<= 1;
                    }
                }
                if (b != 1)
                {
                    binaryWriter.Write(b2);
                }
                for (i = 0; i < TileID.Count; i++)
                {
                    if (MapHelper.tileOptionCounts[i] != 1)
                    {
                        binaryWriter.Write((byte)MapHelper.tileOptionCounts[i]);
                    }
                }
                for (i = 0; i < WallID.Count; i++)
                {
                    if (MapHelper.wallOptionCounts[i] != 1)
                    {
                        binaryWriter.Write((byte)MapHelper.wallOptionCounts[i]);
                    }
                }
                binaryWriter.Flush();
                for (int j = 0; j < Main.maxTilesY; j++)
                {
                    if (!MapHelper.noStatusText)
                    {
                        float num2 = (float)j / (float)Main.maxTilesY;
                        Main.statusText = Lang.gen[66].Value + " " + (int)(num2 * 100f + 1f) + "%";
                    }
                    int num3;
                    for (num3 = 0; num3 < Main.maxTilesX; num3++)
                    {
                        MapTile mapTile = Main.Map[num3, j];
                        byte b6;
                        byte b7;
                        byte b5 = (b6 = (b7 = 0));
                        int num4 = 0;
                        bool flag = true;
                        bool flag2 = true;
                        int num5 = 0;
                        int num6 = 0;
                        byte b8 = 0;
                        int num7;
                        ushort num8;
                        if (mapTile.Light <= 18 || mapTile.Type >= MapHelper.modPosition)
                        {
                            flag2 = false;
                            flag = false;
                            num7 = 0;
                            num8 = 0;
                            num4 = 0;
                            int num9 = num3 + 1;
                            int num10 = Main.maxTilesX - num3 - 1;
                            while (num10 > 0 && Main.Map[num9, j].Light <= 18)
                            {
                                num4++;
                                num10--;
                                num9++;
                            }
                        }
                        else
                        {
                            b8 = mapTile.Color;
                            num8 = mapTile.Type;
                            if (num8 < MapHelper.wallPosition)
                            {
                                num7 = 1;
                                num8 -= MapHelper.tilePosition;
                            }
                            else if (num8 < MapHelper.liquidPosition)
                            {
                                num7 = 2;
                                num8 -= MapHelper.wallPosition;
                            }
                            else if (num8 < MapHelper.skyPosition)
                            {
                                int num11 = num8 - MapHelper.liquidPosition;
                                if (num11 == 3)
                                {
                                    b6 |= 0x40;
                                    num11 = 0;
                                }
                                num7 = 3 + num11;
                                flag = false;
                            }
                            else if (num8 < MapHelper.dirtPosition)
                            {
                                num7 = 6;
                                flag2 = false;
                                flag = false;
                            }
                            else if (num8 < MapHelper.hellPosition)
                            {
                                num7 = 7;
                                num8 = ((num8 >= MapHelper.rockPosition) ? ((ushort)(num8 - MapHelper.rockPosition)) : ((ushort)(num8 - MapHelper.dirtPosition)));
                            }
                            else
                            {
                                num7 = 6;
                                flag = false;
                            }
                            if (mapTile.Light == byte.MaxValue)
                            {
                                flag2 = false;
                            }
                            if (flag2)
                            {
                                num4 = 0;
                                int num12 = num3 + 1;
                                int num13 = Main.maxTilesX - num3 - 1;
                                num5 = num12;
                                while (num13 > 0)
                                {
                                    MapTile other = Main.Map[num12, j];
                                    if (mapTile.EqualsWithoutLight(ref other))
                                    {
                                        num13--;
                                        num4++;
                                        num12++;
                                        continue;
                                    }
                                    num6 = num12;
                                    break;
                                }
                            }
                            else
                            {
                                num4 = 0;
                                int num14 = num3 + 1;
                                int num15 = Main.maxTilesX - num3 - 1;
                                while (num15 > 0)
                                {
                                    MapTile other2 = Main.Map[num14, j];
                                    if (!mapTile.Equals(ref other2))
                                    {
                                        break;
                                    }
                                    num15--;
                                    num4++;
                                    num14++;
                                }
                            }
                        }
                        if (b8 > 0)
                        {
                            b6 |= (byte)(b8 << 1);
                        }
                        if (b7 != 0)
                        {
                            b6 |= 1;
                        }
                        if (b6 != 0)
                        {
                            b5 |= 1;
                        }
                        b5 |= (byte)(num7 << 1);
                        if (flag && num8 > 255)
                        {
                            b5 |= 0x10;
                        }
                        if (flag2)
                        {
                            b5 |= 0x20;
                        }
                        if (num4 > 0)
                        {
                            b5 = ((num4 <= 255) ? ((byte)(b5 | 0x40)) : ((byte)(b5 | 0x80)));
                        }
                        array[num] = b5;
                        num++;
                        if (b6 != 0)
                        {
                            array[num] = b6;
                            num++;
                        }
                        if (b7 != 0)
                        {
                            array[num] = b7;
                            num++;
                        }
                        if (flag)
                        {
                            array[num] = (byte)num8;
                            num++;
                            if (num8 > 255)
                            {
                                array[num] = (byte)(num8 >> 8);
                                num++;
                            }
                        }
                        if (flag2)
                        {
                            array[num] = mapTile.Light;
                            num++;
                        }
                        if (num4 > 0)
                        {
                            array[num] = (byte)num4;
                            num++;
                            if (num4 > 255)
                            {
                                array[num] = (byte)(num4 >> 8);
                                num++;
                            }
                        }
                        for (int k = num5; k < num6; k++)
                        {
                            array[num] = Main.Map[k, j].Light;
                            num++;
                        }
                        num3 += num4;
                        if (num >= 4096)
                        {
                            //((Stream)(object)deflateStream).Write(array, 0, num);
                            deflateStream.Write(array, 0, num);

                            num = 0;
                        }
                    }
                }
                if (num > 0)
                {
                    //((Stream)(object)deflateStream).Write(array, 0, num);
                    deflateStream.Write(array, 0, num);

                }
                //((Stream)(object)deflateStream).Dispose();
                deflateStream.Dispose();

                FileUtilities.WriteAllBytes(text, memoryStream.ToArray(), isCloudSave);

                // Debug print
                Log.Debug($"Save map: {text}, bytes: {memoryStream.Length}");

                MapIO.WriteModFile(text, isCloudSave);
            }
            finally
            {
                //((IDisposable)deflateStream)?.Dispose();
            }
        }
        MapHelper.noStatusText = false;
    }

}
