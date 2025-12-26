using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common;
using PvPAdventure.Common.Debug;
using PvPAdventure.System;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.SpawnAndSpectate.UI;

/// <summary>
/// Character row of a player in the spawn selector UI.
/// </summary>
internal class SpawnAndSpectateCharacter : UIPanel
{
    internal const float ItemWidth = 260f;
    internal const float ItemHeight = 72f;

    private readonly Asset<Texture2D> dividerTexture;
    private readonly Asset<Texture2D> innerPanelTexture;
    private readonly Asset<Texture2D> playerBGTexture;

    private readonly int playerIndex;
    public int PlayerIndex => playerIndex;

    public SpawnAndSpectateCharacter(int _playerIndex)
    {
        // Set player index
        playerIndex = _playerIndex;

        // Load assets
        dividerTexture = Main.Assets.Request<Texture2D>("Images/UI/Divider");
        innerPanelTexture = Main.Assets.Request<Texture2D>("Images/UI/InnerPanelBackground");
        playerBGTexture = Ass.CustomPlayerBackground;
        //_playerBGTexture = Main.Assets.Request<Texture2D>("Images/UI/PlayerBackground");
    }

    public override void OnActivate()
    {
        BorderColor = Color.Black;
        BackgroundColor = new Color(63, 82, 151) * 0.7f;

        Height.Set(ItemHeight, 0f);
        Width.Set(ItemWidth, 0f);

        SetPadding(6f);
    }

    public override void MouseOver(UIMouseEvent evt)
    {
        base.MouseOver(evt);

        Player p = Main.player[playerIndex];
        if (p == null || !p.active || p.dead)
            return;
        BackgroundColor = new Color(73, 92, 161, 150);
        SpawnAndSpectateSystem.HoveredPlayerIndex = playerIndex;
    }

    public override void MouseOut(UIMouseEvent evt)
    {
        base.MouseOut(evt);

        Player p = Main.player[playerIndex];
        if (p == null || !p.active || p.dead)
            return;

        BackgroundColor = new Color(63, 82, 151) * 0.8f;
        if (SpawnAndSpectateSystem.HoveredPlayerIndex == playerIndex)
            SpawnAndSpectateSystem.HoveredPlayerIndex = null;
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);

        var gm = ModContent.GetInstance<GameManager>();
        if (gm.CurrentPhase != GameManager.Phase.Playing)
            return;

        var respawnPlayer = Main.LocalPlayer.GetModPlayer<RespawnPlayer>();

        // Alive + in spawn region: instant teleport, no commit, no border semantics.
        if (SpawnAndSpectateSystem.IsAliveSpawnRegionInstant)
        {
            respawnPlayer.TeammateTeleport(playerIndex);
            return;
        }

