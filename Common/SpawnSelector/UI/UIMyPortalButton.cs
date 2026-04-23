using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.World.Outlines.ItemOutlines;
using PvPAdventure.Core.Utilities;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.SpawnSelector.UI;

public class UIMyPortalButton : UIPanel
{
    private bool hasPortal;
    public UIMyPortalButton(float size, bool hasPortal)
    {
        this.hasPortal = hasPortal;

        Width.Set(size, 0f);
        Height.Set(size, 0f);

        BackgroundColor = new Color(63, 82, 151) * 0.8f;
        BorderColor = Color.Black;
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);

        if (HasPortal() && SpawnSystem.CanUseStoredPortal(Main.LocalPlayer))
        {
            Main.LocalPlayer.GetModPlayer<SpawnPlayer>().ToggleSelection(SpawnType.MyPortal);
        }
    }

    private bool HasPortal()
    {
        return PortalSystem.HasPortal(Main.LocalPlayer);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        bool nowHasPortal = HasPortal();
        if (hasPortal != nowHasPortal)
            hasPortal = nowHasPortal;

        Player local = Main.LocalPlayer;
        var sp = local?.GetModPlayer<SpawnPlayer>();
        bool selected = sp?.SelectedType == SpawnType.MyPortal;
        bool available = SpawnSystem.CanUseStoredPortal(local);

        if (IsMouseHovering && HasPortal() && available)
        {
            SpectateSystem.TrySetHover(SpawnType.MyPortal, Main.myPlayer);
        }
        else
        {
            SpectateSystem.ClearHoverIfMatch(SpawnType.MyPortal, Main.myPlayer);
        }

        BackgroundColor =
            selected && available ? new Color(220, 220, 0) :
            !hasPortal || !available ? new Color(230, 40, 10) * 0.37f :
            IsMouseHovering ? new Color(73, 92, 161, 150) :
            new Color(63, 82, 151) * 0.8f;
    }

    public override void Draw(SpriteBatch sb)
    {
        base.Draw(sb);

        if (IsMouseHovering)
            DrawHoverText();

        var d = GetDimensions();
        Vector2 pos = new(d.X + d.Width * 0.5f, d.Y + d.Height * 0.5f);

        float iconScale = 0.8f;

        if (hasPortal && SpawnSystem.CanUseStoredPortal(Main.LocalPlayer))
            PortalDrawer.DrawPortalPreview(sb, Main.LocalPlayer, pos, iconScale);
        else
            PortalDrawer.DrawPortalPreview(sb, Main.LocalPlayer, pos, iconScale, outline: false, drawColor: Color.White * 0.65f);

        // Draw forbidden icon above if the player does not have a portal or cannot use it right now
        if (!hasPortal || !SpawnSystem.CanUseStoredPortal(Main.LocalPlayer))
        {
            Vector2 origin = Ass.Icon_Forbidden.Value.Size() * 0.5f;
            sb.Draw(Ass.Icon_Forbidden.Value, pos, null, Color.White, 0f, origin, 2f, SpriteEffects.None, 0f);
        }
    }


    private void DrawHoverText()
    {
        Player p = Main.LocalPlayer;
        if (p == null || !p.active)
            return;

        // Prevent clicks while hovering the UI element.
        p.mouseInterface = true;

        var sp = p.GetModPlayer<SpawnPlayer>();

        bool committed = sp.SelectedType == SpawnType.MyPortal;
        bool ready = !SpawnSystem.IsLocalPlayerReadyForSpawnUi;

        string text;

        if (!HasPortal())
        {
            text = "No portal set";
        }
        else if (!SpawnSystem.CanUseStoredPortal(p))
        {
            //text = "Can only teleport to your portal from a spawn region";
            text = "Your portal is being created...";
        }
        else if (ready)
        {
            text = committed
                ? Language.GetTextValue("Mods.PvPAdventure.Spawn.CancelMyPortal", Main.LocalPlayer.name)
                : Language.GetTextValue("Mods.PvPAdventure.Spawn.SelectMyPortal", Main.LocalPlayer.name);
        }
        else
        {
            text = Language.GetTextValue("Mods.PvPAdventure.Spawn.TeleportToMyPortal", Main.LocalPlayer.name);
        }

        Main.instance.MouseText(text);
    }
}
