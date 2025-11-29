using Microsoft.Xna.Framework;
using MonoMod.Cil;
using PvPAdventure.Core.Helpers;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.Matchmaking;

/// <summary>
/// Adds a Matchmaking button to the Main Menu.
/// </summary>
[Autoload(Side = ModSide.Client)]
public class MainMenuSystem : ModSystem
{
    public UserInterface ui;
    private bool wasHovered;

    public override void Load()
    {
        Main.QueueMainThreadAction(() => IL_Main.DrawMenu += InjectMatchmakingButton);

        ui = new UserInterface();

        On_Main.DrawVersionNumber += DrawMenuUI;
        On_Main.UpdateUIStates += PostUpdateUIStates;
    }

    public override void Unload()
    {
        Main.QueueMainThreadAction(() => IL_Main.DrawMenu -= InjectMatchmakingButton);
        On_Main.DrawVersionNumber -= DrawMenuUI;
        On_Main.UpdateUIStates -= PostUpdateUIStates;
        ui = null;
    }

    private void InjectMatchmakingButton(ILContext il)
    {
        IL.Edit(il, c =>
        {
            int array9Index = -1;
            int array7Index = -1;
            int offYIndex = -1;
            int spacingIndex = -1;
            int num9Index = -1;
            int num11Index = -1;

            if (!c.TryGotoNext(MoveType.After,
                    i => i.MatchLdarg(0),
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<Main>("selectedMenu"),
                    i => i.MatchLdloc(out array9Index),
                    i => i.MatchLdloc(out array7Index),
                    i => i.MatchLdloca(out offYIndex),
                    i => i.MatchLdloca(out spacingIndex),
                    i => i.MatchLdloca(out num9Index),
                    i => i.MatchLdloca(out num11Index),
                    i => i.MatchCall(out var m)
                         && m.DeclaringType.FullName == "Terraria.ModLoader.UI.Interface"
                         && m.Name == "AddMenuButtons"))
            {
                return;
            }

            c.Index = 0;

            int num11InitIndex = -1;
            int num2Index = -1;
            int num5Index = -1;
            int num4Index = -1;

            if (!c.TryGotoNext(MoveType.After,
                    i => i.MatchLdcI4(0),
                    i => i.MatchStloc(out num11InitIndex),
                    i => i.MatchLdcI4(220),
                    i => i.MatchStloc(out num2Index),
                    i => i.MatchLdcI4(7),
                    i => i.MatchStloc(out num5Index),
                    i => i.MatchLdcI4(52),
                    i => i.MatchStloc(out num4Index)))
            {
                return;
            }

            c.EmitLdloc(array9Index);
            c.EmitLdloca(num11InitIndex);
            c.EmitLdloca(num5Index);

            c.EmitDelegate((string[] labels, ref int num11, ref int num5) =>
            {
                if (labels == null)
                    return;

                labels[num11] = "Matchmaking";
                num11++;
                num5++;
            });
        });
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

        DrawPvPButton();
    }

    private void DrawPvPButton()
    {
        const string label = "Matchmaking";

        var font = FontAssets.DeathText.Value;
        Vector2 baseSize = font.MeasureString(label);
        Vector2 center = new(Main.screenWidth * 0.5f, 200);
        Vector2 topLeft = center - baseSize * 0.5f;

        var hitbox = new Rectangle(
            (int)(topLeft.X - 30),
            (int)(topLeft.Y + 54),
            (int)(baseSize.X + 60),
            (int)(baseSize.Y - 12)
        );

        bool hovered = hitbox.Contains(Main.MouseScreen.ToPoint());

        if (hovered && !wasHovered)
            SoundEngine.PlaySound(SoundID.MenuTick);

        Color color = hovered ? new Color(255, 240, 20) : Color.Gray;

         //Utils.DrawBorderStringBig(Main.spriteBatch, label, topLeft, color, 1f);

        if (hovered)
        {
            Main.blockMouse = true;

            if (Main.mouseLeft && Main.mouseLeftRelease)
            {
                Main.mouseLeftRelease = false;
                SoundEngine.PlaySound(SoundID.MenuOpen);
                Main.menuMode = 888;

                var state = new MatchmakingState();
                state.Activate();
                ui?.SetState(state);
            }
        }

        wasHovered = hovered;
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

        // If we're no longer in our custom empty-background menu, kill the matchmaking UI.
        if (ui?.CurrentState != null && Main.menuMode != 888)
        {
            ui.SetState(null);
            Main.blockMouse = false;
        }

        if (ui?.CurrentState != null)
            ui.Update(gameTime);
    }
}