        // Dead: click selects (and spectates) the teammate. Only one selection at a time.
        if (Main.LocalPlayer.dead && SpawnAndSpectateSystem.IsValidTeammateIndex(playerIndex))
        {
            bool wasSelected = respawnPlayer.IsTeammateCommitted(playerIndex);
            respawnPlayer.ToggleCommitTeammate(playerIndex);

            bool isSelectedNow = respawnPlayer.IsTeammateCommitted(playerIndex);
            if (isSelectedNow)
                SpawnAndSpectateSystem.TrySetSpectate(playerIndex);
            else if (wasSelected && SpawnAndSpectateSystem.SpectatePlayerIndex == playerIndex)
                SpawnAndSpectateSystem.ClearSpectate();
        }
    }

    public override void RightClick(UIMouseEvent evt)
    {
        base.RightClick(evt);
        SpawnAndSpectateSystem.ToggleSpectateOnPlayerIndex(playerIndex);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        bool hovered = IsMouseHovering;

        var respawnPlayer = Main.LocalPlayer?.GetModPlayer<RespawnPlayer>();
        bool selectedSpawn = respawnPlayer != null && respawnPlayer.IsTeammateCommitted(playerIndex);
        bool spectated = SpawnAndSpectateSystem.SpectatePlayerIndex == playerIndex;

        BackgroundColor = hovered ? new Color(73, 92, 161, 150) : new Color(63, 82, 151) * 0.8f;
        BorderColor = spectated ? Color.Cyan : selectedSpawn ? Color.Yellow : Color.Black;
    }

    protected override void DrawSelf(SpriteBatch sb)
    {
        base.DrawSelf(sb);

        CalculatedStyle inner = GetInnerDimensions();

        Player player = Main.player[playerIndex];
        var dims = GetDimensions();

        if (player == null || !player.active)
        {
            var rect2 = dims.ToRectangle();

            Utils.DrawBorderString(sb, "Unable to find player :(", rect2.Location.ToVector2() + new Vector2(50, 0), Color.White);
            return;
        }

        // Left player background
        Vector2 pos = new(inner.X, inner.Y);
        //sb.Draw(_playerBGTexture.Value, pos, Color.White*0.5f);

        Rectangle rect = new(
            x: (int)pos.X - 6,
            y: (int)pos.Y - 6,
            width: 100,
            height: 72);


        //DrawMask(sb, Ass.CornerMask4px.Value, rect, 9);
        DrawNineSlice(sb, rect.X, rect.Y, rect.Width, rect.Height, playerBGTexture.Value, Color.White, 5);
        DrawMapFullscreenBackground(sb, rect, player);

        try
        {
            Vector2 playerDrawPos = pos + Main.screenPosition + new Vector2(34, 9);
            Vector2 headDrawPos = pos + new Vector2(38, 30);

            Color myTeamColor = Main.teamColor[Main.LocalPlayer.team];

            //Main.PlayerRenderer.DrawPlayer(Main.Camera,player,playerDrawPos,player.fullRotation,player.fullRotationOrigin,0f,0.9f);
            Main.MapPlayerRenderer.DrawPlayerHead(Main.Camera, player, headDrawPos, scale: 1.0f, borderColor: myTeamColor);
        }
        catch (Exception e)
        {
            Log.Error("Failed to draw p: " + e);
        }

        // Use the actual layout widths, not the texture width
        const float leftColumnWidth = 106f;              // player background column
        const float rightAreaWidth = ItemWidth - 22f - leftColumnWidth; // 260 - 12 - 106 = 142

        float rightAreaLeft = inner.X + leftColumnWidth;
        float rightAreaCenterX = rightAreaLeft + rightAreaWidth * 0.5f;

        // Name centered in the right-area
        string name = string.IsNullOrEmpty(player.name) ? "Unknown player" : player.name + "";
        float nameScale = 1f;
        if (player.name.Length > 16) nameScale = 0.85f;
        Vector2 nameSize = FontAssets.MouseText.Value.MeasureString(name) * nameScale;

        Vector2 namePos = new(
            rightAreaCenterX - nameSize.X * 0.5f,
            inner.Y
        );

        // Draw name
        Utils.DrawBorderString(sb, name, namePos, Color.White, nameScale);

        // Draw divider
        sb.Draw(dividerTexture.Value, new Vector2(rightAreaLeft - 12, inner.Y + 21f), null, Color.White, 0f, Vector2.Zero, new Vector2(rightAreaWidth / 8 + 2.2f, 1f), SpriteEffects.None, 0f);

        // Stat panel settings
        float statScale = 0.88f;
        float statGap = 5f * statScale;

        // Draw stat panel
        float panelWidth = rightAreaWidth + 24;
        Vector2 panelPos = new(rightAreaLeft - 14, inner.Y + 29f);
        DrawPanel(sb, panelPos, panelWidth);

        string hpText = $"{player.statLife} HP";
        string mpText = $"{player.statMana} MP";

        Vector2 hpSize = FontAssets.MouseText.Value.MeasureString(hpText) * statScale;
        Vector2 mpSize = FontAssets.MouseText.Value.MeasureString(mpText) * statScale;

        var heart = TextureAssets.Heart;
        var mana = TextureAssets.Mana;

        float heartW = heart.Width() * statScale;
        float manaW = mana.Width() * statScale;
        float hpBlockW = heartW + hpSize.X;
        float mpBlockW = manaW + mpSize.X;
        float totalWidth = hpBlockW + statGap + mpBlockW;
        float startX = panelPos.X + (panelWidth - totalWidth) * 0.5f;
        float y = panelPos.Y + 4f;

        // Draw health
        sb.Draw(heart.Value, new Vector2(startX, y), null, Color.White, 0f, Vector2.Zero, statScale, SpriteEffects.None, 0f);
        startX += heartW;
        Utils.DrawBorderString(sb, hpText, new Vector2(startX + 1, y + 1f), Color.White, statScale);
        startX += hpSize.X;
        startX += statGap;

        // Draw mana
        sb.Draw(mana.Value, new Vector2(startX, y - 2), null, Color.White, 0f, Vector2.Zero, statScale, SpriteEffects.None, 0f);
        startX += manaW;
        Utils.DrawBorderString(sb, mpText, new Vector2(startX + 1, y + 1f), Color.White, statScale);

        // Draw player respawn timer if it exists
        string respawnTimeInSeconds = (player.respawnTimer / 60 + 1).ToString();

        // Override text for spawn selector mode
        if (player.respawnTimer <= 2)
            respawnTimeInSeconds = 0.ToString();

        if (player.respawnTimer != 0)
        {
            Utils.DrawBorderStringBig(sb, respawnTimeInSeconds, pos + new Vector2(31, 4), Color.Gray, scale: 1f);
        }

        // Draw teleport to if it exists
        if (IsMouseHovering)
        {
            Main.LocalPlayer.mouseInterface = true;

            if (!player.dead)
            {
                var respawnPlayer = Main.LocalPlayer.GetModPlayer<RespawnPlayer>();
                var spawnPointPlayer = Main.LocalPlayer.GetModPlayer<SpawnPointPlayer>();
                bool ready = SpawnAndSpectateSystem.CanRespawn || spawnPointPlayer.IsPlayerInSpawnRegion();
                bool selectedSpawn = respawnPlayer.IsTeammateCommitted(player.whoAmI);

                string text;
                if (ready)
                {
                    // No prior commit at the ready gate: first click will commit and execute immediately.
                    text = Language.GetTextValue("Mods.PvPAdventure.SpawnAndSpectate.TeleportToPlayer", player.name);
                }
                else
                {
                    text = selectedSpawn
                        ? $"{player.name} selected for spawn"
                        : Language.GetTextValue("Mods.PvPAdventure.SpawnAndSpectate.SelectPlayerSpawn", player.name);
                }

                bool canSpectate = SpawnAndSpectateSystem.IsAliveSpawnRegionInstant;
                if (canSpectate)
                {
                    text += SpawnAndSpectateSystem.SpectatePlayerIndex == player.whoAmI
                        ? "\n" + Language.GetTextValue("Mods.PvPAdventure.SpawnAndSpectate.StopSpectatingPlayer", player.name)
                        : "\n" + Language.GetTextValue("Mods.PvPAdventure.SpawnAndSpectate.SpectatePlayer", player.name);
                }

                Main.instance.MouseText(text);
            }
        }
    }

    #region Draw Helpers

    // Draws the inner panel background with a given width.
    private void DrawPanel(SpriteBatch spriteBatch, Vector2 position, float width)
    {
        spriteBatch.Draw(innerPanelTexture.Value, position,
            new Rectangle(0, 0, 8, innerPanelTexture.Height()), Color.White);

        spriteBatch.Draw(
            innerPanelTexture.Value,
            new Vector2(position.X + 8f, position.Y),
            new Rectangle(8, 0, 8, innerPanelTexture.Height()),
            Color.White,
            0f,
            Vector2.Zero,
            new Vector2((width - 16f) / 8f, 1f),
            SpriteEffects.None,
            0f
        );

        spriteBatch.Draw(
            innerPanelTexture.Value,
            new Vector2(position.X + width - 8f, position.Y),
            new Rectangle(16, 0, 8, innerPanelTexture.Height()),
            Color.White
        );
    }

    private void DrawNineSlice(SpriteBatch sb, int x, int y, int w, int h, Texture2D tex, Color color, int inset, int c = 5)
    {
        x += inset; y += inset; w -= inset * 2; h -= inset * 2;
        int ew = tex.Width - c * 2;
        int eh = tex.Height - c * 2;

        sb.Draw(tex, new Rectangle(x, y, c, c), new Rectangle(0, 0, c, c), color);
        sb.Draw(tex, new Rectangle(x + c, y, w - c * 2, c), new Rectangle(c, 0, ew, c), color);
        sb.Draw(tex, new Rectangle(x + w - c, y, c, c), new Rectangle(tex.Width - c, 0, c, c), color);

        sb.Draw(tex, new Rectangle(x, y + c, c, h - c * 2), new Rectangle(0, c, c, eh), color);
        sb.Draw(tex, new Rectangle(x + c, y + c, w - c * 2, h - c * 2), new Rectangle(c, c, ew, eh), color);
        sb.Draw(tex, new Rectangle(x + w - c, y + c, c, h - c * 2), new Rectangle(tex.Width - c, c, c, eh), color);

        sb.Draw(tex, new Rectangle(x, y + h - c, c, c), new Rectangle(0, tex.Height - c, c, c), color);
        sb.Draw(tex, new Rectangle(x + c, y + h - c, w - c * 2, c), new Rectangle(c, tex.Height - c, ew, c), color);
        sb.Draw(tex, new Rectangle(x + w - c, y + h - c, c, c), new Rectangle(tex.Width - c, tex.Height - c, c, c), color);
    }
    #endregion

    // Finds the biome of the given player and draws it.
    public static void DrawMapFullscreenBackground(SpriteBatch sb, Rectangle rect, Player player)
    {
        if (player == null || !player.active)
            return;

        // Player tile coordinates
        int tileX = (int)(player.Center.X / 16f);
        int tileY = (int)(player.Center.Y / 16f);

        Tile tile = Main.tile[tileX, tileY];
        if (tile == null)
            return;

        int wall = tile.WallType;
        int bgIndex = -1;
        Color color = Color.White;

        // Use player Y position to determine underground/cavern/hell layers
        float playerYWorld = player.Center.Y;
        float playerYTiles = playerYWorld / 16f;

        // Hell layer
        if (playerYWorld > (Main.maxTilesY - 232) * 16)
        {
            bgIndex = 2;
        }
        // Dungeon
        else if (player.ZoneDungeon)
        {
            bgIndex = 4;
        }
        // Spider cave (?) wall
        else if (wall == 87)
        {
            bgIndex = 13;
        }
        // Underground / cavern backgrounds
        else if (playerYWorld > Main.worldSurface * 16.0)
        {
            bgIndex = wall switch
            {
                86 or 108 => 15,
                180 or 184 => 16,
                178 or 183 => 17,
                62 or 263 => 18,
                _ => player.ZoneGlowshroom ? 20 :
                     player.ZoneCorrupt ? player.ZoneDesert ? 39 : player.ZoneSnow ? 33 : 22 :
                     player.ZoneCrimson ? player.ZoneDesert ? 40 : player.ZoneSnow ? 34 : 23 :
                     player.ZoneHallow ? player.ZoneDesert ? 41 : player.ZoneSnow ? 35 : 21 :
                     player.ZoneSnow ? 3 :
                     player.ZoneJungle ? 12 :
                     player.ZoneDesert ? 14 :
                     player.ZoneRockLayerHeight ? 31 : 1
            };
        }
        // Surface mushroom biome
        else if (player.ZoneGlowshroom)
        {
            bgIndex = 19;
        }
        else
        {
            color = Color.White;

            if (player.dead)
                color = new Color(50, 50, 50, 255);

            // Use the *player’s* X position
            int midTileX = tileX;

            if (player.ZoneSkyHeight)
                bgIndex = 32;
            else if (player.ZoneCorrupt)
                bgIndex = player.ZoneDesert ? 36 : 5;
            else if (player.ZoneCrimson)
                bgIndex = player.ZoneDesert ? 37 : 6;
            else if (player.ZoneHallow)
                bgIndex = player.ZoneDesert ? 38 : 7;
            // "Ocean" style edges: use player’s tileY + tileX
            else if (playerYTiles < Main.worldSurface + 10.0 &&
                     (midTileX < 380 || midTileX > Main.maxTilesX - 380))
                bgIndex = 10;
            else if (player.ZoneSnow)
                bgIndex = 11;
            else if (player.ZoneJungle)
                bgIndex = 8;
            else if (player.ZoneDesert)
                bgIndex = 9;
            else if (Main.bloodMoon)
            {
                bgIndex = 25;
                color *= 2f;
            }
            else if (player.ZoneGraveyard)
                bgIndex = 26;
        }

        int safeIndex = bgIndex >= 0 && bgIndex < Ass.MapBG.Length ? bgIndex : 0;
        var asset = Ass.MapBG[safeIndex];

        rect.X += 10;
        rect.Y += 10;
        rect.Width -= 20;
        rect.Height -= 20;

        if (asset == null || asset.Value == null)
            return;

        sb.Draw(asset.Value, rect, color);
    }

}
