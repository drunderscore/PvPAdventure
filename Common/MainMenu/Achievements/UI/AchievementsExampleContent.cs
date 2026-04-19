namespace PvPAdventure.Common.MainMenu.Achievements.UI;

internal static class AchievementsExampleContent
{
    public static AchievementsUIContent Create()
    {
        AchievementUIEntry[] entries = new AchievementUIEntry[Achievements.All.Length];

        for (int i = 0; i < Achievements.All.Length; i++)
        {
            (string id, AchievementDefinition def) = Achievements.All[i];
            int target = def.Target <= 0 ? 1 : def.Target;

            int progress = i switch
            {
                0 => target,
                1 => target,
                2 => target,
                _ => target / 2
            };

            entries[i] = new AchievementUIEntry(
                id,
                def.IconIndex,
                def.Title,
                def.Description,
                target,
                progress,
                def.GemsReward,
                i == 0);
        }

        return new AchievementsUIContent(entries);
    }
}
