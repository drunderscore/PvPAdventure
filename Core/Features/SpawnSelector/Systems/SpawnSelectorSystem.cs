using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.Features.SpawnSelector.Systems;

[Autoload(Side = ModSide.Client)]
public class SpawnSelectorSystem : ModSystem
{
    public UserInterface ui;
    public SpawnSelectorState state;

    // Track whether the spawn selector is enabled
    private static bool Enabled;
    public static void SetEnabled(bool newValue) => Enabled = newValue;
    public static bool GetEnabled() => Enabled;

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
        if (state.spawnSelectorPanel != null && state.spawnSelectorPanel.IsMouseHovering)
        {
            return;
        }
        orig(position);
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
