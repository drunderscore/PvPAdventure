using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.UI.WorldItems;
using PvPAdventure.Core.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.Loot;

internal sealed class BossDropPerTeamSystem : ModSystem
{
    public override void Load()
    {
        On_CommonCode.DropItem_Rectangle_IEntitySource_int_int_bool += DropItem_TagBossLoot;
        On_CommonCode.DropItemLocalPerClientAndSetNPCMoneyTo0 += DropBossBag_TeamOnly;
        On_Main.DrawItem += DisableBossBagGlow;
    }

    public override void Unload()
    {
        On_CommonCode.DropItem_Rectangle_IEntitySource_int_int_bool -= DropItem_TagBossLoot;
        On_CommonCode.DropItemLocalPerClientAndSetNPCMoneyTo0 -= DropBossBag_TeamOnly;
        On_Main.DrawItem -= DisableBossBagGlow;
    }

    private static void DisableBossBagGlow(On_Main.orig_DrawItem orig, Main self, Item item, int whoami)
    {
        if (!item.active || item.IsAir)
        {
            return;
        }
        Main.instance.LoadItem(item.type);
        Main.instance.DrawItem_GetBasics(item, whoami, out var texture, out var frame, out var glowmaskFrame);
        Vector2 vector = frame.Size() / 2f;
        Vector2 vector2 = new Vector2((float)(item.width / 2) - vector.X, item.height - frame.Height);
        Vector2 vector3 = item.position - Main.screenPosition + vector + vector2;
        float num = item.velocity.X * 0.2f;
        if (item.shimmered)
        {
            num = 0f;
        }
        float scale = 1f;
        Color color = Lighting.GetColor(item.Center.ToTileCoordinates());
        Color currentColor = item.GetAlpha(color);
        if (item.shimmered)
        {
            currentColor.R = (byte)(255f * (1f - item.shimmerTime));
            currentColor.G = (byte)(255f * (1f - item.shimmerTime));
            currentColor.B = (byte)(255f * (1f - item.shimmerTime));
            currentColor.A = (byte)(255f * (1f - item.shimmerTime));
        }
        else if (item.shimmerTime > 0f)
        {
            currentColor.R = (byte)((float)(int)currentColor.R * (1f - item.shimmerTime));
            currentColor.G = (byte)((float)(int)currentColor.G * (1f - item.shimmerTime));
            currentColor.B = (byte)((float)(int)currentColor.B * (1f - item.shimmerTime));
            currentColor.A = (byte)((float)(int)currentColor.A * (1f - item.shimmerTime));
        }
        ItemSlot.GetItemLight(ref currentColor, ref scale, item);
        if (ItemLoader.PreDrawInWorld(item, Main.spriteBatch, color, currentColor, ref num, ref scale, whoami))
        {
            int num2 = item.glowMask;
            if (!Main.gamePaused && (item.IsACoin || item.type == 58 || item.type == 109) && color.R > 60 && (float)Main.rand.Next(500) - (Math.Abs(item.velocity.X) + Math.Abs(item.velocity.Y)) * 10f < (float)(color.R / 50))
            {
                int type = 43;
                Color newColor = Color.White;
                int alpha = 254;
                float scale2 = 0.5f;
                if (item.IsACoin)
                {
                    newColor = default(Color);
                    alpha = 0;
                    scale2 = 1f;
                }
                switch (item.type)
                {
                    case 71:
                        type = 244;
                        break;
                    case 72:
                        type = 245;
                        break;
                    case 73:
                        type = 246;
                        break;
                    case 74:
                        type = 247;
                        break;
                }
                int num3 = Dust.NewDust(item.position, item.width, item.height, type, 0f, 0f, alpha, newColor, scale2);
                Main.dust[num3].velocity *= 0f;
            }
            if (ItemID.Sets.BossBag[item.type])
            {
                float num4 = (float)item.timeSinceItemSpawned / 240f + Main.GlobalTimeWrappedHourly * 0.04f;
                float globalTimeWrappedHourly = Main.GlobalTimeWrappedHourly;
                globalTimeWrappedHourly %= 4f;
                globalTimeWrappedHourly /= 2f;
                if (globalTimeWrappedHourly >= 1f)
                {
                    globalTimeWrappedHourly = 2f - globalTimeWrappedHourly;
                }
                globalTimeWrappedHourly = globalTimeWrappedHourly * 0.5f + 0.5f;
                for (float num5 = 0f; num5 < 1f; num5 += 0.25f)
                {
                    //Main.spriteBatch.Draw(texture, vector3 + new Vector2(0f, 8f).RotatedBy((num5 + num4) * ((float)Math.PI * 2f)) * globalTimeWrappedHourly, frame, new Color(90, 70, 255, 50), num, vector, scale, SpriteEffects.None, 0f);
                }
                for (float num6 = 0f; num6 < 1f; num6 += 0.34f)
                {
                    //Main.spriteBatch.Draw(texture, vector3 + new Vector2(0f, 4f).RotatedBy((num6 + num4) * ((float)Math.PI * 2f)) * globalTimeWrappedHourly, frame, new Color(140, 120, 255, 77), num, vector, scale, SpriteEffects.None, 0f);
                }
            }
            else if (item.type == 75)
            {
                float num7 = (float)item.timeSinceItemSpawned / 240f + Main.GlobalTimeWrappedHourly * 0.04f;
                float globalTimeWrappedHourly2 = Main.GlobalTimeWrappedHourly;
                globalTimeWrappedHourly2 %= 5f;
                globalTimeWrappedHourly2 /= 2.5f;
                if (globalTimeWrappedHourly2 >= 1f)
                {
                    globalTimeWrappedHourly2 = 2f - globalTimeWrappedHourly2;
                }
                globalTimeWrappedHourly2 = globalTimeWrappedHourly2 * 0.5f + 0.5f;
                for (float num8 = 0f; num8 < 1f; num8 += 0.25f)
                {
                    Main.spriteBatch.Draw(TextureAssets.Item[item.type].Value, vector3 + new Vector2(0f, 8f).RotatedBy((num8 + num7) * ((float)Math.PI * 2f)) * globalTimeWrappedHourly2, frame, new Color(50, 50, 255, 50), num, vector, scale, SpriteEffects.None, 0f);
                }
                for (float num9 = 0f; num9 < 1f; num9 += 0.34f)
                {
                    Main.spriteBatch.Draw(TextureAssets.Item[item.type].Value, vector3 + new Vector2(0f, 4f).RotatedBy((num9 + num7) * ((float)Math.PI * 2f)) * globalTimeWrappedHourly2, frame, new Color(120, 120, 255, 127), num, vector, scale, SpriteEffects.None, 0f);
                }
            }
            else if (item.type == 4143)
            {
                float num10 = (float)item.timeSinceItemSpawned / 240f + Main.GlobalTimeWrappedHourly * 0.04f;
                float globalTimeWrappedHourly3 = Main.GlobalTimeWrappedHourly;
                globalTimeWrappedHourly3 %= 5f;
                globalTimeWrappedHourly3 /= 2.5f;
                if (globalTimeWrappedHourly3 >= 1f)
                {
                    globalTimeWrappedHourly3 = 2f - globalTimeWrappedHourly3;
                }
                globalTimeWrappedHourly3 = globalTimeWrappedHourly3 * 0.5f + 0.5f;
                for (float num11 = 0f; num11 < 1f; num11 += 0.34f)
                {
                    Main.spriteBatch.Draw(TextureAssets.Item[item.type].Value, vector3 + new Vector2(0f, 8f).RotatedBy((num11 + num10) * ((float)Math.PI * 2f)) * globalTimeWrappedHourly3, frame, new Color(30, 30, 155, 60), num, vector, scale, SpriteEffects.None, 0f);
                }
                for (float num12 = 0f; num12 < 1f; num12 += 0.34f)
                {
                    Main.spriteBatch.Draw(TextureAssets.Item[item.type].Value, vector3 + new Vector2(0f, 4f).RotatedBy((num12 + num10) * ((float)Math.PI * 2f)) * globalTimeWrappedHourly3, frame, new Color(60, 60, 127, 57), num, vector, scale, SpriteEffects.None, 0f);
                }
                Main.spriteBatch.Draw(texture, vector3, frame, new Color(255, 255, 255, 128), num, vector, scale, SpriteEffects.None, 0f);
            }
            if ((item.type >= 1522 && item.type <= 1527) || item.type == 3643)
            {
                currentColor = ((!(item.shimmerTime > 0f)) ? new Color(250, 250, 250, Main.mouseTextColor / 2) : new Color((int)(250f * (1f - item.shimmerTime)), (int)(250f * (1f - item.shimmerTime)), (int)(250f * (1f - item.shimmerTime)), (int)((float)(Main.mouseTextColor / 2) * (1f - item.shimmerTime))));
                scale = (float)(int)Main.mouseTextColor / 1000f + 0.8f;
            }
            if (item.type == 3779)
            {
                num2 = -1;
            }
            Main.spriteBatch.Draw(texture, vector3, frame, currentColor, num, vector, scale, SpriteEffects.None, 0f);
            if (item.shimmered)
            {
                Main.spriteBatch.Draw(texture, vector3, frame, new Color(currentColor.R, currentColor.G, currentColor.B, 0), num, vector, scale, SpriteEffects.None, 0f);
            }
            if (item.color != Color.Transparent)
            {
                Main.spriteBatch.Draw(texture, vector3, frame, item.GetColor(color), num, vector, scale, SpriteEffects.None, 0f);
            }
            if (num2 != -1)
            {
                Color color2 = new Color(250, 250, 250, item.alpha);
                if (item.type == 5146)
                {
                    color2 = new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB);
                }
                Main.spriteBatch.Draw(TextureAssets.GlowMask[num2].Value, vector3, frame, color2, num, vector, scale, SpriteEffects.None, 0f);
            }
            if (ItemID.Sets.TrapSigned[item.type])
            {
                Main.spriteBatch.Draw(TextureAssets.Wire.Value, vector3 + frame.Size().RotatedBy(num) * 0.45f * item.scale, new Rectangle(4, 58, 8, 8), currentColor, 0f, new Vector2(4f), 1f, SpriteEffects.None, 0f);
            }
            if (item.type == 3858)
            {
                Main.spriteBatch.Draw(TextureAssets.GlowMask[233].Value, vector3, glowmaskFrame, new Color(255, 255, 255, 63) * 0.75f, num, glowmaskFrame.Size() / 2f, scale, SpriteEffects.None, 0f);
            }
            if (ItemID.Sets.DrawUnsafeIndicator[item.type])
            {
                Vector2 vector4 = new Vector2(-4f, -4f) * scale;
                Texture2D value = TextureAssets.Extra[258].Value;
                Rectangle rectangle = value.Frame();
                Main.spriteBatch.Draw(value, vector3 + vector4 + frame.Size().RotatedBy(num) * 0.45f * item.scale, rectangle, currentColor, num, rectangle.Size() / 2f, 1f, SpriteEffects.None, 0f);
            }
        }
        ItemLoader.PostDrawInWorld(item, Main.spriteBatch, color, currentColor, num, scale, whoami);
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

        // draw regular items normally
        if (!team.HasValue || team.Value == Team.None)
            return base.PreDrawInWorld(item, sb, lightColor, alphaColor, ref rotation, ref scale, whoAmI);

        // debug: draw ALL items with outlines. performance/stress-test.
        //Color border = team.HasValue && team.Value != Team.None
        //? Main.teamColor[(int)team.Value]
        //: Color.White;

        Color border = Main.teamColor[(int)team.Value].MultiplyRGBA(lightColor);
        //Color border = Main.teamColor[(int)team.Value];
        border.A = 255;

        DrawWorldItemOutline(item, sb, lightColor, alphaColor, rotation, scale, border);

        return base.PreDrawInWorld(item, sb, lightColor, alphaColor, ref rotation, ref scale, whoAmI);
    }

    public override void Update(Item item, ref float gravity, ref float maxFallSpeed)
    {
        //base.Update(item, ref gravity, ref maxFallSpeed);
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
