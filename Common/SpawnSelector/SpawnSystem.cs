using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.GameTimer;
using PvPAdventure.Common.SpawnSelector.UI;
using PvPAdventure.Content.Items;
using PvPAdventure.Core.Debug;
using PvPAdventure.Core.Net;
using SubworldLibrary;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config.UI;
using Terraria.UI;

namespace PvPAdventure.Common.SpawnSelector;

[Autoload(Side = ModSide.Client)]
public class SpawnSystem : ModSystem
{
    public UserInterface ui;
    public UISpawnState spawnState;

    public static bool Enabled { get; private set; }
    public static bool CanTeleport { get; private set; }
    public static bool IsLocalPlayerInSpawnRegion { get; private set; }
    public static float TeleportIconOpacity => (CanTeleport || IsLocalPlayerInSpawnRegion) ? 1f : 0.3f;

    public static void SetCanTeleport(bool value) => CanTeleport = value;

    private ulong _nextUiRebuildTick;
    private const ulong UiRebuildIntervalTicks = 60 * 5;

    public static bool IsUiOpen
    {
        get
        {
            var sys = ModContent.GetInstance<SpawnSystem>();
            return sys != null && sys.ui?.CurrentState == sys.spawnState;
        }
    }
    private bool wasInSpawnRegion;

    public override void UpdateUI(GameTime gameTime)
    {
        Player local = Main.LocalPlayer;
        if (local == null || local.ghost)
        {
            IsLocalPlayerInSpawnRegion = false;
            ui?.SetState(null);
            return;
        }

        SpawnPlayer sp = local.GetModPlayer<SpawnPlayer>();

        bool playing = ModContent.GetInstance<GameManager>().CurrentPhase == GameManager.Phase.Playing;
        bool inSubworld = SubworldSystem.AnyActive();
        bool inSpawnRegion = sp.IsPlayerInSpawnRegion();
        IsLocalPlayerInSpawnRegion = inSpawnRegion;
        bool enteredSpawnRegion = inSpawnRegion && !wasInSpawnRegion;
        wasInSpawnRegion = inSpawnRegion;

        if (enteredSpawnRegion && !local.dead)
            sp.ClearSelection();

        bool usingMirror = IsUsingAdventureMirror(local, out bool mirrorReady, out _);

        Enabled = (playing || inSubworld) && (inSpawnRegion || usingMirror);

        bool show = (playing || inSubworld) && (local.dead || Enabled);
        if (!show)
        {
            ui.SetState(null);
            return;
        }

        SetCanTeleport(ComputeCanTeleport(local, inSpawnRegion, usingMirror, mirrorReady));

        if (!CanTeleport)
            sp.ClearExecuteRequest();

        if (!local.dead && CanTeleport)
        {
            bool allowExecute = inSpawnRegion || usingMirror || sp.ExecuteRequested;

            if (allowExecute)
                TryExecuteSelection(local, sp);
        }

        if (ui.CurrentState != spawnState)
        {
            spawnState = new UISpawnState();
            ui.SetState(spawnState);

            _nextUiRebuildTick = Main.GameUpdateCount + UiRebuildIntervalTicks; // initialize / reset rebuild

            if (!local.dead && !inSpawnRegion) // Do NOT auto-select latest while in a spawn region (prevents instant execution).
                sp.TryAutoSelectLatestSelection();
        }

        if (ui.CurrentState == spawnState && Main.GameUpdateCount >= _nextUiRebuildTick)
        {
            _nextUiRebuildTick = Main.GameUpdateCount + UiRebuildIntervalTicks;
            spawnState.RequestRebuild();
        }

        ui.Update(gameTime);
    }

    private static bool ComputeCanTeleport(Player local, bool inSpawnRegion, bool usingMirror, bool mirrorReady)
    {
        SpawnPlayer sp = local.GetModPlayer<SpawnPlayer>();
        if (!sp.CanTeleportNow())
            return false;

        if (local.dead)
            return local.respawnTimer <= 2;

        if (!Enabled)
            return false;

        if (inSpawnRegion)
            return true;

        if (usingMirror)
            return mirrorReady;

        return false;
    }

