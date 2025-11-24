using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Features.SpawnSelector.Systems;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.Features.SpawnSelector.UI;

internal class DrawBedsOnMapHook : ModSystem
{
    public override void Load()
    {
        On_Player.ChangeSpawn += OnPlayerChangeSpawn;
        On_SpawnMapLayer.Draw += OnSpawnMapLayer;
    }

    public override void Unload()
    {
        On_Player.ChangeSpawn -= OnPlayerChangeSpawn;
        On_SpawnMapLayer.Draw -= OnSpawnMapLayer;
    }

    private void OnPlayerChangeSpawn(On_Player.orig_ChangeSpawn orig, Player self, int x, int y)
    {
        orig(self, x, y);

        // Send packet to server, notifying the server of a updated player spawn position for a player.
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            var packet = Mod.GetPacket();
            packet.Write((byte)AdventurePacketIdentifier.PlayerBed);
            packet.Write((byte)self.whoAmI);
            packet.Write(x);
            packet.Write(y);
            packet.Send();
        }
    }

    private void OnSpawnMapLayer(On_SpawnMapLayer.orig_Draw orig,
        SpawnMapLayer self,
        ref MapOverlayDrawContext context,
        ref string text)
    {
        // Let vanilla draw spawn point + local bed first
        orig(self, ref context, ref text);

        // If spawn selector state is not active, skip
        if (!SpawnSelectorSystem.GetEnabled())
            return;

        Texture2D bedTex = TextureAssets.SpawnBed.Value;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player p = Main.player[i];
            if (p == null || !p.active)
                continue;

            if (p.team != Main.LocalPlayer.team)
                continue;

            //Main.NewText(p.SpawnX);

        // No bed set for this player
        if (p.SpawnX == -1 || p.SpawnY == -1)
                continue;

            Vector2 bedTilePos = new Vector2(p.SpawnX, p.SpawnY);

            var hoverCheck = context.Draw(
                texture: bedTex,
                position: bedTilePos,
                color: Color.White,
                frame: new SpriteFrame(1, 1),
                scaleIfNotSelected: 1.0f,
                scaleIfSelected: 1.8f,
                alignment: Alignment.Bottom,
                spriteEffects: SpriteEffects.None
            );

            if (!hoverCheck.IsMouseOver)
                continue;

            // Hover text
            text = $"Teleport to {p.name}'s Bed";

            if (Main.mouseLeft && Main.mouseLeftRelease)
            {
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    // Singleplayer: teleport directly to bed spawn
                    Vector2 spawn = new Vector2(p.SpawnX * 16, p.SpawnY * 16 - 48);
                    p.Teleport(spawn);

                    // Ask server to teleport this player to their spawn position
                    NetMessage.SendData(MessageID.TeleportEntity, number: Main.myPlayer);
                }
                else
                {
                    // Singleplayer: teleport directly to bed spawn
                    Vector2 spawn = new Vector2(p.SpawnX * 16, p.SpawnY * 16 - 48);
                    p.Teleport(spawn);
                }

                Main.mapFullscreen = false;
            }
        }
    }
}
