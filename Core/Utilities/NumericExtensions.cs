using System.Numerics;

namespace PvPAdventure.Core.Utilities;

public static class NumericExtensions
{
    public static T Remap<T>(this T value, T fromMin, T fromMax, T toMin, T toMax) where T : INumber<T> =>
        (value - fromMin) * (toMax - toMin) / (fromMax - fromMin) + toMin;
}