using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Helpers;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI.Elements;

namespace PvPAdventure.Core.SpawnSelector.UI;

public class SpawnSelectorBasePanel : UIPanel
{
    private UIPanel _randomPanel;
    private readonly List<SpawnSelectorCharacter> _playerItems = new();

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

                if (p == null)
                    continue;

                if (p.whoAmI == local.whoAmI || p.team != local.team)
                    continue;

                players.Add(p);
            }
        }

//#if DEBUG
//        players.Add(Main.LocalPlayer);
//        Main.NewText($"[DEBUG] Added local player '{Main.LocalPlayer.name}' to SpawnSelectorBasePanel");
//#endif

        int playerCount = players.Count;

        float itemWidth = SpawnSelectorCharacter.ItemWidth;
        float itemHeight = SpawnSelectorCharacter.ItemHeight;
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

            var row = new SpawnSelectorCharacter(players[i]);
            row.Left.Set(x, 0f);
            row.Top.Set(y, 0f);

            Append(row);
            _playerItems.Add(row);
        }

        _randomPanel = new SpawnSelectorQuestionMark(
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

        Log.Debug($"UISpawnSelectorPanel.BuildLayout: players={playerCount}");
    }

    public void Rebuild()
    {
        BuildLayout();
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
            if (p == null || !p.active)
                continue;
            if (p.whoAmI == local.whoAmI || p.team != local.team)
                continue;

            players.Add(p.whoAmI);
        }

//#if DEBUG
//        players.Add(local.whoAmI);
//#endif

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
