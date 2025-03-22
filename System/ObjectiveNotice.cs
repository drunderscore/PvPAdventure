using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.GameContent.UI.Chat;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace PvPAdventure.System;

[Autoload(Side = ModSide.Client)]
public class ObjectiveNotice : ModSystem
{
    private readonly ObjectiveNoticeInterfaceLayer _objectiveNoticeInterfaceLayer = new();

    private class TeamTagHandler : ITagHandler
    {
        public TextSnippet Parse(string text, Color baseColor = default, string options = null)
        {
            TextSnippet snippet;

            // TryParse doesn't check the bounds (aside from whether the enum ordinal is wide enough), so do it ourselves.
            if (Enum.TryParse(text, true, out Team team))
            {
                var outline = false;

                if (options != null)
                {
                    var splitOptions = options.Split(',');
                    foreach (var individualOption in splitOptions)
                    {
                        if (individualOption == "o")
                            outline = true;
                    }
                }

                snippet = new TeamSnippet(team, outline);
                snippet.Color = baseColor;
            }
            else
            {
                snippet = new TextSnippet(text, baseColor);
            }

            snippet.DeleteWhole = true;

            return snippet;
        }
    }

    private class TeamSnippet(Team team, bool outline) : TextSnippet
    {
        private readonly Rectangle _rectangle = TextureAssets.Pvp[1].Value.Frame(6, 1, (int)team);
        public bool Outline { get; } = outline;

        public override bool UniqueDraw(bool justCheckingString, out Vector2 size, SpriteBatch spriteBatch,
            Vector2 position = new(), Color color = new(), float scale = 1)
        {
            const int padding = 2;
            position += new Vector2(padding);

            size = new Vector2(_rectangle.Width + (padding * 2), _rectangle.Height + (padding * 2));
            if (Outline)
            {
                size += new Vector2(4.0f);
                position += new Vector2(2.0f);
            }

            size *= scale;

            if (justCheckingString)
                return true;

            // Check for Black without alpha to prevent shadow versions of this from being rendered.
            // The non-alpha check is a hack akin to what TML did for ItemTagHandler because some overload messes with it.
            if (color is { R: 0, G: 0, B: 0 })
                return true;

            spriteBatch.Draw(
                TextureAssets.Pvp[1].Value,
                position,
                _rectangle,
                color,
                0.0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0.0f
            );

            if (Outline)
            {
                spriteBatch.Draw(
                    TextureAssets.Pvp[2].Value,
                    position - new Vector2(2.0f),
                    null,
                    color,
                    0.0f,
                    Vector2.Zero,
                    scale,
                    SpriteEffects.None,
                    0.0f
                );
            }

            return true;
        }

        public override float GetStringLength(DynamicSpriteFont font)
        {
            const int padding = 2;
            float length = _rectangle.Width + padding;

            if (Outline)
                length += 2.0f * 2;

            return length * Scale;
        }
    }

    public override void Load()
    {
        ChatManager.Register<TeamTagHandler>(["t", "team"]);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if (!layers.Contains(_objectiveNoticeInterfaceLayer))
        {
            var layerIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Inventory");

            if (layerIndex != -1)
                layers.Insert(layerIndex + 1, _objectiveNoticeInterfaceLayer);
        }
    }

    public abstract class Notice
    {
        public float TimeRemaining { get; set; } = 6;
        public abstract Vector2 Draw(Vector2 position);
    }

    public void AddPlayerDeathNotice(Player killer, Player victim, Item item)
    {
        _objectiveNoticeInterfaceLayer._notices.Add(new PlayerDeathNotice(killer.name, (Team)killer.team, victim.name,
            (Team)victim.team, item));
    }

    public void AddBossDeathNotice(Team team, Item item, NPC npc)
    {
        _objectiveNoticeInterfaceLayer._notices.Add(new BossDeathNotice(team, item, npc.FullName));
    }

    public void AddClaimReceivedNotice(Team team)
    {
        _objectiveNoticeInterfaceLayer._notices.Add(new ClaimReceivedNotice(team));
    }

    public class PlayerDeathNotice : Notice
    {
        private readonly TextSnippet[] _textSnippets;
        private readonly Vector2 _textSnippetsSize;

        public PlayerDeathNotice(string killerName, Team killerTeam, string victimName, Team victimTeam, Item item)
        {
            _textSnippets =
            [
                new TextSnippet(killerName + " ", Main.teamColor[(int)killerTeam]),
                // FIXME: Stupid because they made ItemSnippet private. Could reflect into it for less dumbassery.
                ..ChatManager.ParseMessage(ItemTagHandler.GenerateTag(item), Color.White),
                new TextSnippet(" " + victimName, Main.teamColor[(int)victimTeam])
            ];

            // FIXME: Duplicated/disassociated font usage here and below
            _textSnippetsSize = ChatManager.GetStringSize(FontAssets.MouseText.Value, _textSnippets, Vector2.One);
        }

