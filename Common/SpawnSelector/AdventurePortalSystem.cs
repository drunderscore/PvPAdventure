using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.SpawnSelector.Net;
using PvPAdventure.Core.Debug;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.SpawnSelector;

internal sealed class AdventurePortalSystem : ModSystem
{
    private static readonly Dictionary<int, Vector2> Portals = [];
    private static readonly HashSet<int> SyncedClients = [];
    private static readonly Vector2 PortalDrawOffset = new(10f, 28f);

    public override void OnWorldLoad()
    {
        Portals.Clear();
        SyncedClients.Clear();
    }

    public override void OnWorldUnload()
    {
        Portals.Clear();
        SyncedClients.Clear();
    }

    public override void PreUpdatePlayers()
    {
        for (int i = Main.maxPlayers - 1; i >= 0; i--)
        {
            Player player = Main.player[i];
            if (player?.active == true)
                continue;

            SyncedClients.Remove(i);

            if (!Portals.Remove(i))
                continue;

            Log.Chat($"Adventure portal cleared for player: {GetPlayerNameSafe(i)}, moreinfo: inactive player slot={i}");

            if (Main.netMode == NetmodeID.Server)
                AdventurePortalNetHandler.SendClear(i);
        }

        if (Main.netMode != NetmodeID.Server)
            return;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];
            if (player?.active != true || SyncedClients.Contains(i))
                continue;

