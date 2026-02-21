//using System;
//using System.IO;
//using System.Text;

//namespace PvPAdventure.Common.ReeseRecorder;

//public sealed class ReplayFile_CleanNotWorking : IDisposable
//{
//    public const string Extension = ".tpvpademo";

//    private const string Magic = "TPVPA";
//    private const int Version = 1;

//    private readonly BinaryReader r;
//    private readonly BinaryWriter w;

//    public uint NextTick { get; private set; } = uint.MaxValue;
//    public int BytesRemaining { get; private set; }
//    private readonly object _ioLock = new();
//    private bool _disposed = false;

//    private ReplayFile_CleanNotWorking(BinaryReader reader)
//    {
//        r = reader;
//        if (r.ReadString() != Magic || r.ReadInt32() != Version)
//            throw new InvalidDataException("Invalid tpvpademo");
//        ReadNextHeader();
//    }

//    private ReplayFile_CleanNotWorking(BinaryWriter writer)
//    {
//        w = writer;
//        w.Write(Magic);
//        w.Write(Version);
//    }

//    public static ReplayFile_CleanNotWorking OpenWrite(string path)
//    {
//        Directory.CreateDirectory(Path.GetDirectoryName(path));
//        var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
//        return new ReplayFile_CleanNotWorking(new BinaryWriter(fs, Encoding.UTF8, true));
//    }

//    public static ReplayFile_CleanNotWorking OpenRead(string path)
//    {
//        var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
//        return new ReplayFile_CleanNotWorking(new BinaryReader(fs, Encoding.UTF8, true));
//    }

    

//    public void WritePacket(ReadOnlySpan<byte> data, uint tick)
//    {
//        lock (_ioLock)
//        {
//            if (_disposed) return;
//            w.Write(tick);
//            w.Write(data.Length);
//            w.Write(data);
//        }
//    }

//    public void Dispose()
//    {
//        lock (_ioLock)
//        {
//            if (_disposed) return;
//            _disposed = true;
//        }
//        try { w?.Flush(); } catch { }
//        r?.Dispose();
//        w?.Dispose();
//        r?.BaseStream.Dispose();
//        w?.BaseStream.Dispose();
//    }

//    public int ReadPacketData(Span<byte> dst)
//    {
//        if (BytesRemaining <= 0)
//            return 0;

//        var toRead = Math.Min(dst.Length, BytesRemaining);
//        var read = r.Read(dst[..toRead]);
//        BytesRemaining -= read;

//        if (BytesRemaining <= 0)
//            ReadNextHeader();

//        return read;
//    }

//    private void ReadNextHeader()
//    {
//        try
//        {
//            if (r.BaseStream.Position >= r.BaseStream.Length)
//            {
//                NextTick = uint.MaxValue;
//                BytesRemaining = 0;
//                return;
//            }

//            NextTick = r.ReadUInt32();
//            BytesRemaining = r.ReadInt32();
//            if (BytesRemaining < 0)
//                throw new InvalidDataException("Corrupt tpvpademo");
//        }
//        catch (EndOfStreamException)
//        {
//            NextTick = uint.MaxValue;
//            BytesRemaining = 0;
//        }
//    }
//}