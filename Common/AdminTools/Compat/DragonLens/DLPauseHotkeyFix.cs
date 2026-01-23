using System;
using System.Collections.Generic;
using DragonLens.Core.Systems.ToolSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Common.AdminTools.Compat.DragonLens.Tools;
using PvPAdventure.Common.GameTimer;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.AdminTools.Compat.DragonLens;

[JITWhenModsEnabled("DragonLens")]
[ExtendsFromMod("DragonLens")]
internal sealed class DLPauseHotkeyWhilePausedSystem : ModSystem
{
    private static bool _pendingResume;
    private static bool _prevAnyDown;
    private static ulong _lastHandledUpdate;

    public override void UpdateUI(GameTime gameTime)
    {
        var pm = ModContent.GetInstance<PauseManager>();
        DLPauseTool tool = ModContent.GetInstance<DLPauseTool>();
        if (tool?.keybind == null)
            return;

        bool paused = pm.IsPaused;
        bool anyDown = IsAnyAssignedKeyDown(tool.keybind, Keyboard.GetState());

        if (!paused)
        {
            _pendingResume = false;
            _prevAnyDown = anyDown;
            return;
        }

        if (_lastHandledUpdate == Main.GameUpdateCount)
        {
            _prevAnyDown = anyDown;
            return;
        }

        bool pressedEdge = anyDown && !_prevAnyDown;

        if (!_pendingResume && pressedEdge)
        {
            _pendingResume = true;
            _prevAnyDown = anyDown;
            return;
        }

        if (_pendingResume && !anyDown)
        {
            _pendingResume = false;
            _lastHandledUpdate = Main.GameUpdateCount;
            tool.OnActivate();
            _prevAnyDown = anyDown;
            return;
        }

        _prevAnyDown = anyDown;
    }

    private static bool IsAnyAssignedKeyDown(ModKeybind bind, KeyboardState state)
    {
        IList<string> assigned = bind.GetAssignedKeys();
        if (assigned == null || assigned.Count == 0)
            return false;

        for (int i = 0; i < assigned.Count; i++)
        {
            if (!TryParseKey(assigned[i], out Keys key))
                continue;

            if (state.IsKeyDown(key))
                return true;
        }

        return false;
    }

    private static bool TryParseKey(string keyName, out Keys key)
    {
        key = Keys.None;
        if (string.IsNullOrWhiteSpace(keyName))
            return false;

        return Enum.TryParse(keyName, ignoreCase: true, out key);
    }
}
