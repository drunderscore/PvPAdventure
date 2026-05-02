using Microsoft.Xna.Framework;
using PvPAdventure.Core.Config;
using PvPAdventure.Common.Spectator.SpectatorMode;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.ModLoader.Config.UI;
using Terraria.UI;

namespace PvPAdventure.Common.TeammateSpectator;

[Autoload(Side = ModSide.Client)]
public sealed class TeammateSpectatorUISystem : ModSystem
{
    private UserInterface teamSpecUI;
    private TeammateSpectatorUIState teamSpecUIState;

    public static bool IsEnabled => ModContent.GetInstance<ServerConfig>()?.IsTeammateSpectatingEnabled ?? false;
    public static bool IsHudPreviewAvailable => ShouldDraw();

    public override void OnWorldLoad()
    {
        EnsureUI();
    }

    public override void OnWorldUnload()
    {
        teamSpecUI?.SetState(null);
        teamSpecUI = null;
        teamSpecUIState = null;
    }

    public override void UpdateUI(GameTime gameTime)
    {
        if (!ShouldDraw())
            return;

        EnsureUI();
        teamSpecUI?.Update(gameTime);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(l => l.Name == "Vanilla: Inventory");

        // Draw the UI below the config if the config is open, otherwise draw it above the death text
        if (IsAnyConfigUIOpen())
            index = layers.FindIndex(l => l.Name == "Vanilla: Interface Logic 1");

        if (index == -1)
            return;

        layers.Insert(index + 1, new LegacyGameInterfaceLayer(
            "PvPAdventure: Teammate Spectator UI",
            delegate
            {
                if (!ShouldDraw() || teamSpecUI?.CurrentState is null)
                    return true;

                teamSpecUI.Draw(Main.spriteBatch, Main._drawInterfaceGameTime);
                DrawSpectatingLabel();
                return true;
            },
            InterfaceScaleType.UI
        ));
    }

    private static void DrawSpectatingLabel()
    {
        if (!TeammateSpectateSystem.TryGetActivePlayerHudTarget(out Player player))
            return;

        string text = $"Spectating {player.name}";
        float scale = 1.1f;
        Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * scale;
        Vector2 position = new((Main.screenWidth - size.X) * 0.5f, Main.screenHeight - 86f);

        Utils.DrawBorderString(Main.spriteBatch, text, position, Color.White, scale);
    }

    private void EnsureUI()
    {
        if (!IsEnabled || teamSpecUI is not null)
            return;

        teamSpecUI = new UserInterface();
        teamSpecUIState = new TeammateSpectatorUIState();
        teamSpecUI.SetState(teamSpecUIState);
    }

    private static bool ShouldDraw()
    {
        if (!IsEnabled)
            return false;

        if (ModContent.GetInstance<ClientConfig>()?.ShowTeammatesToSpectate != true)
            return false;

        Player local = Main.LocalPlayer;

        if (local?.active != true || SpectatorModeSystem.IsInSpectateMode(local))
            return false;

        if (!Main.playerInventory)
            return false;

        if (Main.mapFullscreen)
            return false;

        return true;
    }

    private static bool IsAnyConfigUIOpen()
    {
        UIState state = Main.InGameUI?._currentState;
        return Main.ingameOptionsWindow || state is UIModConfig or UIModConfigList;
    }
}