    private static bool IsUsingAdventureMirror(Player player, out bool ready, out int secondsLeft)
    {
        ready = false;
        secondsLeft = 0;

        if (player == null || !player.active)
            return false;

        if (player.itemAnimation <= 0)
            return false;

        int mirrorType = ModContent.ItemType<AdventureMirror>();
        if (player.HeldItem == null || player.HeldItem.type != mirrorType)
            return false;

        ready = player.itemTime <= 2;

        int framesLeft = player.itemTime - 2;
        if (framesLeft <= 2)
            framesLeft = 0;

        secondsLeft = (framesLeft + 59) / 60;
        return true;
    }

    private void TryExecuteSelection(Player p, SpawnPlayer sp)
    {
        if (sp.SelectedType == SpawnType.None)
            return;

        if (p.whoAmI == Main.myPlayer)
            Log.Chat("Executing spawn: " + DescribeSelection(sp.SelectedType, sp.SelectedPlayerIndex));

        bool executed = PerformTeleport(p, sp.SelectedType, sp.SelectedPlayerIndex);
        if (!executed)
            return;

        if (Main.netMode != NetmodeID.MultiplayerClient)
            TeleportChat.Announce(p, sp.SelectedType, sp.SelectedPlayerIndex);

        sp.ClearExecuteRequest();
        OnTeleportExecuted(p);
    }

    private static bool PerformTeleport(Player p, SpawnType type, int idx)
    {
        // MP client: always request (no local execution)
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            SendTeleportRequest(type, idx);
            return true;
        }

