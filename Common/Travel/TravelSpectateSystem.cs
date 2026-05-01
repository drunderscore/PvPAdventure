using Microsoft.Xna.Framework;
using PvPAdventure.Common.Spectator;
using PvPAdventure.Common.Spectator.Drawers.Inventory;
using PvPAdventure.Common.Spectator.SpectatorMode;
using PvPAdventure.Common.Travel.Portals;
using PvPAdventure.Common.Travel.UI;
using PvPAdventure.Content.Portals;
using Terraria;
using Terraria.ModLoader;

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
    private static int hudLockedPlayerIndex = -1;
    private static ulong lastBuffDrawUpdate = ulong.MaxValue;
    private static int lastBuffDrawPlayerIndex = -1;

    private static bool wasHovering;
    private static bool hasRestorePos;
    private static Vector2 restoreScreenPos;

    public override void Load()
    {
        On_Main.DrawInventory += DrawTravelPreviewInventory;
        On_Main.GUIHotbarDrawInner += DrawTravelPreviewHotbar;
        On_Main.DrawInterface_25_ResourceBars += DrawTravelPreviewResourceBars;
        On_Main.DrawInterface_Resources_Buffs += DrawTravelPreviewResourcesAndBuffs;
    }

    public override void Unload()
    {
        On_Main.DrawInventory -= DrawTravelPreviewInventory;
        On_Main.GUIHotbarDrawInner -= DrawTravelPreviewHotbar;
        On_Main.DrawInterface_25_ResourceBars -= DrawTravelPreviewResourceBars;
        On_Main.DrawInterface_Resources_Buffs -= DrawTravelPreviewResourcesAndBuffs;
    }

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

    public static bool IsPlayerHudLocked(int playerIndex)
    {
        return hudLockedPlayerIndex == playerIndex && IsValidHudPlayer(playerIndex);
    }

    public static void TogglePlayerHudLock(int playerIndex)
    {
        if (IsPlayerHudLocked(playerIndex))
        {
            hudLockedPlayerIndex = -1;
            return;
        }

        hudLockedPlayerIndex = IsValidHudPlayer(playerIndex) ? playerIndex : -1;
    }

    public override void ModifyScreenPosition()
    {
        Player local = Main.LocalPlayer;

        if (local?.active != true)
        {
            return;
        }

        if (SpectatorModeSystem.IsInSpectateMode(local))
        {
            hudLockedPlayerIndex = -1;
            Restore();
            return;
        }

        if (!TravelTeleportSystem.ShouldShowTravelUI(local))
        {
            hudLockedPlayerIndex = -1;
            RestoreIfPreviewing(local);
            return;
        }

        ValidateHover();

        bool lockedPlayer = IsValidHudPlayer(hudLockedPlayerIndex);
        bool transientHover = hasHover || hoveringPlayer;

        if (transientHover && !TravelUISystem.IsMouseHovering)
        {
            ClearHover();
            transientHover = false;
        }

        bool previewing = lockedPlayer || transientHover;

        HandleHoverTransitions(local, previewing);

        if (previewing)
            ApplyCamera();
    }

    private static void DrawTravelPreviewInventory(On_Main.orig_DrawInventory orig, Main self)
    {
        if (TryGetPlayerHudTarget(out Player player))
        {
            if (Main.playerInventory)
                PlayerHudOverlay.DrawInventoryHud(Main.spriteBatch, player);

            return;
        }

        PlayerHudOverlay.ClearOwnedHover();
        orig(self);
    }

    private static void DrawTravelPreviewHotbar(On_Main.orig_GUIHotbarDrawInner orig, Main self)
    {
        if (TryGetPlayerHudTarget(out Player player))
        {
            if (!Main.playerInventory)
                PlayerHudOverlay.DrawHotbarHud(Main.spriteBatch, player);

            return;
        }

        PlayerHudOverlay.ClearOwnedHover();
        orig(self);
    }

    private static void DrawTravelPreviewResourceBars(On_Main.orig_DrawInterface_25_ResourceBars orig, Main self)
    {
        if (TryGetPlayerHudTarget(out Player player))
        {
            if (!Main.playerInventory)
            {
                PlayerHudOverlay.DrawResourceBarsHud(Main.spriteBatch, player);
                DrawTravelPreviewBuffsOnce(player);
            }

            return;
        }

        PlayerHudOverlay.ClearOwnedHover();
        orig(self);
    }

    private static void DrawTravelPreviewResourcesAndBuffs(On_Main.orig_DrawInterface_Resources_Buffs orig, Main self)
    {
        if (TryGetPlayerHudTarget(out Player player))
        {
            if (!Main.playerInventory)
                DrawTravelPreviewBuffsOnce(player);

            return;
        }

        PlayerHudOverlay.ClearOwnedHover();
        orig(self);
    }

    private static void DrawTravelPreviewBuffsOnce(Player player)
    {
        if (player?.active != true)
            return;

        if (lastBuffDrawUpdate == Main.GameUpdateCount && lastBuffDrawPlayerIndex == player.whoAmI)
            return;

        PlayerHudOverlay.DrawBuffHud(Main.spriteBatch, player);
        lastBuffDrawUpdate = Main.GameUpdateCount;
        lastBuffDrawPlayerIndex = player.whoAmI;
    }

    private static bool TryGetPlayerHudTarget(out Player player)
    {
        player = null;

        Player local = Main.LocalPlayer;

        if (local?.active != true || SpectatorModeSystem.IsInSpectateMode(local) || !TravelTeleportSystem.ShouldShowTravelUI(local))
            return false;

        int playerIndex =
            IsValidHudPlayer(hudLockedPlayerIndex) ? hudLockedPlayerIndex :
            IsValidHudPlayer(hoveredPlayerIndex) ? hoveredPlayerIndex :
            -1;

        if (playerIndex == -1)
            return false;

        player = Main.player[playerIndex];
        return player?.active == true;
    }

    public static bool TryGetActivePlayerHudTarget(out Player player)
    {
        return TryGetPlayerHudTarget(out player);
    }

    private static void ValidateHover()
    {
        if (hoveringPlayer && !IsValidFriendlyPlayer(hoveredPlayerIndex))
            ClearHover();

        if (hudLockedPlayerIndex != -1 && !IsValidHudPlayer(hudLockedPlayerIndex))
            hudLockedPlayerIndex = -1;

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
        if (IsValidHudPlayer(hudLockedPlayerIndex))
        {
            SetCameraToPlayer(Main.player[hudLockedPlayerIndex]);
            return;
        }

        if (hoveringPlayer && IsValidPlayerIndex(hoveredPlayerIndex))
        {
            SetCameraToPlayer(Main.player[hoveredPlayerIndex]);
            return;
        }

        if (!hasHover)
        {
            return;
        }

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
        {
            SetCameraTo(portalPosition);
        }
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

    private static void RestoreIfPreviewing(Player player)
    {
        if (!wasHovering && !hasHover && !hoveringPlayer && !hasRestorePos && !MapRestore)
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
            SpectateCameraFade.SetScreenPosition(player.Center - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f);
        else if (hasRestorePos)
            SpectateCameraFade.SetScreenPosition(restoreScreenPos);

        hasRestorePos = false;
        ClearHover();
        wasHovering = false;
    }

    private static void SetCameraTo(Vector2 worldPosition)
    {
        SpectateCameraFade.SetScreenPosition(worldPosition - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f);
    }

    private static void SetCameraToPlayer(Player player)
    {
        SetCameraTo(player.Center);
    }

    private static bool IsValidHudPlayer(int playerIndex)
    {
        return playerIndex != Main.myPlayer && IsValidFriendlyPlayer(playerIndex);
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
