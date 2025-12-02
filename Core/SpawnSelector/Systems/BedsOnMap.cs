using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.SpawnSelector.Systems;

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

    private void OnSpawnMapLayer(On_SpawnMapLayer.orig_Draw orig, SpawnMapLayer self, ref MapOverlayDrawContext context, ref string text)
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
}
