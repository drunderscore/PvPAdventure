using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Assets;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
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

        bedIcon = new Item();
        bedIcon.SetDefaults(ItemID.Bed);

        Width.Set(ButtonSize, 0f);
        Height.Set(ButtonSize, 0f);
        SetPadding(0f);
    }

    private static bool HasValidBed(Player p)
    {
        return p.SpawnX >= 0 &&
               p.SpawnY >= 0 &&
               Player.CheckSpawn(p.SpawnX, p.SpawnY);
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

    protected override void DrawSelf(SpriteBatch sb)
    {
        // update start
        Player local = Main.LocalPlayer;
        if (local == null || !local.active)
            return;

        bool valid =
            playerIndex >= 0 &&
            playerIndex < Main.maxPlayers &&
            Main.player[playerIndex] != null &&
            Main.player[playerIndex].active &&
            Main.player[playerIndex].SpawnX >= 0 &&
            Main.player[playerIndex].SpawnY >= 0 &&
            Player.CheckSpawn(
                Main.player[playerIndex].SpawnX,
                Main.player[playerIndex].SpawnY
            );

        if (IsMouseHovering && valid)
        {
            SpectateSystem.TrySetHover(SpawnType.TeammateBed, playerIndex);
            local.mouseInterface = true;
        }
        else
        {
            SpectateSystem.ClearHoverIfMatch(SpawnType.TeammateBed, playerIndex);
        }
        // update end

        var dims = GetDimensions();
        Rectangle rect = dims.ToRectangle();

        var sp = local.GetModPlayer<SpawnPlayer>();

        bool selected =
        sp != null &&
        sp.SelectedType == SpawnSystem.SpawnType.TeammateBed &&
        sp.SelectedPlayerIndex == playerIndex;

        bool hasValidBed = HasValidBed(Main.player[playerIndex]);

        Texture2D bg =
            selected ? TextureAssets.InventoryBack14.Value :
            IsMouseHovering ? TextureAssets.InventoryBack15.Value :
            TextureAssets.InventoryBack7.Value;

        if (!hasValidBed) bg = TextureAssets.InventoryBack5.Value;
        //if (!hasValidBed) bg = TextureAssets.InventoryBack11.Value;
        //if (!hasValidBed) bg = TextureAssets.InventoryBack2.Value;

        sb.Draw(bg, rect, Color.White);

        Vector2 iconCenter = rect.Center.ToVector2();
        float scale = ButtonSize / 32f;

        ItemSlot.DrawItemIcon(bedIcon,ItemSlot.Context.InventoryItem,sb,iconCenter,scale,ButtonSize,Color.White);

        if (!hasValidBed) 
        {
            Vector2 origin = Ass.Icon_Forbidden.Value.Size() * 0.5f;
            sb.Draw(Ass.Icon_Forbidden.Value, iconCenter, null, Color.White, 0f, origin, 1f, SpriteEffects.None, 0f);
        }

        if (IsMouseHovering)
        {
            local.mouseInterface = true;

            Player target = Main.player[playerIndex];
            string name = target?.name ?? "player";

            bool canRespawn = SpawnSystem.CanTeleport;

            string text =
                !hasValidBed ? "No bed set" :
                selected
                    ? Language.GetTextValue("Mods.PvPAdventure.Spawn.CancelBedSpawn", name)
                    : canRespawn
                        ? Language.GetTextValue("Mods.PvPAdventure.Spawn.TeleportToTeammatesBed", name)
                        : Language.GetTextValue("Mods.PvPAdventure.Spawn.SelectBedSpawn", name);

            Main.instance.MouseText(text);
        }
    }
}

