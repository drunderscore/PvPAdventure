using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace PvPAdventure.Common.SpawnSelector.UI;

/// <summary>
/// Character row of a player in the spawn selector UI.
/// </summary>
public class UISpawnCharacter : UIPanel
{
    public const float ItemWidth = 260f;
    public const float ItemHeight = 72f;

    private readonly Asset<Texture2D> dividerTexture;
    private readonly Asset<Texture2D> innerPanelTexture;
    private readonly Asset<Texture2D> playerBGTexture;

    private readonly int playerIndex;
    public int PlayerIndex => playerIndex;

    // UI
    private UIBedButton bedButton;
    
    #region Row Density
    public enum RowDensity
    {
        Normal,
        Compact,
        UltraCompact
    }
    private readonly RowDensity rowDensity;
    private readonly float itemWidth;

    public static RowDensity GetDensityForTeammateCount(int teammateCount)
    {
        if (teammateCount > 8)
            return RowDensity.UltraCompact;

        if (teammateCount >= 5)
            return RowDensity.Compact;

        return RowDensity.Normal;
    }

    public static float GetItemWidth(RowDensity density)
    {
        return density switch
        {
            RowDensity.Compact => 200f,
            RowDensity.UltraCompact => 110f,
            _ => ItemWidth,// 260f
        };
    }
    
    #endregion

    public UISpawnCharacter(int _playerIndex, RowDensity density)
    {
        playerIndex = _playerIndex;
        rowDensity = density;
        itemWidth = GetItemWidth(density);

        dividerTexture = Main.Assets.Request<Texture2D>("Images/UI/Divider");
        innerPanelTexture = Main.Assets.Request<Texture2D>("Images/UI/InnerPanelBackground");
        playerBGTexture = Ass.CustomPlayerBackground;

        // Bed button (top-right)
        var player = Main.player[playerIndex];
        bool hasBed = player.SpawnX != -1 && player.SpawnY != -1;
        bedButton = new UIBedButton(playerIndex, hasBed);
        bedButton.Top.Set(-2f, 0f);
        bedButton.Left.Set(itemWidth - 52, 0f);
        Append(bedButton);
    }

    // Set properties when activating.
    // Do not delete!
    public override void OnActivate()
    {
        BorderColor = Color.Black;
        BackgroundColor = new Color(63, 82, 151) * 0.7f;

        const float ItemHeight = 64;

        Height.Set(ItemHeight, 0f);
        Width.Set(itemWidth, 0f);

        SetPadding(6f);
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);

        if (bedButton != null && bedButton.IsMouseHovering)
            return;

        //bool playing = ModContent.GetInstance<GameManager>().CurrentPhase == GameManager.Phase.Playing;
        //if (!playing)
            //return;

        Player local = Main.LocalPlayer;
        if (local == null || !local.active)
            return;

        // Only allow selecting valid teammates
        if (!SpawnSystem.IsValidTeammateIndex(playerIndex))
            return;

        local.GetModPlayer<SpawnPlayer>()
            .ToggleSelection(SpawnType.Teammate, playerIndex);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        Player local = Main.LocalPlayer;
        if (local == null || !local.active)
            return;

        Player target = Main.player[playerIndex];
        bool validTarget =
            target != null &&
            target.active &&
            !target.dead &&
            SpawnSystem.IsValidTeammateIndex(playerIndex);

        bool hovering = IsMouseHovering && validTarget;

        // Hover routing
        if (hovering)
        {
            if (SpectateSystem.HoveringType != SpawnType.TeammateBed)
            {
                SpectateSystem.TrySetHover(SpawnType.Teammate, playerIndex);
            }

            local.mouseInterface = true;
        }
        else
        {
            if (SpectateSystem.HoveringType == SpawnType.Teammate &&
                SpectateSystem.HoveredPlayerIndex == playerIndex)
            {
                SpectateSystem.ClearHoverIfMatch(SpawnType.Teammate, playerIndex);
            }
        }

