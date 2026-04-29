using Microsoft.Xna.Framework;
using PvPAdventure.Common.Spectator;
using PvPAdventure.Common.Spectator.SpectatorMode;
using PvPAdventure.Common.Travel.Portals;
using PvPAdventure.Content.Portals;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Config.UI;
using Terraria.UI;

namespace PvPAdventure.Common.Travel;

/// <summary>
/// Allows players to spectate travel points (<see cref="TravelType"/> in the world (currently beds, portals and world spawn).
/// </summary>
[Autoload(Side = ModSide.Client)]
internal sealed class TravelSpectateSystem : ModSystem
{
    public static bool MapRestore { get; private set; }

    private static bool hasHover;
    public static bool HasTargetHover => hasHover;
    private static TravelTarget hoveredTarget;

    private static bool hoveringPlayer;
    private static int hoveredPlayerIndex = -1;

    private static bool wasHovering;
    private static bool hasRestorePos;
    private static Vector2 restoreScreenPos;

    public static void TrySetHover(TravelTarget target)
    {
        if (!target.Available || target.Type == TravelType.Random)
        {
            ClearHoverIfMatch(target);
            return;
        }

        hoveredTarget = target;
        hasHover = true;
        hoveringPlayer = false;
        hoveredPlayerIndex = -1;
    }

    public static void TrySetPlayerHover(int playerIndex)
    {
        if (!IsValidFriendlyPlayer(playerIndex))
        {
            ClearPlayerHoverIfMatch(playerIndex);
            return;
        }

        hoveredPlayerIndex = playerIndex;
        hoveringPlayer = true;
        hasHover = false;
        hoveredTarget = default;
    }

    public static void ClearHover()
    {
        hasHover = false;
        hoveredTarget = default;
        hoveringPlayer = false;
        hoveredPlayerIndex = -1;
    }

    public static void ClearHoverIfMatch(TravelTarget target)
    {
        if (hasHover && Matches(hoveredTarget, target))
            ClearHover();
    }

    public static void ClearPlayerHoverIfMatch(int playerIndex)
    {
        if (hoveringPlayer && hoveredPlayerIndex == playerIndex)
            ClearHover();
    }

    public override void ModifyScreenPosition()
    {
        Player local = Main.LocalPlayer;

        if (local?.active != true)
            return;

        if (SpectatorModeSystem.IsInSpectateMode(local))
        {
            Restore();
            return;
        }

        ValidateHover();

        bool hovering = hasHover || hoveringPlayer;
        HandleHoverTransitions(local, hovering);

        if (hovering)
            ApplyCamera();
    }

    private static void ValidateHover()
    {
        if (hoveringPlayer && !IsValidFriendlyPlayer(hoveredPlayerIndex))
            ClearHover();

        if (!hasHover)
            return;

        if (hoveredTarget.Type == TravelType.World)
            return;

        if (hoveredTarget.Type == TravelType.Bed && !TryGetBedPosition(hoveredTarget.PlayerIndex, out _))
            ClearHover();

        if (hoveredTarget.Type == TravelType.Portal && !TryGetPortalPosition(Main.LocalPlayer, hoveredTarget.PlayerIndex, out _))
            ClearHover();
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

    private static void ApplyCamera()
    {
        if (hoveringPlayer && IsValidPlayerIndex(hoveredPlayerIndex))
        {
            SetCameraTo(Main.player[hoveredPlayerIndex].Center);
            return;
        }

        if (!hasHover)
            return;

        if (hoveredTarget.Type == TravelType.World)
        {
            SetCameraTo(new Vector2(Main.spawnTileX, Main.spawnTileY - 3).ToWorldCoordinates());
            return;
        }

        if (hoveredTarget.Type == TravelType.Bed && TryGetBedPosition(hoveredTarget.PlayerIndex, out Vector2 bedPosition))
        {
            SetCameraTo(bedPosition);
            return;
        }

        if (hoveredTarget.Type == TravelType.Portal && TryGetPortalPosition(Main.LocalPlayer, hoveredTarget.PlayerIndex, out Vector2 portalPosition))
            SetCameraTo(portalPosition);
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
            SpectateCameraFade.SetScreenPosition(restoreScreenPos);
            hasRestorePos = false;
        }

        ClearHover();
        wasHovering = false;
    }

    private static void SetCameraTo(Vector2 worldPosition)
    {
        SpectateCameraFade.SetScreenPosition(worldPosition - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f);
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
