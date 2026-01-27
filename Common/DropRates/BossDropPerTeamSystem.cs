using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.ItemDropRules;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.DropRates;

internal sealed class BossDropPerTeamSystem : ModSystem
{
    public override void Load()
    {
        On_CommonCode.DropItem_Rectangle_IEntitySource_int_int_bool += DropItem_TagBossLoot;
        On_CommonCode.DropItemLocalPerClientAndSetNPCMoneyTo0 += DropBossBag_TeamOnly;
    }

    public override void Unload()
    {
        On_CommonCode.DropItem_Rectangle_IEntitySource_int_int_bool -= DropItem_TagBossLoot;
        On_CommonCode.DropItemLocalPerClientAndSetNPCMoneyTo0 -= DropBossBag_TeamOnly;
    }

    private static Team GetAwardTeam(NPC npc)
    {
        // FIXME: Not consistent with how the rest of the codebase determines the last hit.
        if (npc.lastInteraction == 255)
            return Team.None;

        Player p = Main.player[npc.lastInteraction];
        return (p != null && p.active) ? (Team)p.team : Team.None;
    }

    private static bool TryGetLootNpc(IEntitySource src, out NPC npc)
    {
        npc = null;

        var t = src.GetType();

        var fNpc = t.GetField("npc", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? t.GetField("NPC", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (fNpc?.GetValue(src) is NPC n1) { npc = n1; return true; }

        var pNpc = t.GetProperty("NPC", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? t.GetProperty("Entity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (pNpc?.GetValue(src) is NPC n2) { npc = n2; return true; }

        return false;
    }

    private int DropItem_TagBossLoot(On_CommonCode.orig_DropItem_Rectangle_IEntitySource_int_int_bool orig, Rectangle rectangle, IEntitySource entitySource, int itemId, int stack, bool scattered)
    {
        int idx = orig(rectangle, entitySource, itemId, stack, scattered);

        // If we are the server, check if the drops should be instanced instead.
        if (Main.netMode != NetmodeID.Server || idx < 0 || idx >= Main.maxItems)
            return idx;

        if (!TryGetLootNpc(entitySource, out NPC npc) || npc == null || !npc.boss)
            return idx;

        Item item = Main.item[idx];
        if (!item.active || ItemID.Sets.BossBag[item.type])
            return idx;

        Team team = GetAwardTeam(npc);
        item.GetGlobalItem<BossDropPerTeamGlobalItem>()._team = team;

        Log.Chat($"{Lang.GetItemNameValue(item.type)}->{team}");
        NetMessage.SendData(MessageID.SyncItem, number: idx);

        return idx;
    }

    private void DropBossBag_TeamOnly(On_CommonCode.orig_DropItemLocalPerClientAndSetNPCMoneyTo0 orig, NPC npc, int itemId, int stack, bool interactionRequired = true)
    {
        // If we are the server, check if the drops should be instanced instead.
        if (Main.netMode != NetmodeID.Server || !npc.boss || !ItemID.Sets.BossBag[itemId])
        {
            orig(npc, itemId, stack, interactionRequired);
            return;
        }

        Team team = GetAwardTeam(npc);
        if (team == Team.None)
        {
            orig(npc, itemId, stack, interactionRequired);
            return;
        }

        int idx = Item.NewItem(npc.GetItemSource_Loot(), npc.Hitbox, itemId, stack, noBroadcast: true, prefixGiven: -1);
        Main.timeItemSlotCannotBeReusedFor[idx] = 54000;

        Item item = Main.item[idx];
        item.GetGlobalItem<BossDropPerTeamGlobalItem>()._team = team;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player p = Main.player[i];
            if (p == null || !p.active)
                continue;

            if (interactionRequired && !npc.playerInteraction[i])
                continue;

            if ((Team)p.team != team)
                continue;

            NetMessage.SendData(MessageID.InstancedItem, i, -1, null, idx);
        }

        Main.item[idx].active = false;
        npc.value = 0f;

        Log.Chat($"{Lang.GetItemNameValue(itemId)}->{team}");
    }
}

public sealed class BossDropPerTeamGlobalItem : GlobalItem
{
    public override bool InstancePerEntity => true;
    public Team? _team;

    public override void NetSend(Item item, BinaryWriter writer)
    {
        bool has = _team.HasValue;
        writer.Write(has);

        if (has)
            writer.Write7BitEncodedInt((int)_team!.Value);
    }

    public override void NetReceive(Item item, BinaryReader reader)
    {
        _team = reader.ReadBoolean() ? (Team)reader.Read7BitEncodedInt() : null;
    }

    public override bool CanPickup(Item item, Player player)
    {
        Team? team = _team;
        return !team.HasValue || team.Value == (Team)player.team;
    }

    public override bool OnPickup(Item item, Player player)
    {
        _team = null;
        return true;
    }

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(item, tooltips);
        Team? team = _team;

        bool isTeamItem = team.HasValue;
        if (isTeamItem)
        {
            Log.Chat(item.Name);
        }
    }
}

[Autoload(Side =ModSide.Client)]
internal sealed class WorldItemHoverTeamTagSystem : ModSystem
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
            text = $"{text} [c/{teamHex}:({team.Value} Team)]";

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
