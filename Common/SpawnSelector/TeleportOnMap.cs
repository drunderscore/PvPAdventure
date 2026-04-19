using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.SpawnSelector;

/// <summary>
/// Draws world icons: world spawn, own bed, teammates' beds.
/// Allows hovering and clicking to select spawn points and teleport when allowed.
/// </summary>
[Autoload(Side = ModSide.Client)]
public class TeleportOnMap : ModSystem
{
    public override void Load()
    {
        On_SpawnMapLayer.Draw += OnSpawnMapLayer;
    }

    public override void Unload()
    {
        On_SpawnMapLayer.Draw -= OnSpawnMapLayer;
    }

    private void OnSpawnMapLayer(On_SpawnMapLayer.orig_Draw orig, SpawnMapLayer self, ref MapOverlayDrawContext context, ref string text)
    {
        Player local = Main.LocalPlayer;
        if (local == null)
            return;

        SpawnPlayer sp = local.GetModPlayer<SpawnPlayer>();

        bool selectorOpen = SpawnSystem.IsUiOpen;
        bool instantTeleport = SpawnSystem.CanTeleport && !local.dead;

        bool recallActive = selectorOpen && !instantTeleport;

        bool handledHover = false;

        handledHover |= DrawWorldIcon(local, sp, selectorOpen, instantTeleport, recallActive, ref context, ref text);
        handledHover |= DrawMyBedIcon(local, sp, selectorOpen, instantTeleport, recallActive, ref context, ref text);
        handledHover |= DrawTeamBedIcons(local, sp, selectorOpen, instantTeleport, recallActive, ref context, ref text);
        handledHover |= DrawAdventurePortalIcons(local, sp, selectorOpen, instantTeleport, recallActive, ref context, ref text);
    }

    private bool DrawWorldIcon(Player local, SpawnPlayer sp, bool selectorOpen, bool instantTeleport, bool recallActive, ref MapOverlayDrawContext context, ref string text)
    {
        Vector2 pos = new(Main.spawnTileX, Main.spawnTileY);
        bool selected = sp.SelectedType == SpawnType.World;

        if (!DrawIcon(TextureAssets.SpawnPoint.Value, pos, selected, instantTeleport, recallActive, ref context, out bool hover))
            return false;

        if (!hover)
            return false;

        if (!selectorOpen)
        {
            text = Language.GetTextValue("UI.SpawnPoint");
            return false;
        }

        if (instantTeleport)
        {
            text = Language.GetTextValue("Mods.PvPAdventure.Spawn.TeleportToWorldSpawn");
            if (IsClick())
            {
                sp.ToggleSelection(SpawnType.World);
                sp.RequestExecute();
            }

            return true;
        }

        text = selected
            ? Language.GetTextValue("Mods.PvPAdventure.Spawn.CancelWorldSpawn")
            : Language.GetTextValue("Mods.PvPAdventure.Spawn.SelectWorldSpawn");

        if (IsClick())
            sp.ToggleSelection(SpawnType.World);

        return true;
    }

    private bool DrawMyBedIcon(Player local, SpawnPlayer sp, bool selectorOpen, bool instantTeleport, bool recallActive, ref MapOverlayDrawContext context, ref string text)
    {
        if (!HasValidBedSpawn(local))
            return false;

        Vector2 pos = new Vector2(local.SpawnX, local.SpawnY);
        bool selected = sp.SelectedType == SpawnType.MyBed;


        if (!DrawIcon(TextureAssets.SpawnBed.Value, pos, selected, instantTeleport, recallActive, ref context, out bool hover))
            return false;

        if (!hover)
            return false;

        if (!selectorOpen)
        {
            text = Language.GetTextValue("UI.SpawnBed");
            return false;
        }

        if (instantTeleport)
        {
            text = Language.GetTextValue("Mods.PvPAdventure.Spawn.TeleportToMyBed", local.name);
            if (IsClick())
            {
                sp.ToggleSelection(SpawnType.MyBed, local.whoAmI);
                sp.RequestExecute();
            }

            return true;
        }

        text = selected
            ? Language.GetTextValue("Mods.PvPAdventure.Spawn.CancelMyBed", local.name)
            : Language.GetTextValue("Mods.PvPAdventure.Spawn.SelectMyBed", local.name);

        if (IsClick())
            sp.ToggleSelection(SpawnType.MyBed, local.whoAmI);

        return true;
    }

