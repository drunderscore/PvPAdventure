using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.Spectate;

[Autoload(Side = ModSide.Client)]
internal class SpectateSystem : ModSystem
{
    // Spectate UI
    public UserInterface ui;
    public UIState spectateState;
    public SpectateElement spectateElement;
    public bool IsActive() => ui?.CurrentState == spectateState;

    // Options
    public bool ShowAllPlayers; // used for admin tools

    public override void OnWorldLoad()
    {
        ui = new();
        spectateState = new();
        spectateElement = new();
        spectateState.Append(spectateElement);
    }

    /// <summary>
    /// Sets the UI to show spectateState.
    /// </summary>
    /// <param name="clearTarget">Whether to clear the target when entering the state (the player we're tracking)</param>
    public void EnterSpectateUI(bool clearTarget)
    {
        if (clearTarget)
            Main.LocalPlayer.GetModPlayer<SpectatePlayer>().SetTarget(null);

        spectateElement?.Rebuild();
        ui.SetState(spectateState);
    }

    public void ExitSpectateUI()
    {
        ui.SetState(null);
        Main.LocalPlayer.GetModPlayer<SpectatePlayer>().SetTarget(null);
    }

    public override void UpdateUI(GameTime gameTime)
    {
        ui?.Update(gameTime);

        if (Main.gameMenu)
            return;

        var sp = Main.LocalPlayer.GetModPlayer<SpectatePlayer>();

        // Force enter spectate UI when player is dead and no target is set
        if (Main.LocalPlayer.dead &&
            sp.TargetPlayerIndex is not null &&
            ui?.CurrentState != spectateState)
        {
            EnterSpectateUI(clearTarget: false);
        }

        if (!IsActive() || Main.drawingPlayerChat || Main.editSign || Main.editChest)
            return;

        if (Main.keyState.IsKeyDown(Keys.A) && Main.oldKeyState.IsKeyUp(Keys.A))
            Cycle(-1);

        if (Main.keyState.IsKeyDown(Keys.D) && Main.oldKeyState.IsKeyUp(Keys.D))
            Cycle(1);
    }

    private void Cycle(int dir)
    {
        if (!Main.LocalPlayer.dead)
            return;

        var sp = Main.LocalPlayer.GetModPlayer<SpectatePlayer>();
        sp.Cycle(dir);
        spectateElement?.Rebuild();
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int idx = layers.FindIndex(l => l.Name == "Vanilla: Interface Logic 1");

        layers.Insert(idx, new LegacyGameInterfaceLayer(
            "PvPAdventure: SpectateSystem",
            () =>
            {
                if (ui?.CurrentState != null)
                {
                    // Debug
                    //Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.Red * 0.5f);

                    ui.Draw(Main.spriteBatch, Main._drawInterfaceGameTime);
                }

                return true;
            },
            InterfaceScaleType.UI));
    }
}
