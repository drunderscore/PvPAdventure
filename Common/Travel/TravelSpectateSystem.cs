using Microsoft.Xna.Framework;
using PvPAdventure.Common.Travel.Portals;
using PvPAdventure.Common.Travel.UI;
using PvPAdventure.Content.Portals;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Travel;

/// <summary>
/// Handles camera previewing for travel destinations.
/// </summary>
[Autoload(Side = ModSide.Client)]
internal sealed class TravelSpectateSystem : ModSystem
{
    public static bool MapRestore { get; private set; }

    private static bool hasHover;
    public static bool HasTargetHover => hasHover;
    private static TravelTarget hoveredTarget;

    private static bool wasHovering;
    private static bool hasRestorePos;
    private static Vector2 restoreScreenPos;

    public static void TrySetHover(TravelTarget target)
    {
        if (!CanPreview(target))
        {
            ClearHover();
            return;
        }

        hoveredTarget = target;
        hasHover = true;
    }

    public static void ClearHover()
    {
        hasHover = false;
        hoveredTarget = default;
    }

    public static void ClearHoverIfMatch(TravelTarget target)
    {
        if (hasHover && Matches(hoveredTarget, target))
            ClearHover();
    }

    public static void ClearPreviewForTravelExecution()
    {
        MapRestore = false;
        hasHover = false;
        hoveredTarget = default;
        wasHovering = false;
        hasRestorePos = false;
        restoreScreenPos = Vector2.Zero;
    }

    public override void ModifyScreenPosition()
    {
        Player local = Main.LocalPlayer;

        if (local?.active != true)
            return;

        if (!TravelTeleportSystem.ShouldShowTravelUI(local))
        {
            RestoreIfPreviewing(local);
            return;
        }

        ValidateHover();

        if (hasHover && !TravelUISystem.IsMouseHovering)
            ClearHover();

        bool previewing = hasHover;
        HandleHoverTransitions(local, previewing);

        if (previewing)
            ApplyCamera();
    }

    public override void OnWorldUnload()
    {
        Reset();
    }

    public override void Unload()
    {
        Reset();
    }

    private static void ValidateHover()
    {
        if (!hasHover)
            return;

        if (!CanPreview(hoveredTarget))
        {
            ClearHover();
            return;
        }

        if (!TryGetPreviewPosition(hoveredTarget, out _))
            ClearHover();
    }

    private static void ApplyCamera()
    {
        if (!hasHover)
            return;

        if (TryGetPreviewPosition(hoveredTarget, out Vector2 position))
            SetCameraTo(position);
    }

    private static void HandleHoverTransitions(Player local, bool hovering)
    {
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
            SpectateCameraFade.SetScreenPosition(restoreScreenPos, allowFade: true);
            hasRestorePos = false;
        }

        ClearHover();
        wasHovering = false;
    }

    private static void RestoreIfPreviewing(Player player)
    {
        if (!wasHovering && !hasHover && !hasRestorePos && !MapRestore)
            return;

        RestoreToPlayer(player);
    }

    private static void RestoreToPlayer(Player player)
    {
        if (MapRestore)
        {
            Main.mapFullscreen = true;
            MapRestore = false;
        }

        if (player?.active == true)
            SpectateCameraFade.SetScreenPosition(player.Center - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f, allowFade: true);
        else if (hasRestorePos)
            SpectateCameraFade.SetScreenPosition(restoreScreenPos, allowFade: true);

        hasRestorePos = false;
        ClearHover();
        wasHovering = false;
    }

    private static void SetCameraTo(Vector2 worldPosition)
    {
        SpectateCameraFade.SetScreenPosition(worldPosition - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f, allowFade: true);
    }

    private static void Reset()
    {
        if (MapRestore)
            Main.mapFullscreen = true;

        MapRestore = false;
        hasHover = false;
        hoveredTarget = default;
        wasHovering = false;
        hasRestorePos = false;
        restoreScreenPos = Vector2.Zero;
    }

    private static bool CanPreview(TravelTarget target)
    {
        return TravelRules.AllowSpectating && TryGetPreviewPosition(target, out _);
    }

    private static bool TryGetPreviewPosition(TravelTarget target, out Vector2 position)
    {
        position = Vector2.Zero;

        if (target.Type == TravelType.World)
            return TryGetWorldSpawnPosition(out position);

        if (target.Type == TravelType.Bed)
            return TryGetBedPosition(target.PlayerIndex, out position);

        if (target.Type == TravelType.Portal)
            return TryGetPortalPosition(Main.LocalPlayer, target.PlayerIndex, out position);

        return false;
    }

    private static bool TryGetWorldSpawnPosition(out Vector2 position)
    {
        position = Vector2.Zero;

        if (Main.spawnTileX <= 0 || Main.spawnTileY <= 0)
            return false;

        position = new Vector2(Main.spawnTileX, Main.spawnTileY - 3).ToWorldCoordinates();
        return true;
    }

    private static bool TryGetBedPosition(int playerIndex, out Vector2 position)
    {
        position = Vector2.Zero;

        if (!IsValidFriendlyPlayer(playerIndex))
            return false;

        Player player = Main.player[playerIndex];

        if (player.SpawnX < 0 || player.SpawnY < 0 || !Player.CheckSpawn(player.SpawnX, player.SpawnY))
            return false;

        position = new Vector2(player.SpawnX, player.SpawnY - 3).ToWorldCoordinates();
        return true;
    }

    private static bool TryGetPortalPosition(Player local, int ownerIndex, out Vector2 position)
    {
        position = Vector2.Zero;

        foreach (PortalNPC portal in PortalSystem.ActivePortals())
        {
            if (portal.OwnerIndex != ownerIndex || !PortalSystem.IsFriendlyPortal(local, portal))
                continue;

            position = portal.WorldPosition;
            return true;
        }

        return false;
    }

    private static bool IsValidFriendlyPlayer(int playerIndex)
    {
        if (!IsValidPlayerIndex(playerIndex))
            return false;

        Player local = Main.LocalPlayer;
        Player player = Main.player[playerIndex];

        if (local?.active != true || player?.active != true)
            return false;

        return playerIndex == local.whoAmI || local.team > 0 && player.team == local.team;
    }

    private static bool IsValidPlayerIndex(int playerIndex)
    {
        return playerIndex >= 0 && playerIndex < Main.maxPlayers && Main.player[playerIndex]?.active == true;
    }

    private static bool Matches(TravelTarget a, TravelTarget b)
    {
        return a.Type == b.Type && a.PlayerIndex == b.PlayerIndex;
    }
}
