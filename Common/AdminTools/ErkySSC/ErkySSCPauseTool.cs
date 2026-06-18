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
using static PvPAdventure.Common.Game.GameTimerNetHandler;

namespace PvPAdventure.Common.AdminTools.ErkySSC;

[Autoload(Side = ModSide.Client)]
internal sealed class ErkySSCPauseTool : ModSystem
{
    private const string Owner = "PvPAdventure.Pause";

    public override void Load()
    {
        RegisterErkySSCQuickbarEntries();
    }

    public override void OnWorldLoad()
    {
        RegisterErkySSCQuickbarEntries();
    }

    public override void Unload()
    {
        if (ModLoader.TryGetMod("ErkySSC", out Mod erky))
            erky.Call("ClearAdminQuickbarEntries", Owner);
    }

    private static void RegisterErkySSCQuickbarEntries()
    {
        if (!ModLoader.TryGetMod("ErkySSC", out Mod erky))
            return;

        Asset<Texture2D> icon = Ass.IconPauseGame;

        erky.Call(
            "RegisterAdminQuickbarEntry",
            Owner,
            "pause_game",
            "PvPAdventure : Pause",
            "Pause the game",
            icon,
            new Action(PauseGame),
            new Func<string>(MainActionText),
            new Func<Color>(() => Color.White),
            true,
            20,
            "Ctrl+P"
        );
    }

    private static string MainActionText()
    {
        GameManager gm = ModContent.GetInstance<GameManager>();

        if (gm.CurrentPhase == GameManager.Phase.Playing)
            return "Pause";

        if (gm._startGameCountdown.HasValue)
            return "Countdown";

        return "Play";
    }

    private static void PauseGame()
    {
        var pm = ModContent.GetInstance<PauseManager>();

        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            pm.TogglePause();
        }
        else if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            var packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
            packet.Write((byte)AdventurePacketIdentifier.GameTimer);
            packet.Write((byte)GameTimerPacketType.PauseGame);
            packet.Send();
        }
    }
}