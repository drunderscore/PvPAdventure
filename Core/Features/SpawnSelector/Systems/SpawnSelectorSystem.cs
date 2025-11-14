using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Features.SpawnSelector.UI;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.Features.SpawnSelector.Systems;

[Autoload(Side = ModSide.Client)]
public class SpawnSelectorSystem : ModSystem
{
    private UserInterface ui;
    public SpawnSelectorState state;
    private UISpawnSelectorPanel teleportPanel;

    // Track whether the spawn selector is enabled
    private static bool Enabled; // whether the spawn selector is currently showing in the fullscreen map
    public static void SetEnabled(bool newValue) => Enabled = newValue;
    public static bool GetEnabled() => Enabled;

    // Load and unload hooks
    public override void Load()
    {
        if (!Main.dedServ)
        {
            ui = new();
            state = new();
            state.Activate();

            Main.OnPostFullscreenMapDraw += DrawOverFullscreenMap;
        }

        On_Main.TriggerPing += OnTriggerPing;
    }

    public override void Unload()
    {
        if (!Main.dedServ)
            Main.OnPostFullscreenMapDraw -= DrawOverFullscreenMap;

        On_Main.TriggerPing -= OnTriggerPing;
    }

    private void OnTriggerPing(On_Main.orig_TriggerPing orig, Vector2 position)
    {
        // Skip ping execution if our panel is being hovered
        if (teleportPanel != null && !teleportPanel.IsMouseHovering)
        {
            orig(position);
        }
    }

    public override void UpdateUI(GameTime gameTime)
    {
        if (ui == null) return;

        bool visible = GetEnabled() && Main.mapFullscreen;

        if (visible)
        {
            if (ui.CurrentState != state)
                ui.SetState(state);

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
            sb.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,DepthStencilState.None,RasterizerState.CullCounterClockwise,null,Main.UIScaleMatrix);
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
