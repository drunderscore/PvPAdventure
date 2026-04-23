using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Utilities;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.UI;

namespace PvPAdventure.Common.SpawnSelector.UI;

public sealed class UITeammatePortalButton : UIElement
{
    private readonly int playerIndex;
    private bool hasPortal;

    public UITeammatePortalButton(int playerIndex, bool hasPortal)
    {
        this.playerIndex = playerIndex;
        this.hasPortal = hasPortal;
        Width.Set(40, 0f);
        Height.Set(26, 0f);
        SetPadding(0f);
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);

        Player local = Main.LocalPlayer;
        if (local == null || !local.active)
            return;

        if (playerIndex < 0 || playerIndex >= Main.maxPlayers)
            return;

        Player owner = Main.player[playerIndex];
        if (owner == null || !owner.active)
            return;

        if (local.team == 0 || owner.team != local.team)
            return;

        var sp = local.GetModPlayer<SpawnPlayer>();
        bool selected = sp.SelectedType == SpawnType.TeammatePortal && sp.SelectedPlayerIndex == playerIndex;

        if (selected)
        {
            sp.ToggleSelection(SpawnType.TeammatePortal, playerIndex);
            return;
        }

        if (!PortalSystem.HasPortal(owner))
            return;

        sp.ToggleSelection(SpawnType.TeammatePortal, playerIndex);
    }

    protected override void DrawSelf(SpriteBatch sb)
    {
        Player local = Main.LocalPlayer;
        if (local == null || !local.active)
            return;

        Player owner = playerIndex >= 0 && playerIndex < Main.maxPlayers ? Main.player[playerIndex] : null;
        hasPortal = owner != null && owner.active && PortalSystem.HasPortal(owner);

        if (IsMouseHovering && hasPortal)
        {
            SpectateSystem.TrySetHover(SpawnType.TeammatePortal, playerIndex);
            local.mouseInterface = true;
        }
        else
        {
            SpectateSystem.ClearHoverIfMatch(SpawnType.TeammatePortal, playerIndex);
        }

        Rectangle rect = GetDimensions().ToRectangle();
        var sp = local.GetModPlayer<SpawnPlayer>();
        bool selected = sp.SelectedType == SpawnType.TeammatePortal && sp.SelectedPlayerIndex == playerIndex;

        Texture2D bg =
            selected ? TextureAssets.InventoryBack14.Value :
            IsMouseHovering ? TextureAssets.InventoryBack15.Value :
            TextureAssets.InventoryBack7.Value;

        if (!hasPortal)
            bg = TextureAssets.InventoryBack5.Value;

        sb.Draw(bg, rect, Color.White);

        Vector2 iconCenter = rect.Center.ToVector2();
        iconCenter.Y += 0f;
        Texture2D icon = Ass.Icon_Portal3.Value;
        float iconScale = 0.65f;

        if (hasPortal && owner != null)
            PortalSystem.DrawOutlinedPortalIcon(sb, icon, iconCenter, iconScale, PortalSystem.GetPortalColor(owner));
        else
            sb.Draw(icon, iconCenter, null, Color.White, 0f, icon.Size() * 0.5f, iconScale, SpriteEffects.None, 0f);

        if (!hasPortal)
            sb.Draw(Ass.Icon_Forbidden.Value, iconCenter, null, Color.White, 0f, Ass.Icon_Forbidden.Value.Size() * 0.5f, 1.25f, SpriteEffects.None, 0f);

        if (IsMouseHovering)
        {
            local.mouseInterface = true;

            string name = owner?.name ?? "player";
            bool canRespawn = SpawnSystem.CanTeleport;

            string text =
                !hasPortal ? "No portal set" :
                selected
                    ? Language.GetTextValue("Mods.PvPAdventure.Spawn.CancelTeammatesPortal", name)
                    : canRespawn
                        ? Language.GetTextValue("Mods.PvPAdventure.Spawn.TeleportToTeammatesPortal", name)
                        : Language.GetTextValue("Mods.PvPAdventure.Spawn.SelectTeammatesPortal", name);

            Main.instance.MouseText(text);
        }
    }
}
