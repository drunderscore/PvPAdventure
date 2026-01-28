using Microsoft.Xna.Framework;
using PvPAdventure.Common.DropRates;
using Terraria;
using Terraria.Enums;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace PvPAdventure.Common.UI.WorldItems;

[Autoload(Side = ModSide.Client)]
internal sealed class WorldItemsHoverText : ModSystem
{
    private static int _lastIdx = -1;

    public override void Load()
    {
        On_Main.MouseTextHackZoom_string_int_byte_string += MouseTextHackZoom_TagWorldItem;
    }

    public override void Unload()
    {
        On_Main.MouseTextHackZoom_string_int_byte_string -= MouseTextHackZoom_TagWorldItem;
    }

    private void MouseTextHackZoom_TagWorldItem(On_Main.orig_MouseTextHackZoom_string_int_byte_string orig, Main self, string text, int itemRarity, byte diff, string buffTooltip)
    {
        // If we are the server, check if the drops should be instanced instead.
        if (Main.dedServ || !TryGetHoveredWorldItemIndex(out int idx))
        {
            orig(self, text, itemRarity, diff, buffTooltip);
            return;
        }

        Item item = Main.item[idx];
        Team? team = item.GetGlobalItem<BossDropPerTeamGlobalItem>()._team;
        if (team.HasValue && team.Value != Team.None)
        {
            string teamHex = Main.teamColor[(int)team.Value].Hex3();
            //text = $"{text} [c/{teamHex}:({team.Value} Team)]";
            text = $"[c/{teamHex}:{text}]";

            if (_lastIdx != idx)
            {
                _lastIdx = idx;
                Log.Chat($"{item.Name}->{team.Value}");
            }
        }

        orig(self, text, itemRarity, diff, buffTooltip);
    }

    private static bool TryGetHoveredWorldItemIndex(out int idx)
    {
        idx = -1;

        PlayerInput.SetZoom_Unscaled();
        PlayerInput.SetZoom_MouseInWorld();

        Rectangle mouseRect = new((int)(Main.mouseX + Main.screenPosition.X), (int)(Main.mouseY + Main.screenPosition.Y), 1, 1);
        if (Main.player[Main.myPlayer].gravDir == -1f)
            mouseRect.Y = (int)Main.screenPosition.Y + Main.screenHeight - Main.mouseY;

        PlayerInput.SetZoom_UI();

        if (Main.mouseText)
            return false;

        for (int i = 0; i < 400; i++)
        {
            if (!Main.item[i].active)
                continue;

            Rectangle hitbox = Item.GetDrawHitbox(Main.item[i].type, null);
            Vector2 bottom = Main.item[i].Bottom;
            Rectangle itemRect = new((int)(bottom.X - hitbox.Width * 0.5f), (int)(bottom.Y - hitbox.Height), hitbox.Width, hitbox.Height);

            if (mouseRect.Intersects(itemRect))
            {
                idx = i;
                return true;
            }
        }

        return false;
    }
}

