using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Chat;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.System;

[Autoload(Side = ModSide.Both)]
public class PointsManager : ModSystem
{
    private readonly Dictionary<Team, int> _points = new();
    private readonly Dictionary<Team, ISet<short>> _downedNpcs = new();

    public BossCompletionInterfaceLayer BossCompletion { get; private set; }

    public IReadOnlyDictionary<Team, int> Points => _points;
    public IReadOnlyDictionary<Team, ISet<short>> DownedNpcs => _downedNpcs;

    public UIScoreboard UiScoreboard { get; private set; }

    public override void Load()
    {
        if (!Main.dedServ)
        {
            UiScoreboard = new UIScoreboard(this);
            BossCompletion = new(this) { Active = false };
        }
    }

    public override void ClearWorld()
    {
        foreach (var team in Enum.GetValues<Team>())
        {
            _points[team] = 0;
            _downedNpcs[team] = new HashSet<short>();
        }
    }

    public override void SaveWorldData(TagCompound tag)
    {
        var points = new int[_points.Count];

        foreach (var (team, teamPoints) in _points)
            points[(int)team] = teamPoints;

        var downedNpcs = new int[_downedNpcs.Count][];

        foreach (var (team, downedNpc) in _downedNpcs)
            downedNpcs[(int)team] = downedNpc.Select(id => (int)id).ToArray();

        tag["points"] = points;
        tag["downedNpcs"] = downedNpcs.ToList();
    }

    public override void LoadWorldData(TagCompound tag)
    {
        var points = (int[])tag["points"];
        for (var i = 0; i < points.Length; i++)
            _points[(Team)i] = points[i];

        var downedNpcs = (List<int[]>)tag["downedNpcs"];
        for (var i = 0; i < downedNpcs.Count; i++)
            _downedNpcs[(Team)i] = downedNpcs[i].Select(id => (short)id).ToHashSet();
    }

    public override void NetSend(BinaryWriter writer)
    {
        writer.Write(_points.Count);
        foreach (var (team, teamPoints) in _points)
        {
            writer.Write((int)team);
            writer.Write(teamPoints);
        }

        writer.Write(_downedNpcs.Count);
        foreach (var (team, downedNpcs) in _downedNpcs)
        {
            writer.Write((int)team);
            writer.Write(downedNpcs.Count);
            foreach (var id in downedNpcs)
                writer.Write(id);
        }
    }

    public override void NetReceive(BinaryReader reader)
    {
        _points.Clear();
        _downedNpcs.Clear();

        var numberOfPointEntries = reader.ReadInt32();
        for (var i = 0; i < numberOfPointEntries; i++)
        {
            var team = (Team)reader.ReadInt32();
            var points = reader.ReadInt32();
            _points[team] = points;
        }

        var numberOfDownedNpcsEntries = reader.ReadInt32();
        for (var i = 0; i < numberOfDownedNpcsEntries; i++)
        {
            var team = (Team)reader.ReadInt32();
            _downedNpcs[team] = new HashSet<short>();

            var numberOfIdEntries = reader.ReadInt32();
            for (var j = 0; j < numberOfIdEntries; j++)
                _downedNpcs[team].Add(reader.ReadInt16());
        }

        // FIXME: Not really where this belongs? unsure.
        UiScoreboard.Invalidate();
    }

    // FIXME: We could be MUCH smarter.
    public override bool HijackGetData(ref byte messageType, ref BinaryReader reader, int playerNumber)
    {
        if (!Main.dedServ && messageType is MessageID.PlayerTeam or MessageID.PlayerActive)
            Main.QueueMainThreadAction(() => UiScoreboard.Invalidate());

        return false;
    }

    // FIXME: We could be MUCH smarter.
    public override bool HijackSendData(int whoAmI, int msgType, int remoteClient, int ignoreClient, NetworkText text,
        int number,
        float number2, float number3, float number4, int number5, int number6, int number7)
    {
        if (!Main.dedServ && msgType == MessageID.PlayerTeam)
            Main.QueueMainThreadAction(() => UiScoreboard.Invalidate());

        return false;
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if (!layers.Contains(BossCompletion))
        {
            var layerIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Inventory");

            if (layerIndex != -1)
                layers.Insert(layerIndex + 1, BossCompletion);
        }
    }

