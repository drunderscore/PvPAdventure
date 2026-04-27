using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Spectator.SpectatorMode;
using PvPAdventure.Core.Config;
using PvPAdventure.Core.Utilities;
using ReLogic.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace PvPAdventure.Common.Spectator.Visualization;

/// <summary>
/// Skip drawing ghost player nameplates 
/// (which are only drawn when they are on same team, which usually never happens, but whatever).
/// </summary>
[Autoload(Side = ModSide.Client)]
internal sealed class DrawNameplatesSpectator : ModSystem
{
    public override void Load()
    {
        On_NewMultiplayerClosePlayersOverlay.Draw += DrawNamesAfterNewOverlay;
    }

    public override void Unload()
    {
        On_NewMultiplayerClosePlayersOverlay.Draw -= DrawNamesAfterNewOverlay;
    }

    private static void DrawNamesAfterNewOverlay(On_NewMultiplayerClosePlayersOverlay.orig_Draw orig, NewMultiplayerClosePlayersOverlay self)
    {
        ModContent.GetInstance<DrawNameplatesSpectator>().VanillaDraw();
    }

    private struct PlayerOnScreenCache
    {
        private string _name;

        private Vector2 _pos;

        private Color _color;

        public PlayerOnScreenCache(string name, Vector2 pos, Color color)
        {
            this._name = name;
            this._pos = pos;
            this._color = color;
        }