        public override Vector2 Draw(Vector2 position)
        {
            const int horizontalContainerPadding = 8;
            const int verticalContainerPadding = 6;
            const int verticalContentMargin = 2;

            var containerSize = new Vector2(_textSnippetsSize.X + (horizontalContainerPadding * 2),
                _textSnippetsSize.Y + (verticalContainerPadding * 2));

            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle((int)position.X, (int)position.Y, (int)containerSize.X, (int)containerSize.Y),
                Color.Black with { A = 100 });

            // FIXME: padding
            // FIXME: Bounding box
            // FIXME: What does this even return? not the size, not position or smth. idk! we manually query size for now.
            ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.MouseText.Value, _textSnippets,
                position + new Vector2(horizontalContainerPadding, verticalContainerPadding + verticalContentMargin),
                0.0f, Vector2.Zero, Vector2.One, out var hoveredSnippet);

            if (hoveredSnippet != -1)
                _textSnippets[hoveredSnippet].OnHover();

            return containerSize;
        }
    }

    public class BossDeathNotice : Notice
    {
        private readonly TextSnippet[] _textSnippets;
        private readonly Vector2 _textSnippetsSize;

        public BossDeathNotice(Team team, Item item, string bossName)
        {
            _textSnippets =
            [
                new TeamSnippet(team, false),
                new TextSnippet(" "),
                // FIXME: Stupid because they made ItemSnippet private. Could reflect into it for less dumbassery.
                ..ChatManager.ParseMessage(ItemTagHandler.GenerateTag(item), Color.White),
                new TextSnippet(" " + bossName)
            ];

            // FIXME: Duplicated/disassociated font usage here and below
            _textSnippetsSize = ChatManager.GetStringSize(FontAssets.MouseText.Value, _textSnippets, Vector2.One);
        }

        public override Vector2 Draw(Vector2 position)
        {
            const int horizontalContainerPadding = 8;
            const int verticalContainerPadding = 6;
            const int verticalContentMargin = 2;

            var containerSize = new Vector2(_textSnippetsSize.X + (horizontalContainerPadding * 2),
                _textSnippetsSize.Y + (verticalContainerPadding * 2));

            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle((int)position.X, (int)position.Y, (int)containerSize.X, (int)containerSize.Y),
                Color.Black with { A = 100 });

            // FIXME: padding
            // FIXME: Bounding box
            // FIXME: What does this even return? not the size, not position or smth. idk! we manually query size for now.
            ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.MouseText.Value, _textSnippets,
                position + new Vector2(horizontalContainerPadding, verticalContainerPadding + verticalContentMargin),
                0.0f, Vector2.Zero, Vector2.One, out var hoveredSnippet);

            if (hoveredSnippet != -1)
                _textSnippets[hoveredSnippet].OnHover();

            return containerSize;
        }
    }

    public class ClaimReceivedNotice(Team team) : Notice
    {
        public override Vector2 Draw(Vector2 position)
        {
            var containerSize = new Vector2(120.0f, 40.0f);

            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle((int)position.X, (int)position.Y, (int)containerSize.X, (int)containerSize.Y),
                Color.Black with { A = 90 });

            var teamIconsTexture = TextureAssets.Pvp[1].Value;
            Main.spriteBatch.Draw(teamIconsTexture, new Vector2(position.X + 4.0f, position.Y + 4.0f),
                teamIconsTexture.Frame(6, 1, (int)team), Color.White);

            ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.MouseText.Value, "got a claim!",
                position + new Vector2(4.0f + 50.0f + 10.0f, 10.0f), Color.White, 0.0f, Vector2.Zero, Vector2.One);

            return containerSize;
        }
    }

    private class ObjectiveNoticeInterfaceLayer()
        : GameInterfaceLayer("PvPAdventure: Objective Notice", InterfaceScaleType.UI)
    {
        // FIXME: not public
        public readonly IList<Notice> _notices = new List<Notice>();

        protected override bool DrawSelf()
        {
            const int verticalSeparationBetweenNotices = 4;

            var position = new Vector2(1200, 20);

            // FIXME: wanted to reverse-iterate to remove notices, but this fucks up the order of the notices themselves.
            // we should go top-to-bottom, oldest-to-newest.
            for (var i = _notices.Count - 1; i >= 0; i--)
            {
                var notice = _notices[i];
                if ((notice.TimeRemaining -= (float)Main._drawInterfaceGameTime.ElapsedGameTime.TotalSeconds) <= 0)
                {
                    _notices.RemoveAt(i);
                    continue;
                }

                var noticeSize = notice.Draw(position);
                position.Y += noticeSize.Y + verticalSeparationBetweenNotices;
            }

            return base.DrawSelf();
        }
    }
}