    private void VisualizePointChange(int change, Team team, Vector2 at, string defeated = null)
    {
        var firstPersonMessage = NetworkText.FromLiteral(FormatPointsVisually(change));
        NetworkText thirdPersonMessage = null;

        if (change > 0 && defeated != null)
            thirdPersonMessage =
                NetworkText.FromLiteral(
                    $"{team} Team awarded +{change} point{(change == 1 ? "" : "s")} for defeating {defeated}!");

        NetMessage.SendData(MessageID.CombatTextString,
            text: firstPersonMessage,
            number: (int)Main.teamColor[(int)team].PackedValue,
            number2: at.X,
            number3: at.Y
        );

        foreach (var player in Main.ActivePlayers)
        {
            if ((Team)player.team == team)
                ChatHelper.SendChatMessageToClient(firstPersonMessage, Main.teamColor[(int)team], player.whoAmI);
            else if (thirdPersonMessage != null)
                ChatHelper.SendChatMessageToClient(thirdPersonMessage, Main.teamColor[(int)team], player.whoAmI);
        }
    }

    public void AwardNpcKillToTeam(Team team, NPC npc)
    {
        var config = ModContent.GetInstance<AdventureConfig>();

        // Is this NPC assigned custom point values?
        if (!config.Points.Npc.TryGetValue(new NPCDefinition(npc.type), out var points))
        {
            // No, but they might be a boss.
            if (!npc.boss)
                return;

            points = config.Points.Boss;
        }

        var hasBeenDownedByThisTeam = _downedNpcs[team].Contains((short)npc.type);

        // This team has already downed this NPC, and it is not repeatable, don't award any points.
        if (hasBeenDownedByThisTeam && !points.Repeatable)
            return;

        var hasBeenDownedByAnyTeam = hasBeenDownedByThisTeam ||
                                     _downedNpcs.Values.Any(downedNpcs => downedNpcs.Contains((short)npc.type));

        var pointsToAward = hasBeenDownedByAnyTeam ? points.Additional : points.First;
        _points[team] += pointsToAward;

        _downedNpcs[team].Add((short)npc.type);

        // If this is part of the Eater of Worlds, mark ALL parts as defeated.
        // This specialization is not needed for The Destroyer -- only it's head is ever marked as a boss.
        if (AdventureNpc.IsPartOfEaterOfWorlds((short)npc.type))
        {
            _downedNpcs[team].Add(NPCID.EaterofWorldsHead);
            _downedNpcs[team].Add(NPCID.EaterofWorldsBody);
            _downedNpcs[team].Add(NPCID.EaterofWorldsTail);
        }

        string fullName;

        if (npc.type == NPCID.Retinazer || npc.type == NPCID.Spazmatism)
        {
            _downedNpcs[team].Add(NPCID.Spazmatism);
            _downedNpcs[team].Add(NPCID.Retinazer);
            fullName = "The Twins";
        }
        else
        {
            fullName = npc.FullName;
        }

        VisualizePointChange(pointsToAward, team, npc.position, $"[c/F58522:{fullName}]");

        if (Main.dedServ)
            NetMessage.SendData(MessageID.WorldData);
        else
            UiScoreboard.Invalidate();
    }

    public void AwardPlayerKillToTeam(Player killer, Player victim)
    {
        var config = ModContent.GetInstance<AdventureConfig>();
        var killerTeam = (Team)killer.team;

        // Even if certain oddities allowed this to happen, no point exchanging would actually occur.
        if (killerTeam == (Team)victim.team)
            return;

        var victimTeamPoints = _points[(Team)victim.team];
        var killerTeamPints = _points[killerTeam];
        // Find the lowest denomination of points we can take (can't take more than the other team has!)
        var pointsToTrade = Math.Min(victimTeamPoints, config.Points.PlayerKill);

        // If they had no points, then there isn't any work to do.
        if (pointsToTrade <= 0)
            return;

        _points[(Team)victim.team] -= pointsToTrade;
        _points[killerTeam] += pointsToTrade;

        if (victimTeamPoints > killerTeamPints)
            ModContent.GetInstance<BountyManager>().Award(killer, victim);

        VisualizePointChange(pointsToTrade, (Team)killer.team, killer.position,
            $"[c/{Main.teamColor[victim.team].Hex3()}:{victim.name}]");
        VisualizePointChange(-pointsToTrade, (Team)victim.team, victim.position);

        if (Main.dedServ)
            NetMessage.SendData(MessageID.WorldData);
        else
            UiScoreboard.Invalidate();
    }

    public class UIScoreboard(PointsManager pointsManager) : UIState
    {
        public override void OnInitialize()
        {
            Invalidate();
        }

