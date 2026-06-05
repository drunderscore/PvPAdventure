using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Common.World
{
    /// <summary>
    /// Temporary system that makes certain events occur at certain times, always
    ///  40 min elapsed / Day 3 @ 12:00 AM, Hardmode
    ///  85 min elapsed / Day 4 @  8:15 PM, Mech bosses
    /// 110 min elapsed / Day 5 @ 10:09 PM,  Plantera
    /// 130 min elapsed / Day 6 @  2:45 PM, Golem
    /// </summary>
    internal class BespokeProgression : ModSystem
    {
        public static int WorldDay { get; private set; } = 1;
        private bool _passedMidnightThisNight;

        private bool _hardmodeStarted;
        private bool _mechBossesDowned;
        private bool _planteraDowned;
        private bool _golemDowned;

        private const double MidnightTick = 18_000;

        private const double NightTick_MechBoss = 6_300;
        private const double NightTick_Plantera = 9_900;
        private const double DayTick_Golem = 49_500;

        public override void OnWorldLoad()
        {
            _passedMidnightThisNight = false;
        }

        public override void ClearWorld()
        {
            WorldDay = 1;
            _passedMidnightThisNight = false;
            _hardmodeStarted = false;
            _mechBossesDowned = false;
            _planteraDowned = false;
            _golemDowned = false;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            tag["worldDay"] = WorldDay;
            tag["hardmodeStarted"] = _hardmodeStarted;
            tag["mechBossesDowned"] = _mechBossesDowned;
            tag["planteraDowned"] = _planteraDowned;
            tag["golemDowned"] = _golemDowned;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            WorldDay = tag.ContainsKey("worldDay") ? tag.GetAsInt("worldDay") : 1;
            _hardmodeStarted = tag.ContainsKey("hardmodeStarted") ? tag.Get<bool>("hardmodeStarted") : false;
            _mechBossesDowned = tag.ContainsKey("mechBossesDowned") ? tag.Get<bool>("mechBossesDowned") : false;
            _planteraDowned = tag.ContainsKey("planteraDowned") ? tag.Get<bool>("planteraDowned") : false;
            _golemDowned = tag.ContainsKey("golemDowned") ? tag.Get<bool>("golemDowned") : false;
        }

        public override void NetSend(System.IO.BinaryWriter writer)
        {
            writer.Write(WorldDay);
            writer.Write(_hardmodeStarted);
            writer.Write(_mechBossesDowned);
            writer.Write(_planteraDowned);
            writer.Write(_golemDowned);
        }

        public override void NetReceive(System.IO.BinaryReader reader)
        {
            WorldDay = reader.ReadInt32();
            _hardmodeStarted = reader.ReadBoolean();
            _mechBossesDowned = reader.ReadBoolean();
            _planteraDowned = reader.ReadBoolean();
            _golemDowned = reader.ReadBoolean();
        }

        public override void PostUpdateWorld()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            AdvanceDayCounter();
            CheckScheduledEvents();
        }

        private void AdvanceDayCounter()
        {
            if (!Main.dayTime)
            {
                if (!_passedMidnightThisNight && Main.time >= MidnightTick)
                {
                    WorldDay++;
                    _passedMidnightThisNight = true;
                }
            }
            else
            {
                _passedMidnightThisNight = false;
            }
        }

        private void CheckScheduledEvents()
        {
            if (!_hardmodeStarted
                && WorldDay >= 3
                && !Main.dayTime
                && Main.time >= MidnightTick)
            {
                TriggerHardmode();
                _hardmodeStarted = true;
            }
            if (!_mechBossesDowned
                && WorldDay == 4
                && !Main.dayTime
                && Main.time >= NightTick_MechBoss
                && Main.time < MidnightTick)
            {
                TriggerMechBossesDefeated();
                _mechBossesDowned = true;
            }
            if (!_planteraDowned
                && WorldDay == 5
                && !Main.dayTime
                && Main.time >= NightTick_Plantera
                && Main.time < MidnightTick)
            {
                TriggerPlanteraDefeated();
                _planteraDowned = true;
            }
            if (!_golemDowned
                && WorldDay >= 6
                && Main.dayTime
                && Main.time >= DayTick_Golem
                && _planteraDowned)
            {
                TriggerGolemDefeated();
                _golemDowned = true;
            }
        }

        private static void TriggerHardmode()
        {
            if (Main.hardMode)
                return;

            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                WorldGen.StartHardmode();
            });
        }

        private static void TriggerMechBossesDefeated()
        {
            NPC.downedMechBoss1 = true;
            NPC.downedMechBoss2 = true;
            NPC.downedMechBoss3 = true;
            NPC.downedMechBossAny = true;

            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.WorldData);

            BroadcastMessage("The jungle grows restless...",
                new Microsoft.Xna.Framework.Color(150, 255, 50));
        }

        private static void TriggerPlanteraDefeated()
        {
            NPC.downedPlantBoss = true;

            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.WorldData);

            BroadcastMessage("Screams echo from the dungeon...",
                new Microsoft.Xna.Framework.Color(200, 50, 255));
        }

        private static void TriggerGolemDefeated()
        {
            NPC.downedGolemBoss = true;

            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.WorldData);
        }

        private static void BroadcastMessage(string text, Microsoft.Xna.Framework.Color colour)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                Terraria.Localization.NetworkText netText =
                    Terraria.Localization.NetworkText.FromLiteral(text);
                ChatHelper.BroadcastChatMessage(netText, colour);
            }
            else
            {
                Main.NewText(text, colour.R, colour.G, colour.B);
            }
        }
    }
}