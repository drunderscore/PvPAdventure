using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Helpers;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI.Elements;

namespace PvPAdventure.Core.Features.SpawnSelector.UI
{
    public class UISpawnSelectorPanel : UIPanel
    {
        private UIPanel _randomPanel;
        private readonly List<UISpawnSelectorCharacterListItem> _playerItems = new();

        private const float Spacing = 8f;
        private const float HorizontalPadding = 16f;
        private const float VerticalPadding = 12f;

        public override void OnInitialize()
        {
            HAlign = 0.5f;
            VAlign = 0.0f;
            Top.Set(52, 0);

            BackgroundColor = new Color(33, 43, 79) * 0.8f;
            SetPadding(0f);

            BuildLayout();

            Log.Info("UISpawnSelectorPanel.OnInitialize: layout built");
        }

        private void BuildLayout()
        {
            RemoveAllChildren();
            _playerItems.Clear();

            var players = new List<Player>();
            var local = Main.LocalPlayer;

            if (local.team != 0)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player p = Main.player[i];

                    if (p == null || !p.active || p.dead || p.statLife <= 0)
                        continue;

                    if (p.whoAmI == local.whoAmI || p.team != local.team)
                        continue;

                    players.Add(p);
                }
            }

            int playerCount = players.Count;

            float itemWidth = UISpawnSelectorCharacterListItem.ItemWidth;
            float itemHeight = UISpawnSelectorCharacterListItem.ItemHeight;
            float randomWidth = itemHeight;

            float contentWidth = playerCount * itemWidth
                                 + playerCount * Spacing
                                 + randomWidth;

            float panelWidth = contentWidth + HorizontalPadding * 2f;
            float panelHeight = itemHeight + VerticalPadding * 2f;

            if (panelWidth <= 0f)
                panelWidth = itemWidth + HorizontalPadding * 2f;

            if (panelHeight <= 0f)
                panelHeight = itemHeight + VerticalPadding * 2f;

            Width.Set(panelWidth, 0f);
            Height.Set(panelHeight, 0f);

            float startX = HorizontalPadding;
            float y = VerticalPadding;

            for (int i = 0; i < playerCount; i++)
            {
                float x = startX + i * (itemWidth + Spacing);

                var row = new UISpawnSelectorCharacterListItem(players[i]);
                row.Left.Set(x, 0f);
                row.Top.Set(y, 0f);

                Append(row);
                _playerItems.Add(row);
            }

            _randomPanel = new UISpawnSelectorRandomPanel(
                startX,
                itemHeight,
                playerCount,
                itemWidth,
                Spacing,
                y
            );
            Append(_randomPanel);

            Recalculate();
            RecalculateChildren();

            Log.Debug($"UISpawnSelectorPanel.BuildLayout: players={playerCount}, panelWidth={panelWidth}");
        }

        public void Rebuild()
        {
            BuildLayout();
            Log.Debug("UISpawnSelectorPanel.Rebuild: UI rebuilt");
        }

        public override void Update(GameTime gameTime)
        {
            if (NeedsRebuild())
            {
                Rebuild();
            }

            base.Update(gameTime);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
        }

        private bool NeedsRebuild()
        {
            var dims = GetDimensions();
            if (dims.Width <= 0f || dims.Height <= 0f)
                return true;

            if (_randomPanel == null)
                return true;

            var randomDims = _randomPanel.GetDimensions();
            if (randomDims.Width <= 0f || randomDims.Height <= 0f)
                return true;

            var local = Main.LocalPlayer;
            var players = new List<int>();

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                var p = Main.player[i];
                if (p == null || !p.active || p.dead || p.statLife <= 0)
                    continue;
                if (p.whoAmI == local.whoAmI || p.team != local.team)
                    continue;

                players.Add(p.whoAmI);
            }

            if (players.Count != _playerItems.Count)
                return true;

            for (int i = 0; i < players.Count; i++)
            {
                if (_playerItems[i] == null) return true;
                if (_playerItems[i].PlayerIndex != players[i]) return true;
            }

            return false;
        }
    }
}