        public void DrawPlayerName_WhenPlayerIsOnScreen(SpriteBatch spriteBatch)
        {
            this._pos = this._pos.Floor();
            spriteBatch.DrawString(FontAssets.MouseText.Value, this._name, new Vector2(this._pos.X - 2f, this._pos.Y), Color.Black, 0f, default(Vector2), 1f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(FontAssets.MouseText.Value, this._name, new Vector2(this._pos.X + 2f, this._pos.Y), Color.Black, 0f, default(Vector2), 1f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(FontAssets.MouseText.Value, this._name, new Vector2(this._pos.X, this._pos.Y - 2f), Color.Black, 0f, default(Vector2), 1f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(FontAssets.MouseText.Value, this._name, new Vector2(this._pos.X, this._pos.Y + 2f), Color.Black, 0f, default(Vector2), 1f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(FontAssets.MouseText.Value, this._name, this._pos, this._color, 0f, default(Vector2), 1f, SpriteEffects.None, 0f);
        }
    }

    private struct PlayerOffScreenCache
    {
        private Player player;

        private string nameToShow;

        private Vector2 namePlatePos;

        private Color namePlateColor;

        private Vector2 distanceDrawPosition;

        private string distanceString;

        private Vector2 measurement;

        public PlayerOffScreenCache(string name, Vector2 pos, Color color, Vector2 npDistPos, string npDist, Player thePlayer, Vector2 theMeasurement)
        {
            this.nameToShow = name;
            this.namePlatePos = pos.Floor();
            this.namePlateColor = color;
            this.distanceDrawPosition = npDistPos.Floor();
            this.distanceString = npDist;
            this.player = thePlayer;
            this.measurement = theMeasurement;
        }

        public void DrawPlayerName(SpriteBatch spriteBatch)
        {
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, this.nameToShow, this.namePlatePos + new Vector2(0f, -40f), this.namePlateColor, 0f, Vector2.Zero, Vector2.One);
        }

        public void DrawPlayerHead()
        {
            float num = 20f;
            float num2 = -27f;
            num2 -= (this.measurement.X - 85f) / 2f;
            Color playerHeadBordersColor = Main.GetPlayerHeadBordersColor(this.player);
            Vector2 vec = new Vector2(this.namePlatePos.X, this.namePlatePos.Y - num);
            vec.X -= 22f + num2;
            vec.Y += 8f;
            vec = vec.Floor();
            Main.MapPlayerRenderer.DrawPlayerHead(Main.Camera, this.player, vec, 1f, 0.8f, playerHeadBordersColor);

            if (player.ghost)
            {
                Texture2D texture = this.player.direction == -1 ? Ass.GhostLeft.Value : Ass.Ghost.Value;
                Main.spriteBatch.Draw(texture, vec, null, Color.White, 0f, texture.Size() * 0.5f, 1.4f, SpriteEffects.None, 0f);
            }
        }

        public void DrawLifeBar()
        {
            Vector2 vector = Main.screenPosition + this.distanceDrawPosition + new Vector2(26f, 20f);
            if (this.player.statLife != this.player.statLifeMax2)
            {
                Main.instance.DrawHealthBar(vector.X, vector.Y, this.player.statLife, this.player.statLifeMax2, 1f, 1.25f, noFlip: true);
            }
        }

        public void DrawPlayerDistance(SpriteBatch spriteBatch)
        {
            float scale = 0.85f;
            spriteBatch.DrawString(FontAssets.MouseText.Value, this.distanceString, new Vector2(this.distanceDrawPosition.X - 2f, this.distanceDrawPosition.Y), Color.Black, 0f, default(Vector2), scale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(FontAssets.MouseText.Value, this.distanceString, new Vector2(this.distanceDrawPosition.X + 2f, this.distanceDrawPosition.Y), Color.Black, 0f, default(Vector2), scale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(FontAssets.MouseText.Value, this.distanceString, new Vector2(this.distanceDrawPosition.X, this.distanceDrawPosition.Y - 2f), Color.Black, 0f, default(Vector2), scale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(FontAssets.MouseText.Value, this.distanceString, new Vector2(this.distanceDrawPosition.X, this.distanceDrawPosition.Y + 2f), Color.Black, 0f, default(Vector2), scale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(FontAssets.MouseText.Value, this.distanceString, this.distanceDrawPosition, this.namePlateColor, 0f, default(Vector2), scale, SpriteEffects.None, 0f);
        }
    }


    private List<PlayerOnScreenCache> _playerOnScreenCache = new List<PlayerOnScreenCache>();

    private List<PlayerOffScreenCache> _playerOffScreenCache = new List<PlayerOffScreenCache>();

    public void VanillaDraw()
    {
        int teamNamePlateDistance = Main.teamNamePlateDistance;
        if (teamNamePlateDistance <= 0)
        {
            return;
        }
        this._playerOnScreenCache.Clear();
        this._playerOffScreenCache.Clear();
        SpriteBatch spriteBatch = Main.spriteBatch;
        PlayerInput.SetZoom_World();
        int screenWidth = Main.screenWidth;
        int screenHeight = Main.screenHeight;
        Vector2 screenPosition = Main.screenPosition;
        PlayerInput.SetZoom_UI();
        int num = teamNamePlateDistance * 8;
        Player[] player = Main.player;
        int myPlayer = Main.myPlayer;
        byte mouseTextColor = Main.mouseTextColor;
        Color[] teamColor = Main.teamColor;
        _ = Main.screenPosition;
        Player player2 = player[myPlayer];
        float num2 = (float)(int)mouseTextColor / 255f;
        if (player2.team == 0)
        {
            return;
        }
        DynamicSpriteFont value = FontAssets.MouseText.Value;
        for (int i = 0; i < 255; i++)
        {
            if (i == myPlayer)
            {
                continue;
            }
            Player player3 = player[i];
            if (!ShouldDrawNamePlate(player2, player3))
                continue;
            string name = player3.name;
            NewMultiplayerClosePlayersOverlay.GetDistance(screenWidth, screenHeight, screenPosition, player2, value, player3, name, out var namePlatePos, out var namePlateDist, out var measurement);
            Color color = new Color((byte)((float)(int)teamColor[player3.team].R * num2), (byte)((float)(int)teamColor[player3.team].G * num2), (byte)((float)(int)teamColor[player3.team].B * num2), mouseTextColor);
            if (namePlateDist > 0f)
            {
                float num3 = player3.Distance(player2.Center);
                if (!(num3 > (float)num))
                {
                    float num4 = 20f;
                    float num5 = -27f;
                    num5 -= (measurement.X - 85f) / 2f;
                    string textValue = Language.GetTextValue("GameUI.PlayerDistance", (int)(num3 / 16f * 2f));
                    Vector2 npDistPos = value.MeasureString(textValue);
                    npDistPos.X = namePlatePos.X - num5;
                    npDistPos.Y = namePlatePos.Y + measurement.Y / 2f - npDistPos.Y / 2f - num4;
                    this._playerOffScreenCache.Add(new PlayerOffScreenCache(name, namePlatePos, color, npDistPos, textValue, player3, measurement));
                }
            }
            else
            {
                this._playerOnScreenCache.Add(new PlayerOnScreenCache(name, namePlatePos, color));
            }
        }
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, Main.UIScaleMatrix);
        for (int j = 0; j < this._playerOnScreenCache.Count; j++)
        {
            this._playerOnScreenCache[j].DrawPlayerName_WhenPlayerIsOnScreen(spriteBatch);
        }
        for (int k = 0; k < this._playerOffScreenCache.Count; k++)
        {
            this._playerOffScreenCache[k].DrawPlayerName(spriteBatch);
        }
        for (int l = 0; l < this._playerOffScreenCache.Count; l++)
        {
            this._playerOffScreenCache[l].DrawPlayerDistance(spriteBatch);
        }
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, Main.UIScaleMatrix);
        for (int m = 0; m < this._playerOffScreenCache.Count; m++)
        {
            this._playerOffScreenCache[m].DrawLifeBar();
        }
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Immediate, null, null, null, null, null, Main.UIScaleMatrix);
        for (int n = 0; n < this._playerOffScreenCache.Count; n++)
        {
            this._playerOffScreenCache[n].DrawPlayerHead();
        }
    }

    private static bool ShouldDrawNamePlate(Player localPlayer, Player otherPlayer)
    {
        if (otherPlayer == null || !otherPlayer.active || otherPlayer.dead || otherPlayer.whoAmI == Main.myPlayer)
            return false;

        if (otherPlayer.ghost || SpectatorModeSystem.IsInSpectateMode(otherPlayer))
            return false;

        if (SpectatorModeSystem.IsInSpectateMode(localPlayer))
        {
            return true;
        }

        return localPlayer.team != 0 && otherPlayer.team == localPlayer.team;
    }
}