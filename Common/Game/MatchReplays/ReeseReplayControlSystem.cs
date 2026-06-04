using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

#nullable enable

namespace PvPAdventure.Common.Game.MatchReplays;

internal sealed class ReeseReplayControlSystem : ModSystem
{
    private Action<string, string, string[], uint, string>? recordingFinishedCallback;

    public override void PostSetupContent()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        if (!ModLoader.TryGetMod("Reese", out Mod reese))
            return;

        recordingFinishedCallback = OnRecordingFinished;

        object result = reese.Call("RegisterRecordingFinishedCallback", recordingFinishedCallback);
        if (result is true)
            Log.Info("Registered Reese recording finished callback.");
        else
            Log.Chat("Failed to register Reese recording finished callback.");
    }

    public override void Unload()
    {
        if (recordingFinishedCallback == null)
            return;

        if (ModLoader.TryGetMod("Reese", out Mod reese))
            reese.Call("UnregisterRecordingFinishedCallback", recordingFinishedCallback);

        recordingFinishedCallback = null;
    }

    public void StartMatchRecording()
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        if (!ModLoader.TryGetMod("Reese", out Mod reese))
        {
            Log.Chat("Reese is not loaded. Match recording skipped.");
            return;
        }

        object result = reese.Call("StartRecording");

        if (result is true)
            Log.Chat("Started Reese recording for PvPAdventure match.");
        else
            Log.Chat("Failed to start Reese recording.");
    }

    public void StopMatchRecording()
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        if (!ModLoader.TryGetMod("Reese", out Mod reese))
        {
            Log.Chat("Reese is not loaded. No match recording to stop.");
            return;
        }

        object result = reese.Call("StopRecording", "PvPAdventure match ended");

        if (result is true)
            Log.Chat("Stopped Reese recording for PvPAdventure match.");
        else
            Log.Chat("Failed to stop Reese recording.");
    }

    private static void OnRecordingFinished(string filePath, string worldName, string[] modNames, uint durationTicks, string reason)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        Log.Chat($"Reese recording finished: {Path.GetFileName(filePath)}");
        Log.Info($"Reese recording finished. FilePath={filePath}, World={worldName}, DurationTicks={durationTicks}, Reason={reason}");

        GameManager.ReportCompletedMatchToBackend(filePath);
    }
}