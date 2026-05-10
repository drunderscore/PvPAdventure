using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PvPAdventure.Core.Utilities;

internal static class ColorHelper
{
    internal static readonly Color[] TeamColors =
    [
        new(0xC5, 0xC1, 0xD8),
        new(0xDA, 0x3B, 0x3B),
        new(0x3B, 0xDA, 0x55),
        new(0x3B, 0x95, 0xDA),
        new(0xDA, 0xB7, 0x3B),
        new(0xE0, 0x64, 0xF2),
    ];
}