        var sp = local.GetModPlayer<SpawnPlayer>();
        bool selected =
            sp.SelectedType == SpawnType.Teammate &&
            sp.SelectedPlayerIndex == playerIndex;

        BackgroundColor =
            selected ? Color.Gold :
            hovering ? new Color(73, 92, 161, 150) :
            new Color(63, 82, 151) * 0.8f;
    }

    private void DrawHoverText(Player player)
    {
        if (player == null || !player.active)
            return;

        if (!player.dead)
        {
            // Prevent clicks/pings while hovering the UI element.
            player.mouseInterface = true;

            var sp = player.GetModPlayer<SpawnPlayer>();

            bool committed = sp.SelectedType == SpawnType.Teammate;
            bool ready = !SpawnSystem.CanTeleport;

            string text;

            if (ready)
            {
                text = committed
                    ? Language.GetTextValue("Mods.PvPAdventure.Spawn.CancelTeammateSpawn", player.name)
                    : Language.GetTextValue("Mods.PvPAdventure.Spawn.SelectTeammateSpawn", player.name);
            }
            else
            {
                text = Language.GetTextValue("Mods.PvPAdventure.Spawn.TeleportToTeammate", player.name);
            }
            Main.instance.MouseText(text);
            return;
        }

        Main.instance.MouseText("Cannot respawn (player dead)");
    }

    protected override void DrawSelf(SpriteBatch sb)
    {
        base.DrawSelf(sb);
        SetPadding(6);

        // Get player
        Player player = Main.player[playerIndex];

        if (player == null || !player.active)
        {
            var d = GetDimensions();
            var rect2 = d.ToRectangle();
            Utils.DrawBorderString(sb, "Unable to find player :(", rect2.Location.ToVector2() + new Vector2(50, 0), Color.White);
            return;
        }

        // Set Dimensions.
        // Dimensions of MapBGs is 115x65.
        CalculatedStyle inner = GetInnerDimensions();
        Vector2 pos = new(inner.X, inner.Y);

        // Set Ultra compact width
        int leftRectWidth = (int)(115 / 1.1f);
        if (rowDensity == RowDensity.UltraCompact)
            leftRectWidth = (int)itemWidth;

        // Main rectangle for player drawing
        Rectangle rect = new(
            x: (int)pos.X - 6,
            y: (int)pos.Y - 6,
            width: leftRectWidth,
            height: 65);

        DrawNineSlice(sb, rect.X, rect.Y, rect.Width, rect.Height, playerBGTexture.Value, Color.White, 5);
        DrawMapFullscreenBackground(sb, rect, player);
        DrawPlayerHead(player, pos);

        // Draw hover text for ultra compact mode
        if (rowDensity == RowDensity.UltraCompact)
        {
            if (IsMouseHovering)
                DrawHoverText(player);
            return;
        }

        // Draw statpanel
        DrawNameAndStatPanel(sb, inner, player.name, player.statLife, player.statMana);

        // Draw skull and respawn timer
        int respawnTimer = player.respawnTimer;
        if (respawnTimer != 0)
            DrawRespawnTimerAndDeadIcon(sb, respawnTimer, rect);

        // Draw hover text
        if (IsMouseHovering)
            DrawHoverText(player);
    }

    #region Draw Helpers

    private void DrawPlayerHead(Player player, Vector2 pos)
    {
        try
        {
            Vector2 playerDrawPos = pos + Main.screenPosition + new Vector2(34, 9);
            Vector2 headDrawPos = pos + new Vector2(42, 24);

            Color myTeamColor = Main.teamColor[Main.LocalPlayer.team];

            //Main.PlayerRenderer.DrawPlayer(Main.Camera,player,playerDrawPos,player.fullRotation,player.fullRotationOrigin,0f,0.9f);
            Main.MapPlayerRenderer.DrawPlayerHead(Main.Camera, player, headDrawPos, scale: 1.2f, borderColor: myTeamColor);
        }
        catch (Exception e)
        {
            Log.Error("Failed to draw p: " + e);
        }
    }

    private void DrawNameAndStatPanel(SpriteBatch sb, CalculatedStyle inner, string playerName, int playerLife, int playerMana)
    {
        const float leftColumnWidth = 115 - 3; // player background column
        float rightAreaLeft = inner.X + leftColumnWidth;

        // Exclude bed
        float bedExclude = 0f;
        const int bedGap = 0;

        if (bedButton != null)
        {
            bedExclude = bedButton.GetDimensions().Width + bedGap;
        }

        float rightAreaRight = inner.X + inner.Width - bedExclude;
        float rightAreaWidth = Math.Max(0f, rightAreaRight - rightAreaLeft);

        string name = string.IsNullOrWhiteSpace(playerName) ? "Unknown player" : playerName;

        // Debug long name: do not delete!
        //name = "123456";
        //name = "123456789";
        //name = "1234567890";
        //name = "1234567890123";
        //name = "1234567890123456";

        // WORKING logic for right-aligning name to the edge of the bed properly!
        int rightInset = rowDensity == RowDensity.Compact ? 41 : 48;
        float nameRight = inner.X + inner.Width - rightInset;
        float nameScale = rowDensity == RowDensity.Compact ? 0.8f : 1f;
        float nameY = inner.Y + (rowDensity == RowDensity.Compact ? 2f : 0f);
        Vector2 nameSize = FontAssets.MouseText.Value.MeasureString(name);
        float borderPad = 2f * nameScale;
        float scaledW = nameSize.X * nameScale;
        float nameX = nameRight - (scaledW + borderPad);
        if (nameX < inner.X)
            nameX = inner.X;

        // Custom extra logic to handle long names with smaller smale
        if (rowDensity == RowDensity.Normal)
        {
            if (name.Length > 4 && name.Length <=6)
            {
                nameX -= 9f;
            }
            if (name.Length == 12) nameX += 5f;

            if (name.Length > 12)
            {
                nameScale = 0.8f;
                nameY += 3f;
                nameX += 17f;
            }
            if (name.Length > 13) nameX += 9f;
            if (name.Length > 14) nameX += 9f;
            if (name.Length > 15) nameX += 5f;
        }

        Utils.DrawBorderString(sb, name, new Vector2(nameX, nameY), Color.White, nameScale);


        // Draw divider
        sb.Draw(dividerTexture.Value, new Vector2(rightAreaLeft - 15f, inner.Y + 21f), null, Color.White, 0f, Vector2.Zero, new Vector2(rightAreaWidth / 8 + 6.6f, 1f), SpriteEffects.None, 0f);

        // Stat panel settings
        float statScale = 0.88f;
        float statGap = 5f * statScale;

        // Draw stat panel
        float panelWidth = rightAreaWidth + 55;
        Vector2 panelPos = new(rightAreaLeft - 16, inner.Y + 26f);
        DrawPanel(sb, panelPos, panelWidth);

        bool drawMana = rowDensity == RowDensity.Normal;

        string hpText;
        string mpText;

        if (rowDensity == RowDensity.Compact)
        {
            hpText = $"{playerLife} HP";
            mpText = string.Empty;
        }
        else
        {
            hpText = $"{playerLife} HP";
            mpText = $"{playerMana} MP";
        }

        Vector2 hpSize = FontAssets.MouseText.Value.MeasureString(hpText) * statScale;
        Vector2 mpSize = FontAssets.MouseText.Value.MeasureString(mpText) * statScale;

        var heart = TextureAssets.Heart;
        var mana = TextureAssets.Mana;

        float heartW = heart.Width() * statScale;
        float manaW = mana.Width() * statScale;

        float hpBlockW = heartW + hpSize.X;

        float totalWidth = hpBlockW;
        if (drawMana)
        {
            float mpBlockW = manaW + mpSize.X;
            totalWidth = hpBlockW + statGap + mpBlockW;
        }

        float startX = panelPos.X + (panelWidth - totalWidth) * 0.5f;
        float y = panelPos.Y + 4f;

        // Draw health
        sb.Draw(heart.Value, new Vector2(startX, y), null, Color.White, 0f, Vector2.Zero, statScale, SpriteEffects.None, 0f);
        startX += heartW;
        Utils.DrawBorderString(sb, hpText, new Vector2(startX + 1, y + 1f), Color.White, statScale);
        startX += hpSize.X;

        // Draw mana
        if (drawMana)
        {
            startX += statGap;
            sb.Draw(mana.Value, new Vector2(startX, y - 2), null, Color.White, 0f, Vector2.Zero, statScale, SpriteEffects.None, 0f);
            startX += manaW;
            Utils.DrawBorderString(sb, mpText, new Vector2(startX + 1, y + 1f), Color.White, statScale);
        }
    }

    private void DrawRespawnTimerAndDeadIcon(SpriteBatch sb, int respawnTimer, Rectangle rect)
    {
        // Draw dead icon
        var tex = Ass.Icon_Dead.Value;
        Vector2 skullCenter = new(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.5f);
        float skullScale = 0.09f * 16f;
        sb.Draw(tex, skullCenter, null, Color.White * 0.5f, 0f, tex.Size() * 0.5f, skullScale, SpriteEffects.None, 0f);
        string respawnTimeInSeconds = (respawnTimer / 60 + 1).ToString();

        // Override text for spawn selector mode
        if (respawnTimer <= 2)
            respawnTimeInSeconds = "0";

        // Testing; do not delete!
        //respawnTimeInSeconds = "abcde";

        float timerScale = 1f;

        Vector2 timerSize = FontAssets.DeathText.Value.MeasureString(respawnTimeInSeconds) * timerScale;

        Vector2 timerPos = new(
            rect.X + (rect.Width - timerSize.X) * 0.5f,
            rect.Y + (rect.Height - timerSize.Y) * 0.5f
        );

        // manual adjustment
        timerPos.Y += 10;


        Utils.DrawBorderStringBig(sb, respawnTimeInSeconds, timerPos, Color.LightGray, scale: timerScale);
    }

    // Draws the inner panel background with a given width.
    private void DrawPanel(SpriteBatch spriteBatch, Vector2 position, float width)
    {
        Color color = Color.White;

        var sp = Main.LocalPlayer?.GetModPlayer<SpawnPlayer>();

        bool selectedSpawn = sp.SelectedType == SpawnType.Teammate && sp.SelectedPlayerIndex == playerIndex;

        if (selectedSpawn)
        {
            color = Color.Yellow * 0.5f;
        }

        // left
        spriteBatch.Draw(innerPanelTexture.Value, position,new Rectangle(0, 0, 8, innerPanelTexture.Height()), color);

        // middle
        spriteBatch.Draw(
            innerPanelTexture.Value,
            new Vector2(position.X + 8f, position.Y),
            new Rectangle(8, 0, 8, innerPanelTexture.Height()),
            color,
            0f,
            Vector2.Zero,
            new Vector2((width - 16f) / 8f, 1f),
            SpriteEffects.None,
            0f
        );

        // right
        spriteBatch.Draw(innerPanelTexture.Value,new Vector2(position.X + width - 8f, position.Y),new Rectangle(16, 0, 8, innerPanelTexture.Height()),color);
    }

    // Draws a nine-slice rectangle of a background.
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

            int midTileX = tileX;

            if (player.ZoneSkyHeight)
                bgIndex = 32;
            else if (player.ZoneCorrupt)
                bgIndex = player.ZoneDesert ? 36 : 5;
            else if (player.ZoneCrimson)
                bgIndex = player.ZoneDesert ? 37 : 6;
            else if (player.ZoneHallow)
                bgIndex = player.ZoneDesert ? 38 : 7;

            // "Ocean" style edges
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
    #endregion
}
