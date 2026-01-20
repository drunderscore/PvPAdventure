using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.GameTimer;
using PvPAdventure.Common.SpawnSelector.UI;
using PvPAdventure.Content.Items;
using PvPAdventure.Core.Config;
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

    // The visibility of the state
    public static bool Enabled { get; private set; }
    // Whether or not we can instantly execute teleports
    public static bool CanTeleport { get; private set; }
    public static void SetCanTeleport(bool value) => CanTeleport = value;

    public enum SpawnType : byte
    {
        None,
        World,
        MyBed,
        Random,
        Teammate,
        TeammateBed
    }

    // Map timer
    private bool wasShowingUI;
    private bool wasInSpawnRegion;
    private bool sessionWasOpen;
    private static bool SessionOpen => Main.mapFullscreen || SpectateSystem.MapRestore;

    public override void UpdateUI(GameTime gameTime)
    {
        Player local = Main.LocalPlayer;
        if (local == null || local.ghost)
        {
            ui.SetState(null);
            return;
        }

        var sp = local.GetModPlayer<SpawnPlayer>();

        // Conditions whether we should open the spawn UI or not
        bool playing = ModContent.GetInstance<GameManager>().CurrentPhase == GameManager.Phase.Playing;
        bool inSubworld = SubworldSystem.AnyActive();
        bool inSpawnRegion = sp.IsPlayerInSpawnRegion();
        bool usingMirror = IsUsingAdventureMirror(local, out bool mirrorReady, out _);

        Enabled = (playing || inSubworld) && (inSpawnRegion || usingMirror);

        bool enteredSpawnRegion = inSpawnRegion && !wasInSpawnRegion;
        wasInSpawnRegion = inSpawnRegion;

        bool blockExecuteThisTick = enteredSpawnRegion && !usingMirror && !local.dead;

        if (blockExecuteThisTick)
        {
            sp.ClearSelection();
            SetCanTeleport(false);
        }

        if (local.dead)
        {
            SetCanTeleport(local.respawnTimer <= 2);
        }
        else
        {
            if (InstantTeleport(local))
            {
                SetCanTeleport(true);
            }
            else if (usingMirror)
            {
                SetCanTeleport(mirrorReady);
            }
            else
            {
                SetCanTeleport(false);
            }
        }

        if (CanTeleport && !blockExecuteThisTick)
            TryExecuteSelection(local);

        bool show = (playing || inSubworld) && (local.dead || Enabled);
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

            var config = ModContent.GetInstance<ClientConfig>();
            if (config.AutoSelectLatestSpawnOption && usingMirror)
                Main.LocalPlayer?.GetModPlayer<SpawnPlayer>().RestoreLastSelection();

            wasShowingUI = true;
        }

        ui.Update(gameTime);
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

    #region Teleport methods
    private static bool InstantTeleport(Player p)
    {
        if (p == null || !p.active || p.dead || !Enabled)
            return false;

        return p.GetModPlayer<SpawnPlayer>().IsPlayerInSpawnRegion();
    }

    private void OnTeleportExecuted()
    {
        var p = Main.LocalPlayer;
        if (p != null && p.active)
        {
            // HARD reset mirror use state
            p.itemTime = 0;
            p.itemAnimation = 0;
            p.reuseDelay = 0;
        }

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
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            var pkt = ModContent.GetInstance<PvPAdventure>().GetPacket();
            pkt.Write((byte)AdventurePacketIdentifier.TeleportRequest);
            pkt.Write((byte)Main.myPlayer);
            pkt.Write((byte)SpawnType.Random);
            pkt.Send();
            return;
        }

        p.TeleportationPotion();
        SoundEngine.PlaySound(SoundID.Item6, p.Center);
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
            pkt.Write((byte)SpawnType.Teammate);
            pkt.Write((short)idx);
            pkt.Send();
            return;
        }

        p.UnityTeleport(t.position);
    }

    private static void TeleportToTeammatesBed(Player p, int idx)
    {
        //if (!IsValidTeammateIndex(idx))
            //return;

        Player t = Main.player[idx];
        if (t == null || !t.active)
            return;

        // Server decides final position in MP
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            var pkt = ModContent.GetInstance<PvPAdventure>().GetPacket();
            pkt.Write((byte)AdventurePacketIdentifier.TeleportRequest);
            pkt.Write((byte)Main.myPlayer);
            pkt.Write((byte)SpawnType.TeammateBed);
            pkt.Write((short)idx);
            pkt.Send();
            return;
        }

        if (t.SpawnX < 0 || t.SpawnY < 0 || !Player.CheckSpawn(t.SpawnX, t.SpawnY))
            return;

        Vector2 pos = new Vector2(t.SpawnX, t.SpawnY - 6).ToWorldCoordinates();
        p.Teleport(pos, TeleportationStyleID.RecallPotion);
    }

    private static void TeleportMyBed(Player p)
    {
        if (p.SpawnX < 0 || p.SpawnY < 0 || !Player.CheckSpawn(p.SpawnX, p.SpawnY))
            return;

        Vector2 pos = new Vector2(p.SpawnX, p.SpawnY - 6).ToWorldCoordinates();

        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            var pkt = ModContent.GetInstance<PvPAdventure>().GetPacket();
            pkt.Write((byte)AdventurePacketIdentifier.TeleportRequest);
            pkt.Write((byte)Main.myPlayer);
            pkt.Write((byte)SpawnType.MyBed);
            pkt.Send();
            return;
        }

        p.Teleport(pos, TeleportationStyleID.RecallPotion);
    }
    #endregion

    private void ClearSelection()
    {
        Player p = Main.LocalPlayer;
        if (p == null)
            return;

        p.GetModPlayer<SpawnPlayer>().ClearSelection();
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

        if (p.whoAmI == Main.myPlayer)
        {
            string extra =
                (sp.SelectedType == SpawnType.Teammate || sp.SelectedType == SpawnType.TeammateBed)
                    ? " (" + Main.player[sp.SelectedPlayerIndex].name + ")"
                    : "";

            Log.Chat("Executing spawn: " + sp.SelectedType + extra);
        }

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

        if (sp.SelectedType == SpawnType.MyBed)
        {
            TeleportMyBed(p);
            OnTeleportExecuted();
            return;
        }

        if (sp.SelectedType == SpawnType.Teammate)
        {
            TeleportToPlayer(p, sp.SelectedPlayerIndex);
            OnTeleportExecuted();
            return;
        }

        if (sp.SelectedType == SpawnType.TeammateBed)
        {
            TeleportToTeammatesBed(p, sp.SelectedPlayerIndex);
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

    //private void DrawMapCountdown(SpriteBatch sb)
    //{
    //    if (Main.LocalPlayer?.GetModPlayer<SpawnPlayer>()?.IsPlayerInSpawnRegion() == true)
    //        return;

    //    string text = "";

    //    if (Main.LocalPlayer != null && Main.LocalPlayer.dead && Main.mapFullscreen)
    //    {
    //        int secondsLeft = (Main.LocalPlayer.respawnTimer + 59) / 60;
    //        if (Main.LocalPlayer.respawnTimer <= 2)
    //            secondsLeft = 0;

    //        text = "Dead: " + secondsLeft.ToString();
    //    }
    //    else if (SessionOpen)
    //    {
    //        int secondsLeft = (mapTimer + 59) / 60;
    //        if (secondsLeft < 0)
    //            secondsLeft = 0;

    //        text = secondsLeft.ToString();
    //    }

    //    if (text.Length == 0)
    //        return;

    //    Vector2 size = FontAssets.DeathText.Value.MeasureString(text);
    //    Vector2 pos = new Vector2(Main.screenWidth * 0.5f - size.X * 0.5f, -4f);
    //    Utils.DrawBorderStringBig(sb, text, pos, Color.White);
    //}

    private void DrawAdventureMirrorTimer(SpriteBatch sb)
    {
        var local = Main.LocalPlayer;
        if (local == null)
            return;

        if (!IsUsingAdventureMirror(local, out _, out int secondsLeft))
            return;

        if (spawnState?.TitlePanel == null)
            return;

        var dims = spawnState.TitlePanel.GetDimensions();

        string text = secondsLeft.ToString();

        Vector2 size = FontAssets.DeathText.Value.MeasureString(text);

        // Right of title panel, vertically centered
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
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None,
            RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

        ui.Draw(sb, Main._drawInterfaceGameTime);
        DrawAdventureMirrorTimer(sb);
        //DrawMapCountdown(sb);

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

                var sb = Main.spriteBatch;

                ui.Draw(sb, Main._drawInterfaceGameTime);
                //DrawMapCountdown(sb);
                DrawAdventureMirrorTimer(sb);
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
