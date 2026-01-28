using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.UI.WorldItems;
using PvPAdventure.Core.Config;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.ItemDropRules;
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

    public override bool PreDrawInWorld(Item item, SpriteBatch sb, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        // Disable: world item outline drawing
        var config = ModContent.GetInstance<ClientConfig>();
        if (!config.PlayerOutline.BossItems)
            return base.PreDrawInWorld(item, sb, lightColor, alphaColor, ref rotation, ref scale, whoAmI);

        Team? team = _team;

        // debug: draw ONLY team items, HIDE all other items. do not delete.
        //if (!team.HasValue)
        //return false;

        if (!team.HasValue || team.Value == Team.None)
            return base.PreDrawInWorld(item, sb, lightColor, alphaColor, ref rotation, ref scale, whoAmI);

        // debug: draw ALL items with outlines. performance/stress-test.
        Color border = team.HasValue && team.Value != Team.None
        ? Main.teamColor[(int)team.Value]
        : Color.White;

        //Color border = Main.teamColor[(int)team.Value].MultiplyRGBA(lightColor);
        border.A = 255;

        DrawWorldItemOutline(item, sb, lightColor, alphaColor, rotation, scale, border);

        return base.PreDrawInWorld(item, sb, lightColor, alphaColor, ref rotation, ref scale, whoAmI);
    }

    private void DrawWorldItemOutline(Item item, SpriteBatch sb, Color lightColor, Color alphaColor, float rotation, float scale, Color borderColor)
    {
        Rectangle hitbox = Item.GetDrawHitbox(item.type, null);
        Vector2 worldCenter = new(item.Bottom.X, item.Bottom.Y - hitbox.Height * 0.5f);
        Vector2 screenCenter = worldCenter - Main.screenPosition;

        // debug: draw item hitbox. do not delete.
        //int sx = (int)(item.Bottom.X - hitbox.Width * 0.5f - Main.screenPosition.X);
        //int sy = (int)(item.Bottom.Y - hitbox.Height - Main.screenPosition.Y);
        //sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(sx, sy, hitbox.Width, hitbox.Height), Color.Black);

        var outlineSys = ModContent.GetInstance<WorldItemOutlineRenderTargetSystem>();

        if (outlineSys.TryGet(item.type, hitbox.Width, hitbox.Height, borderColor, out RenderTarget2D rt, out Vector2 origin))
        {
            sb.Draw(rt, screenCenter, null, Color.White, rotation, origin, scale, SpriteEffects.None, 0f);
        }
    }

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(item, tooltips);
    }

    public override void PostDrawInWorld(Item item, SpriteBatch sb, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
    {
        base.PostDrawInWorld(item, sb, lightColor, alphaColor, rotation, scale, whoAmI);
    }

}
