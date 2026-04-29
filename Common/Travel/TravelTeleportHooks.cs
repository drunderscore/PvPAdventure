using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Spectator.SpectatorMode;
using PvPAdventure.Common.Travel.UI;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Travel;

/// <summary>
/// Various hooks for player spawn behaviour, ping and death text.
/// </summary>
[Autoload(Side = ModSide.Client)]
public class TravelTeleportHooks : ModSystem
{
    public override void Load()
    {
        On_Player.HasUnityPotion += ForceUnityPotion;
        //On_Player.Spawn_SetPosition += ApplySelectedSpawn;
        On_Player.Teleport += TPOverride;
        On_Main.DrawInterface_35_YouDied += DrawDeathText;
        On_Main.TriggerPing += SkipPingWhileHoveringSelector;
    }

    public override void Unload()
    {
        On_Player.HasUnityPotion -= ForceUnityPotion;
        On_Player.Teleport -= TPOverride;
        //On_Player.Spawn_SetPosition -= ApplySelectedSpawn;
        On_Main.DrawInterface_35_YouDied -= DrawDeathText;
        On_Main.TriggerPing -= SkipPingWhileHoveringSelector;
    }

    private static bool ForceUnityPotion(On_Player.orig_HasUnityPotion orig, Player self)
    {
        // Spectators/ghosts can always teleport
        if (self.whoAmI == Main.myPlayer && SpectatorModeSystem.IsInSpectateMode(self))
            return true;

        // Spawn selector UI is open and player can teleport, allow teleportation
        //if (SpawnSystem.IsUiOpen && SpawnSystem.CanTeleport)
        //    return true;

        //return false;
        return orig(self);
    }

    private void TPOverride(On_Player.orig_Teleport orig, Player self, Vector2 newPos, int Style = 0, int extraInfo = 0)
    {
        orig(self, newPos, Style, extraInfo);
        // TODO: Remove ghost teleport text if teleporting via spawn selector
    }

    private static void TeleportAndSync(Player p, Vector2 pos)
    {
        p.Teleport(pos, TeleportationStyleID.RecallPotion);

        if (Main.netMode != NetmodeID.Server)
            return;

        NetMessage.SendData(
            MessageID.TeleportEntity,
            -1, -1, null,
            number: 0,
            number2: p.whoAmI,
            number3: pos.X,
            number4: pos.Y,
            number5: TeleportationStyleID.RecallPotion
        );
    }

    private static Vector2 PortalTeleportPos(Player player, Vector2 portalWorldPos)
    {
        return portalWorldPos - new Vector2(player.width * 0.5f, player.height);
    }

    //private void ApplySelectedSpawn(On_Player.orig_Spawn_SetPosition orig, Player self, int floorX, int floorY)
    //{
    //    SpawnPlayer sp = self.GetModPlayer<SpawnPlayer>();
    //    SpawnType type = sp.SelectedType;

    //    if (type == SpawnType.None)
    //    {
    //        orig(self, floorX, floorY);
    //        return;
    //    }

    //    if (type == SpawnType.World)
    //    {
    //        int fx = Main.spawnTileX;
    //        int fy = Main.spawnTileY;

    //        bool ok = self.Spawn_GetPositionAtWorldSpawn(ref fx, ref fy);
    //        if (ok && !self.Spawn_IsAreaAValidWorldSpawn(fx, fy))
    //            Player.Spawn_ForceClearArea(fx, fy);

    //        orig(self, fx, fy);
    //        TeleportChat.Announce(self, type);
    //        sp.ClearSelection();
    //        return;
    //    }

    //    orig(self, floorX, floorY);

    //    if (type == SpawnType.Random)
    //    {
    //        if (Main.netMode == NetmodeID.MultiplayerClient)
    //            NetMessage.SendData(MessageID.RequestTeleportationByServer);
    //        else
    //        {
    //            self.TeleportationPotion();
    //            TeleportChat.Announce(self, type);
    //        }

    //        sp.ClearSelection();
    //        return;
    //    }

    //    if (type == SpawnType.MyPortal)
    //    {
    //        if (PortalSystem.TryGetPortalWorldPos(self, out Vector2 portalWorldPos))
    //        {
    //            TeleportAndSync(self, PortalTeleportPos(self, portalWorldPos));
    //            TeleportChat.Announce(self, type);
    //        }

    //        sp.ClearSelection();
    //        return;
    //    }

    //    if (type == SpawnType.TeammatePortal)
    //    {
    //        int idx = sp.SelectedPlayerIndex;
    //        if (SpawnPlayer.IsValidTeammatePortalIndex(self, idx))
    //        {
    //            Player portalOwner = Main.player[idx];
    //            if (PortalSystem.TryGetPortalWorldPos(portalOwner, out Vector2 portalWorldPos))
    //            {
    //                TeleportAndSync(self, PortalTeleportPos(self, portalWorldPos));
    //                TeleportChat.Announce(self, type, idx);
    //            }
    //        }

    //        sp.ClearSelection();
    //    }
    //}

    private void SkipPingWhileHoveringSelector(On_Main.orig_TriggerPing orig, Vector2 position)
    {
        if (TravelUISystem.IsMouseHovering)
            return;

        orig(position);
    }

    private void DrawDeathText(On_Main.orig_DrawInterface_35_YouDied orig)
    {
        if (!Main.LocalPlayer.dead)
        {
            orig();
            return;
        }

        Player p = Main.LocalPlayer;

        int seconds = (int)(1f + p.respawnTimer / 60f);
        if (p.respawnTimer <= 2 
            //&& TravelRegionSystem.CanUseTravelUI(p)
            )
            seconds = 0;

        float y = -60f;

        DrawCentered(FontAssets.DeathText.Value, Lang.inter[38].Value, y, 1f, p);
        if (p.lostCoins > 0)
        {
            y += 50f;
            DrawCentered(FontAssets.MouseText.Value, Language.GetTextValue("Game.DroppedCoins", p.lostCoinString), y, 1f, p);
            y += 24f;
        }
        else
        {
            y += 50f;
        }

        y += 20f;

        float scale = 0.7f;
        string respawnText = Language.GetTextValue("Game.RespawnInSuffix", seconds.ToString());
        DrawCentered(FontAssets.DeathText.Value, respawnText, y, scale, p);
    }

    private static void DrawCentered(DynamicSpriteFont font, string text, float yOffset, float scale, Player p)
    {
        Vector2 size = font.MeasureString(text) * scale;
        Vector2 pos = new(
            Main.screenWidth * 0.5f - size.X * 0.5f,
            Main.screenHeight * 0.5f + yOffset
        );

        DynamicSpriteFontExtensionMethods.DrawString(
            Main.spriteBatch,
            font,
            text,
            pos,
            p.GetDeathAlpha(Color.Transparent),
            0f,
            Vector2.Zero,
            scale,
            SpriteEffects.None,
            0f
        );
    }
}