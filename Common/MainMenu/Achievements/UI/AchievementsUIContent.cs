using System;

namespace PvPAdventure.Common.MainMenu.Achievements.UI;

internal readonly record struct AchievementsUIContent(AchievementUIEntry[] Entries);

internal readonly record struct AchievementUIEntry(
    string Id,
    int IconIndex,
    string Title,
    string Description,
    int Target,
    int Progress,
    int GemsReward,
    bool IsCollected)
{
    public int ClampedTarget => Math.Max(Target, 1);
    public int ClampedProgress => Math.Clamp(Progress, 0, ClampedTarget);
    public bool IsCompleted => ClampedProgress >= ClampedTarget;
}
