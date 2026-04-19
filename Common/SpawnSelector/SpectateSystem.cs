using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Config.UI;
using Terraria.UI;

namespace PvPAdventure.Common.SpawnSelector;

// Allows spectating teammates and their beds, as well as world spawn and own bed, when dead or spawn selector is open.
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
        if (type == SpawnType.MyBed)
        {
            HoveringType = SpawnType.MyBed;
            HoveredPlayerIndex = idx;
            return;
        }

        if (type == SpawnType.TeammateBed)
        {
            HoveringType = SpawnType.TeammateBed;
            HoveredPlayerIndex = idx;
            return;
        }

        if (HoveringType == SpawnType.TeammateBed)
            return;

        if (type == SpawnType.Teammate && HoveringType != SpawnType.None)
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
        Player local = Main.LocalPlayer;
        if (idx < 0 || idx >= Main.maxPlayers || local == null || !local.active)
            return false;

        return AdventurePortalSystem.IsValidTeammatePortalIndex(local, idx);
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
        if (IsAnyConfigUIOpen())
        {
            Restore();
            return;
        }

        Player local = Main.LocalPlayer;
        if (local == null || !local.active)
            return;

        if (!local.dead && !SpawnSystem.Enabled)
        {
            Restore();
            return;
        }

        ValidateHover();

        bool hovering = HoveringType != SpawnType.None;

        HandleHoverTransitions(local, hovering);

        if (!hovering)
            return;

        ApplyCamera();
    }

    private static bool IsAnyConfigUIOpen()
    {
        UIState s = Main.InGameUI?._currentState;
        return s is UIModConfig || s is UIModConfigList || Main.ingameOptionsWindow;
    }

    private static void ValidateHover()
    {
        if (HoveringType == SpawnType.Teammate &&
            !IsValidPlayer(HoveredPlayerIndex ?? -1))
            ClearHover();

        if (HoveringType == SpawnType.TeammateBed &&
            !IsValidBed(HoveredPlayerIndex ?? -1))
            ClearHover();
    }

    private static void HandleHoverTransitions(Player local, bool hovering)
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

    private static void ApplyCamera()
    {
        // Always spectate world spawn when hovering world spawn.
        if (HoveringType == SpawnType.World)
        {
            Vector2 pos = new Vector2(Main.spawnTileX, Main.spawnTileY - 3).ToWorldCoordinates();
            Main.screenPosition = pos - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
            return;
        }

        // Spectate my bed.
        if (HoveringType == SpawnType.MyBed)
        {
            Player me = Main.LocalPlayer;
            Vector2 myBedPos = new Vector2(me.SpawnX, me.SpawnY - 3).ToWorldCoordinates();
            Main.screenPosition = myBedPos - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
            return;
        }

        // Spectate teammate or teammate bed.
        if (HoveredPlayerIndex is int idx)
        {
            Player p = Main.player[idx];
            if (p == null || !p.active)
                return;

            if (HoveringType == SpawnType.Teammate)
            {
                if (AdventurePortalSystem.TryGetPortalWorldPosition(idx, out Vector2 portalPosition))
                {
                    Main.screenPosition = portalPosition - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
                    return;
                }
            }
            if (HoveringType == SpawnType.TeammateBed)
            {
                Vector2 teammateBedPos = new Vector2(p.SpawnX, p.SpawnY - 3).ToWorldCoordinates();
                Main.screenPosition = teammateBedPos - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
                return;
            }
        }
    }
}
