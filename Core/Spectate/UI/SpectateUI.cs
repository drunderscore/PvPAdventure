using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace PvPAdventure.Core.Spectate.UI;

internal class SpectateUI : UIState
{
    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);

        var sp = Main.LocalPlayer.GetModPlayer<SpectatePlayer>();
        int? index = sp.TargetPlayerIndex;

        if (index is null || index == -1)
            return;

        Player target = Main.player[index.Value];
        if (!target.active)
            return;

        string mainText = $"Spectating: {target.name}";

        // Get next/prev strings
        sp.GetNextPrev(out Player prev, out Player next);

        string nextText = next != null ? $"Next: {next.name}" : "";
        string prevText = prev != null ? $"Prev: {prev.name}" : "";

        var font = FontAssets.MouseText.Value;

        // Measure
        Vector2 size = font.MeasureString(mainText);
        Vector2 pos = new(Main.screenWidth / 2f-100, Main.screenHeight / 2f+200);

        // Draw main centered
        Utils.DrawBorderStringBig(spriteBatch, mainText,
            pos - size / 2, Color.White, 1f);

        // Draw prev below-left
        if (prev != null)
        {
            var pSize = font.MeasureString(prevText);
            Utils.DrawBorderString(spriteBatch, prevText,
                new Vector2(pos.X - pSize.X / 2, pos.Y + size.Y), Color.Gray);
        }

        // Draw next below-right
        if (next != null)
        {
            var nSize = font.MeasureString(nextText);
            Utils.DrawBorderString(spriteBatch, nextText,
                new Vector2(pos.X - nSize.X / 2, pos.Y + size.Y + 24), Color.Gray);
        }
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        var sp = Main.LocalPlayer.GetModPlayer<SpectatePlayer>();

        if (Main.mouseLeft && Main.mouseLeftRelease)
            sp.SelectNext();

        if (Main.mouseRight && Main.mouseRightRelease)
            sp.SelectPrev();
    }
}
