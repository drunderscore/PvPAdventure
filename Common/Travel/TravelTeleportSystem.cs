using Microsoft.Xna.Framework;
using PvPAdventure.Common.GameTimer;
using PvPAdventure.Common.Travel.Portals;
using PvPAdventure.Content.Portals;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Travel;

[Autoload(Side = ModSide.Client)]
internal class TravelTeleportSystem : ModSystem
{
    private static TravelTarget selectedTarget;
    private static bool hasSelection;
    private static bool sentSelection;

    public static bool HasSelection => hasSelection;

    public static List<TravelTarget> GetTargets(Player player)
    {
        List<TravelTarget> targets = [];

        if (player?.active != true)
            return targets;

        targets.Add(new TravelTarget(TravelType.World, -1, GetPlayerTopLeftAtTile(player, Main.spawnTileX, Main.spawnTileY), "World Spawn", "World", true));

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player targetPlayer = Main.player[i];

            if (targetPlayer?.active != true || i != player.whoAmI && (player.team <= 0 || targetPlayer.team != player.team))
                continue;

            bool hasBed = targetPlayer.SpawnX >= 0 && targetPlayer.SpawnY >= 0 && Player.CheckSpawn(targetPlayer.SpawnX, targetPlayer.SpawnY);
            targets.Add(new TravelTarget(
                TravelType.Bed,
                i,
                hasBed ? GetPlayerTopLeftAtTile(player, targetPlayer.SpawnX, targetPlayer.SpawnY) : Vector2.Zero,
                $"{targetPlayer.name}'s Bed",
                "Bed",
                hasBed,
                i == player.whoAmI ? "You have no valid bed set" : $"{targetPlayer.name} has no valid bed set"
            ));

            bool hasPortal = TryGetPortalPosition(player, i, out Vector2 portalPos);
            targets.Add(new TravelTarget(
                TravelType.Portal,
                i,
                hasPortal ? portalPos : Vector2.Zero,
                $"{targetPlayer.name}'s Portal",
                "Portal",
                hasPortal,
                i == player.whoAmI ? "You have no portal" : $"{targetPlayer.name} has no portal"
            ));
        }

        targets.Add(new TravelTarget(TravelType.Random, -1, Vector2.Zero, "Random", "Random", true));
        return targets;
    }

    public static bool ShouldShowTravelUI(Player player)
    {
        if (player?.active != true || player.ghost)
            return false;

        if (ModContent.GetInstance<GameManager>().CurrentPhase == GameManager.Phase.Waiting)
            return false;

        return player.dead || TravelRegionSystem.CanUseTravelUI(player) || IsUsingPortalCreator(player);
    }

    public static bool ShouldUseDeathTravelSelection(Player player)
    {
        return player?.active == true &&
            player.dead &&
            !player.ghost &&
            ModContent.GetInstance<GameManager>().CurrentPhase != GameManager.Phase.Waiting;
    }

    public static bool IsSelected(TravelTarget target)
    {
        return hasSelection && selectedTarget.Type == target.Type && selectedTarget.PlayerIndex == target.PlayerIndex;
    }

    public static void ActivateTarget(TravelTarget target)
    {
        if (!target.Available)
            return;

        Player player = Main.LocalPlayer;

        if (player?.active != true)
            return;

        if (player.dead || player.ghost || IsWaitingForPortalCreator(player))
        {
            ToggleSelection(target);
            return;
        }

        TravelTeleportNetHandler.SendTeleportRequest(target);
        ClearSelection();
    }

    public static void ToggleSelection(TravelTarget target)
    {
        if (!target.Available)
            return;

        if (IsSelected(target))
        {
            ClearSelection();
            return;
        }

        selectedTarget = target;
        hasSelection = true;
    }

    public static void ClearSelection()
    {
        selectedTarget = default;
        hasSelection = false;
    }

    public override void PostUpdateEverything()
    {
        Player player = Main.LocalPlayer;

        if (Main.netMode == NetmodeID.Server || player?.active != true || !hasSelection)
            return;

        if (player.dead || player.ghost)
            return;

        if (IsUsingPortalCreator(player) && PortalCreatorFramesLeft(player) > 0)
            return;

        if (!TryGetTarget(player, selectedTarget.Type, selectedTarget.PlayerIndex, out TravelTarget current) || !current.Available)
        {
            ClearSelection();
            return;
        }

        TravelTeleportNetHandler.SendTeleportRequest(current);
        //Log.Chat($"New travel request: {current}");
        ClearSelection();
    }

    public static bool TryGetTarget(Player player, TravelType type, int playerIndex, out TravelTarget target)
    {
        foreach (TravelTarget candidate in GetTargets(player))
        {
            if (candidate.Type == type && candidate.PlayerIndex == playerIndex)
            {
                target = candidate;
                return true;
            }
        }

        target = default;
        return false;
    }

    public static bool CanTeleport(Player player, TravelTarget target, out string reason)
    {
        reason = "";

        if (player?.active != true)
        {
            reason = "Player is not active";
            return false;
        }

        if (player.dead || player.ghost)
        {
            reason = "Cannot teleport while dead";
            return false;
        }

        if (!target.Available)
        {
            reason = target.DisabledReason;
            return false;
        }

        if (target.Type is TravelType.World or TravelType.Bed or TravelType.Portal or TravelType.Random)
            return true;

        reason = "Unsupported travel target";
        return false;
    }

    public static bool TryTeleport(Player player, TravelTarget target, out string reason)
    {
        if (!CanTeleport(player, target, out reason))
            return false;

        if (target.Type == TravelType.Random)
        {
            player.velocity = Vector2.Zero;
            player.TeleportationPotion();
            player.fallStart = (int)(player.position.Y / 16f);
            //Log.Chat($"[TravelTeleport] Singleplayer random teleport pos={player.position}");
            return true;
        }

        player.velocity = Vector2.Zero;
        player.Teleport(target.WorldPosition, TeleportationStyleID.RodOfDiscord);
        player.fallStart = (int)(target.WorldPosition.Y / 16f);
        return true;
    }

    public static int PortalCreatorFramesLeft(Player player)
    {
        if (!IsUsingPortalCreator(player))
            return 0;

        return Math.Max(0, player.itemAnimation - 3);
    }

    public static int PortalCreatorSecondsLeft(Player player)
    {
        int frames = PortalCreatorFramesLeft(player);
        return frames <= 0 ? 0 : (frames + 59) / 60;
    }

    public static bool IsWaitingForPortalCreator(Player player)
    {
        return PortalCreatorFramesLeft(player) > 0;
    }

    private static bool IsUsingPortalCreator(Player player)
    {
        return player?.active == true && player.itemAnimation > 0 && player.HeldItem?.ModItem is PortalCreatorItem;
    }

    private static bool TryGetPortalPosition(Player player, int ownerIndex, out Vector2 position)
    {
        foreach (PortalNPC portal in PortalSystem.ActivePortals())
        {
            if (portal.OwnerIndex == ownerIndex && PortalSystem.IsFriendlyPortal(player, portal))
            {
                position = GetPlayerTopLeftAtWorldBottom(player, portal.WorldPosition);
                return true;
            }
        }

        position = Vector2.Zero;
        return false;
    }

    private static Vector2 GetPlayerTopLeftAtTile(Player player, int tileX, int tileY)
    {
        return new Vector2(tileX * 16f + 8f - player.width * 0.5f, tileY * 16f - player.height);
    }

    private static Vector2 GetPlayerTopLeftAtWorldBottom(Player player, Vector2 worldBottom)
    {
        return new Vector2(worldBottom.X - player.width * 0.5f, worldBottom.Y - player.height);
    }
}