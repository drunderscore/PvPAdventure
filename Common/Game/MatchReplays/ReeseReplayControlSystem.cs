using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

#nullable enable

namespace PvPAdventure.Common.Game.MatchReplays;

internal sealed class ReeseReplayControlSystem : ModSystem
{
    private const string StopReasonPrefix = "PvPAdventure match ended:";

    public void StartMatchRecording(string matchToken)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        if (!ModLoader.TryGetMod("Reese", out Mod reese))
        {
            Log.Chat("Reese is not loaded. Match recording skipped.");
            return;
        }

        try
        {
            object result = reese.Call("StartRecording");
            if (result is true)
                Log.Chat($"Started Reese recording. MatchToken={matchToken}");
            else
                Log.Warn($"Failed to start Reese recording. MatchToken={matchToken}");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to start Reese recording. MatchToken={matchToken}, Error={ex}");
        }
    }

    /// <summary>
    /// Reese finishes the file synchronously and returns its exact path. Using that API keeps the
    /// replay tied to this match and avoids a callback from an older recording reporting a newer one.
    /// </summary>
    public string? StopMatchRecording(string matchToken)
    {
        if (Main.netMode != NetmodeID.Server)
            return null;

        if (!ModLoader.TryGetMod("Reese", out Mod reese))
        {
            Log.Chat("Reese is not loaded. Match will be reported without a replay.");
            return null;
        }

        try
        {
            object result = reese.Call("StopRecordingAndGetFilePath", StopReasonPrefix + matchToken);
            string? filePath = result as string;
            if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
            {
                Log.Chat($"Stopped Reese recording. MatchToken={matchToken}, Replay={Path.GetFileName(filePath)}");
                return filePath;
            }

            Log.Warn($"Reese returned no completed replay. MatchToken={matchToken}");
            return null;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to stop Reese recording. MatchToken={matchToken}, Error={ex}");
            return null;
        }
    }
}
