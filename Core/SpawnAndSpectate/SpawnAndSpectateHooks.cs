using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Content.Items;
using ReLogic.Graphics;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Core.SpawnAndSpectate;

/// <summary>
/// Various player hooks related to teleporting, spawning, and spawn selector.
/// </summary>
public class SpawnAndSpectateHooks : ModSystem
{
    public override void Load()
    {
        On_Player.HasUnityPotion += ForceUnityPotionWhenSpawnSelectorIsEnabled;
        On_Player.Spawn_SetPosition += ForceWorldSpawn;
        On_SoundEngine.PlaySound_refSoundStyle_Nullable1_SoundUpdateCallback += DisableMirrorSound;
        On_Main.TriggerPing += SkipPingWhileHoveringSpawnSelector;
        On_Main.DrawInterface_35_YouDied += DrawDeathText;
    }

    public override void Unload()
    {
        On_Player.HasUnityPotion -= ForceUnityPotionWhenSpawnSelectorIsEnabled;
        On_Player.Spawn_SetPosition -= ForceWorldSpawn;
        On_SoundEngine.PlaySound_refSoundStyle_Nullable1_SoundUpdateCallback -= DisableMirrorSound;
        On_Main.TriggerPing -= SkipPingWhileHoveringSpawnSelector;
        On_Main.DrawInterface_35_YouDied -= DrawDeathText;
    }

    // Custom death text drawing to properly show respawn timer as 0 when spawn selector is open.
    // Largely copied from Terraria source code with minor changes.
    private void DrawDeathText(On_Main.orig_DrawInterface_35_YouDied orig)
    {
        if (!Main.LocalPlayer.dead)
            return;

        Player p = Main.LocalPlayer;

        float y = -60f;
        string str = Lang.inter[38].Value;

        // Draw "You were slain" text
        DynamicSpriteFontExtensionMethods.DrawString(
            Main.spriteBatch, FontAssets.DeathText.Value, str,
            new Vector2(Main.screenWidth / 2f - FontAssets.DeathText.Value.MeasureString(str).X / 2f, Main.screenHeight / 2f + y),
            p.GetDeathAlpha(Color.Transparent), 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

        // Draw dropped coins text
        if (p.lostCoins > 0)
        {
            y += 50f;
            string dropped = Language.GetTextValue("Game.DroppedCoins", p.lostCoinString);

            DynamicSpriteFontExtensionMethods.DrawString(
                Main.spriteBatch, FontAssets.MouseText.Value, dropped,
                new Vector2(Main.screenWidth / 2f - FontAssets.MouseText.Value.MeasureString(dropped).X / 2f,
                    Main.screenHeight / 2f + y),
                p.GetDeathAlpha(Color.Transparent), 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
        }

        y += (p.lostCoins > 0 ? 24f : 50f) + 20f;
        float scale = 0.7f;

        // Seconds remaining until respawn
        int seconds = (int)(1f + p.respawnTimer / 60f);

        // When spawn selector is open and can respawn, show 0 seconds remaining
        if (p.respawnTimer <= 2 && SpawnAndSpectateSystem.CanRespawn)
        {
            seconds = 0;
        }

        string respawnText = Language.GetTextValue("Game.RespawnInSuffix", seconds.ToString());

        // If we are spectating someone and seconds is at 0, skip drawing the respawn timer
        if (seconds == 0 && SpawnAndSpectateSystem.SpectatePlayerIndex.HasValue)
        {
            return;
        }

        // Draw respawn timer
        DynamicSpriteFontExtensionMethods.DrawString(
            Main.spriteBatch, FontAssets.DeathText.Value, respawnText,
            new Vector2(Main.screenWidth / 2f - FontAssets.MouseText.Value.MeasureString(respawnText).X * scale / 2f,
                Main.screenHeight / 2f + y),
            p.GetDeathAlpha(Color.Transparent), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    private void SkipPingWhileHoveringSpawnSelector(On_Main.orig_TriggerPing orig, Vector2 position)
    {
        // Skip ping execution if our panel is being hovered
        var sys = ModContent.GetInstance<SpawnAndSpectateSystem>();
        if (sys != null && sys.ui.CurrentState != null && sys.ui.CurrentState.IsMouseHovering)
        {
            return;
        }
        orig(position);
    }

    private static bool ForceUnityPotionWhenSpawnSelectorIsEnabled(On_Player.orig_HasUnityPotion orig, Player self)
    {
        var sys = ModContent.GetInstance<SpawnAndSpectateSystem>();

        if (sys.ui.CurrentState == sys.spawnSelectorState)
        {
            return true;
        }

        return orig(self);
    }
    private void ForceWorldSpawn(On_Player.orig_Spawn_SetPosition orig, Player self, int floorX, int floorY)
    {
        //orig(self, floorX, floorY);

        // Force all respawns to world spawn
        int spawnX = Main.spawnTileX;
        int spawnY = Main.spawnTileY;

        // Skip this for now.
        //bool num = self.Spawn_GetPositionAtWorldSpawn(ref floorX, ref floorY);
        //self.Spawn_SetPosition(floorX, floorY);

        // Clears the area. Also skip this for now.
        //if (num && !self.Spawn_IsAreaAValidWorldSpawn(floorX, floorY))
        //{
        //    Player.Spawn_ForceClearArea(floorX, floorY);
        //}

        // Note: This is ran on the SERVER, so we need to ensure we only execute this for the local client.
        if (self == Main.LocalPlayer)
        {
            Main.LocalPlayer.position.X = spawnX * 16 + 8 - Main.LocalPlayer.width / 2;
            Main.LocalPlayer.position.Y = spawnY * 16 - Main.LocalPlayer.height;
        }
    }
    private SlotId DisableMirrorSound(On_SoundEngine.orig_PlaySound_refSoundStyle_Nullable1_SoundUpdateCallback orig, ref SoundStyle style, Vector2? position, SoundUpdateCallback updatecallback)
    {
        if (style == SoundID.Item6)
        {
            var config = ModContent.GetInstance<AdventureClientConfig>();
            if (!config.PlaySound)
            {
                Player p = Main.LocalPlayer;
                if (p.HeldItem?.ModItem is AdventureMirror)
                {
                    return SlotId.Invalid; // suppress sound completely
                }
            }
        }

        // Otherwise normal playback
        return orig.Invoke(ref style, position, updatecallback);
    }
}