            AdventurePortalNetHandler.SendFullSync(i);
            SyncedClients.Add(i);
        }
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if (Main.dedServ)
            return;

        int index = layers.FindIndex(layer => layer.Name == "Vanilla: Interface Logic 1");
        if (index < 0)
            return;

        layers.Insert(index + 1, new LegacyGameInterfaceLayer(
            "PvPAdventure: Adventure Portals",
            DrawAdventurePortals,
            InterfaceScaleType.Game));
    }

    public static bool HasActivePortal(int playerIndex)
    {
        return playerIndex >= 0 && playerIndex < Main.maxPlayers && Portals.ContainsKey(playerIndex);
    }

    public static bool TryGetPortalWorldPosition(int playerIndex, out Vector2 worldPosition)
    {
        return Portals.TryGetValue(playerIndex, out worldPosition);
    }

    public static bool TryGetPortalTilePosition(int playerIndex, out Vector2 tilePosition)
    {
        tilePosition = default;

        if (!Portals.TryGetValue(playerIndex, out Vector2 worldPosition))
            return false;

        tilePosition = (worldPosition + PortalDrawOffset).ToTileCoordinates().ToVector2();
        return true;
    }

    public static bool IsValidPortalOwner(Player requester, int playerIndex)
    {
        if (requester == null || !requester.active)
            return false;

        if (playerIndex < 0 || playerIndex >= Main.maxPlayers)
            return false;

        Player owner = Main.player[playerIndex];
        if (owner == null || !owner.active)
            return false;

        if (playerIndex == requester.whoAmI)
            return true;

        return requester.team != 0 && owner.team == requester.team;
    }

    public static bool IsValidTeammatePortalIndex(Player requester, int playerIndex)
    {
        return playerIndex != requester?.whoAmI &&
               IsValidPortalOwner(requester, playerIndex) &&
               HasActivePortal(playerIndex);
    }

    public static bool TryGetTeleportPosition(Player requester, int playerIndex, out Vector2 teleportPosition)
    {
        teleportPosition = default;

        if (!IsValidTeammatePortalIndex(requester, playerIndex))
            return false;

        return Portals.TryGetValue(playerIndex, out teleportPosition);
    }

    public static void SetPortal(Player owner, Vector2 worldPosition)
    {
        if (owner == null || owner.whoAmI < 0 || owner.whoAmI >= Main.maxPlayers)
            return;

        bool existed = Portals.ContainsKey(owner.whoAmI);
        Portals[owner.whoAmI] = worldPosition;

        Point tilePos = worldPosition.ToTileCoordinates();
        Log.Chat($"Adventure portal {(existed ? "updated" : "created")} for player: {owner.name}, moreinfo: world=({worldPosition.X:0},{worldPosition.Y:0}) tile=({tilePos.X},{tilePos.Y}) netmode={Main.netMode}");

        if (Main.netMode == NetmodeID.Server)
            AdventurePortalNetHandler.SendUpdate(owner.whoAmI, worldPosition);
    }

    public static void ClearPortal(int playerIndex)
    {
        if (!Portals.Remove(playerIndex))
            return;

        Log.Chat($"Adventure portal cleared for player: {GetPlayerNameSafe(playerIndex)}, moreinfo: explicit clear slot={playerIndex} netmode={Main.netMode}");

        if (Main.netMode == NetmodeID.Server)
            AdventurePortalNetHandler.SendClear(playerIndex);
    }

    internal static List<KeyValuePair<int, Vector2>> GetPortalSnapshot()
    {
        List<KeyValuePair<int, Vector2>> snapshot = [];

        foreach ((int playerIndex, Vector2 worldPosition) in Portals)
            snapshot.Add(new KeyValuePair<int, Vector2>(playerIndex, worldPosition));

        return snapshot;
    }

    internal static void ApplyFullSync(List<KeyValuePair<int, Vector2>> snapshot)
    {
        Portals.Clear();

        for (int i = 0; i < snapshot.Count; i++)
            Portals[snapshot[i].Key] = snapshot[i].Value;
    }

    internal static void ApplyUpdate(int playerIndex, Vector2 worldPosition)
    {
        Portals[playerIndex] = worldPosition;
    }

    internal static void ApplyClear(int playerIndex)
    {
        Portals.Remove(playerIndex);
    }

    private static bool DrawAdventurePortals()
    {
        if (Main.gameMenu || Main.mapFullscreen || Portals.Count == 0)
            return true;

        SpriteBatch sb = Main.spriteBatch;
        Texture2D portalTexture = TextureAssets.Item[ItemID.PotionOfReturn].Value;
        Texture2D pixel = TextureAssets.MagicPixel.Value;
        Vector2 origin = new(portalTexture.Width * 0.5f, portalTexture.Height * 0.5f);

        Rectangle worldView = new(
            (int)Main.screenPosition.X - 80,
            (int)Main.screenPosition.Y - 80,
            Main.screenWidth + 160,
            Main.screenHeight + 160);

        foreach ((int playerIndex, Vector2 worldPosition) in Portals)
        {
            Player owner = Main.player[playerIndex];
            if (owner == null || !owner.active)
                continue;

            Vector2 worldAnchor = worldPosition + PortalDrawOffset;
            if (!worldView.Contains(worldAnchor.ToPoint()))
                continue;

            Vector2 screenPos = worldAnchor - Main.screenPosition;
            float pulse = 1f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f + playerIndex) * 0.08f;
            float scale = 1.05f * pulse;
            Color teamColor = owner.team > 0 ? Main.teamColor[owner.team] : new Color(160, 220, 255);

            Rectangle glowRect = new(
                (int)(screenPos.X - 14f * scale),
                (int)(screenPos.Y - 14f * scale),
                (int)(28f * scale),
                (int)(28f * scale));

            sb.Draw(pixel, glowRect, teamColor * 0.18f);
            sb.Draw(portalTexture, screenPos, null, teamColor * 0.55f, 0f, origin, scale * 1.2f, SpriteEffects.None, 0f);
            sb.Draw(portalTexture, screenPos, null, Color.White, 0f, origin, scale, SpriteEffects.None, 0f);

            if (Vector2.Distance(Main.MouseScreen, screenPos) > 28f * scale)
                continue;

            if (Main.LocalPlayer != null)
                Main.LocalPlayer.mouseInterface = true;
            Main.instance.MouseText($"{owner.name}'s adventure portal");
        }

        return true;
    }

    private static string GetPlayerNameSafe(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= Main.maxPlayers)
            return $"Player {playerIndex}";

        Player player = Main.player[playerIndex];
        if (player == null || string.IsNullOrWhiteSpace(player.name))
            return $"Player {playerIndex}";

        return player.name;
    }
}