    private bool DrawTeamBedIcons(Player local, SpawnPlayer sp, bool selectorOpen, bool instantTeleport, bool recallActive, ref MapOverlayDrawContext context, ref string text)
    {
        if (local.team == 0)
            return false;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            if (i == local.whoAmI)
                continue;

            Player other = Main.player[i];
            if (other == null || !other.active || other.team != local.team)
                continue;

            if (!HasValidBedSpawn(other))
                continue;

            Vector2 pos = new Vector2(other.SpawnX, other.SpawnY);
            bool selected = sp.SelectedType == SpawnType.TeammateBed && sp.SelectedPlayerIndex == i;

            if (!DrawIcon(TextureAssets.SpawnBed.Value, pos, selected, instantTeleport, recallActive, ref context, out bool hover))
                continue;

            if (!hover)
                continue;

            if (!selectorOpen)
            {
                text = Language.GetTextValue("Mods.PvPAdventure.Spawn.TeammatesBed", other.name);
                return true;
            }

            if (instantTeleport)
            {
                text = Language.GetTextValue("Mods.PvPAdventure.Spawn.TeleportToTeammatesBed", other.name);
                if (IsClick())
                {
                    sp.ToggleSelection(SpawnType.TeammateBed, i);
                    sp.RequestExecute();
                }

                return true;
            }

            text = selected
                ? Language.GetTextValue("Mods.PvPAdventure.Spawn.CancelTeammatesBed", other.name)
                : Language.GetTextValue("Mods.PvPAdventure.Spawn.SelectTeammatesBed", other.name);

            if (IsClick())
                sp.ToggleSelection(SpawnType.TeammateBed, i);

            return true;
        }
        return false;
    }

    private bool DrawAdventurePortalIcons(Player local, SpawnPlayer sp, bool selectorOpen, bool instantTeleport, bool recallActive, ref MapOverlayDrawContext context, ref string text)
    {
        Texture2D portalTexture = TextureAssets.Item[ItemID.PotionOfReturn].Value;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player other = Main.player[i];
            if (other == null || !other.active)
                continue;

            if (!AdventurePortalSystem.TryGetPortalTilePosition(i, out Vector2 tilePos))
                continue;

            bool canInteract = AdventurePortalSystem.IsValidTeammatePortalIndex(local, i);
            bool selected = canInteract && sp.SelectedType == SpawnType.Teammate && sp.SelectedPlayerIndex == i;

            if (!DrawIcon(portalTexture, tilePos, selected, instantTeleport, recallActive, ref context, out bool hover))
                continue;

            if (!hover)
                continue;

            if (!selectorOpen)
            {
                text = $"{other.name}'s adventure portal";
                return true;
            }

            if (!canInteract)
            {
                text = $"{other.name}'s adventure portal";
                return true;
            }

            if (instantTeleport)
            {
                text = $"Teleport to {other.name}'s adventure portal";
                if (IsClick())
                {
                    sp.ToggleSelection(SpawnType.Teammate, i);
                    sp.RequestExecute();
                }

                return true;
            }

            text = selected
                ? $"Cancel {other.name}'s adventure portal"
                : $"Select {other.name}'s adventure portal";

            if (IsClick())
                sp.ToggleSelection(SpawnType.Teammate, i);

            return true;
        }

        return false;
    }

    private static bool DrawIcon(Texture2D tex, Vector2 tilePos, bool selected, bool instantTeleport, bool recallActive,
    ref MapOverlayDrawContext context, out bool hover)
    {
        bool canHoverZoom = instantTeleport || recallActive;

        // Stick: if selected, always 1.8
        float baseScale = selected ? 1.8f : 1.0f;

        // Hover/highlight zoom only when allowed; otherwise keep same as base
        float hoverScale = canHoverZoom ? 1.8f : baseScale;

        var result = context.Draw(
            texture: tex,
            position: tilePos,
            color: Color.White,
            frame: new SpriteFrame(1, 1),
            scaleIfNotSelected: baseScale,
            scaleIfSelected: hoverScale,
            alignment: Alignment.Bottom,
            spriteEffects: SpriteEffects.None
        );

        hover = result.IsMouseOver;
        return true;
    }

    private static bool IsClick() => Main.mouseLeft && Main.mouseLeftRelease;

    private static bool HasValidBedSpawn(Player player)
    {
        if (player.SpawnX < 0 || player.SpawnY < 0)
            return false;

        return Player.CheckSpawn(player.SpawnX, player.SpawnY);
    }
}
