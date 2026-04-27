using Terraria;
using Terraria.GameContent.Events;
using Terraria.ID;

namespace PvPAdventure.Common.Spectator.UI.Tabs.World;

internal static class WorldEventInfoHelper
{
    public readonly record struct EventEntry(int ItemId, string Name, bool Downed);

    public static EventEntry[] GetEventEntries()
    {
        return
        [
            new EventEntry(ItemID.GoblinBattleStandard, "Goblin Army", NPC.downedGoblins),
            new EventEntry(ItemID.SnowGlobe, "Frost Legion", NPC.downedFrost),
            new EventEntry(ItemID.PirateMap, "Pirate Invasion", NPC.downedPirates),
            new EventEntry(ItemID.MartianConduitWall, "Martian Madness", NPC.downedMartians),
            new EventEntry(ItemID.PumpkinMoonMedallion, "Pumpkin Moon", NPC.downedHalloweenKing || NPC.downedHalloweenTree),
            new EventEntry(ItemID.NaughtyPresent, "Frost Moon", NPC.downedChristmasIceQueen || NPC.downedChristmasSantank || NPC.downedChristmasTree),
            new EventEntry(ItemID.DD2ElderCrystal, "Old One's Army", DD2Event.DownedInvasionT1 || DD2Event.DownedInvasionT2 || DD2Event.DownedInvasionT3),
            new EventEntry(ItemID.BloodMoonStarter, "Blood Moon", Main.bloodMoon),
            new EventEntry(ItemID.SolarTablet, "Solar Eclipse", Main.eclipse)
        ];
    }
}
