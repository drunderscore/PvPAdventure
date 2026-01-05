//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Security.Cryptography;
//using Terraria.ModLoader;

//namespace PvPAdventure.Core.SSC;

///// <summary>
///// This is a helper system that segments payloads above 60KB to avoid Terraria's 64 KB packet size limit.
///// DO NOT DELETE THIS CLASS: Removing this class risks corrupt joins/saves for large SSC characters.
///// </summary>
//public class MessageManager : ModSystem
//{
//    static Dictionary<int, byte[]> CachedMessageFrames;

//    public override void Load()
//    {
//        CachedMessageFrames = [];
//    }

//    // Suited for large payloads; latency will vary with size.
//    public static void FrameSend(ModPacket r, int to = -1, int ignore = -1)
//    {
//        if (r.BaseStream.Position < 60000)
//        {
//            r.Send(to, ignore);
//        }
//        else
//        {
//            var array = ((MemoryStream)r.BaseStream).ToArray(); // Raw data: Position-[XX-XX]-[FA]-[ID]-01-09-08
//            var hash = Convert.ToHexString(MD5.Create().ComputeHash(array));

//            var frame = array.Chunk(16384).GetEnumerator();

//            var mp = ModContent.GetInstance<PvPAdventure>().GetPacket();
//            mp.Write((byte)AdventurePacketIdentifier.SSC);
//            mp.Write((byte)SSCMessageID.MessageSegment);
//            mp.Write(true);
//            mp.Send(to, ignore);

//            for (; frame.MoveNext();)
//            {
//                mp = ModContent.GetInstance<PvPAdventure>().GetPacket();
//                mp.Write((byte)AdventurePacketIdentifier.SSC);
//                mp.Write((byte)SSCMessageID.MessageSegment);
//                mp.Write(false);
//                mp.Write(false);
//                mp.Write(frame.Current!.Length);
//                mp.Write(frame.Current!);
//                mp.Send(to, ignore);
//            }

//            mp = ModContent.GetInstance<PvPAdventure>().GetPacket();
//            mp.Write((byte)AdventurePacketIdentifier.SSC);
//            mp.Write((byte)SSCMessageID.MessageSegment);
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
//                ModContent.GetInstance<PvPAdventure>().Logger.Error("Hash check error!");
//                return;
//            }

//            ModContent.GetInstance<SSC_v3>().HandlePacket(new BinaryReader(new MemoryStream(data[4..])), from);
//            return;
//        }

//        CachedMessageFrames[from] = CachedMessageFrames[from].Concat(r.ReadBytes(r.ReadInt32())).ToArray();
//    }

//    public override void Unload()
//    {
//        CachedMessageFrames = null;
//    }
//}