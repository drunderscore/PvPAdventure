using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Utilities;

public class NativeLibraryLoader : ModSystem
{
    private static readonly ConcurrentDictionary<string, Lazy<nint>> libraries = new();

    [ModuleInitializer]
    internal static void Initialize() =>
        NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), ResolveDllImport);

    // FIXME: Native libraries are never unloaded! I blame TML for not providing a late enough unload...

    private static nint ResolveDllImport(string library, Assembly assembly, DllImportSearchPath? searchPath)
    {
        var mod = ModLoader.GetMod("PvPAdventure");

        var lazyLibrary = libraries.GetOrAdd(library, name =>
            new Lazy<nint>(() =>
            {
                var nativeLibraryPathInMod = $"lib/Native/{name}";
                if (!mod.FileExists(nativeLibraryPathInMod))
                    return nint.Zero;

                var nativeLibraryPathOnDisk = Path.GetTempFileName();
                using var nativeLibraryModStream = mod.GetFileStream(nativeLibraryPathInMod);

                // FIXME: This temporary file is never cleaned up!
                using (var nativeLibraryDiskStream = File.Open(nativeLibraryPathOnDisk, new FileStreamOptions
                       {
                           Access = FileAccess.Write,
                           Mode = FileMode.Create,
                           // FIXME: Don't think this works on Windows correctly.
                           // Options = FileOptions.DeleteOnClose
                       }))
                    nativeLibraryModStream.CopyTo(nativeLibraryDiskStream);

                Log.Debug($"Found native library {name} and wrote to filesystem {nativeLibraryPathOnDisk}");

                return NativeLibrary.Load(nativeLibraryPathOnDisk);
            })
        );

        return lazyLibrary.Value;
    }
}