        // FIXME: We could be smarter, but is that worth it?
        public void Invalidate()
        {
            RemoveAllChildren();

            var playerTeamsEnumeration = Main.player
                .Where(player => player.active)
                .Where(player => (Team)player.team != Team.None)
                .GroupBy(player => (Team)player.team)
                .MaxBy(group => group.Count());

            // If this is null, there couldn't possibly be anything to display.
            if (playerTeamsEnumeration == null)
                return;

            var root = new UIElement
            {
                Top = { Pixels = 280 },
                Width = { Percent = 1.0f },
                Height = { Percent = 1.0f }
            };

            const int paddingBetweenPlayerPanels = 10;
            var xOffset = 0;

            var numberOfPlayersOnLargestTeam = Math.Min(playerTeamsEnumeration.Count(), 8);

            foreach (var team in Enum.GetValues<Team>())
            {
                if (team == Team.None)
                    continue;

                var players = Main.player
                    .Where(player => player.active)
                    .Where(player => (Team)player.team == team)
                    .ToArray();

                if (players.Length == 0)
                    continue;

                var element = new UIElement
                {
                    // FIXME: not in pixels?
                    Width = { Pixels = 275 },
                    // FIXME: not in pixels?
                    Height = { Pixels = 400 },
                    Left = { Pixels = xOffset },
                };

                xOffset += 275 + paddingBetweenPlayerPanels;

                var playersPanel = new UIPanel
                {
                    Width = { Percent = 1.0f },
                    Height = { Percent = 1.0f },
                    BackgroundColor = Main.teamColor[(int)team] * 0.7f
                };

                var playersList = new UIList
                {
                    Width = { Percent = 1.0f },
                    Height = { Percent = 1.0f }
                };

                var points = pointsManager.Points[team];
                var pointsText = $"{points} point";
                if (points != 1)
                    pointsText += 's';

                playersList.Add(new UIText(pointsText, 0.5f, large: true)
                {
                    HAlign = 0.5f,
                    // So we don't get cut off by our parent panel at the top.
                    PaddingTop = 4.0f,
                    PaddingBottom = 12.0f,
                });

                var bountyShards = ModContent.GetInstance<BountyManager>().Bounties[team].Count;
                var bountyShardsText = $"{bountyShards} bounty shard";
                if (bountyShards != 1)
                    bountyShardsText += 's';

                playersList.Add(new UIText(bountyShardsText, 0.5f, large: true)
                {
                    HAlign = 0.5f,
                    PaddingBottom = 12.0f,
                });

                for (var i = 0; i < numberOfPlayersOnLargestTeam; i++)
                {
                    Player player = null;
                    if (i < players.Length)
                        player = players[i];

                    var playerContainer = new UIGrid
                    {
                        Width = { Percent = 1.0f },
                        Height = { Pixels = 40 },
                    };

                    var namePanel = new UIPanel
                    {
                        Width = { Percent = 0.6f },
                        Height = { Percent = 1.0f },
                        BackgroundColor = Color.Transparent
                    };

                    if (player != null)
                        namePanel.Append(new UIText(player.name));

                    playerContainer.Add(namePanel);

                    var kdPanel = new UIPanel
                    {
                        Width = { Percent = 0.35f },
                        Height = { Percent = 1.0f },
                        BackgroundColor = Color.Transparent
                    };

                    if (player != null)
                    {
                        var adventurePlayer = player.GetModPlayer<AdventurePlayer>();
                        var kills = adventurePlayer.Kills;
                        var deaths = adventurePlayer.Deaths;

                        kdPanel.Append(new UIText($"{kills} / {deaths}")
                        {
                            HAlign = 0.5f
                        });
                    }

                    playerContainer.Add(kdPanel);
                    playersList.Add(playerContainer);
                }

                playersPanel.Append(playersList);

                element.Append(playersPanel);
                root.Append(element);
            }

            root.Left = new()
            {
                Percent = 0.5f,
                Pixels = -xOffset + (xOffset / 2.0f),
            };

            Append(root);
        }
    }

