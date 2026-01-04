using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using static PvPAdventure.Core.SpawnAndSpectate.SpawnSystem_v2;

namespace PvPAdventure.Core.SpawnAndSpectate;

[Autoload(Side = ModSide.Client)]
public class SpectateSystem : ModSystem
{
    public static bool MapRestore;
    public static int? HoveredPlayerIndex;
    public static SpawnType HoveringType = SpawnType.None;

    private static bool wasHovering;
    private static bool hasRestorePos;
    private static Vector2 restoreScreenPos;

    public static void ClearHover()
    {
        HoveredPlayerIndex = null;
        HoveringType = SpawnType.None;
    }

    private static void RestoreIfNeeded(Player local)
    {
        if (MapRestore)
        {
            if (!local.GetModPlayer<SpawnPlayer>().HasSelection && !Main.mapFullscreen)
                Main.mapFullscreen = true;

            MapRestore = false;
        }

        if (hasRestorePos)
        {
            Main.screenPosition = restoreScreenPos;
            hasRestorePos = false;
        }
    }
    public override void ModifyScreenPosition()
    {
        Player local = Main.LocalPlayer;
        if (local == null || !local.active)
            return;

        bool canUse = local.dead || SpawnSystem_v2.Enabled;

        // If we cannot use spectate, restore + clear and exit.
        if (!canUse)
        {
            if (MapRestore)
            {
                if (!local.GetModPlayer<SpawnPlayer>().HasSelection && !Main.mapFullscreen)
                    Main.mapFullscreen = true;

                MapRestore = false;
            }

            if (hasRestorePos)
            {
                Main.screenPosition = restoreScreenPos;
                hasRestorePos = false;
            }

            ClearHover();
            wasHovering = false;
            return;
        }

        bool inSpawnRegion = !local.dead && local.GetModPlayer<SpawnPlayer>().IsPlayerInSpawnRegion();

        if (HoveringType == SpawnType.Player)
        {
            if (HoveredPlayerIndex is not int idx || idx < 0 || idx >= Main.maxPlayers)
            {
                ClearHover();
            }
            else
            {
                Player t = Main.player[idx];
                if (t == null || !t.active || t.dead)
                    ClearHover();
            }
        }

        bool hovering = HoveringType != SpawnType.None &&
                        !(inSpawnRegion && HoveringType == SpawnType.World);

        // Hover START
        if (hovering && !wasHovering)
        {
            restoreScreenPos = local.Center - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
            hasRestorePos = true;

            if (Main.mapFullscreen)
            {
                Main.mapFullscreen = false;
                MapRestore = true;
            }
        }

        // Hover END
        if (!hovering && wasHovering)
        {
            if (MapRestore)
            {
                if (!local.GetModPlayer<SpawnPlayer>().HasSelection && !Main.mapFullscreen)
                    Main.mapFullscreen = true;

                MapRestore = false;
            }

            if (hasRestorePos)
            {
                Main.screenPosition = restoreScreenPos;
                hasRestorePos = false;
            }
        }

        wasHovering = hovering;

        if (!hovering)
            return;

        if (HoveringType == SpawnType.World)
        {
            if (inSpawnRegion)
                return;

            Vector2 spawnWorld = new Vector2(Main.spawnTileX, Main.spawnTileY - 3).ToWorldCoordinates();
            Main.screenPosition = spawnWorld - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
            return;
        }

        if (HoveringType == SpawnType.Player && HoveredPlayerIndex is int pidx && pidx >= 0 && pidx < Main.maxPlayers)
        {
            Player target = Main.player[pidx];
            if (target != null && target.active && !target.dead)
                Main.screenPosition = target.position - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
        }
    }

}
