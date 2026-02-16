using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Terraria.ModLoader;

namespace PvPAdventure.Discord.Systems;

// FIXME: What order do ModSystems load in? Are we sure this will be loaded first (at LEAST before DiscordSdk?)
[Autoload(Side = ModSide.Client)]
public class NativeLibraryLoader : ModSystem
{
    public override void Load()
    {
        NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), ResolveDllImport);
    }

    private IntPtr ResolveDllImport(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        Log.Info($"Resolving native DLL: {libraryName}");
        var nativeLibraryPathInMod = $"lib/Native/{libraryName}";
        if (!Mod.FileExists(nativeLibraryPathInMod))
            return IntPtr.Zero;

        var nativeLibraryPathOnDisk = Path.GetTempFileName();

        using var nativeLibraryModStream = Mod.GetFileStream(nativeLibraryPathInMod);

        // FIXME: This temporary file is never cleaned up!
        using (var nativeLibraryDiskStream = File.Open(nativeLibraryPathOnDisk, new FileStreamOptions
        {
            Access = FileAccess.Write,
            Mode = FileMode.Create,
            // FIXME: Don't think this works on Windows correctly.
            // Options = FileOptions.DeleteOnClose
        }))
            nativeLibraryModStream.CopyTo(nativeLibraryDiskStream);

        Mod.Logger.Info($"> Extracted and written to disk as {nativeLibraryPathOnDisk}");

        return NativeLibrary.Load(nativeLibraryPathOnDisk);
    }
}