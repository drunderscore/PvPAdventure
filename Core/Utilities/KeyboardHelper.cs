using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace PvPAdventure.Core.Utilities;

public static class KeyboardHelper
{
    public static bool JustPressed(Keys key) => Main.keyState.IsKeyDown(key) && Main.oldKeyState.IsKeyUp(key);
}
