//using PvPAdventure.Common.MainMenu.MatchHistory;
//using System;

//namespace PvPAdventure.Common.MainMenu.Achievements;

///// <summary>
///// Represents a TPVPA achievement that tracks progress toward a specific goal.
///// </summary>
///// <param name="IconIndex">The index of the icon in the Ass.Achievements spritesheet.</param>
///// <param name="Title">The title of the achievement.</param>
///// <param name="Description">A detailed description of the achievement, explaining its purpose or requirements to the user.</param>
///// <param name="Target">The target value that must be reached to complete or unlock the achievement.</param>
///// <param name="Delta">A function that computes the incremental progress toward the achievement based on a given match result. The function
///// should return the amount to add to the current progress for each match.</param>
//public sealed record AchievementDefinition(
//    int IconIndex,
//    string Title,
//    string Description,
//    int Target,
//    int GemsReward,
//    Func<MatchResult, int> Delta);