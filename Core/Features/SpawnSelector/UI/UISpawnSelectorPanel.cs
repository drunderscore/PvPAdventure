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

#if DEBUG
            // If we have debug players, use them instead of scanning teammates
            if (_debugPlayers.Count > 0)
            {
                players.AddRange(_debugPlayers);
            }
            else
#endif
            {
                // ADD TEAMMATES
                if (local.team != 0)
                {
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player p = Main.player[i];

                        if (p == null || !p.active)
                            continue;

                        if (p.whoAmI == local.whoAmI || p.team != local.team)
                            continue;

                        players.Add(p);
                    }
                }
            }

            int playerCount = players.Count;

            float itemWidth = UISpawnSelectorCharacterListItem.ItemWidth;
            float itemHeight = UISpawnSelectorCharacterListItem.ItemHeight;
            float randomWidth = itemHeight; // square random panel

            float contentWidth = playerCount * itemWidth
                                 + playerCount * Spacing
                                 + randomWidth;

            float panelWidth = contentWidth + HorizontalPadding * 2f;
            float panelHeight = itemHeight + VerticalPadding * 2f;

            Width.Set(panelWidth, 0f);
            Height.Set(panelHeight, 0f);

            float startX = HorizontalPadding;
            float y = VerticalPadding;

            // Add players in a row
            for (int i = 0; i < playerCount; i++)
            {
                float x = startX + i * (itemWidth + Spacing);

                var row = new UISpawnSelectorCharacterListItem(players[i]);
                row.Left.Set(x, 0f);
                row.Top.Set(y, 0f);

                Append(row);
                _playerItems.Add(row);
            }

            // Square random panel to the right of the last player
            _randomPanel = new UISpawnSelectorRandomPanel(
                startX,
                itemHeight,
                playerCount,
                itemWidth,
                Spacing,
                y
            );
            Append(_randomPanel);

            Log.Debug($"UISpawnSelectorPanel.BuildLayout: players={playerCount}, panelWidth={panelWidth}");
        }

        public void Rebuild()
        {
            BuildLayout();
            Recalculate();
            RecalculateChildren();
            Log.Debug("UISpawnSelectorPanel.Rebuild: UI rebuilt");
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
        }

        #region Debug
#if DEBUG
        private readonly List<Player> _debugPlayers = new();
#endif
        public void DebugAddPlayer()
        {
#if DEBUG
            _debugPlayers.Add(Main.LocalPlayer);
            BuildLayout();
            Log.Debug($"SpawnSelector DebugAddPlayer: debugPlayers={_debugPlayers.Count}");
#endif
        }

        public void DebugClearPlayers()
        {
#if DEBUG
            _debugPlayers.Clear();
            BuildLayout();
            Log.Debug("SpawnSelector DebugClearPlayers: cleared debug players");
#endif
        }
        #endregion


    }
}
