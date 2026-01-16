using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;
using static PvPAdventure.Common.SpawnSelector.SpawnSystem;

namespace PvPAdventure.Common.SpawnSelector.UI;

public class UIWorldSpawnPanel : UIPanel
{
    public UIWorldSpawnPanel(float size)
    {
        Width.Set(size, 0f);
        Height.Set(size, 0f);

        BackgroundColor = new Color(63, 82, 151) * 0.8f;
        BorderColor = Color.Black;
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);

        Main.LocalPlayer.GetModPlayer<SpawnPlayer>().ToggleSelection(SpawnType.World);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        var sp = Main.LocalPlayer?.GetModPlayer<SpawnPlayer>();
        bool selected = sp?.SelectedType == SpawnType.World;

        BackgroundColor =
            selected ? new Color(220, 220, 0) :
            IsMouseHovering ? new Color(73, 92, 161, 150) :
            new Color(63, 82, 151) * 0.8f;
    }

    public override void Draw(SpriteBatch sb)
    {
        base.Draw(sb);

        if (IsMouseHovering)
        {
            SpectateSystem.HoveringType = SpawnType.World;
            SpectateSystem.HoveredPlayerIndex = null;

            DrawHoverText();
        }
        else if (SpectateSystem.HoveringType == SpawnType.World)
        {
            SpectateSystem.ClearHover();
        }

        // Draw spawn point
        var d = GetDimensions();
        var tex = TextureAssets.SpawnPoint.Value;

        Vector2 pos = new(
            d.X + d.Width * 0.5f,
            d.Y + d.Height * 0.5f
        );

        float scale = 1.6f;
        sb.Draw(tex, pos, null, Color.White, 0f, tex.Size() * 0.5f, scale, SpriteEffects.None, 0f);
    }

    private void DrawHoverText()
    {
        Player p = Main.LocalPlayer;
        if (p == null || !p.active)
            return;

        var sp = p.GetModPlayer<SpawnPlayer>();

        bool committed = sp.SelectedType == SpawnType.World;
        bool ready = !SpawnSystem.CanTeleport;

        string text;

        if (ready)
        {
            text = committed
                ? Language.GetTextValue("Mods.PvPAdventure.Spawn.CancelWorldSpawn")
                : Language.GetTextValue("Mods.PvPAdventure.Spawn.SelectWorldSpawn");
        }
        else
        {
            text = Language.GetTextValue("Mods.PvPAdventure.Spawn.TeleportToWorldSpawn");
        }

        Main.instance.MouseText(text);
    }
}
