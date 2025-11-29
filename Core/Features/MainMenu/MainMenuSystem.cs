using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

[Autoload(Side = ModSide.Client)]
internal sealed class MainMenuSystem : ModSystem
{
    // States
    public UserInterface ui;
    public PVPScreenState pvpScreenState;

    // Button
    private Rectangle pvpTextButtonHitbox;
    private bool wasHovered;
    private float pvpTextScale = 1.0f;              

    public override void PostSetupContent()
    {
        ui = new UserInterface();

        pvpScreenState = new PVPScreenState(
            onBack: () => {
                Main.menuMode = 0;
                ui.SetState(null);
                Main.blockMouse = false;
            },
            onCloseUi: () => {
                ui.SetState(null);        
                Main.blockMouse = false;
            }
        );

        pvpScreenState.Activate(); // ensures OnInitialize runs

        On_Main.DrawVersionNumber += DrawMenuUI;
        On_Main.UpdateUIStates += PostUpdateUIStates;
    }

    private void DrawMenuUI(On_Main.orig_DrawVersionNumber orig, Color menuColor, float upBump)
    {
        orig(menuColor, upBump);

        // State handling
        if (!Main.gameMenu) return;

        if (ui?.CurrentState != null)
        {
            ui.Draw(Main.spriteBatch, new GameTime());
            return;
        }

        if (Main.menuMode != 0) return;

        const string label = "Play PvP";

        // Positioning
        var font = FontAssets.DeathText.Value;
        Vector2 baseSize = font.MeasureString(label);
        Vector2 center = new(Main.screenWidth * 0.5f, 200);
        Vector2 topLeft = center - baseSize * (pvpTextScale * 0.5f);
        pvpTextButtonHitbox = new Rectangle(
            (int)(topLeft.X - 6),
            (int)(topLeft.Y - 6),
            (int)(baseSize.X * pvpTextScale + 12),
            (int)(baseSize.Y * pvpTextScale - 12)
        );
        bool hovered = pvpTextButtonHitbox.Contains(Main.MouseScreen.ToPoint());

        // Debug
        //Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, pvpTextButtonHitbox, Color.Red * 0.5f);

        // Play sound
        if (hovered && !wasHovered)
            SoundEngine.PlaySound(SoundID.MenuTick);

        // Scaling
        float target = hovered ? 1.1f : 0.9f;
        pvpTextScale = MathHelper.Lerp(pvpTextScale, target, 0.2f);
        topLeft = center - baseSize * (pvpTextScale * 0.5f);

        // Color
        Color color = hovered ? new Color(255, 240, 20) : Color.Gray;

        // Draw text
        Utils.DrawBorderStringBig(Main.spriteBatch, label, topLeft, color, pvpTextScale);

        // Handle click
        if (hovered)
        {
            Main.blockMouse = true; // avoid click-through
            if (Main.mouseLeft && Main.mouseLeftRelease)
            {
                Main.mouseLeftRelease = false;
                SoundEngine.PlaySound(SoundID.MenuOpen);
                Main.menuMode = 888;      // empty background mode
                ui?.SetState(pvpScreenState);
            }
        }
        wasHovered = hovered; // Reset hover state
    }

    private void PostUpdateUIStates(On_Main.orig_UpdateUIStates orig, GameTime gameTime)
    {
        orig(gameTime);

        if (!Main.gameMenu)
        {
            if (ui?.CurrentState != null) ui.SetState(null);
            return;
        }

        if (ui?.CurrentState != null)
            ui.Update(gameTime);
    }

    public override void Unload()
    {
        On_Main.DrawVersionNumber -= DrawMenuUI;
        On_Main.UpdateUIStates -= PostUpdateUIStates;
        ui = null;
        pvpScreenState = null;
    }
}