    public class BossCompletionInterfaceLayer(PointsManager pointsManager)
        : GameInterfaceLayer("PvPAdventure: Boss Completion", InterfaceScaleType.None)
    {
        protected override bool DrawSelf()
        {
            const int horizontalSpaceBetweenBossHeads = 50;
            const int verticalSeparationBetweenBossHeadAndTeamIcon = 40;
            const int verticalSpaceBetweenTeamIcons = 35;
            const int containerPadding = 35;
            const int teamIconXOffset = 8;
            const int bossHeadTeamIconVisualSeparatorYOffset = 28;
            var teamIconsTexture = TextureAssets.Pvp[1].Value;

            var adventureConfig = ModContent.GetInstance<AdventureConfig>();
            var bosses = adventureConfig.BossOrder
                .Select(npcDefinition => (short)npcDefinition.Type)
                // Remove invalid/unloaded NPCs
                .Where(id => id != -1)
                .ToList();
            var numberOfBosses = bosses.Count;

            var onlyDisplayWorldEvilBoss = ModContent.GetInstance<AdventureConfig>().OnlyDisplayWorldEvilBoss &&
                                           bosses.Contains(NPCID.EaterofWorldsHead) &&
                                           bosses.Contains(NPCID.BrainofCthulhu);

            // We might have both evil bosses in this list, but we actually just want to display the one that
            // pertains to this world.
            if (onlyDisplayWorldEvilBoss)
                numberOfBosses -= 1;

            // FIXME: The left and right padding visually based on the team icons is somewhat off
            //        (too much right padding from team icon)
            // FIXME: Constant height is good, but should be calculated based on number of teams
            //        (which is also a constant), which would remove the "none" team and the height for it's indicator.
            var containerSize =
                new Vector2((containerPadding + (numberOfBosses * horizontalSpaceBetweenBossHeads)) - teamIconXOffset,
                    245);

            // Cast to int so no strange aliasing occurs on anything we render -- we only work with whole numbers in the end.
            var containerPosition = new Vector2((int)((Main.screenWidth / 2.0f) - (containerSize.X / 2.0f)),
                Main.screenHeight - (1080 - 740));

            Utils.DrawInvBG(Main.spriteBatch, containerPosition.X, containerPosition.Y, containerSize.X,
                containerSize.Y);

            var nextBossHeadPosition = new Vector2(containerPosition.X + containerPadding,
                containerPosition.Y + containerPadding);

            // FIXME: This alpha doesn't seem to work.
            var visualSeparatorColor = Color.White with { A = 60 };
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle((int)(containerPosition.X + 2),
                    (int)(nextBossHeadPosition.Y + bossHeadTeamIconVisualSeparatorYOffset), (int)(containerSize.X - 4),
                    2), visualSeparatorColor);

            foreach (var bossId in bosses)
            {
                if (onlyDisplayWorldEvilBoss)
                {
                    if (bossId == NPCID.BrainofCthulhu && !WorldGen.crimson ||
                        bossId == NPCID.EaterofWorldsHead && WorldGen.crimson)
                        continue;
                }

                // FIXME: Stupid hack for Golem head texture
                var headId = NPCID.Sets.BossHeadTextures[bossId == NPCID.Golem ? NPCID.GolemHead : bossId];

                var hasAnyTeamDownedThisBoss = pointsManager
                    .DownedNpcs
                    .Values
                    .Any(downedNpcs => downedNpcs.Contains(bossId));

                // If we don't have a texture, we just won't render one, but we'll keep the gap and indicators.
                if (headId != -1)
                    Main.BossNPCHeadRenderer.DrawWithOutlines(null, headId, nextBossHeadPosition,
                        hasAnyTeamDownedThisBoss ? Color.White : Color.Gray, 0.0f, 1.0f, SpriteEffects.None);

                var nextTeamIconPosition = new Vector2(nextBossHeadPosition.X - teamIconXOffset,
                    nextBossHeadPosition.Y + verticalSeparationBetweenBossHeadAndTeamIcon);

                foreach (var team in Enum.GetValues<Team>())
                {
                    if (team == Team.None)
                        continue;

                    if (!pointsManager.DownedNpcs[team].Contains(bossId))
                        continue;

                    Main.spriteBatch.Draw(teamIconsTexture, nextTeamIconPosition,
                        teamIconsTexture.Frame(6, 1, (int)team), Color.White);

                    nextTeamIconPosition.Y += verticalSpaceBetweenTeamIcons;
                }

                nextBossHeadPosition.X += horizontalSpaceBetweenBossHeads;
            }

            return base.DrawSelf();
        }
    }

    public class SetPointsCommand : ModCommand
    {
        public override void Action(CommandCaller caller, string input, string[] args)
        {
            // You can only use this command from chat in singleplayer.
            if (caller.CommandType == CommandType.Chat && Main.netMode != NetmodeID.SinglePlayer)
                return;

            if (args.Length < 2)
                return;

            if (!Enum.TryParse(args[0], true, out Team team) || (int)team >= Enum.GetValues<Team>().Length)
            {
                caller.Reply("Invalid team.", Color.Red);
                return;
            }

            if (!int.TryParse(args[1], out var points))
            {
                caller.Reply("Invalid points.", Color.Red);
                return;
            }

            var pointsManager = ModContent.GetInstance<PointsManager>();
            pointsManager._points[team] = points;

            if (Main.dedServ)
                NetMessage.SendData(MessageID.WorldData);
            else
                ModContent.GetInstance<PointsManager>().UiScoreboard.Invalidate();
        }

        public override string Command => "setpoints";
        public override CommandType Type => CommandType.Chat | CommandType.Console;
    }

    private static string FormatPointsVisually(int points) =>
        $"{points:+0;-#} point{(Math.Abs(points) == 1 ? "" : "s")}";
}