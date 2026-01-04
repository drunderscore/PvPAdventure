using Terraria;
using Terraria.ModLoader;
using static PvPAdventure.Core.SpawnAndSpectate.SpawnSystem;

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
        HoveringType = SpawnType.None;
        HoveredPlayerIndex = null;
    }

    public static void TrySetHover(SpawnType type, int idx)
    {
        if (type == SpawnType.Bed)
        {
            HoveringType = SpawnType.Bed;
            HoveredPlayerIndex = idx;
            return;
        }

        if (HoveringType == SpawnType.Bed)
            return;

        if (type == SpawnType.Player && HoveringType != SpawnType.None)
            return;

        HoveringType = type;
        HoveredPlayerIndex = idx;
    }

    public static void ClearHoverIfMatch(SpawnType type, int idx)
    {
        if (HoveringType == type && HoveredPlayerIndex == idx)
            ClearHover();
    }

    private static bool IsValidPlayer(int idx)
    {
        if (idx < 0 || idx >= Main.maxPlayers)
            return false;

        Player p = Main.player[idx];
        return p != null && p.active && !p.dead;
    }

    private static bool IsValidBed(int idx)
    {
        if (idx < 0 || idx >= Main.maxPlayers)
            return false;

        Player p = Main.player[idx];
        if (p == null || !p.active)
            return false;

        return p.SpawnX >= 0 &&
               p.SpawnY >= 0 &&
               Player.CheckSpawn(p.SpawnX, p.SpawnY);
    }

    public override void ModifyScreenPosition()
    {
        Player local = Main.LocalPlayer;
        if (local == null || !local.active)
            return;

        if (!local.dead && !SpawnSystem.Enabled)
        {
            Restore();
            return;
        }

        ValidateHover();

        bool inSpawnRegion =
            !local.dead &&
            local.GetModPlayer<SpawnPlayer>().IsPlayerInSpawnRegion();

        bool hovering =
            HoveringType != SpawnType.None &&
            !(inSpawnRegion && HoveringType == SpawnType.World);

        HandleHoverTransitions(local, hovering);

        if (!hovering)
            return;

        ApplyCamera(inSpawnRegion);
    }

    private static void ValidateHover()
    {
        if (HoveringType == SpawnType.Player &&
            !IsValidPlayer(HoveredPlayerIndex ?? -1))
            ClearHover();

        if (HoveringType == SpawnType.Bed &&
            !IsValidBed(HoveredPlayerIndex ?? -1))
            ClearHover();
    }

    private void HandleHoverTransitions(Player local, bool hovering)
    {
        if (hovering && !wasHovering)
        {
            restoreScreenPos =
                local.Center -
                new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;

            hasRestorePos = true;

            if (Main.mapFullscreen)
            {
                Main.mapFullscreen = false;
                MapRestore = true;
            }
        }

        if (!hovering && wasHovering)
            Restore();

        wasHovering = hovering;
    }

    private static void Restore()
    {
        if (MapRestore)
        {
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
    }

    private static void ApplyCamera(bool inSpawnRegion)
    {
        if (HoveringType == SpawnType.World)
        {
            // Only world spawn spectate is blocked in spawn region (handled by hovering calc).
            Vector2 pos = new Vector2(Main.spawnTileX, Main.spawnTileY - 3).ToWorldCoordinates();
            Main.screenPosition = pos - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
            return;
        }

        if (HoveredPlayerIndex is not int idx)
            return;

        Player p = Main.player[idx];
        if (p == null || !p.active)
            return;

        Vector2 target =
            HoveringType == SpawnType.Bed
                ? new Vector2(p.SpawnX, p.SpawnY - 3).ToWorldCoordinates()
                : p.position;

        Main.screenPosition =
            target - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
    }
}
