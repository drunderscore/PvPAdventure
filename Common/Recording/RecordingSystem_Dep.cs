//using System;
//using System.Collections.Generic;
//using System.IO;
//using Microsoft.Xna.Framework;
//using Terraria;
//using Terraria.ID;
//using Terraria.ModLoader;

//namespace PvPAdventure.Common.MatchHistory.Recording;

//internal enum RecordingMode
//{
//    None,
//    Recording,
//    Playback
//}

//internal readonly struct RecordingFrame(int tick, Vector2 cameraCenter)
//{
//    public readonly int Tick = tick;
//    public readonly Vector2 CameraCenter = cameraCenter;
//}

//internal sealed class RecordingSystem : ModSystem
//{
//    private const int DemoFormatVersion = 1;
//    private const string DemoExt = ".pvpdem";

//    private RecordingMode mode;
//    private string activeDemoPath;

//    private FileStream stream;
//    private BinaryWriter writer;

//    private List<RecordingFrame> playbackFrames = [];
//    private int playbackIndex;
//    private Vector2 playbackCameraCenter;

//    public override void OnWorldLoad()
//    {
//        if (Main.netMode != NetmodeID.SinglePlayer)
//            return;

//        if (mode != RecordingMode.None)
//            EndAnyMode();

//        BeginRecording();
//    }

//    public override void PreSaveAndQuit()
//    {
//        if (mode == RecordingMode.Recording)
//            EndRecording();
//    }

//    public override void OnWorldUnload()
//    {
//        EndAnyMode();
//    }

//    public override void PostUpdateEverything()
//    {
//        if (Main.gameMenu)
//            return;

//        if (mode == RecordingMode.Recording)
//            RecordFrame();

//        if (mode == RecordingMode.Playback)
//            StepPlayback();
//    }

//    public override void ModifyScreenPosition()
//    {
//        if (mode != RecordingMode.Playback)
//            return;

//        Vector2 halfScreen = new(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f);
//        Main.screenPosition = playbackCameraCenter - halfScreen;
//    }

//    public void BeginPlayback(string path)
//    {
//        if (mode != RecordingMode.None)
//            EndAnyMode();

//        if (!File.Exists(path))
//        {
//            Log.Debug($"Demo file not found: {Path.GetFileName(path)}");
//            return;
//        }

//        List<RecordingFrame> frames = LoadAllFrames(path);
//        if (frames.Count == 0)
//        {
//            Log.Debug($"Demo file is empty or invalid: {Path.GetFileName(path)}");
//            return;
//        }

//        mode = RecordingMode.Playback;
//        playbackFrames = frames;
//        playbackIndex = 0;
//        playbackCameraCenter = frames[0].CameraCenter;
//        activeDemoPath = path;

//        Log.Debug($"Playback started: {Path.GetFileName(path)}");
//    }

//    public void StopPlayback()
//    {
//        if (mode != RecordingMode.Playback)
//            return;

//        mode = RecordingMode.None;
//        playbackFrames.Clear();
//        playbackIndex = 0;
//        activeDemoPath = null;

//        Log.Debug("Playback stopped.");
//    }

//    private void BeginRecording()
//    {
//        string dir = GetDemoDirectory();
//        Directory.CreateDirectory(dir);

//        string fileName = MakeDemoFileName();
//        activeDemoPath = Path.Combine(dir, fileName);

//        stream = new FileStream(activeDemoPath, FileMode.Create, FileAccess.Write, FileShare.Read);
//        writer = new BinaryWriter(stream);

//        WriteHeader(writer);

//        mode = RecordingMode.Recording;
//        Log.Debug($"Recording started: {fileName}");
//    }

//    private void EndRecording()
//    {
//        if (mode != RecordingMode.Recording)
//            return;

//        mode = RecordingMode.None;

//        try
//        {
//            writer?.Flush();
//            stream?.Flush();
//        }
//        catch
//        {
//        }

//        writer?.Dispose();
//        stream?.Dispose();

//        writer = null;
//        stream = null;

//        Log.Debug($"Recording saved: {Path.GetFileName(activeDemoPath)}");
//    }

//    private void EndAnyMode()
//    {
//        if (mode == RecordingMode.Recording)
//        {
//            EndRecording();
//            return;
//        }

//        if (mode == RecordingMode.Playback)
//        {
//            StopPlayback();
//            return;
//        }

//        mode = RecordingMode.None;

//        writer?.Dispose();
//        stream?.Dispose();

//        writer = null;
//        stream = null;

//        playbackFrames.Clear();
//        playbackIndex = 0;
//        activeDemoPath = null;
//    }

//    private void RecordFrame()
//    {
//        if (writer == null)
//            return;

//        int tick = (int)Main.GameUpdateCount;
//        Vector2 center = GetCameraCenter();

//        writer.Write(tick);
//        writer.Write(center.X);
//        writer.Write(center.Y);
//    }

//    private void StepPlayback()
//    {
//        if (playbackIndex >= playbackFrames.Count)
//        {
//            StopPlayback();
//            return;
//        }

//        RecordingFrame frame = playbackFrames[playbackIndex];
//        playbackCameraCenter = frame.CameraCenter;
//        playbackIndex++;
//    }

//    private static Vector2 GetCameraCenter()
//    {
//        Vector2 halfScreen = new(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f);
//        return Main.screenPosition + halfScreen;
//    }

//    private static void WriteHeader(BinaryWriter w)
//    {
//        w.Write(0x50445056); // "VPDP"
//        w.Write(DemoFormatVersion);

//        w.Write(Main.ActiveWorldFileData?.Name ?? "");
//        w.Write(Main.ActiveWorldFileData?.UniqueId.ToString() ?? "");
//        w.Write(DateTime.UtcNow.ToBinary());
//    }

//    private static List<RecordingFrame> LoadAllFrames(string path)
//    {
//        using FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
//        using BinaryReader r = new(fs);

//        int magic = r.ReadInt32();
//        int version = r.ReadInt32();

//        if (magic != 0x50445056 || version != DemoFormatVersion)
//            return [];

//        _ = r.ReadString();
//        _ = r.ReadString();
//        _ = r.ReadInt64();

//        List<RecordingFrame> frames = [];

//        while (fs.Position < fs.Length)
//        {
//            int tick = r.ReadInt32();
//            float x = r.ReadSingle();
//            float y = r.ReadSingle();

//            frames.Add(new RecordingFrame(tick, new Vector2(x, y)));
//        }

//        return frames;
//    }

//    private static string GetDemoDirectory()
//    {
//        return Path.Combine(Main.SavePath, "PvPAdventureReplays");
//    }

//    private static string MakeDemoFileName()
//    {
//        string world = Main.ActiveWorldFileData?.Name ?? "UnknownWorld";
//        world = SanitizeFileName(world);

//        string stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
//        return $"{stamp}_{world}{DemoExt}";
//    }

//    private static string SanitizeFileName(string s)
//    {
//        foreach (char c in Path.GetInvalidFileNameChars())
//            s = s.Replace(c, '_');

//        return s;
//    }
//}
