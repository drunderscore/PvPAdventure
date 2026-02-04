using System;
using System.Diagnostics;
using System.IO;
using MonoMod.Cil;

namespace PvPAdventure.Core.Helpers;
public sealed class ILEditException(string message, Exception innerException) : Exception(message, innerException)
{
    // empty constructor
}

public static class IL
{
    /// <summary>
    /// Patches the IL of a method using the provided action.
    /// Catches exceptions and provides the most relevant debugging information.
    /// </summary>
    /// <param name="il"></param>
    /// <param name="ilAction"></param>
    /// <exception cref="ILEditException"></exception>
    public static void Edit(ILContext il, Action<ILCursor> ilAction)
    {
        try
        {
            var c = new ILCursor(il);
            ilAction(c); // Execute the provided IL patch action
        }
        catch (Exception ex)
        {
            string fullMethodName = il.Method.FullName;
            string displayMethodName = fullMethodName;

            // Attempt to clean up DMD prefixes
            int dmdIndex = fullMethodName.IndexOf("::");
            if (dmdIndex > 0 && fullMethodName.Substring(0, dmdIndex).Contains("DMD<>"))
            {
                displayMethodName = fullMethodName.Substring(dmdIndex + 2);
            }

            string errorSourceFileName = "unknown file";
            int errorSourceLineNumber = 0;

            // Get the assembly name of the mod that owns this ILHelper class.
            // This assumes the lambda (patchAction) causing the error is also in this mod.
            string modAssemblyName = typeof(IL).Assembly.GetName().Name;

            var stackTrace = new StackTrace(ex, true); // true to capture file/line info
            StackFrame errorFrame = null;

            // Find the first stack frame from the original exception (ex)
            // that belongs to the mod's assembly. This is likely the lambda.
            for (int i = 0; i < stackTrace.FrameCount; i++)
            {
                StackFrame frame = stackTrace.GetFrame(i);
                if (frame != null)
                {
                    var frameMethod = frame.GetMethod();
                    if (frameMethod?.DeclaringType?.Assembly.GetName().Name == modAssemblyName)
                    {
                        errorFrame = frame;
                        break; // Found the most relevant frame in the mod's code
                    }
                }
            }

            if (errorFrame != null)
            {
                string fullPath = errorFrame.GetFileName();
                if (!string.IsNullOrEmpty(fullPath))
                {
                    errorSourceFileName = Path.GetFileName(fullPath); // Get just the file name
                }
                errorSourceLineNumber = errorFrame.GetFileLineNumber();
            }

            // Construct the detailed message
            string lineInfo = errorSourceLineNumber > 0 ? errorSourceLineNumber.ToString() : "N/A";
            // Use displayMethodName in the message
            var message = $"ERROR: IL Patch failed for '{displayMethodName}' :: \n Error in file '{errorSourceFileName}' :: line {lineInfo}. Please contact Erky!";

            throw new ILEditException(message, ex);
        }
    }

    // public static void InjectVec2CtorOffset(ILContext il, float offsetX, float offsetY)
    // {
    //     Edit(il, c =>
    //     {
    //         while (c.TryGotoNext(MoveType.After,
    //             i => i.MatchNewobj<Vector2>()))
    //         {
    //             c.EmitDelegate<Func<Vector2, Vector2>>(pos =>
    //             {
    //                 return new Vector2(pos.X + offsetX, pos.Y + offsetY);
    //             });
    //         }
    //     });
    // }

    // private void InjectMapOffset(ILContext il)
    // {
    //     try
    //     {
    //         ILCursor c = new(il);

    //         while (c.TryGotoNext(MoveType.After,
    //             i => i.MatchNewobj<Vector2>()))
    //         {
    //             c.EmitDelegate<Func<Vector2, Vector2>>(pos =>
    //             {
    //                 return new Vector2(pos.X + OffsetX, pos.Y + OffsetY);
    //             });
    //         }
    //     }
    //     catch (Exception e)
    //     {
    //         throw new ILPatchFailureException(Mod, il, e);
    //     }
    // }
}