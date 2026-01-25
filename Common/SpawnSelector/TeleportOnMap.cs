using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.SpawnSelector;

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

        if (DrawWorld(local, sp, selectorOpen, instantTeleport, ref context, ref text))
            return;

        if (DrawMyBed(local, sp, selectorOpen, instantTeleport, ref context, ref text))
            return;

        DrawTeamBeds(local, sp, selectorOpen, instantTeleport, ref context, ref text);
    }

    private bool DrawWorld(Player local, SpawnPlayer sp, bool selectorOpen, bool instantTeleport, ref MapOverlayDrawContext context, ref string text)
    {
        Vector2 pos = new Vector2(Main.spawnTileX, Main.spawnTileY);
        bool selected = sp.SelectedType == SpawnType.World;

        if (!DrawIcon(TextureAssets.SpawnPoint.Value, pos, selected, ref context, out bool hover))
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

    private bool DrawMyBed(Player local, SpawnPlayer sp, bool selectorOpen, bool instantTeleport, ref MapOverlayDrawContext context, ref string text)
    {
        if (!HasValidBedSpawn(local))
            return false;

        Vector2 pos = new Vector2(local.SpawnX, local.SpawnY);
        bool selected = sp.SelectedType == SpawnType.MyBed;

        if (!DrawIcon(TextureAssets.SpawnBed.Value, pos, selected, ref context, out bool hover))
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

    private void DrawTeamBeds(Player local, SpawnPlayer sp, bool selectorOpen, bool instantTeleport, ref MapOverlayDrawContext context, ref string text)
    {
        if (local.team == 0)
            return;

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

            if (!DrawIcon(TextureAssets.SpawnBed.Value, pos, selected, ref context, out bool hover))
                continue;

            if (!hover)
                continue;

            if (!selectorOpen)
            {
                text = Language.GetTextValue("Mods.PvPAdventure.Spawn.TeammatesBed", other.name);
                return;
            }

            if (instantTeleport)
            {
                text = Language.GetTextValue("Mods.PvPAdventure.Spawn.TeleportToTeammatesBed", other.name);
                if (IsClick())
                {
                    sp.ToggleSelection(SpawnType.TeammateBed, i);
                    sp.RequestExecute();
                }

                return;
            }

            text = selected
                ? Language.GetTextValue("Mods.PvPAdventure.Spawn.CancelTeammatesBed", other.name)
                : Language.GetTextValue("Mods.PvPAdventure.Spawn.SelectTeammatesBed", other.name);

            if (IsClick())
                sp.ToggleSelection(SpawnType.TeammateBed, i);

            return;
        }
    }

    private static bool DrawIcon(Texture2D tex, Vector2 tilePos, bool selected, ref MapOverlayDrawContext context, out bool hover)
    {
        float scale = selected ? 1.8f : 1.0f;

        var result = context.Draw(
            texture: tex,
            position: tilePos,
            color: Color.White,
            frame: new SpriteFrame(1, 1),
            scaleIfNotSelected: scale,
            scaleIfSelected: 1.8f,
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
