using PvPAdventure.Core.Helpers;
using PvPAdventure.System;
using System;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Integrations;

[JITWhenModsEnabled("HEROsMod")]
public sealed class HerosModIntegration : ModSystem
{
    private const string PauseGamePermissionKey = "PauseGame";

    public override void PostSetupContent()
    {
        if (ModLoader.TryGetMod("HEROsMod", out Mod herosMod))
        {
            AddButtons(herosMod);
        }
    }

    private void AddButtons(Mod herosMod)
    {
        herosMod.Call("AddSimpleButton",
            PauseGamePermissionKey,
            Ass.Pause,
            (Action)(() =>
            {
                var mgr = ModContent.GetInstance<PauseManager>();
                if (mgr != null)
                    mgr.PauseGame();
                else
                    Log.Info("PauseManager not loaded yet.");
            }),
            (Action<bool>)(hasPerm => PermissionChanged(hasPerm, PauseGamePermissionKey)),
            (Func<string>)(() =>
            {
                var mgr = ModContent.GetInstance<PauseManager>();
                return (mgr != null && mgr.IsPaused)
                    ? "Continue game"
                    : "Play game";
            })
        );
    }

    private static void PermissionChanged(bool hasPerm, string permissionName)
    {
        if (!hasPerm)
        {
            //Main.NewText($"⛔ You lost permission to use the {permissionName} button!", ColorHelper.CalamityRed);
            Log.Info($"You lost permission for {permissionName} button. You cannot use it anymore.");
        }
        else
        {
            //Main.NewText($"✅ You regained permission to use the {permissionName} button!", OutlineColor.LightGreen);
            Log.Info($"You regained permission for {permissionName} button. You can use it again.");
        }
    }
}