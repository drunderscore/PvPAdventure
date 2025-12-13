using Microsoft.Xna.Framework;
using PvPAdventure.Content.Items;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Core.SpawnSelector.Systems;

/// <summary>
/// Various player hooks related to teleporting, spawning, and spawn selector.
/// </summary>
public class PlayerHooks : ModSystem
{
    public override void Load()
    {
        On_Player.HasUnityPotion += OnHasUnityPotion;
        On_Player.Spawn_SetPosition += ForceWorldSpawn;
        On_SoundEngine.PlaySound_refSoundStyle_Nullable1_SoundUpdateCallback += DisableMirrorSound;
    }

    public override void Unload()
    {
        On_Player.HasUnityPotion -= OnHasUnityPotion;
        On_Player.Spawn_SetPosition -= ForceWorldSpawn;
        On_SoundEngine.PlaySound_refSoundStyle_Nullable1_SoundUpdateCallback -= DisableMirrorSound;
    }

    private static bool OnHasUnityPotion(On_Player.orig_HasUnityPotion orig, Player self)
    {
        if (SpawnSelectorSystem.GetEnabled())
            return true;

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

        // Clears the area. Skip this for now.
        //if (num && !self.Spawn_IsAreaAValidWorldSpawn(floorX, floorY))
        //{
        //    Player.Spawn_ForceClearArea(floorX, floorY);
        //}

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
