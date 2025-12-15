using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.SpawnSelector.UI;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.SpawnSelector;

/// <summary>
/// A system to manage the spawn selector UI.
/// Is active when player is within the spawn region or a bed region
/// Allows the player to teleport to teammates and teammates beds on the fullscreen map.
/// </summary>
[Autoload(Side = ModSide.Client)]
public class SpawnSelectorSystem : ModSystem
{
    public UserInterface ui;
    public UIState spawnSelectorState;
    public SpawnSelectorBasePanel spawnSelectorPanel;

    // Track whether the spawn selector is enabled
    private static bool Enabled;
    public static void SetEnabled(bool newValue) => Enabled = newValue;
    public static bool GetEnabled() => Enabled;

    // Hovering players
    internal static int HoveredPlayerIndex = -1;

    // Track visibility state
    private bool _wasVisible = false;

    public override void OnWorldLoad()
    {
        if (!Main.dedServ)
        {
            ui = new();
            RebuildState();

            Main.OnPostFullscreenMapDraw += DrawOverFullscreenMap;
        }

    }

    public override void Unload()
    {
        Main.OnPostFullscreenMapDraw -= DrawOverFullscreenMap;
    }

    private void RebuildState()
    {
        HoveredPlayerIndex = -1;

        spawnSelectorState = new UIState();
        spawnSelectorPanel = new SpawnSelectorBasePanel();

        UITextPanel<string> chooseYourSpawnPanel =
            new(Language.GetTextValue("Mods.PvPAdventure.SpawnSelector.ChooseYourSpawn"), 0.8f, true)
            {
                HAlign = 0.5f,
                BackgroundColor = new Color(73, 94, 171),
                Top = new StyleDimension(10,0)
            };

        spawnSelectorState.Append(spawnSelectorPanel);
        spawnSelectorState.Append(chooseYourSpawnPanel);

        spawnSelectorState.Activate();
    }

    public override void UpdateUI(GameTime gameTime)
    {
        if (ui == null)
            return;

        bool visible = GetEnabled() && Main.mapFullscreen;

        if (visible && !_wasVisible)
        {
            RebuildState();
            ui.SetState(spawnSelectorState);
        }

        _wasVisible = visible;

        if (visible)
        {
            if (ui.CurrentState != spawnSelectorState)
                ui.SetState(spawnSelectorState);

            ui.Update(gameTime);
        }
        else if (ui.CurrentState != null)
        {
            ui.SetState(null);
        }
    }
    // Draw AFTER the map, with our own Begin/End for UI
    private void DrawOverFullscreenMap(Vector2 _mapPos, float _mapScale)
    {
        if (ui?.CurrentState == null) return;

        var sb = Main.spriteBatch;
        bool began = false;

        try
        {
            sb.Begin(SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullCounterClockwise
                ,null,
                Main.UIScaleMatrix);
            began = true;
            ui.Draw(sb, Main._drawInterfaceGameTime);
        }
        finally
        {
            if (began)
                sb.End();
        }
    }
}
