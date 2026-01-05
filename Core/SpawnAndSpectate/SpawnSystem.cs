using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.SpawnAndSpectate.UI;
using PvPAdventure.System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config.UI;
using Terraria.UI;

namespace PvPAdventure.Core.SpawnAndSpectate;

[Autoload(Side = ModSide.Client)]
public class SpawnSystem : ModSystem
{
    public UserInterface ui;
    public UISpawnState spawnState;

    // The visibility of the state
    public static bool Enabled { get; private set; }
    // Whether or not we can instantly execute teleports
    public static bool CanTeleport { get; private set; }
    public static void SetCanTeleport(bool value) => CanTeleport = value;

    public enum SpawnType : byte
    {
        None,
        World,
        Random,
        Player,
        Bed
    }

    // Map timer
    private bool wasShowingUI;
    private bool sessionWasOpen;
    private static int mapTimer;
    private static bool SessionOpen => Main.mapFullscreen || SpectateSystem.MapRestore;

    public static void ResetMapTimer()
    {
        int frames = ModContent.GetInstance<AdventureServerConfig>().MapRecallFrames;
        mapTimer = frames > 0 ? frames : 5 * 60;
    }

    private static bool InstantTeleport(Player p)
    {
        if (p == null || !p.active || p.dead || !Enabled)
            return false;

        return p.GetModPlayer<SpawnPlayer>().IsPlayerInSpawnRegion();
    }

    public static bool IsValidTeammateIndex(Player requester, int idx)
    {
        if (requester == null || !requester.active)
            return false;

        if (idx < 0 || idx >= Main.maxPlayers)
            return false;

        Player t = Main.player[idx];
        if (t == null || !t.active || t.dead)
            return false;

        if (requester.team == 0 || t.team != requester.team)
            return false;

        return true;
    }

    public static bool IsValidTeammateIndex(int idx) => IsValidTeammateIndex(Main.LocalPlayer, idx);

    private void ClearSelection()
    {
        Player p = Main.LocalPlayer;
        if (p == null)
            return;

        p.GetModPlayer<SpawnPlayer>().ClearSelection();
    }

    private void OnTeleportExecuted()
    {
        ResetMapTimer();
        SetCanTeleport(false);

        SpectateSystem.MapRestore = false;
        Main.mapFullscreen = false;

        ClearSelection();
    }

    private static void TeleportWorld(Player p)
    {
        Vector2 pos = new Vector2(Main.spawnTileX, Main.spawnTileY - 3).ToWorldCoordinates();

        if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient)
        {
            var pkt = ModContent.GetInstance<PvPAdventure>().GetPacket();
            pkt.Write((byte)AdventurePacketIdentifier.TeleportRequest);
            pkt.Write((byte)Main.myPlayer);
            pkt.Write((byte)SpawnType.World);
            pkt.Send();
            return;
        }

