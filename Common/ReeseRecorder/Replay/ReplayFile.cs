using System;
using System.IO;
using System.Linq;
using System.Text;
using log4net;

namespace PvPAdventure.Common.ReeseRecorder.Replay;

// FIXME: Some of the bullshit we do would be better buffered instead of manually counting bytes, in both directions.

public class ReplayFile : IDisposable
{
    public const string Identifier = "Reese";
    private static readonly byte[] IdentifierASCII = Encoding.ASCII.GetBytes(Identifier);
    private static readonly ILog Logger = LogManager.GetLogger(typeof(ReplayFile));

    private BinaryWriter _binaryWriter;
    private BinaryReader _binaryReader;

    public uint Tick { get; private set; }

    // FIXME: We should just buffer this.
    public int NumberOfPacketDataBytesRemaining { get; private set; }

    private ReplayFile()
    {
    }

    private void WriteIdentifier()
    {
        _binaryWriter.Write(IdentifierASCII);
    }

    // FIXME: Ever heard of async? We have the opportunity upstream.
    public void WritePacketData(byte[] data, uint tick)
    {
        // Can't go backwards
        if (Tick > tick)
            throw new Exception("Cannot write packet data into the past");

        if (Tick < tick)
            FlushTick(tick);

        if (NumberOfPacketDataBytesRemaining == 0)
        {
            _binaryWriter.Write(0);
            _binaryWriter.Write(0);
        }

        _binaryWriter.Write(data);
        NumberOfPacketDataBytesRemaining += data.Length;
    }

    private void ReadPacketDataHeader()
    {
        // FIXME: This seems like a shitty way to handle EOF? idek
        try
        {
            Tick += _binaryReader.ReadUInt32();
            NumberOfPacketDataBytesRemaining = _binaryReader.ReadInt32();
        }
        catch (EndOfStreamException)
        {
            Tick = 0;
            NumberOfPacketDataBytesRemaining = 0;
        }
    }

    public int ReadPacketData(Span<byte> data)
    {
        var numberOfBytesRead = _binaryReader.Read(data[..Math.Min(data.Length, NumberOfPacketDataBytesRemaining)]);
        NumberOfPacketDataBytesRemaining = Math.Max(0, NumberOfPacketDataBytesRemaining - numberOfBytesRead);

        if (NumberOfPacketDataBytesRemaining == 0)
            ReadPacketDataHeader();

        return numberOfBytesRead;
    }

    public void FlushTick()
    {
        FlushTick(Tick);
    }

    private void FlushTick(uint tick)
    {
        if (NumberOfPacketDataBytesRemaining > 0)
        {
            _binaryWriter.Seek((-NumberOfPacketDataBytesRemaining) - 8, SeekOrigin.Current);
            _binaryWriter.Write(tick - Tick);
            _binaryWriter.Write(NumberOfPacketDataBytesRemaining);
            _binaryWriter.Seek(0, SeekOrigin.End);

            Tick = tick;
            NumberOfPacketDataBytesRemaining = 0;
        }
    }

    public static ReplayFile Write(Stream stream)
    {
        var replayFile = new ReplayFile
        {
            _binaryWriter = new BinaryWriter(stream, Encoding.UTF8, true)
        };

        replayFile.WriteIdentifier();

        return replayFile;
    }

    public static ReplayFile Read(Stream stream)
    {
        var replayFile = new ReplayFile
        {
            _binaryReader = new BinaryReader(stream, Encoding.UTF8, true)
        };

        var identifier = replayFile._binaryReader.ReadBytes(Identifier.Length);
        if (!identifier.SequenceEqual(IdentifierASCII))
            throw new InvalidDataException("Not a Reese file");

        // Do this once right now, so we have some data to deal in once it comes time to.
        // FIXME: This honestly smells like implementation detail from upstream (them checking on how many bytes we have)
        //        but honestly it might be okay to assume that if we have data we should say we do. yeah i agree hard with that.
        replayFile.ReadPacketDataHeader();

        return replayFile;
    }

    public void Dispose()
    {
        _binaryWriter?.Dispose();
        _binaryReader?.Dispose();

        // We told both binary streams to leaveOpen, so let's close it once ourselves now.
        if (_binaryReader != null)
            _binaryReader.BaseStream.Dispose();
        else if (_binaryWriter != null)
            _binaryWriter.BaseStream.Dispose();
    }
}