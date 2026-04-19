using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Core.Utilities;
using PvPAdventure.UI;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Events;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI.World;

internal sealed class SpectatorWorldPanel : UIDraggablePanel
{
    private UIList sectionList;
    private UIScrollbar scrollbar;
    private bool rebuildQueued;

    public SpectatorWorldPanel() : base("World")
    {
        Width.Set(400f, 0f);
        Height.Set(500f, 0f);
        HAlign = 0.82f;
        VAlign = 0.48f;
        Build();
    }

    protected override float MinResizeW => 300f;
    protected override float MinResizeH => 200f;
    protected override float MaxResizeW => 800;
    protected override float MaxResizeH => 980f;

    protected override void OnClosePanelLeftClick() => Remove();
    protected override void OnRefreshPanelLeftClick() => Build();

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

#if DEBUG
        if (Main.keyState.IsKeyDown(Keys.F5) && !Main.oldKeyState.IsKeyDown(Keys.F5))
            rebuildQueued = true;
#endif

        if (rebuildQueued)
        {
            rebuildQueued = false;
            Build();
        }
    }

    private void Build()
    {
        ContentPanel.RemoveAllChildren();

        scrollbar = new UIScrollbar();
        scrollbar.Left.Set(-28f, 1f);
        scrollbar.Top.Set(14f, 0f);
        scrollbar.Height.Set(-66f, 1f);
        ContentPanel.Append(scrollbar);

        sectionList = new UIList { ListPadding = 12f };
        sectionList.Top.Set(10f, 0f);
        sectionList.Left.Set(12f, 0f);
        sectionList.Width.Set(-44f, 1f);
        sectionList.Height.Set(-30f, 1f);
        sectionList.SetScrollbar(scrollbar);
        sectionList.ManualSortMethod = _ => { };
        ContentPanel.Append(sectionList);

        RebuildSections();
    }

    private void RebuildSections()
    {
        if (sectionList is null)
            return;

        sectionList.Clear();

        sectionList.Add(new SpectatorWorldSection("World Information", 222f, static (_, sb, box) =>
        {
            Rectangle inner = new(box.X + 12, box.Y + 34, box.Width - 24, box.Height - 44);
            int leftX = inner.X + 6;
            int separatorX = inner.X + inner.Width / 2;
            int rightLabelX = separatorX + 18;
            int rightValueX = rightLabelX + 120;
            int startY = inner.Y + 4;
            int rowStep = 21;

            string timeText = WorldStatsHelper.GetTimeText();
            string weatherText = WorldStatsHelper.GetWeatherText();
            string sizeText = WorldStatsHelper.GetWorldSizeText();
            string seedText = WorldStatsHelper.GetSeedText();
            string evilText = WorldStatsHelper.GetEvilText();
            string moonText = WorldStatsHelper.GetMoonText();
            string windText = WorldStatsHelper.GetWindText();
            string surfaceText = WorldStatsHelper.GetSurfaceText();
            string cavernText = WorldStatsHelper.GetCavernText();
            string infectionText = WorldStatsHelper.GetWorldInfectionText();
            string difficultyText = WorldStatsHelper.GetDifficultyText();
            Color difficultyColor = WorldStatsHelper.GetDifficultyColor();
            Color evilColor = WorldStatsHelper.GetEvilColor();
            var worldIconAsset = WorldStatsHelper.TryGetWorldIconAsset();

            WorldStatsHelper.DrawWorldInfoBackground(sb, new Rectangle(separatorX - 12, inner.Y, inner.Right - separatorX + 12, inner.Height));
            WorldStatsHelper.DrawRowSeparator(sb, inner.X, inner.Y + 108, inner.Width);

            Rectangle worldIconBox = new(leftX - 2, startY - 4, 22, 22);
            if (worldIconAsset?.Value != null)
                sb.Draw(worldIconAsset.Value, worldIconBox, Color.White);

            Item timeItem = new(ItemID.GoldWatch);
            Item weatherItem = new(ItemID.Umbrella);
            Item moonItem = new(ItemID.Moondial);
            Item windItem = new(ItemID.Feather);

            string[] leftRows =
            [
                $"World Name: {Main.worldName}",
                $"Seed: {seedText}",
                $"Size: {sizeText}",
                $"Difficulty: {difficultyText}",
                $"Evil Type: {evilText}",
                $"Surface Layer Ft: {surfaceText}",
                $"Caverns Layer Ft: {cavernText}"
            ];

            string[] rightLabels =
            [
                "Time",
                "Weather",
                "Moon Phase",
                "Wind",
                "World Infection %"
            ];

            string[] rightValues =
            [
                timeText,
                weatherText,
                moonText,
                windText,
                infectionText
            ];

            for (int i = 0; i < leftRows.Length; i++)
            {
                int y = startY + i * rowStep;
                Vector2 pos = new(leftX + (i == 0 ? 28f : 0f), y);

                if (i == 3 || i == 4)
                {
                    int split = leftRows[i].IndexOf(':');
                    string label = leftRows[i][..(split + 2)];
                    string value = leftRows[i][(split + 2)..];
                    Utils.DrawBorderString(sb, label, pos, Color.White, 0.72f);
                    Utils.DrawBorderString(sb, value, pos + new Vector2(FontAssets.MouseText.Value.MeasureString(label).X * 0.72f, 0f), i == 3 ? difficultyColor : evilColor, 0.72f);
                }
                else
                {
                    Utils.DrawBorderString(sb, leftRows[i], pos, Color.White, 0.72f);
                }
            }

            for (int i = 0; i < rightLabels.Length; i++)
            {
                int y = startY + i * rowStep;
                Vector2 iconPos = new(rightLabelX - 10, y + 8);

                if (i == 0)
                    ItemSlot.DrawItemIcon(timeItem, ItemSlot.Context.InventoryItem, sb, iconPos, 0.5f, 20, Color.White);
                else if (i == 1)
                    ItemSlot.DrawItemIcon(weatherItem, ItemSlot.Context.InventoryItem, sb, iconPos, 0.5f, 20, Color.White);
                else if (i == 2)
                    ItemSlot.DrawItemIcon(moonItem, ItemSlot.Context.InventoryItem, sb, iconPos, 0.5f, 20, Color.White);
                else if (i == 3)
                    ItemSlot.DrawItemIcon(windItem, ItemSlot.Context.InventoryItem, sb, iconPos, 0.5f, 20, Color.White);

                Utils.DrawBorderString(sb, rightLabels[i], new Vector2(rightLabelX + 8, y), Color.White, 0.72f);
                Utils.DrawBorderString(sb, rightValues[i], new Vector2(rightValueX, y), Color.White, 0.72f);
            }
        }));

        sectionList.Add(new SpectatorWorldSection("Boss Information", 226f, static (_, sb, box) =>
        {
            Rectangle inner = new(box.X + 12, box.Y + 34, box.Width - 24, box.Height - 44);

            const float textScale = 0.78f;
            const int slotSize = 38;
            const int slotStep = 46;

            int textX = inner.X + 6;
            int separatorX = inner.X + inner.Width / 2;
            int gridX = separatorX + 18;
            int gridY = inner.Y + 4;
            int gridColumns = Math.Max(1, (inner.Right - gridX + 8) / slotStep);

            int bossAlive = WorldStatsHelper.CountActiveBosses();
            var bosses = WorldStatsHelper.GetBossEntries();
            Texture2D checkTexture = Ass.Icon_CheckmarkGreen.Value;

            Utils.DrawBorderString(sb, $"Total Boss Damage: TBD", new Vector2(textX, inner.Y + 4), Color.White, textScale);
            Utils.DrawBorderString(sb, $"Current Bosses Alive: {bossAlive}", new Vector2(textX, inner.Y + 22), Color.White, textScale);

            WorldStatsHelper.DrawColumnSeparator(sb, inner, separatorX, 0, 0);
            WorldStatsHelper.DrawRowSeparator(sb, inner.X, inner.Y + 50, separatorX - inner.X - 8);

            for (int i = 0; i < bosses.Length; i++)
            {
                int col = i % gridColumns;
                int row = i / gridColumns;
                Rectangle slot = new(gridX + col * slotStep, gridY + row * slotStep, slotSize, slotSize);

                Utils.DrawInvBG(sb, slot, new Color(83, 97, 168) * 0.80f);

                int headNpc = WorldStatsHelper.GetBossHeadNpcId(bosses[i].NpcId);
                if (headNpc >= 0 && headNpc < NPCID.Sets.BossHeadTextures.Length && NPCID.Sets.BossHeadTextures[headNpc] != -1)
                    Main.BossNPCHeadRenderer.DrawWithOutlines(null, NPCID.Sets.BossHeadTextures[headNpc], slot.Center.ToVector2(), bosses[i].Downed ? Color.White : Color.White * 0.25f, 0f, 0.78f, SpriteEffects.None);

                if (bosses[i].Downed)
                    sb.Draw(checkTexture, slot.Center.ToVector2(), null, Color.White, 0f, checkTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);

                if (slot.Contains(Main.MouseScreen.ToPoint()))
                {
                    Main.LocalPlayer.mouseInterface = true;
                    Main.instance.MouseText($"{bosses[i].Name} ({(bosses[i].Downed ? "Downed" : "Not downed")})");
                }
            }
        }));

        sectionList.Add(new SpectatorWorldSection("Events Information", 214f, static (_, sb, box) =>
        {
            Rectangle inner = new(box.X + 12, box.Y + 34, box.Width - 24, box.Height - 44);
            int leftX = inner.X + 6;
            int separatorX = inner.X + inner.Width / 2;
            int rightX = separatorX + 18;

            string invasionText = WorldStatsHelper.GetActiveInvasionText();
            var events = WorldStatsHelper.GetEventEntries();

            string[] eventLines =
            [
                $"Active Invasion: {invasionText}",
                $"Goblin Army Defeated: {(NPC.downedGoblins ? "1" : "0")}",
                $"Frost Legion Defeated: {(NPC.downedFrost ? "1" : "0")}",
                $"Pirate Invasion Defeated: {(NPC.downedPirates ? "1" : "0")}",
                $"Martian Madness Defeated: {(NPC.downedMartians ? "1" : "0")}",
                $"Pumpkin Moon Defeated: {(NPC.downedHalloweenKing || NPC.downedHalloweenTree ? "1" : "0")}",
                $"Frost Moon Defeated: {(NPC.downedChristmasIceQueen || NPC.downedChristmasSantank || NPC.downedChristmasTree ? "1" : "0")}",
                $"Old One's Army T1: {(DD2Event.DownedInvasionT1 ? "1" : "0")}",
                $"Old One's Army T2: {(DD2Event.DownedInvasionT2 ? "1" : "0")}",
                $"Old One's Army T3: {(DD2Event.DownedInvasionT3 ? "1" : "0")}"
            ];

            for (int i = 0; i < eventLines.Length; i++)
                Utils.DrawBorderString(sb, eventLines[i], new Vector2(leftX, inner.Y + 4 + i * 16), Color.White, 0.66f);

            WorldStatsHelper.DrawColumnSeparator(sb, inner, separatorX, 0, 0);

            for (int i = 0; i < events.Length; i++)
            {
                int col = i % 4;
                int row = i / 4;
                Rectangle slot = new(rightX + col * 46, inner.Y + row * 46, 38, 38);

                Utils.DrawInvBG(sb, slot, new Color(83, 97, 168) * 0.80f);

                Item item = new(events[i].ItemId);
                ItemSlot.DrawItemIcon(item, ItemSlot.Context.InventoryItem, sb, slot.Center.ToVector2(), 0.85f, 28, Color.White);

                if (slot.Contains(Main.MouseScreen.ToPoint()))
                {
                    Main.LocalPlayer.mouseInterface = true;
                    Main.instance.MouseText($"{events[i].Name} ({(events[i].Downed ? "Active / Done" : "Inactive")})");
                }
            }
        }));

        sectionList.Add(new SpectatorWorldSection("Players Information", 118f, static (_, sb, box) =>
        {
            Rectangle inner = new(box.X + 12, box.Y + 34, box.Width - 24, box.Height - 44);
            int leftX = inner.X + 6;
            int rightX = inner.X + 370;

            int activePlayers = WorldStatsHelper.CountActivePlayers();
            WorldStatsHelper.GetPlayerSummary(out int totalKills, out int totalDeaths, out string topFragger);

            Utils.DrawBorderString(sb, $"Active Player Count: {activePlayers}", new Vector2(leftX, inner.Y + 4), Color.White, 0.72f);
            Utils.DrawBorderString(sb, $"Total Kills: {totalKills}", new Vector2(leftX, inner.Y + 24), Color.White, 0.72f);
            Utils.DrawBorderString(sb, $"Total Deaths: {totalDeaths}", new Vector2(rightX, inner.Y + 4), Color.White, 0.72f);
            Utils.DrawBorderString(sb, $"Top Fragger: {topFragger}", new Vector2(rightX, inner.Y + 24), Color.White, 0.72f);
        }));

        sectionList.Add(new SpectatorWorldSection("Town NPCs", 226f, static (_, sb, box) =>
        {
            Rectangle inner = new(box.X + 12, box.Y + 34, box.Width - 24, box.Height - 44);
            int leftX = inner.X + 6;
            int separatorX = inner.X + inner.Width / 2;
            int gridX = separatorX + 18;
            int gridY = inner.Y + 4;
            int gridColumns = Math.Max(1, (inner.Right - gridX + 8) / 46);

            List<NPC> townNpcs = WorldStatsHelper.GetTownNpcs();
            int housed = WorldStatsHelper.CountHousedTownNpcs(townNpcs);

            Utils.DrawBorderString(sb, $"Town NPCs In World: {townNpcs.Count}", new Vector2(leftX, inner.Y + 4), Color.White, 0.72f);
            Utils.DrawBorderString(sb, $"Town NPCs With Housing: {housed}", new Vector2(leftX, inner.Y + 24), Color.White, 0.72f);

            WorldStatsHelper.DrawColumnSeparator(sb, inner, separatorX, 0, 0);
            WorldStatsHelper.DrawRowSeparator(sb, inner.X, inner.Y + 50, separatorX - inner.X - 8);

            for (int i = 0; i < townNpcs.Count; i++)
            {
                int col = i % gridColumns;
                int row = i / gridColumns;
                Rectangle slot = new(gridX + col * 46, gridY + row * 46, 38, 38);

                Utils.DrawInvBG(sb, slot, new Color(83, 97, 168) * 0.80f);

                Texture2D texture = TextureAssets.Npc[townNpcs[i].type].Value;
                int frameCount = Main.npcFrameCount[townNpcs[i].type] <= 0 ? 1 : Main.npcFrameCount[townNpcs[i].type];
                Rectangle frame = texture.Frame(1, frameCount, 0, 0);
                float scale = Math.Min((slot.Width - 8f) / frame.Width, (slot.Height - 8f) / frame.Height);
                sb.Draw(texture, slot.Center.ToVector2(), frame, Color.White, 0f, frame.Size() * 0.5f, scale, SpriteEffects.None, 0f);

                if (slot.Contains(Main.MouseScreen.ToPoint()))
                {
                    Main.LocalPlayer.mouseInterface = true;
                    Main.instance.MouseText(townNpcs[i].FullName);
                }
            }
        }));

        sectionList.Add(new SpectatorWorldSection("Misc Information", 118f, static (_, sb, box) =>
        {
            Rectangle inner = new(box.X + 12, box.Y + 34, box.Width - 24, box.Height - 44);
            int leftX = inner.X + 6;
            int rightX = inner.X + 370;

            Item mostValuable = WorldStatsHelper.GetMostValuableItem();
            string mostValuableText = mostValuable == null ? "None" : mostValuable.Name;
            string bedText = WorldStatsHelper.GetBedsPerTeamText();

            Utils.DrawBorderString(sb, $"Most Valuable Item: {mostValuableText}", new Vector2(leftX, inner.Y + 4), Color.White, 0.72f);
            Utils.DrawBorderString(sb, "Items Crafted: TBD", new Vector2(leftX, inner.Y + 24), Color.White, 0.72f);
            Utils.DrawBorderString(sb, $"Active Beds Per Team: {bedText}", new Vector2(rightX, inner.Y + 4), Color.White, 0.72f);
            Utils.DrawBorderString(sb, "Potions Used: TBD", new Vector2(rightX, inner.Y + 24), Color.White, 0.72f);
        }));

        sectionList.Recalculate();
        Recalculate();
    }
}