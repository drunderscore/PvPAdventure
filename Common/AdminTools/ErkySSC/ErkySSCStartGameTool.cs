using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.AdminTools.Tools.StartGameTool;
using PvPAdventure.Common.Game;
using PvPAdventure.Core.Net;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.AdminTools.ErkySSC;

[Autoload(Side = ModSide.Client)]
internal sealed class ErkySSCStartGameTool : ModSystem
{
    private const string Owner = "PvPAdventure.StartGame";

    public override void PostSetupContent()
    {
        // Don't register here — Ass.IconStartGame may be re-assigned by AssetLoader later
        RegisterErkySSCQuickbarEntries();
    }

    public override void OnWorldLoad()
    {
        // Yep, this works!
        RegisterErkySSCQuickbarEntries();
    }

    public override void Unload()
    {
        if (!ModLoader.TryGetMod("ErkySSC", out Mod erky))
            return;
    }

    private static void RegisterErkySSCQuickbarEntries()
    {
        if (!ModLoader.TryGetMod("ErkySSC", out Mod erky))
            return;

        Asset<Texture2D> icon = Ass.IconStartGame;

        erky.Call(
            "RegisterAdminQuickbarEntry",
            Owner,
            "open_game_timer",
            "PvPAdventure : Game Timer",
            "Open start game / adjust game time",
            icon,
            new Action(ToggleDialog),
            new Func<string>(MainActionText),
            new Func<Color>(() => Color.White),
            true,
            20
        );

        erky.Call(
            "RegisterAdminQuickbarEntry",
            Owner,
            "quick_start_60",
            "PvPAdventure : Quick Start 60m",
            "Start the game instantly for 60 minutes",
            icon,
            new Action(QuickStart60),
            new Func<string>(() => "Start"),
            new Func<Color>(() => Color.LightGreen),
            false,
            21
        );

        erky.Call(
            "RegisterAdminQuickbarEntry",
            Owner,
            "adjust_game_time",
            "PvPAdventure : Adjust Time",
            "Open the adjust game time dialog",
            icon,
            new Action(ShowAdjustTime),
            new Func<string>(() => "Open"),
            new Func<Color>(() => Color.LightCyan),
            false,
            22
        );
    }

    private static string MainActionText()
    {
        GameManager gm = ModContent.GetInstance<GameManager>();

        if (gm.CurrentPhase == GameManager.Phase.Playing)
            return "Adjust";

        if (gm._startGameCountdown.HasValue)
            return "Countdown";

        return "Start";
    }

    private static void ToggleDialog()
    {
        StartGameSystem ui = ModContent.GetInstance<StartGameSystem>();
        if (ui == null)
        {
            Main.NewText("Failed to open StartGameSystem.", Color.Red);
            return;
        }

        if (ui.IsActive())
        {
            ui.Hide();
            return;
        }

        GameManager gm = ModContent.GetInstance<GameManager>();

        if (gm.CurrentPhase == GameManager.Phase.Playing)
        {
            ui.ShowExtendGameDialog();
            return;
        }

        ui.ShowStartDialog();
    }

    private static void ShowAdjustTime()
    {
        StartGameSystem ui = ModContent.GetInstance<StartGameSystem>();
        if (ui == null)
            return;

        if (ui.IsActive())
            ui.Hide();
        else
            ui.ShowExtendGameDialog();
    }

    private static void QuickStart60()
    {
        GameManager gm = ModContent.GetInstance<GameManager>();

        if (gm.CurrentPhase == GameManager.Phase.Playing)
        {
            ShowAdjustTime();
            return;
        }

        StartGame(timeInFrames: 60 * 60 * 60, countdownSeconds: 0);
    }

    private static void StartGame(int timeInFrames, int countdownSeconds)
    {
        GameManager gm = ModContent.GetInstance<GameManager>();

        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            gm.StartGame(time: timeInFrames, countdownTimeInSeconds: countdownSeconds);
            return;
        }

        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.GameTimer);
        packet.Write((byte)GameTimerNetHandler.GameTimerPacketType.StartGame);
        packet.Write(timeInFrames);
        packet.Write(countdownSeconds);
        packet.Send();
    }
}