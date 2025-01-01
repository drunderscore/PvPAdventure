using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.System.Client.Interface;

[Autoload(Side = ModSide.Client)]
public class Scoreboard : ModSystem
{
    private readonly ScoreboardGameInterfaceLayer _scoreboardGameInterfaceLayer = new()
    {
        Active = false
    };

    public UIScoreboard UiScoreboard { get; } = new();

    public bool Visible
    {
        get => _scoreboardGameInterfaceLayer.Active;
        set => _scoreboardGameInterfaceLayer.Active = value;
    }

    // FIXME: We could be MUCH smarter.
    public override bool HijackGetData(ref byte messageType, ref BinaryReader reader, int playerNumber)
    {
        if (messageType is MessageID.PlayerTeam or MessageID.PlayerActive)
            Main.QueueMainThreadAction(() => UiScoreboard.Invalidate());

        return false;
    }

    // FIXME: We could be MUCH smarter.
    public override bool HijackSendData(int whoAmI, int msgType, int remoteClient, int ignoreClient, NetworkText text,
        int number,
        float number2, float number3, float number4, int number5, int number6, int number7)
    {
        if (msgType == MessageID.PlayerTeam)
            Main.QueueMainThreadAction(() => UiScoreboard.Invalidate());

        return false;
    }

    public class UIScoreboard : UIState
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
                Top = { Pixels = 325 },
                Left = { Percent = 0.5f },
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

                var points = ModContent.GetInstance<PointsManager>().Points[team];
                var pointsText = $"{points} point";
                if (points != 1)
                    pointsText += 's';

                playersList.Add(new UIText(pointsText, large: true)
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

            Append(root);
        }
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if (!layers.Contains(_scoreboardGameInterfaceLayer))
        {
            var layerIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Inventory");

            if (layerIndex != -1)
                layers.Insert(layerIndex + 1, _scoreboardGameInterfaceLayer);
        }
    }

    private class ScoreboardGameInterfaceLayer() : GameInterfaceLayer("PvPAdventure: Scoreboard", InterfaceScaleType.UI)
    {
        protected override bool DrawSelf()
        {
            DrawJamesBosses();

            return base.DrawSelf();
        }

        private void DrawJamesBosses()
        {
            const int horizontalSpaceBetweenBossHeads = 50;
            const int verticalSeparationBetweenBossHeadAndTeamIcon = 40;
            const int verticalSpaceBetweenTeamIcons = 35;
            const int containerPadding = 35;
            const int teamIconXOffset = 8;
            const int bossHeadTeamIconVisualSeparatorYOffset = 28;
            var teamIconsTexture = TextureAssets.Pvp[1].Value;

            var adventureConfig = ModContent.GetInstance<AdventureConfig>();
            var bosses = adventureConfig.BossOrder.Select(npcDefinition => (short)npcDefinition.Type).ToList();
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

            // Horizontally center the dialog, with a slight bias towards the right, mostly on 1920x1080, decreasingly
            // so as our width increases.
            // The bias is so we are put slightly past the inventory slots.
            // This likely doesn't work very well on resolutions that are lower than 1920x1080
            //
            // Cast to int so no strange aliasing occurs on anything we render -- we only work with whole numbers in the end.
            var containerPosition =
                new Vector2(
                    (int)(((Main.screenWidth / 2.0f) - (containerSize.X / 2.0f)) +
                          (60.0f * (1920.0f / Main.screenWidth))), 740);

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

                var hasAnyTeamDownedThisBoss = ModContent.GetInstance<PointsManager>()
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

                    if (!ModContent.GetInstance<PointsManager>().DownedNpcs[team].Contains(bossId))
                        continue;

                    Main.spriteBatch.Draw(teamIconsTexture, nextTeamIconPosition,
                        teamIconsTexture.Frame(6, 1, (int)team), Color.White);

                    nextTeamIconPosition.Y += verticalSpaceBetweenTeamIcons;
                }

                nextBossHeadPosition.X += horizontalSpaceBetweenBossHeads;
            }
        }
    }
}