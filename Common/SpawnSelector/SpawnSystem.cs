using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Chat;
using PvPAdventure.Common.GameTimer;
using PvPAdventure.Common.SpawnSelector.UI;
using PvPAdventure.Content.Items;
using PvPAdventure.Core.Debug;
using PvPAdventure.Core.Net;
using PvPAdventure.Core.Utilities;
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
    public static bool IsLocalPlayerOnTeleportCooldown =>
        Main.LocalPlayer?.GetModPlayer<SpawnPlayer>().IsTeleportOnCooldown == true;
    public static string LocalTeleportCooldownText =>
        $"Cooldown: {Main.LocalPlayer?.GetModPlayer<SpawnPlayer>().TeleportCooldownSecondsLeft ?? 0}";
    public static bool CanUseStoredPortal(Player player) =>
        player?.active == true && (player.dead ||
        (player.whoAmI == Main.myPlayer ? IsLocalPlayerInSpawnRegion : player.GetModPlayer<SpawnPlayer>().IsPlayerInSpawnRegion()));

    public static bool IsLocalPlayerReadyForSpawnUi
    {
        get
        {
            Player local = Main.LocalPlayer;
            return local?.active == true &&
                   (local.dead ? local.respawnTimer <= 2 && local.GetModPlayer<SpawnPlayer>().CanTeleportNow() : CanTeleport);
        }
    }

    public static void SetCanTeleport(bool value) => CanTeleport = value;

    public static Color DisabledButtonColor => new Color(230, 40, 10) * 0.37f;

    public static void DrawForbiddenIcon(SpriteBatch sb, Vector2 pos, float scale)
    {
        Texture2D icon = Ass.Icon_Forbidden.Value;
        sb.Draw(icon, pos, null, Color.White, 0f, icon.Size() * 0.5f, scale, SpriteEffects.None, 0f);
    }

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
            wasInSpawnRegion = false;
            ui?.SetState(null);
            return;
        }

        SpawnPlayer sp = local.GetModPlayer<SpawnPlayer>();

        bool playing = ModContent.GetInstance<GameManager>().CurrentPhase == GameManager.Phase.Playing;
        bool inSubworld = SubworldSystem.AnyActive();
        bool inSpawnRegion = sp.IsPlayerInSpawnRegion();
        UpdateSpawnRegionSound(inSpawnRegion, playing || inSubworld);
        IsLocalPlayerInSpawnRegion = inSpawnRegion;

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
            bool allowExecute = inSpawnRegion || sp.ExecuteRequested;

            if (allowExecute)
                TryExecuteSelection(local, sp);
        }

        if (ui.CurrentState != spawnState)
        {
            spawnState = new UISpawnState();
            ui.SetState(spawnState);

            if (!local.dead && !inSpawnRegion) // Do NOT auto-select latest while in a spawn region (prevents instant execution).
                sp.TryAutoSelectLatestSelection();
        }

        ui.Update(gameTime);
    }

    private void UpdateSpawnRegionSound(bool inSpawnRegion, bool enabled)
    {
        if (enabled && inSpawnRegion != wasInSpawnRegion)
        {
            SoundEngine.PlaySound(inSpawnRegion ? SoundID.MenuOpen : SoundID.MenuClose);
        }

        wasInSpawnRegion = enabled && inSpawnRegion;
    }

    private static bool ComputeCanTeleport(Player local, bool inSpawnRegion, bool usingMirror, bool mirrorReady)
    {
        SpawnPlayer sp = local.GetModPlayer<SpawnPlayer>();
        if (!sp.CanTeleportNow())
            return false;

        return local.dead ? local.respawnTimer <= 2 : Enabled && (inSpawnRegion || usingMirror && mirrorReady);
    }

    private static bool IsUsingAdventureMirror(Player player, out bool ready, out int secondsLeft)
    {
        ready = false;
        secondsLeft = 0;

        if (player?.active != true || player.itemAnimation <= 0 || player.HeldItem?.type != ModContent.ItemType<AdventureMirror>())
            return false;

        ready = player.itemTime <= 2;

        int framesLeft = player.itemTime - 2;
        if (framesLeft <= 2)
            framesLeft = 0;

        secondsLeft = (framesLeft + 59) / 60;
        return true;
    }

    internal static bool TryExecuteSelection(Player p, SpawnPlayer sp)
    {
        if (sp.SelectedType == SpawnType.None)
            return false;

        if (p.whoAmI == Main.myPlayer)
            Log.Chat("Executing spawn: " + DescribeSelection(sp.SelectedType, sp.SelectedPlayerIndex));

        bool executed = PerformTeleport(p, sp.SelectedType, sp.SelectedPlayerIndex);
        if (!executed)
            return false;

        if (Main.netMode != NetmodeID.MultiplayerClient)
            TeleportChat.Announce(p, sp.SelectedType, sp.SelectedPlayerIndex);

        sp.ClearExecuteRequest();
        OnTeleportExecuted(p);
        return true;
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
                if (!CanUseStoredPortal(p))
                    return false;

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

    private static bool TryTeleportToMyPortal(Player player) => TryTeleportToPortal(player, player);

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

        return Main.player[idx]?.name ?? "<unknown>";
    }

    private static void OnTeleportExecuted(Player p)
    {
        AdventureMirror.ResetUseState(p);

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
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
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
        return Main.ingameOptionsWindow || s is UIModConfig or UIModConfigList;
    }
    #endregion
}
