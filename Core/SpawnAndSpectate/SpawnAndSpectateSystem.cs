using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.SpawnAndSpectate.UI;
using PvPAdventure.Core.SSC;
using ReLogic.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config.UI;
using Terraria.UI;

namespace PvPAdventure.Core.SpawnAndSpectate;

[Autoload(Side = ModSide.Client)]
public class SpawnAndSpectateSystem : ModSystem
{
    public UserInterface ui;
    public SpawnAndSpectateState spawnSelectorState;

    private static bool Enabled;
    public static void SetEnabled(bool newValue) => Enabled = newValue;
    public static bool GetEnabled() => Enabled;
    private bool _wasShowingUI;

    private static bool canRespawn;
    public static bool CanRespawn => canRespawn;
    public static void SetCanRespawn(bool value) => canRespawn = value;

    public static int? HoveredPlayerIndex;
    public static int? SpectatePlayerIndex;
    public static int? SelectedSpawnPlayerIndex;
    public static int? HoverSpectatePlayerIndex;

    public static bool HoveringWorldSpawn;

    // Map hover restore
    private static bool _restoreFullscreenMapAfterHover;
    public static bool IsFullscreenMapTemporarilyClosedForSpectate => _restoreFullscreenMapAfterHover;
    public static void CancelFullscreenMapRestore()
    {
        _restoreFullscreenMapAfterHover = false;
    }

    private bool _lastMapModeOpen;
    private static int MapTimer;
    public static int GetMapTimer;

    private const int MapTicksMax = 5 * 60;

    // Random commit
    private static bool _mapRandomCommitted;
    public static bool IsMapRandomCommitted => _mapRandomCommitted;

    // Map commit
    private static int _mapCommittedTeammateIndex = -1;
    private static bool _mapWorldSpawnCommitted;
    public static bool HasAnyMapCommit =>
    _mapRandomCommitted ||
    _mapWorldSpawnCommitted ||
    _mapCommittedTeammateIndex != -1;

    #region Map commit

    public static bool IsMapTeleportGated
    {
        get
        {
            Player p = Main.LocalPlayer;
            if (p == null || p.dead)
                return false;

            return Main.mapFullscreen || _restoreFullscreenMapAfterHover;
        }
    }
    public static void OnMapTeleportExecuted()
    {
        ResetMapTimer();
        SetCanRespawn(false);

        _restoreFullscreenMapAfterHover = false;
        Main.mapFullscreen = false;

        ClearMapCommit();
    }

    public static bool ShouldCommitMapTeleport => IsMapTeleportGated && !CanRespawn;
    public static bool IsMapWorldSpawnCommitted => _mapWorldSpawnCommitted;
    public static bool IsMapTeammateCommitted(int idx) => _mapCommittedTeammateIndex == idx;

    private static bool HasMapCommit =>
        _mapRandomCommitted ||
        _mapWorldSpawnCommitted ||
        _mapCommittedTeammateIndex != -1;

    private static void ClearMapCommit()
    {
        _mapCommittedTeammateIndex = -1;
        _mapWorldSpawnCommitted = false;
        _mapRandomCommitted = false;
    }

    public static void ResetMapTimer()
    {
        var config = ModContent.GetInstance<AdventureServerConfig>();
        var frames = config.AdventureMirrorRecallFrames;

        MapTimer = MapTicksMax;
    }

    public static void ToggleMapCommitRandom()
    {
        _mapRandomCommitted = !_mapRandomCommitted;

        if (_mapRandomCommitted)
        {
            _mapWorldSpawnCommitted = false;
            _mapCommittedTeammateIndex = -1;
        }
    }

    public static void ToggleMapCommitWorldSpawn()
    {
        if (_mapWorldSpawnCommitted)
        {
            ClearMapCommit();
            return;
        }

        _mapWorldSpawnCommitted = true;
        _mapCommittedTeammateIndex = -1;
        _mapRandomCommitted = false;
    }

    public static void ToggleMapCommitTeammate(int idx)
    {
        bool wasSame = _mapCommittedTeammateIndex == idx;

        if (wasSame)
        {
            ClearMapCommit();
            return;
        }

        _mapCommittedTeammateIndex = idx;
        _mapWorldSpawnCommitted = false;
        _mapRandomCommitted = false;
    }

    private void TryExecuteMapCommit()
    {
        if (!IsMapTeleportGated)
            return;

        if (MapTimer > 0)
            return;

        if (!HasMapCommit)
            return;

        Player p = Main.LocalPlayer;
        if (p == null || p.dead)
        {
            ClearMapCommit();
            return;
        }

        // Random
        if (_mapRandomCommitted)
        {
            p.GetModPlayer<RespawnPlayer>().RandomTeleport();
            ClearMapCommit();
            ResetMapTimer();
            Main.mapFullscreen = false;
            return;
        }

        // World spawn
        if (_mapWorldSpawnCommitted)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                Vector2 spawnWorld = new Vector2(Main.spawnTileX, Main.spawnTileY - 3).ToWorldCoordinates();
                p.Teleport(spawnWorld, TeleportationStyleID.RecallPotion);
                //Main.mapFullscreen = false;
            }
            else if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ModPacket pkt = Mod.GetPacket();
                pkt.Write((byte)AdventurePacketIdentifier.WorldSpawnTeleport);
                pkt.Write((byte)Main.myPlayer);
                pkt.Send();

