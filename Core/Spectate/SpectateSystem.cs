using Microsoft.Xna.Framework;
using PvPAdventure.Core.Spectate.UI;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.Spectate;

/// <summary>
/// The system responsible for managing the spectate feature, both UI and state.
/// </summary>
[Autoload(Side = ModSide.Client)]
internal class SpectateSystem : ModSystem
{
    public UserInterface ui;
    public SpectateUI spectateState;

    public bool IsActive() => ui?.CurrentState == spectateState;
    public void EnterSpectateUI()
    {
        var sp = Main.LocalPlayer.GetModPlayer<SpectatePlayer>();

        if (sp.TargetPlayerIndex is null)
        {
            var list = sp.GetValidPlayers();
            if (list.Count > 0)
                sp.TargetPlayerIndex = list[0].whoAmI; // first available player
            else
                return; // no valid players → do not open UI
        }

        ui.SetState(spectateState);
    }

    public void ExitSpectateUI()
    {
        ui.SetState(null);

        var sp = Main.LocalPlayer.GetModPlayer<SpectatePlayer>();
        sp.TargetPlayerIndex = null;
    }


    public override void OnWorldLoad()
    {
        ui = new();
        spectateState = new();
    }

    public override void UpdateUI(GameTime gameTime)
    {
        ui?.Update(gameTime);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int idx = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");

        layers.Insert(idx, new LegacyGameInterfaceLayer(
            "PvPAdventure: SpectateSystem",
            () =>
            {
                if (ui?.CurrentState != null)
                    ui.Draw(Main.spriteBatch, Main._drawInterfaceGameTime);
                return true;
            },
            InterfaceScaleType.UI));
    }
}