        switch (type)
        {
            case SpawnType.World:
                p.Teleport(WorldSpawnWorldPos(), TeleportationStyleID.RecallPotion);
                return true;

            case SpawnType.MyBed:
                return TryTeleportToBed(p, p);

            case SpawnType.MyPortal:
                return TryTeleportToMyPortal(p);

            case SpawnType.TeammatePortal:
                if (!SpawnPlayer.IsValidTeammatePortalIndex(p, idx))
                    return false;

                return TryTeleportToPortal(p, Main.player[idx]);

            case SpawnType.Random:
                p.TeleportationPotion();
                SoundEngine.PlaySound(SoundID.Item6, p.Center);
                return true;

            case SpawnType.TeammateBed:
                if (idx < 0 || idx >= Main.maxPlayers)
                    return false;

                Player bedOwner = Main.player[idx];
                if (bedOwner == null || !bedOwner.active)
                    return false;

                return TryTeleportToBed(p, bedOwner);

            default:
                return false;
        }
    }

    private static bool TryTeleportToMyPortal(Player player)
    {
        return TryTeleportToPortal(player, player);
    }

    private static bool TryTeleportToPortal(Player player, Player portalOwner)
    {
        if (portalOwner == null || !portalOwner.active)
            return false;

        if (!PortalSystem.TryGetPortalWorldPos(portalOwner, out Vector2 worldPos))
            return false;

        Vector2 topLeft = worldPos - new Vector2(player.width * 0.5f, player.height);
        player.Teleport(topLeft, TeleportationStyleID.RecallPotion);
        return true;
    }

    private static bool TryTeleportToBed(Player requester, Player bedOwner)
    {
        if (bedOwner.SpawnX < 0 || bedOwner.SpawnY < 0)
            return false;

        if (!Player.CheckSpawn(bedOwner.SpawnX, bedOwner.SpawnY))
            return false;

        Vector2 pos = new Vector2(bedOwner.SpawnX, bedOwner.SpawnY - 6).ToWorldCoordinates();
        requester.Teleport(pos, TeleportationStyleID.RecallPotion);
        return true;
    }

    private static void SendTeleportRequest(SpawnType type, int idx)
    {
        var pkt = ModContent.GetInstance<PvPAdventure>().GetPacket();
        pkt.Write((byte)AdventurePacketIdentifier.TeleportRequest);
        pkt.Write((byte)Main.myPlayer);
        pkt.Write((byte)type);

        if (type == SpawnType.TeammateBed || type == SpawnType.TeammatePortal)
            pkt.Write((short)idx);

        pkt.Send();
    }

    private static Vector2 WorldSpawnWorldPos() =>
        new Vector2(Main.spawnTileX, Main.spawnTileY - 3).ToWorldCoordinates();

    private static string DescribeSelection(SpawnType type, int idx)
    {
        if (type != SpawnType.TeammateBed && type != SpawnType.TeammatePortal)
            return type.ToString();

        return type.ToString() + " (" + GetPlayerNameSafe(idx) + ")";
    }

    private static string GetPlayerNameSafe(int idx)
    {
        if (idx < 0 || idx >= Main.maxPlayers)
            return "<unknown>";

        Player p = Main.player[idx];
        if (p == null)
            return "<unknown>";

        return p.name;
    }

    private static void OnTeleportExecuted(Player p)
    {
        // hard reset mirror use state
        p.itemTime = 0;
        p.itemAnimation = 0;
        p.reuseDelay = 0;

        SetCanTeleport(false);

        SpectateSystem.MapRestore = false;
        Main.mapFullscreen = false;

        SpawnPlayer sp = p.GetModPlayer<SpawnPlayer>();
        sp.StartTeleportCooldown();
        sp.ClearSelection();
    }

    #region Load hooks
    public override void OnWorldLoad()
    {
        ui = new UserInterface();
        spawnState = new UISpawnState();

        SetCanTeleport(false);
        IsLocalPlayerInSpawnRegion = false;
        Main.OnPostFullscreenMapDraw += DrawOnFullscreenMap;
    }

    public override void Unload()
    {
        SetCanTeleport(false);
        IsLocalPlayerInSpawnRegion = false;
        Main.OnPostFullscreenMapDraw -= DrawOnFullscreenMap;
    }
    #endregion

    #region Drawing
    private void DrawAdventureMirrorTimer(SpriteBatch sb)
    {
        Player local = Main.LocalPlayer;
        if (local == null)
            return;

        if (!IsUsingAdventureMirror(local, out _, out int secondsLeft))
            return;

        if (spawnState?.TitlePanel == null)
            return;

        var dims = spawnState.TitlePanel.GetDimensions();
        string text = secondsLeft.ToString();

        Vector2 size = FontAssets.DeathText.Value.MeasureString(text);

        Vector2 pos = new(
            dims.X + dims.Width + 12f,
            dims.Y + (dims.Height - size.Y) * 0.5f + 8f
        );

        Utils.DrawBorderStringBig(sb, text, pos, Color.White);
    }

    private void DrawOnFullscreenMap(Vector2 mapPos, float mapScale)
    {
        if (!Main.mapFullscreen || ui?.CurrentState == null)
            return;

        SpriteBatch sb = Main.spriteBatch;
        sb.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,DepthStencilState.None,RasterizerState.CullCounterClockwise, null,Main.UIScaleMatrix);
        ui.Draw(sb, Main._drawInterfaceGameTime);
        DrawAdventureMirrorTimer(sb);

        sb.End();
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int idx = layers.FindIndex(l => l.Name == "Vanilla: Death Text");

        if (IsAnyConfigUIOpen())
            idx = layers.FindIndex(l => l.Name == "Vanilla: Interface Logic 1");

        if (idx == -1)
            return;

        layers.Insert(idx, new LegacyGameInterfaceLayer(
            "PvPAdventure: SpawnSystem UI",
            delegate
            {
                if (Main.mapFullscreen || ui?.CurrentState == null)
                    return true;

                SpriteBatch sb = Main.spriteBatch;
                ui.Draw(sb, Main._drawInterfaceGameTime);
                DrawAdventureMirrorTimer(sb);
                return true;
            },
            InterfaceScaleType.UI
        ));
    }
    #endregion

    #region Helpers
    private static bool IsAnyConfigUIOpen()
    {
        UIState s = Main.InGameUI?._currentState;
        return s is UIModConfig || s is UIModConfigList || Main.ingameOptionsWindow;
    }
    #endregion
}