                Main.mapFullscreen = false;
                ResetMapTimer();
            }

            ClearMapCommit();
            return;
        }

        // Teammate
        int idx = _mapCommittedTeammateIndex;
        if (IsValidTeammateIndex(idx))
        {
            p.GetModPlayer<RespawnPlayer>().TeammateTeleport(idx);
            Main.mapFullscreen = false;
            _restoreFullscreenMapAfterHover = false;
        }

        ClearMapCommit();
    }

    #endregion

    public static bool ShouldShowUI
    {
        get
        {
            Player p = Main.LocalPlayer;
            if (p == null)
                return false;

            if (p.dead)
                return true;

            return Enabled;
        }
    }

    public static bool IsAliveSpawnRegionInstant
    {
        get
        {
            Player p = Main.LocalPlayer;
            return p != null && !p.dead && Enabled;
        }
    }

    public static bool IsValidTeammateIndex(int idx)
    {
        if (idx < 0 || idx >= Main.maxPlayers)
            return false;

        Player local = Main.LocalPlayer;
        Player t = Main.player[idx];

        if (local == null || t == null || !t.active)
            return false;

        if (t.whoAmI == local.whoAmI)
            return false;

        if (local.team == 0 || t.team != local.team)
            return false;

        if (t.dead)
            return false;

        return true;
    }

    public static void ToggleSpectateOnPlayerIndex(int idx)
    {
        bool canSpectate = Main.LocalPlayer.dead || IsAliveSpawnRegionInstant;
        if (!canSpectate)
            return;

        if (!IsValidTeammateIndex(idx))
            return;

        SpectatePlayerIndex = (SpectatePlayerIndex == idx) ? null : idx;
    }

    public override void OnWorldLoad()
    {
        ui = new UserInterface();
        spawnSelectorState = new SpawnAndSpectateState();

        SetCanRespawn(false);

        Main.OnPostFullscreenMapDraw += DrawOnFullscreenMap;
    }

    public override void Unload()
    {
        SetCanRespawn(false);
        Main.OnPostFullscreenMapDraw -= DrawOnFullscreenMap;
    }

    public override void UpdateUI(GameTime gameTime)
    {
        if (ui == null)
            return;

        bool mapSessionOpen = Main.mapFullscreen || _restoreFullscreenMapAfterHover;

        if (mapSessionOpen && !_lastMapModeOpen)
        {
            Log.Chat("open map");

            // Open the map and reset the timer
            ResetMapTimer();
            ClearMapCommit();
        }
        else if (!mapSessionOpen && _lastMapModeOpen)
        {
            Log.Chat("close map");
            MapTimer = 0;
            ClearMapCommit();
        }

        if (mapSessionOpen && MapTimer > 0)
        {
            MapTimer--;
        }

        _lastMapModeOpen = mapSessionOpen;

        Player p = Main.LocalPlayer;
        if (p != null && !p.dead)
        {
            SetCanRespawn(IsMapTeleportGated && MapTimer <= 0);
        }

        if (mapSessionOpen && MapTimer == 0)
        {
            TryExecuteMapCommit();
        }

        var ssc = ModContent.GetInstance<ServerSystem>();
        if (ssc != null && ssc.UI != null && ssc.UI?.CurrentState != null)
            return;

        bool show = ShouldShowUI;
        if (!show)
        {
            _wasShowingUI = false;
            HoveringWorldSpawn = false;

            if (ui.CurrentState != null)
                ui.SetState(null);

            return;
        }

        if (!_wasShowingUI || ui.CurrentState != spawnSelectorState)
        {
            spawnSelectorState = new SpawnAndSpectateState();
            ui.SetState(spawnSelectorState);
            _wasShowingUI = true;
        }

        ui.Update(gameTime);
    }

    #region Spectating

    public static void ClearSpectate()
    {
        SpectatePlayerIndex = null;
    }

    public static void TrySetSpectate(int playerIndex)
    {
        bool canSpectate = Main.LocalPlayer.dead || IsAliveSpawnRegionInstant;
        if (!canSpectate)
            return;

        if (playerIndex < 0 || playerIndex >= Main.maxPlayers)
            return;

        Player p = Main.player[playerIndex];
        if (p == null || !p.active || p.dead)
            return;

        SpectatePlayerIndex = playerIndex;
    }

    public static void TrySetHoverSpectate(int idx)
    {
        bool canSpectate = Main.LocalPlayer.dead || IsAliveSpawnRegionInstant;
        if (!canSpectate)
        {
            HoverSpectatePlayerIndex = null;
            return;
        }

        if (!IsValidTeammateIndex(idx))
        {
            HoverSpectatePlayerIndex = null;
            return;
        }

        HoverSpectatePlayerIndex = idx;
    }

    public static void ClearHoverSpectateIfMatch(int idx)
    {
        if (HoverSpectatePlayerIndex == idx)
            HoverSpectatePlayerIndex = null;
    }

    public override void ModifyScreenPosition()
    {
        Player local = Main.LocalPlayer;

        bool canSpectate = local != null && local.active && (local.dead || IsAliveSpawnRegionInstant);
        if (!canSpectate)
        {
            if (_restoreFullscreenMapAfterHover && !Main.mapFullscreen)
            {
                Main.mapFullscreen = true;
            }

            _restoreFullscreenMapAfterHover = false;

            HoverSpectatePlayerIndex = null;
            SpectatePlayerIndex = null;
            HoveringWorldSpawn = false;
            return;
        }

        bool hoveringOption = HoveringWorldSpawn || HoverSpectatePlayerIndex != null;

        if (hoveringOption)
        {
            if (Main.mapFullscreen)
            {
                Main.mapFullscreen = false;
                _restoreFullscreenMapAfterHover = true;
            }
        }
        else if (_restoreFullscreenMapAfterHover)
        {
            if (!Main.mapFullscreen)
            {
                Main.mapFullscreen = true;
            }

            _restoreFullscreenMapAfterHover = false;
        }

        if (HoveringWorldSpawn)
        {
            Vector2 spawnWorld = new Vector2(Main.spawnTileX, Main.spawnTileY - 3).ToWorldCoordinates();
            Vector2 targetTopLeft = spawnWorld - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
            Main.screenPosition = targetTopLeft;
            return;
        }

        int? targetIndex = HoverSpectatePlayerIndex ?? SpectatePlayerIndex;
        if (targetIndex is not int idx || idx < 0 || idx >= Main.maxPlayers)
        {
            HoverSpectatePlayerIndex = null;
            return;
        }

        Player target = Main.player[idx];
        if (target == null || !target.active || target.dead)
        {
            if (HoverSpectatePlayerIndex == idx)
                HoverSpectatePlayerIndex = null;

            if (SpectatePlayerIndex == idx)
                SpectatePlayerIndex = null;

            return;
        }

        Main.screenPosition = target.position - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
    }

    #endregion

    #region Draw

    private void DrawMapSpectateCountdown(SpriteBatch sb)
    {
        var player = Main.LocalPlayer.GetModPlayer<SpawnPointPlayer>();
        if (player.IsPlayerInSpawnRegion())
        {
            return;
        }

        string text;

        if (Main.LocalPlayer != null && Main.LocalPlayer.dead && Main.mapFullscreen)
        {
            // Draw respawn timer
            int secondsLeft = (Main.LocalPlayer.respawnTimer + 59) / 60; // 10..0 or 20..0
            if (secondsLeft <= 0)
                secondsLeft = 0;

            text = "Dead: " + secondsLeft.ToString();
        }
        else
        {
            // Draw map timer
            bool mapSessionOpen = Main.mapFullscreen || _restoreFullscreenMapAfterHover;
            bool spectating = HoverSpectatePlayerIndex.HasValue || HoveringWorldSpawn;
            if (mapSessionOpen || spectating && Main.mapFullscreen)
            {
                int secondsLeft = (MapTimer + 59) / 60; // 5..0
                if (secondsLeft <= 0)
                    secondsLeft = 0;

                text = secondsLeft.ToString();
            }
            else
                text = "";
        }

        Vector2 size = FontAssets.DeathText.Value.MeasureString(text);
        Vector2 pos = new(Main.screenWidth * 0.5f - size.X * 0.5f, -4f);

        Utils.DrawBorderStringBig(sb, text, pos, Color.White);
    }

    private void DrawOnFullscreenMap(Vector2 mapPos, float mapScale)
    {
        if (!Main.mapFullscreen || ui?.CurrentState == null)
            return;

        var sb = Main.spriteBatch;
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
        ui.Draw(sb, Main._drawInterfaceGameTime);
        DrawMapSpectateCountdown(sb);
        sb.End();
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int mouseTextIndex = layers.FindIndex(l => l.Name == "Vanilla: Interface Logic 4");

        if (IsAnyConfigUIOpen())
            mouseTextIndex = layers.FindIndex(l => l.Name == "Vanilla: Interface Logic 1");

        if (mouseTextIndex == -1)
            return;

        layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
            "PvPAdventure: SpawnAndSpectate",
            delegate
            {
                if (Main.mapFullscreen || ui?.CurrentState == null)
                    return true;

                ui.Draw(Main.spriteBatch, Main._drawInterfaceGameTime);
                DrawMapSpectateCountdown(Main.spriteBatch);
                return true;
            },
            InterfaceScaleType.UI));
    }

    private bool IsAnyConfigUIOpen()
    {
        UIState s = Main.InGameUI?._currentState;
        if (s != null && s is UIModConfig)
            return true;

        return false;
    }

    #endregion
}
