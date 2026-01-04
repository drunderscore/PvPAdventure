using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using static PvPAdventure.Core.SpawnAndSpectate.SpawnSystem;

namespace PvPAdventure.Core.SpawnAndSpectate;

[Autoload(Side =ModSide.Client)]
public class SpawnHooks : ModSystem
{
    public override void Load()
    {
        On_Player.HasUnityPotion += ForceUnityPotion;
        On_Player.Spawn_SetPosition += ApplySelectedSpawn;
        On_Main.DrawInterface_35_YouDied += DrawDeathText;
        On_Main.TriggerPing += SkipPingWhileHoveringSelector;
    }

    public override void Unload()
    {
        On_Player.HasUnityPotion -= ForceUnityPotion;
        On_Player.Spawn_SetPosition -= ApplySelectedSpawn;
        On_Main.DrawInterface_35_YouDied -= DrawDeathText;
        On_Main.TriggerPing -= SkipPingWhileHoveringSelector;
    }

    private static bool ForceUnityPotion(On_Player.orig_HasUnityPotion orig, Player self)
    {
        var sys = ModContent.GetInstance<SpawnSystem>();
        if (sys?.ui?.CurrentState == sys?.spawnState && SpawnSystem.CanTeleport)
            return true;

        return orig(self);
    }

    private static void TeleportAndSync(Player p, Vector2 pos)
    {
        p.Teleport(pos, TeleportationStyleID.RecallPotion);

        if (Main.netMode == NetmodeID.Server)
        {
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
    }

    private void ApplySelectedSpawn(On_Player.orig_Spawn_SetPosition orig, Player self, int floorX, int floorY)
    {
        SpawnPlayer sp = self.GetModPlayer<SpawnPlayer>();
        SpawnType type = sp.SelectedType;

        if (type == SpawnType.None)
        {
            orig(self, floorX, floorY);
            return;
        }

        if (type == SpawnType.World)
        {
            int fx = Main.spawnTileX;
            int fy = Main.spawnTileY;

            bool ok = self.Spawn_GetPositionAtWorldSpawn(ref fx, ref fy);
            if (ok && !self.Spawn_IsAreaAValidWorldSpawn(fx, fy))
                Player.Spawn_ForceClearArea(fx, fy);

            orig(self, fx, fy);
            sp.ClearSelection();
            return;
        }

        orig(self, floorX, floorY);

        if (type == SpawnType.Random)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.RequestTeleportationByServer);
            else
                self.TeleportationPotion();

            sp.ClearSelection();
            return;
        }

        if (type == SpawnType.Player)
        {
            int idx = sp.SelectedPlayerIndex;

            if (SpawnSystem.IsValidTeammateIndex(idx))
            {
                Player t = Main.player[idx];
                if (t != null && t.active && !t.dead)
                    TeleportAndSync(self, t.position);
            }

            sp.ClearSelection();
        }
    }

    private void SkipPingWhileHoveringSelector(On_Main.orig_TriggerPing orig, Vector2 position)
    {
        var sys = ModContent.GetInstance<SpawnSystem>();
        if (sys?.ui?.CurrentState != null && sys.ui.CurrentState.IsMouseHovering)
            return;

        orig(position);
    }

    private void DrawDeathText(On_Main.orig_DrawInterface_35_YouDied orig)
    {
        if (!Main.LocalPlayer.dead)
            return;

        Player p = Main.LocalPlayer;

        float y = -60f;
        int seconds = (int)(1f + p.respawnTimer / 60f);

        if (p.respawnTimer <= 2 && SpawnSystem.CanTeleport)
            seconds = 0;

        if (SpectateSystem.HoveringType != SpawnType.None || seconds == 0)
            y += 260f;

        string title = Lang.inter[38].Value;

        DynamicSpriteFontExtensionMethods.DrawString(
            Main.spriteBatch,
            FontAssets.DeathText.Value,
            title,
            new Vector2(
                Main.screenWidth / 2f - FontAssets.DeathText.Value.MeasureString(title).X / 2f,
                Main.screenHeight / 2f + y),
            p.GetDeathAlpha(Color.Transparent),
            0f,
            Vector2.Zero,
            1f,
            SpriteEffects.None,
            0f);

        if (p.lostCoins > 0)
        {
            y += 50f;
            string dropped = Language.GetTextValue("Game.DroppedCoins", p.lostCoinString);

            DynamicSpriteFontExtensionMethods.DrawString(
                Main.spriteBatch,
                FontAssets.MouseText.Value,
                dropped,
                new Vector2(
                    Main.screenWidth / 2f - FontAssets.MouseText.Value.MeasureString(dropped).X / 2f,
                    Main.screenHeight / 2f + y),
                p.GetDeathAlpha(Color.Transparent),
                0f,
                Vector2.Zero,
                1f,
                SpriteEffects.None,
                0f);
        }

        y += (p.lostCoins > 0 ? 24f : 50f) + 20f;
        float scale = 0.7f;

        string respawnText = Language.GetTextValue("Game.RespawnInSuffix", seconds.ToString());
        Vector2 respawnTextPos = new Vector2(
                Main.screenWidth / 2f - FontAssets.MouseText.Value.MeasureString(respawnText).X * scale / 2f,
                Main.screenHeight / 2f + y);

        DynamicSpriteFontExtensionMethods.DrawString(
            Main.spriteBatch,
            FontAssets.DeathText.Value,
            respawnText,
            respawnTextPos,
            p.GetDeathAlpha(Color.Transparent),
            0f,
            Vector2.Zero,
            scale,
            SpriteEffects.None,
            0f);
    }
}
