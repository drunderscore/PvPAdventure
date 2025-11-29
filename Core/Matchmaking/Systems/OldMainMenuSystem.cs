using Microsoft.Xna.Framework;
using PvPAdventure.Core.Matchmaking;
using PvPAdventure.Core.Matchmaking.Systems;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

[Autoload(Side = ModSide.Client)]
internal sealed class OldMainMenuSystem : ModSystem
{
    public UserInterface ui;
    public MatchmakingState matchmakingState;
    private bool wasHovered;

    public override void PostSetupContent()
    {
        ui = new UserInterface();

        // previous state is null here since we're opening from the main menu
        matchmakingState = new MatchmakingState();
        matchmakingState.Activate();

        On_Main.DrawVersionNumber += DrawMenuUI;
        On_Main.UpdateUIStates += PostUpdateUIStates;
    }

    private void DrawMenuUI(On_Main.orig_DrawVersionNumber orig, Color menuColor, float upBump)
    {
        orig(menuColor, upBump);

        if (!Main.gameMenu)
            return;

        if (ui?.CurrentState != null)
        {
            ui.Draw(Main.spriteBatch, new GameTime());
            return;
        }

        if (Main.menuMode != 0)
            return;

        //DrawAddRemoveButtons();
        DrawPvPButton();
    }

    private void DrawPvPButton()
    {
        const string label = "Matchmaking";

        // Positioning
        var font = FontAssets.DeathText.Value;
        Vector2 baseSize = font.MeasureString(label);
        Vector2 center = new(Main.screenWidth * 0.5f, 200);
        Vector2 topLeft = center - baseSize * (1 * 0.5f);
        Rectangle pvpTextButtonHitbox = new Rectangle(
            (int)(topLeft.X - 30),
            (int)(topLeft.Y + 54),
            (int)(baseSize.X * 1 + 60),
            (int)(baseSize.Y * 1 - 12)
        );
        bool hovered = pvpTextButtonHitbox.Contains(Main.MouseScreen.ToPoint());

        // Debug
        //Color dc = hovered ? Color.Red : Color.Blue;
        //Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, pvpTextButtonHitbox, dc * 0.5f);

        // Play sound
        if (hovered && !wasHovered)
            SoundEngine.PlaySound(SoundID.MenuTick);

        // Scaling
        //float target = hovered ? 1.1f : 0.9f;
        //pvpTextScale = MathHelper.Lerp(pvpTextScale, target, 0.2f);
        //topLeft = center - baseSize * (pvpTextScale * 0.5f);

        // Color
        Color color = hovered ? new Color(255, 240, 20) : Color.Gray;

        // Draw text
        //Utils.DrawBorderStringBig(Main.spriteBatch, label, topLeft, color, 1f);

        // Handle click
        if (hovered)
        {
            Main.blockMouse = true; // avoid click-through
            if (Main.mouseLeft && Main.mouseLeftRelease)
            {
                Main.mouseLeftRelease = false;
                SoundEngine.PlaySound(SoundID.MenuOpen);
                Main.menuMode = 888;      
                ui?.SetState(matchmakingState);
            }
        }
        wasHovered = hovered; // Reset hover state
    }

    private void DrawAddRemoveButtons()
    {
        var font = FontAssets.MouseText.Value;

        string addText = "Add button";
        string removeText = "Remove button";

        Vector2 addPos = new Vector2(200, 20);
        Vector2 removePos = new Vector2(200, 40);

        Vector2 addSize = font.MeasureString(addText);
        Vector2 removeSize = font.MeasureString(removeText);

        var addRect = new Rectangle((int)addPos.X, (int)addPos.Y, (int)addSize.X, (int)addSize.Y);
        var removeRect = new Rectangle((int)removePos.X, (int)removePos.Y, (int)removeSize.X, (int)removeSize.Y);

        bool addHover = addRect.Contains(Main.MouseScreen.ToPoint());
        bool removeHover = removeRect.Contains(Main.MouseScreen.ToPoint());

        Color addColor = addHover ? Color.Yellow : Color.White;
        Color removeColor = removeHover ? Color.Yellow : Color.White;

        Utils.DrawBorderString(Main.spriteBatch, addText, addPos, addColor);
        Utils.DrawBorderString(Main.spriteBatch, removeText, removePos, removeColor);

        if (addHover && Main.mouseLeft && Main.mouseLeftRelease)
        {
            Main.mouseLeftRelease = false;
            SoundEngine.PlaySound(SoundID.MenuTick);
            MainMenuSystem.ExtraPvpButtons++;
        }

        if (removeHover && Main.mouseLeft && Main.mouseLeftRelease)
        {
            Main.mouseLeftRelease = false;
            SoundEngine.PlaySound(SoundID.MenuTick);
            if (MainMenuSystem.ExtraPvpButtons > 0)
                MainMenuSystem.ExtraPvpButtons--;
        }
    }
    private void PostUpdateUIStates(On_Main.orig_UpdateUIStates orig, GameTime gameTime)
    {
        orig(gameTime);

        if (!Main.gameMenu)
        {
            if (ui?.CurrentState != null)
                ui.SetState(null);
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
        matchmakingState = null;
    }
}
