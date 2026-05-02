using Microsoft.Xna.Framework;
using PvPAdventure.Common.Spectator.SpectatorMode;
using PvPAdventure.Common.TeammateSpectator.TeammateOverlay;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.TeammateSpectator;

/// <summary>
/// Handles teammate player previewing and HUD overlay drawing.
/// </summary>
[Autoload(Side = ModSide.Client)]
internal sealed class TeammateSpectateSystem : ModSystem
{
    public static bool MapRestore { get; private set; }

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
        On_Main.DrawInventory += DrawTeammatePreviewInventory;
        On_Main.GUIHotbarDrawInner += DrawTeammatePreviewHotbar;
        On_Main.DrawInterface_25_ResourceBars += DrawTeammatePreviewResourceBars;
        On_Main.DrawInterface_Resources_Buffs += DrawTeammatePreviewResourcesAndBuffs;
    }

    public override void Unload()
    {
        On_Main.DrawInventory -= DrawTeammatePreviewInventory;
        On_Main.GUIHotbarDrawInner -= DrawTeammatePreviewHotbar;
        On_Main.DrawInterface_25_ResourceBars -= DrawTeammatePreviewResourceBars;
        On_Main.DrawInterface_Resources_Buffs -= DrawTeammatePreviewResourcesAndBuffs;
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
    }

    public static void ClearPlayerHover()
    {
        hoveringPlayer = false;
        hoveredPlayerIndex = -1;
    }

    public static void ClearPlayerHoverIfMatch(int playerIndex)
    {
        if (hoveringPlayer && hoveredPlayerIndex == playerIndex)
            ClearPlayerHover();
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

    public static void ClearPlayerHudLockIfMatch(int playerIndex)
    {
        if (hudLockedPlayerIndex == playerIndex)
            hudLockedPlayerIndex = -1;
    }

    public static bool TryGetActivePlayerHudTarget(out Player player)
    {
        return TryGetPlayerHudTarget(out player);
    }

    public override void ModifyScreenPosition()
    {
        Player local = Main.LocalPlayer;

        if (local?.active != true)
            return;

        if (SpectatorModeSystem.IsInSpectateMode(local))
        {
            hudLockedPlayerIndex = -1;
            Restore();
            return;
        }

        if (!TeammateSpectatorUISystem.IsHudPreviewAvailable)
        {
            hudLockedPlayerIndex = -1;
            ClearPlayerHover();
            RestoreIfPreviewing(local);
            return;
        }

        ValidatePlayerTarget();

        bool previewing = IsValidHudPlayer(hudLockedPlayerIndex) || IsValidHudPlayer(hoveredPlayerIndex);
        HandleHoverTransitions(local, previewing);

        if (previewing)
            ApplyCamera();
    }

    private static void DrawTeammatePreviewInventory(On_Main.orig_DrawInventory orig, Main self)
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

    private static void DrawTeammatePreviewHotbar(On_Main.orig_GUIHotbarDrawInner orig, Main self)
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

    private static void DrawTeammatePreviewResourceBars(On_Main.orig_DrawInterface_25_ResourceBars orig, Main self)
    {
        if (TryGetPlayerHudTarget(out Player player))
        {
            if (!Main.playerInventory)
            {
                PlayerHudOverlay.DrawResourceBarsHud(Main.spriteBatch, player);
                DrawTeammatePreviewBuffsOnce(player);
            }

            return;
        }

        PlayerHudOverlay.ClearOwnedHover();
        orig(self);
    }

    private static void DrawTeammatePreviewResourcesAndBuffs(On_Main.orig_DrawInterface_Resources_Buffs orig, Main self)
    {
        if (TryGetPlayerHudTarget(out Player player))
        {
            if (!Main.playerInventory)
                DrawTeammatePreviewBuffsOnce(player);

            return;
        }

        PlayerHudOverlay.ClearOwnedHover();
        orig(self);
    }

    private static void DrawTeammatePreviewBuffsOnce(Player player)
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

        if (local?.active != true || SpectatorModeSystem.IsInSpectateMode(local) || !TeammateSpectatorUISystem.IsHudPreviewAvailable)
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

    private static void ValidatePlayerTarget()
    {
        if (hoveringPlayer && !IsValidFriendlyPlayer(hoveredPlayerIndex))
            ClearPlayerHover();

        if (hudLockedPlayerIndex != -1 && !IsValidHudPlayer(hudLockedPlayerIndex))
            hudLockedPlayerIndex = -1;
    }

    private static void ApplyCamera()
    {
        if (IsValidHudPlayer(hudLockedPlayerIndex))
        {
            SetCameraToPlayer(Main.player[hudLockedPlayerIndex]);
            return;
        }

        if (hoveringPlayer && IsValidPlayerIndex(hoveredPlayerIndex))
            SetCameraToPlayer(Main.player[hoveredPlayerIndex]);
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

        ClearPlayerHover();
        wasHovering = false;
    }

    private static void RestoreIfPreviewing(Player player)
    {
        if (!wasHovering && !hoveringPlayer && !hasRestorePos && !MapRestore)
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
        ClearPlayerHover();
        wasHovering = false;
    }

    private static void SetCameraTo(Vector2 worldPosition)
    {
        SpectateCameraFade.SetScreenPosition(worldPosition - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f, allowFade: true);
    }

    private static void SetCameraToPlayer(Player player)
    {
        SetCameraTo(player.Center);
    }

    private static bool IsValidHudPlayer(int playerIndex)
    {
        return playerIndex != Main.myPlayer && IsValidFriendlyPlayer(playerIndex);
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

}
