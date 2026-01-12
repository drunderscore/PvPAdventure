//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Security.Cryptography;
//using Terraria.ID;
//using Terraria.ModLoader;

//namespace SSC.Core;

//public class MessageManager : ModSystem
//{
//    static Dictionary<int, byte[]> CachedMessageFrames;

//    public override void Load()
//    {
//        CachedMessageFrames = new();
//    }

//    // 适用于传输大容量数据,注意根据容量的大小会有不同的时延.
//    public static void FrameSend(ModPacket r, int to = -1, int ignore = -1)
//    {
//        if (r.BaseStream.Position < 60000)
//        {
//            r.Send(to, ignore);
//        }
//        else
//        {
//            var array = ((MemoryStream)r.BaseStream).ToArray(); // 原始数据, Position-[XX-XX]-[FA]-[ID]-01-09-08
//            var hash = Convert.ToHexString(MD5.Create().ComputeHash(array));

//            var frame = array.Chunk(16384).GetEnumerator();

//            var mp = ModContent.GetInstance<SSC>().GetPacket();
//            mp.Write((byte)MessageID.MessageSegment);
//            mp.Write(true);
//            mp.Send(to, ignore);

//            for (; frame.MoveNext();)
//            {
//                mp = ModContent.GetInstance<SSC>().GetPacket();
//                mp.Write((byte)MessageID.MessageSegment);
//                mp.Write(false);
//                mp.Write(false);
//                mp.Write(frame.Current!.Length);
//                mp.Write(frame.Current!);
//                mp.Send(to, ignore);
//            }

//            mp = ModContent.GetInstance<SSC>().GetPacket();
//            mp.Write((byte)MessageID.MessageSegment);
//            mp.Write(false);
//            mp.Write(true);
//            mp.Write(hash);
//            mp.Send(to, ignore);

//            frame.Dispose();
//        }
//    }

//    public static void ProcessMessage(BinaryReader r, int from)
//    {
//        if (r.ReadBoolean())
//        {
//            CachedMessageFrames[from] = Array.Empty<byte>();
//            return;
//        }

//        if (r.ReadBoolean())
//        {
//            var hash = r.ReadString();
//            var data = CachedMessageFrames[from];
//            if (Convert.ToHexString(MD5.Create().ComputeHash(data.ToArray())) != hash)
//            {
//                ModContent.GetInstance<SSC>().Logger.Error("Hash check error!");
//                return;
//            }

//            ModContent.GetInstance<SSC>().HandlePacket(new BinaryReader(new MemoryStream(data[4..])), from);
//            return;
//        }

//        CachedMessageFrames[from] = CachedMessageFrames[from].Concat(r.ReadBytes(r.ReadInt32())).ToArray();
//    }

//    public override void Unload()
//    {
//        CachedMessageFrames = null;
//    }
//}