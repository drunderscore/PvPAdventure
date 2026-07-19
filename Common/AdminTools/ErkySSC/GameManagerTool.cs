using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.AdminTools.Tools.GameManagerTool;
using PvPAdventure.Common.Game;
using PvPAdventure.Core.Net;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Common.AdminTools.ErkySSC;

[Autoload(Side = ModSide.Client)]
internal sealed class GameManagerTool : ModSystem
{
    private const string Owner = "PvPAdventure.GameManager";

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

        Asset<Texture2D> startIcon = Ass.IconStartGame;

        erky.Call(
            "RegisterAdminQuickbarEntry",
            Owner,
            "open_game_timer",
            "PvPAdventure : Game Manager",
            "Open the game manager",
            startIcon,
            new Action(ToggleDialog),
            new Func<string>(MainActionText),
            new Func<Color>(() => Color.White),
            true,
            20,
            "Ctrl+Y"
        );
    }

    private static string MainActionText()
    {
        GameManagerUISystem ui = ModContent.GetInstance<GameManagerUISystem>();
        return ui?.IsActive() == true ? "Close" : "Open";
    }

    private static void ToggleDialog()
    {
        GameManagerUISystem ui = ModContent.GetInstance<GameManagerUISystem>();
        if (ui == null)
        {
            Main.NewText("Failed to open GameManagerUISystem.", Color.Red);
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

}
