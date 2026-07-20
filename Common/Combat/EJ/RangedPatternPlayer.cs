using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat.EJ;

/// <summary>
/// Temporary recoil pattern system, will revamp in the future.
/// </summary>
public class RangedPatternPlayer : ModPlayer
{
    private const int ResetFrames = 30;

    private int _shotIndex;
    private int _lastWeaponType = -1;
    private int _resetTimer;

    public override void PostUpdate()
    {
        if (_resetTimer <= 0)
            return;

        if (--_resetTimer == 0)
        {
            _shotIndex = 0;
            _lastWeaponType = -1;
        }
    }
    public float ConsumeOffset(int weaponType, float[] pattern)
    {
        if (weaponType != _lastWeaponType)
        {
            _shotIndex = 0;
            _lastWeaponType = weaponType;
        }

        _resetTimer = ResetFrames;

        float offset = pattern[_shotIndex % pattern.Length];
        _shotIndex++;
        return offset;
    }
}