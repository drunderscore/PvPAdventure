using MonoMod.Cil;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.Net;

namespace PvPAdventure.Common.ReeseRecorder.Replay;

[Autoload(Side = ModSide.Client)]
public class ReplaySystem : ModSystem, ITicker
{
    public uint Ticks { get; private set; }

    public override void Load()
    {
        On_Netplay.ClientLoopSetup += OnClientLoopSetup;
        IL_Main.DoUpdate += il =>
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(i => i.MatchStsfld<Main>("drawSkip"));
            // cursor.Index += 1;
            cursor.EmitDelegate(() =>
            {
                Ticks++;
                if ((Ticks % 60) == 0)
                    Log.Info("Client replay tick: " + Ticks);
            });
        };
    }

    private void OnClientLoopSetup(On_Netplay.orig_ClientLoopSetup orig, RemoteAddress address)
    {
        orig(address);

        // FIXME: shitty way to start watching replays from a specific magic IP lol
        if (address.GetIdentifier() == "10.2.3.4")
        {
            // Get the path of the replay file
            var stagePath = Path.Combine(Main.SavePath, "PvPAdventureReplays", "record.bin");
            if (!File.Exists(stagePath))
            {
                Netplay.Disconnect = true;
                Main.statusText = $"Replay file not found: \n'{stagePath}'";
                Log.Warn(Main.statusText);
                return;
            }

            Ticks = 0;
            Log.Info("Connecting to magic replay IP thingy!");
            Netplay.Connection = new RemoteServer();
            Netplay.Connection.ReadBuffer = new byte[ushort.MaxValue]; // TML: 1024 -> ushort.MaxValue
            Netplay.Connection.Socket = new ReplaySocket(this, ReplayFile.Read(File.OpenRead(stagePath)));
        }
    }
}