        p.Teleport(pos, Terraria.ID.TeleportationStyleID.RecallPotion);
    }

    private static void TeleportRandom(Player p)
    {
        if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient)
        {
            Terraria.NetMessage.SendData(Terraria.ID.MessageID.RequestTeleportationByServer);
            return;
        }

        p.TeleportationPotion();
    }

    private static void TeleportToPlayer(Player p, int idx)
    {
        if (!IsValidTeammateIndex(idx))
            return;

        Player t = Main.player[idx];
        if (t == null || !t.active || t.dead)
            return;

        if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient)
        {
            var pkt = ModContent.GetInstance<PvPAdventure>().GetPacket();
            pkt.Write((byte)AdventurePacketIdentifier.TeleportRequest);
            pkt.Write((byte)Main.myPlayer);
            pkt.Write((byte)SpawnType.Player);
            pkt.Write((short)idx);
            pkt.Send();
            return;
        }

        p.UnityTeleport(t.position);
    }

    private static void TeleportToBed(Player p, int idx)
    {
        if (!IsValidTeammateIndex(idx))
            return;

        Player t = Main.player[idx];
        if (t == null || !t.active)
            return;

        // Server decides final position in MP
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            var pkt = ModContent.GetInstance<PvPAdventure>().GetPacket();
            pkt.Write((byte)AdventurePacketIdentifier.TeleportRequest);
            pkt.Write((byte)Main.myPlayer);
            pkt.Write((byte)SpawnType.Bed);
            pkt.Write((short)idx);
            pkt.Send();
            return;
        }

        if (t.SpawnX < 0 || t.SpawnY < 0 || !Player.CheckSpawn(t.SpawnX, t.SpawnY))
            return;

        Vector2 pos = new Vector2(t.SpawnX, t.SpawnY - 6).ToWorldCoordinates();
        p.Teleport(pos, TeleportationStyleID.RecallPotion);
    }

    private void TryExecuteSelection(Player p)
    {
        if (p == null || !p.active)
            return;

        if (!CanTeleport)
            return;

        if (p.dead)
            return;

        SpawnPlayer sp = p.GetModPlayer<SpawnPlayer>();
        if (sp.SelectedType == SpawnType.None)
            return;

        if (sp.SelectedType == SpawnType.Random)
        {
            TeleportRandom(p);
            OnTeleportExecuted();
            return;
        }

        if (sp.SelectedType == SpawnType.World)
        {
            TeleportWorld(p);
            OnTeleportExecuted();
            return;
        }

        if (sp.SelectedType == SpawnType.Player)
        {
            TeleportToPlayer(p, sp.SelectedPlayerIndex);
            OnTeleportExecuted();
            return;
        }

        if (sp.SelectedType == SpawnType.Bed)
        {
            TeleportToBed(p, sp.SelectedPlayerIndex);
            OnTeleportExecuted();
            return;
        }
    }

    public override void OnWorldLoad()
    {
        ui = new UserInterface();
        spawnState = new UISpawnState();

        SetCanTeleport(false);
        Main.OnPostFullscreenMapDraw += DrawOnFullscreenMap;
    }

    public override void Unload()
    {
        SetCanTeleport(false);
        Main.OnPostFullscreenMapDraw -= DrawOnFullscreenMap;
    }

    public override void UpdateUI(GameTime gameTime)
    {
        Player local = Main.LocalPlayer;
        if (local == null)
            return;

        bool playing = ModContent.GetInstance<GameManager>().CurrentPhase == GameManager.Phase.Playing;
        bool inSpawnRegion = local.GetModPlayer<SpawnPlayer>().IsPlayerInSpawnRegion();
        bool sessionOpen = SessionOpen;

        Enabled = playing && !Main.playerInventory && (inSpawnRegion || sessionOpen);
        if (sessionOpen && !sessionWasOpen)
        {
            ResetMapTimer();
            ClearSelection();
        }
        else if (!sessionOpen && sessionWasOpen)
        {
            ClearSelection();
        }

        if (sessionOpen && mapTimer > 0)
            mapTimer--;

        sessionWasOpen = sessionOpen;

        if (local.dead)
        {
            // If you freeze at 2 elsewhere, this will be stable.
            SetCanTeleport(local.respawnTimer <= 2);
        }
        else
        {
            SetCanTeleport(InstantTeleport(local) || (sessionOpen && mapTimer <= 0));
        }

        // Execute selection whenever we actually can (restores instant teleport from spawn region)
        if (CanTeleport)
            TryExecuteSelection(local);

        bool show = local.dead || Enabled;
        if (!show)
        {
            wasShowingUI = false;
            ui.SetState(null);
            return;
        }

        if (!wasShowingUI || ui.CurrentState != spawnState)
        {
            spawnState = new UISpawnState();
            ui.SetState(spawnState);
            wasShowingUI = true;
        }

        ui.Update(gameTime);
    }

    private void DrawMapCountdown(SpriteBatch sb)
    {
        if (Main.LocalPlayer?.GetModPlayer<SpawnPlayer>()?.IsPlayerInSpawnRegion() == true)
            return;

        string text = "";

        if (Main.LocalPlayer != null && Main.LocalPlayer.dead && Main.mapFullscreen)
        {
            int secondsLeft = (Main.LocalPlayer.respawnTimer + 59) / 60;
            if (secondsLeft < 0)
                secondsLeft = 0;

            text = "Dead: " + secondsLeft.ToString();
        }
        else if (SessionOpen)
        {
            int secondsLeft = (mapTimer + 59) / 60;
            if (secondsLeft < 0)
                secondsLeft = 0;

            text = secondsLeft.ToString();
        }

        if (text.Length == 0)
            return;

        Vector2 size = FontAssets.DeathText.Value.MeasureString(text);
        Vector2 pos = new Vector2(Main.screenWidth * 0.5f - size.X * 0.5f, -4f);
        Utils.DrawBorderStringBig(sb, text, pos, Color.White);
    }

    private void DrawOnFullscreenMap(Vector2 mapPos, float mapScale)
    {
        if (!Main.mapFullscreen || ui?.CurrentState == null)
            return;

        SpriteBatch sb = Main.spriteBatch;
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None,
            RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

        ui.Draw(sb, Main._drawInterfaceGameTime);
        DrawMapCountdown(sb);

        sb.End();
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int idx = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");

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

                ui.Draw(Main.spriteBatch, Main._drawInterfaceGameTime);
                DrawMapCountdown(Main.spriteBatch);
                return true;
            },
            InterfaceScaleType.UI));
    }

    private static bool IsAnyConfigUIOpen()
    {
        UIState s = Main.InGameUI?._currentState;
        return s is UIModConfig || s is UIModConfigList || Main.ingameOptionsWindow;
    }
}
