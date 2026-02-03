using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.World.Outlines.ItemOutlines;
using PvPAdventure.Core.Utilities;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using static PvPAdventure.Common.SpawnSelector.SpawnSystem;

namespace PvPAdventure.Common.SpawnSelector.UI;

/// <summary>
/// A small button that allows the player to teleport to a teammates bed.
/// Added to the top-right corner of a <see cref="UISpawnCharacter"/>
/// </summary>
public sealed class UIBedButton : UIElement
{
    private const float ButtonSize = 26f;

    private readonly int playerIndex;
    private readonly Item bedIcon;
    private bool hasBed;

    public UIBedButton(int playerIndex, bool hasBed)
    {
        this.hasBed = hasBed;
        this.playerIndex = playerIndex;

        bedIcon = new Item(ItemID.Bed);

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

        if (!HasValidBed(Main.player[playerIndex]))
        {
            return;
        }

        var sp = local.GetModPlayer<SpawnPlayer>();

        // Always allow clicking again to cancel, even if the bed owner became invalid.
        bool isSelected =
            sp.SelectedType == SpawnType.TeammateBed &&
            sp.SelectedPlayerIndex == playerIndex;

        if (isSelected)
        {
            sp.ToggleSelection(SpawnType.TeammateBed, playerIndex); // hits "same" => clears
            return;
        }

        Player bedOwner = Main.player[playerIndex];
        if (bedOwner == null || !bedOwner.active)
            return;

        // Allow self bed or same-team bed (do NOT require bedOwner to be alive).
        if (playerIndex != local.whoAmI)
        {
            if (local.team == 0 || bedOwner.team != local.team)
                return;
        }

        // Only allow selection if they actually have a valid bed.
        if (bedOwner.SpawnX < 0 || bedOwner.SpawnY < 0)
            return;

        if (!Player.CheckSpawn(bedOwner.SpawnX, bedOwner.SpawnY))
            return;

        sp.ToggleSelection(SpawnType.TeammateBed, playerIndex);
    }

    private static bool HasValidBed(Player p)
    {
        if (p == null || !p.active)
            return false;

        if (p.SpawnX < 0 || p.SpawnY < 0)
            return false;

        return Player.CheckSpawn(p.SpawnX, p.SpawnY);
    }

    protected override void DrawSelf(SpriteBatch sb)
    {
        Player local = Main.LocalPlayer;
        if (local == null || !local.active)
            return;

        Player owner = (playerIndex >= 0 && playerIndex < Main.maxPlayers) ? Main.player[playerIndex] : null;

        // Refresh every draw
        hasBed = HasValidBed(owner);

        if (IsMouseHovering && hasBed)
        {
            SpectateSystem.TrySetHover(SpawnType.TeammateBed, playerIndex);
            local.mouseInterface = true;
        }
        else
        {
            SpectateSystem.ClearHoverIfMatch(SpawnType.TeammateBed, playerIndex);
        }

        Rectangle rect = GetDimensions().ToRectangle();

        var sp = local.GetModPlayer<SpawnPlayer>();
        bool selected = sp != null && sp.SelectedType == SpawnType.TeammateBed && sp.SelectedPlayerIndex == playerIndex;

        Texture2D bg =
            selected ? TextureAssets.InventoryBack14.Value :
            IsMouseHovering ? TextureAssets.InventoryBack15.Value :
            TextureAssets.InventoryBack7.Value;

        if (!hasBed)
            bg = TextureAssets.InventoryBack5.Value;

        sb.Draw(bg, rect, Color.White);

        Vector2 iconCenter = rect.Center.ToVector2();
        iconCenter.Y -= 0.5f;
        float iconScale = (ButtonSize + 4) / 32f;

        // Proper outline behind icon
        if (hasBed && local.team != 0)
        {
            Color borderColor = Main.teamColor[local.team];

            int drawW = (int)40;
            int drawH = (int)26;

            ItemOutlineSystem outlineSys = ModContent.GetInstance<ItemOutlineSystem>();
            if (outlineSys != null && outlineSys.TryGet(bedIcon.type, drawW, drawH, borderColor, out RenderTarget2D rt, out Vector2 rtOrigin))
            {

                rtOrigin.Y += 0.4f;
                float outlineScale = iconScale - 0.18f;
                sb.Draw(rt, iconCenter, null, Color.White, 0f, rtOrigin, outlineScale, SpriteEffects.None, 0f);
            }
        }

        // Icon on top
        if (!hasBed)
        {
            iconScale = (ButtonSize+8) / 32f;
        }
        else
        {
            //iconScale = (ButtonSize + 6) / 32f;
        }
        ItemSlot.DrawItemIcon(bedIcon, ItemSlot.Context.InventoryItem, sb, iconCenter, iconScale, ButtonSize, Color.White);

        if (!hasBed)
        {
            Vector2 origin = Ass.Icon_Forbidden.Value.Size() * 0.5f;
            sb.Draw(Ass.Icon_Forbidden.Value, iconCenter, null, Color.White*1.0f, 0f, origin, 1.25f, SpriteEffects.None, 0f);
        }

        if (IsMouseHovering)
        {
            local.mouseInterface = true;

            string name = owner?.name ?? "player";
            bool canRespawn = SpawnSystem.CanTeleport;

            string text =
                !hasBed ? "No bed set" :
                selected
                    ? Language.GetTextValue("Mods.PvPAdventure.Spawn.CancelTeammatesBed", name)
                    : canRespawn
                        ? Language.GetTextValue("Mods.PvPAdventure.Spawn.TeleportToTeammatesBed", name)
                        : Language.GetTextValue("Mods.PvPAdventure.Spawn.SelectTeammatesBed", name);

            Main.instance.MouseText(text);
        }
    }
}

