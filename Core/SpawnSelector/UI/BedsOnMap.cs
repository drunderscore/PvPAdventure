using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.SpawnSelector.Systems;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.SpawnSelector.UI;

internal class BedsOnMap : ModSystem
{
    private Asset<Texture2D> spawnBedTexture;

    public override void Load()
    {
        On_SpawnMapLayer.Draw += OnSpawnMapLayer;
    }

    public override void Unload()
    {
        On_SpawnMapLayer.Draw -= OnSpawnMapLayer;
    }

    public override void PostSetupContent()
    {
        if (!Main.dedServ)
        {
            spawnBedTexture = TextureAssets.SpawnBed;
        }
    }

    private void OnSpawnMapLayer(On_SpawnMapLayer.orig_Draw orig,SpawnMapLayer self,ref MapOverlayDrawContext context, ref string text)
    {
        // Let vanilla draw spawn point + local bed first
        orig(self, ref context, ref text);

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];
            if (player == null || !player.active || player.team != Main.LocalPlayer.team)
                continue;


            // No bed set for this player -> don't draw anything
            if (player.SpawnX == -1 || player.SpawnY == -1)
                continue;

            Vector2 bedTilePos = new(player.SpawnX, player.SpawnY);

#if DEBUG
            DrawBedSpawnDebugRect(player, bedTilePos);
#endif

            // Safety check and load texture if needed
            spawnBedTexture ??= TextureAssets.SpawnBed;

            bool selectorEnabled = SpawnSelectorSystem.GetEnabled();

            var hoverCheck = context.Draw(
                texture: spawnBedTexture.Value,
                position: bedTilePos,
                color: Color.White,
                frame: new SpriteFrame(1, 1),
                scaleIfNotSelected: 1.0f,
                scaleIfSelected: selectorEnabled ? 1.8f : 1.0f, // scale up if selector is enabled
                alignment: Alignment.Bottom,
                spriteEffects: SpriteEffects.None
            );

            if (hoverCheck.IsMouseOver)
            {
                text = $"{player.name}'s Bed";

                // Handle hover + click
                if (selectorEnabled)
                {
                    // Hover text
                    text = $"Teleport to {player.name}'s Bed";

                    // Handle click
                    if (Main.mouseLeft && Main.mouseLeftRelease)
                    {
                        if (Main.netMode == NetmodeID.SinglePlayer)
                        {
                            // teleport LOCAL player to the clicked bed
                            Vector2 spawn = new(player.SpawnX * 16, player.SpawnY * 16 - 48);
                            Main.LocalPlayer.Teleport(spawn);
                            Main.mapFullscreen = false;
                            return;
                        }

                        if (Main.netMode == NetmodeID.MultiplayerClient)
                        {
                            ModPacket p = Mod.GetPacket();
                            p.Write((byte)AdventurePacketIdentifier.BedTeleport);
                            p.Write((byte)Main.myPlayer); // send the player to teleport (the player who clicked)
                            p.Write((short)player.SpawnX); // to the target bed position X
                            p.Write((short)player.SpawnY); // to the target bed position Y

                            p.Send();

                            // Close the map afterwards
                            Main.mapFullscreen = false;

#if DEBUG
                            Main.NewText($"[DEBUG/SYSTEM] Sent packet to send player {player.name} to pos: ({player.SpawnX}, {player.SpawnY})");
#endif
                        }
                    }
            
                }
            }

        }
    }

#if DEBUG
    private static void DrawBedSpawnDebugRect(Player player, Vector2 bedTilePos)
    {
        if (!Main.mapFullscreen)
        {
            return;
        }

        // World-space center of the bed (pixels)
        Vector2 bedWorld = bedTilePos * 16f;

        // Convert world -> fullscreen map screen coordinates
        Vector2 screenCenter = new(Main.screenWidth / 2f, Main.screenHeight / 2f);
        Vector2 bedOnMap = (bedWorld - Main.mapFullscreenPos) * Main.mapFullscreenScale + screenCenter;

        float radiusWorld = 25 * 16f; // 25 tiles in world space
        float radiusOnMap = radiusWorld * Main.mapFullscreenScale;

        Texture2D pixel = TextureAssets.MagicPixel.Value;
        float thickness = 2f;

        Rectangle rect = new(
            (int)(bedOnMap.X - radiusOnMap),
            (int)(bedOnMap.Y - radiusOnMap),
            (int)(radiusOnMap * 2f),
            (int)(radiusOnMap * 2f)
        );

        Color color = Main.teamColor[player.team] * 0.75f;

        // Top
        Main.spriteBatch.Draw(pixel, new Rectangle(rect.Left, rect.Top, rect.Width, (int)thickness), color);
        // Bottom
        Main.spriteBatch.Draw(pixel, new Rectangle(rect.Left, rect.Bottom - (int)thickness, rect.Width, (int)thickness), color);
        // Left
        Main.spriteBatch.Draw(pixel, new Rectangle(rect.Left, rect.Top, (int)thickness, rect.Height), color);
        // Right
        Main.spriteBatch.Draw(pixel, new Rectangle(rect.Right - (int)thickness, rect.Top, (int)thickness, rect.Height), color);
    }
#endif
}
