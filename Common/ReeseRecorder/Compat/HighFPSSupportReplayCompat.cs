using MonoMod.RuntimeDetour;
using PvPAdventure.Common.ReeseRecorder.Replay;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using static PvPAdventure.Common.ReeseRecorder.Replay.ReplaySystem;

namespace PvPAdventure.Common.ReeseRecorder.Compat;

internal class HighFPSSupportReplayCompat : ModSystem
{
    private delegate void HighFpsSupportConfigEnsureValidateStateDelegate(object self);
    private Hook _highFpsSupportConfigEnsureValidStateHook;

    private void OnHighFpsSupportConfigEnsureValidState(HighFpsSupportConfigEnsureValidateStateDelegate orig,
        object self)
    {
        // If we are watching a replay, then don't allow this to be invoked -- it will reset the tick rate option to the
        // default, because it is only meant to function in single-player. In our scenario, it's totally okay for it to
        // function with this multiplayer client.
        if (Netplay.Connection?.Socket is ReplaySocket)
            return;

        orig(self);
    }

    public override void Unload()
    {
        _highFpsSupportConfigEnsureValidStateHook?.Dispose();
        _highFpsSupportConfigEnsureValidStateHook = null;
    }

    public override void PostSetupContent()
    {
        if (!Main.dedServ)
        {
            if (ModLoader.TryGetMod("HighFPSSupport", out var highFpsSupport))
            {
                Log.Info("Enabling HighFPSSupport interop to allow tick rate modification for replays");
                // If we have the High FPS Support mod installed and loaded, we want to override their config validator
                // (which ensures their tick rate modification only functions in single-player) to also function for
                // multiplayer clients if a replay is being played.
                _highFpsSupportConfigEnsureValidStateHook = new Hook(
                    highFpsSupport.GetType().Assembly.GetType("HighFPSSupport.Config").GetMethod("EnsureValidState",
                        BindingFlags.Public | BindingFlags.Instance), OnHighFpsSupportConfigEnsureValidState);
            }
        }
    }
}
