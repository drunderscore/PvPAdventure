using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Events;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI.Tabs.World;


internal sealed class WorldTab : UIElement, ISpectatorTab
{
    public SpectatorTab Tab => SpectatorTab.World;
    public string HeaderText => "World";
    public string TooltipText => "World stats";
    public Asset<Texture2D> Icon => Ass.Icon_World;

    public WorldTab()
    {
        Width.Set(0f, 1f);
        Height.Set(0f, 1f);
        SetPadding(0f);
    }

    public void Refresh() => Build();

    private void Build()
    {
        RemoveAllChildren();

        UIScrollbar scrollbar = new();
        scrollbar.Left.Set(-28f, 1f);
        scrollbar.Top.Set(14f, 0f);
        scrollbar.Height.Set(-66f, 1f);
        Append(scrollbar);

        UIList sectionList = new()
        {
            ListPadding = 12f,
            ManualSortMethod = _ => { }
        };

        sectionList.Top.Set(10f, 0f);
        sectionList.Left.Set(12f, 0f);
        sectionList.Width.Set(-44f, 1f);
        sectionList.Height.Set(-30f, 1f);
        sectionList.SetScrollbar(scrollbar);
        Append(sectionList);

        AddSections(sectionList);

        sectionList.Recalculate();
        Recalculate();
    }

    private static void AddSections(UIList sectionList)
    {
        sectionList.Clear();

        sectionList.Add(new SpectatorWorldSection("World info", 222f, static (_, sb, box) => DrawWorldInformation(sb, box)));
        sectionList.Add(new SpectatorWorldSection("Bosses defeated", 226f, static (_, sb, box) => DrawBossInformation(sb, box)));

        // Keep these commented out for now (see Unused region).
        //sectionList.Add(new SpectatorWorldSection("Town NPCs", 226f, static (_, sb, box) => DrawTownNPCs(sb, box)));
        //sectionList.Add(new SpectatorWorldSection("Events Information", 214f, static (_, sb, box) => DrawEventsInformation(sb, box)));
        //sectionList.Add(new SpectatorWorldSection("Players Information", 118f, static (_, sb, box) => DrawPlayersInformation(sb, box)));
        //sectionList.Add(new SpectatorWorldSection("Misc Information", 118f, static (_, sb, box) => DrawMiscInformation(sb, box)));

        sectionList.Recalculate();
    }

    private static void DrawWorldInformation(SpriteBatch sb, Rectangle box)
    {
        Rectangle inner = Inner(box);
        int separatorX = inner.X + inner.Width / 2;
        int leftX = inner.X + 6;
        int rightX = separatorX + 18;
        int startY = inner.Y + 4;

        const int rowHeight = 30;
        const int rowStep = 34;

        //DrawShipBackground(sb, new Rectangle(separatorX - 12, inner.Y, inner.Right - separatorX + 12, inner.Height));
        DrawColumnSeparator(sb, inner, separatorX, 0, 0);

        //StatDrawer.DrawWorldStatPanel(sb, new Rectangle(leftX, startY + 0 * rowStep, separatorX - leftX - 18, rowHeight), WorldInfoHelper.GetWorldIcon(), WorldInfoHelper.GetNameText(), WorldInfoHelper.GetNameText());
        StatDrawer.DrawWorldStatPanel(sb, new Rectangle(leftX, startY + 0 * rowStep, separatorX - leftX - 18, rowHeight), WorldInfoHelper.GetWorldSignTexture(), WorldInfoHelper.GetNameText(), WorldInfoHelper.GetNameText());
        StatDrawer.DrawWorldStatPanel(sb, new Rectangle(leftX, startY + 1 * rowStep, separatorX - leftX - 18, rowHeight), WorldInfoHelper.GetWorldSizeIcon(), WorldInfoHelper.GetWorldSizeText(), WorldInfoHelper.GetWorldSizeText());
        StatDrawer.DrawWorldStatPanel(sb, new Rectangle(leftX, startY + 2 * rowStep, separatorX - leftX - 18, rowHeight), WorldInfoHelper.GetWorldDifficultyIcon(), WorldInfoHelper.GetDifficultyText(), WorldInfoHelper.GetDifficultyText(), textColor: WorldInfoHelper.GetDifficultyColor());
        StatDrawer.DrawWorldStatPanel(sb, new Rectangle(leftX, startY + 3 * rowStep, separatorX - leftX - 18, rowHeight), WorldInfoHelper.GetWorldEvilIcon(), WorldInfoHelper.GetEvilText(), WorldInfoHelper.GetEvilText(), textColor: WorldInfoHelper.GetEvilColor());
        StatDrawer.DrawWorldStatPanel(sb, new Rectangle(leftX, startY + 4 * rowStep, separatorX - leftX - 18, rowHeight), WorldInfoHelper.GetWorldSeedIcon(), WorldInfoHelper.GetSeedText(), WorldInfoHelper.GetSeedText());

        StatDrawer.DrawWorldStatPanel(sb, new Rectangle(rightX, startY + 0 * rowStep, inner.Right - rightX - 6, rowHeight), TextureAssets.InfoIcon[InfoDisplay.Watches.Type].Value, WorldInfoHelper.GetTimeText(), WorldInfoHelper.GetTimeText(), iconSize: 14);
        StatDrawer.DrawWorldStatPanel(sb, new Rectangle(rightX, startY + 1 * rowStep, inner.Right - rightX - 6, rowHeight), TextureAssets.InfoIcon[InfoDisplay.WeatherRadio.Type].Value, WorldInfoHelper.GetWeatherText(), WorldInfoHelper.GetWeatherText(), iconSize: 14);
        StatDrawer.DrawWorldStatPanel(sb, new Rectangle(rightX, startY + 2 * rowStep, inner.Right - rightX - 6, rowHeight), WorldInfoHelper.GetSextantIcon(), WorldInfoHelper.GetMoonText(), WorldInfoHelper.GetMoonText(), iconSize: 14);
    }

    private static void DrawBossInformation(SpriteBatch sb, Rectangle box)
    {
        Rectangle inner = Inner(box);
        int separatorX = inner.X + inner.Width / 2;
        int gridX = separatorX + 18;
        int gridY = inner.Y + 4;
        int gridColumns = Math.Max(1, (inner.Right - gridX + 8) / 46);
        var bosses = WorldBossInfoHelper.GetBossEntries();
        Texture2D checkTexture = Ass.Icon_CheckmarkGreen.Value;

        //StatDrawer.DrawWorldStatPanel(sb, new Rectangle(inner.X + 6, inner.Y + 4, separatorX - inner.X - 24, 30), ItemID.LifeformAnalyzer, $"Bosses Defeated: {WorldBossInfoHelper.GetBossesDefeatedText()}", "Bosses Defeated:");
        StatDrawer.DrawWorldStatPanel(sb, new Rectangle(inner.X + 6, inner.Y + 4, separatorX - inner.X - 24, 30), Ass.Icon_CheckmarkGreen.Value, $"Bosses Defeated: {WorldBossInfoHelper.GetBossesDefeatedText()}", "Bosses Defeated:", iconSize: 18);

        DrawColumnSeparator(sb, inner, separatorX, 0, 0);

        for (int i = 0; i < bosses.Length; i++)
        {
            int col = i % gridColumns;
            int row = i / gridColumns;
            Rectangle slot = new(gridX + col * 46, gridY + row * 46, 38, 38);

            Utils.DrawInvBG(sb, slot, new Color(83, 97, 168) * 0.80f);

            int headNpc = WorldBossInfoHelper.GetBossHeadNpcId(bosses[i].NpcId);
            if (headNpc >= 0 && headNpc < NPCID.Sets.BossHeadTextures.Length && NPCID.Sets.BossHeadTextures[headNpc] != -1)
                Main.BossNPCHeadRenderer.DrawWithOutlines(null, NPCID.Sets.BossHeadTextures[headNpc], slot.Center.ToVector2(), bosses[i].Downed ? Color.White : Color.White * 0.25f, 0f, 0.78f, SpriteEffects.None);

            if (bosses[i].Downed)
                sb.Draw(checkTexture, slot.Center.ToVector2(), null, Color.White, 0f, checkTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);

            ShowHover(slot, $"{bosses[i].Name} ({(bosses[i].Downed ? "Downed" : "Not downed")})");
        }
    }

    #region Helpers

    private static void DrawColumnSeparator(SpriteBatch sb, Rectangle inner, int x, int topOffset = 0, int bottomOffset = 0)
    {
        sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(x, inner.Y + topOffset, 2, inner.Height - topOffset - bottomOffset), Color.White * 0.08f);
    }

    private static void DrawRowSeparator(SpriteBatch sb, int x, int y, int width)
    {
        sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(x, y, width, 2), Color.White * 0.08f);
    }

    //private static void DrawShipBackground(SpriteBatch sb, Rectangle area)
    //{
    //    if (Ass.BG_Ship == null || area.Width <= 0 || area.Height <= 0)
    //        return;

    //    Texture2D texture = Ass.BG_Ship.Value;
    //    if (texture == null)
    //        return;

    //    int sourceWidth = Math.Min(texture.Width, area.Width);
    //    int sourceHeight = Math.Min(texture.Height, area.Height);
    //    Rectangle source = new((texture.Width - sourceWidth) / 2, (texture.Height - sourceHeight) / 2, sourceWidth, sourceHeight);
    //    Rectangle destination = new(area.Right - sourceWidth, area.Y, sourceWidth, sourceHeight);

    //    sb.Draw(texture, destination, source, Color.White * 0.22f);

    //    int fadeWidth = Math.Min(140, destination.Width);
    //    for (int i = 0; i < fadeWidth; i++)
    //    {
    //        float progress = i / (float)Math.Max(1, fadeWidth - 1);
    //        Color fadeColor = Color.Black * (1f - progress) * 0.85f;
    //        sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(destination.X + i, destination.Y, 1, destination.Height), fadeColor);
    //    }
    //}

    private static void ShowHover(Rectangle area, string text)
    {
        if (!area.Contains(Main.MouseScreen.ToPoint()))
            return;

        Main.LocalPlayer.mouseInterface = true;
        Main.instance.MouseText(text);
    }

    private static Rectangle Inner(Rectangle box)
    {
        return new Rectangle(box.X + 12, box.Y + 34, box.Width - 24, box.Height - 44);
    }
    #endregion

    #region Unused sections
    //private static void DrawEventsInformation(SpriteBatch sb, Rectangle box)
    //{
    //    Rectangle inner = Inner(box);
    //    int leftX = inner.X + 6;
    //    int separatorX = inner.X + inner.Width / 2;
    //    int rightX = separatorX + 18;
    //    var events = WorldStatsHelper.GetEventEntries();

    //    string[] eventLines =
    //    [
    //        $"Active Invasion: {WorldStatsHelper.GetActiveInvasionText()}",
    //            $"Goblin Army Defeated: {(NPC.downedGoblins ? "1" : "0")}",
    //            $"Frost Legion Defeated: {(NPC.downedFrost ? "1" : "0")}",
    //            $"Pirate Invasion Defeated: {(NPC.downedPirates ? "1" : "0")}",
    //            $"Martian Madness Defeated: {(NPC.downedMartians ? "1" : "0")}",
    //            $"Pumpkin Moon Defeated: {(NPC.downedHalloweenKing || NPC.downedHalloweenTree ? "1" : "0")}",
    //            $"Frost Moon Defeated: {(NPC.downedChristmasIceQueen || NPC.downedChristmasSantank || NPC.downedChristmasTree ? "1" : "0")}",
    //            $"Old One's Army T1: {(DD2Event.DownedInvasionT1 ? "1" : "0")}",
    //            $"Old One's Army T2: {(DD2Event.DownedInvasionT2 ? "1" : "0")}",
    //            $"Old One's Army T3: {(DD2Event.DownedInvasionT3 ? "1" : "0")}"
    //    ];

    //    for (int i = 0; i < eventLines.Length; i++)
    //        Utils.DrawBorderString(sb, eventLines[i], new Vector2(leftX, inner.Y + 4 + i * 16), Color.White, 0.66f);

    //    WorldStatsHelper.DrawColumnSeparator(sb, inner, separatorX, 0, 0);

    //    for (int i = 0; i < events.Length; i++)
    //    {
    //        int col = i % 4;
    //        int row = i / 4;
    //        Rectangle slot = new(rightX + col * 46, inner.Y + row * 46, 38, 38);

    //        Utils.DrawInvBG(sb, slot, new Color(83, 97, 168) * 0.80f);
    //        Item item = new(events[i].ItemId);
    //        ItemSlot.DrawItemIcon(item, ItemSlot.Context.InventoryItem, sb, slot.Center.ToVector2(), 0.85f, 28, Color.White);
    //        ShowHover(slot, $"{events[i].Name} ({(events[i].Downed ? "Active / Done" : "Inactive")})");
    //    }
    //}

    //private static void DrawPlayersInformation(SpriteBatch sb, Rectangle box)
    //{
    //    Rectangle inner = Inner(box);
    //    WorldStatsHelper.GetPlayerSummary(out int totalKills, out int totalDeaths, out string topFragger);

    //    DrawStatRow(sb, new Rectangle(inner.X + 6, inner.Y + 4, 280, 30), 0, $"Active Player Count: {WorldStatsHelper.CountActivePlayers()}", "Players currently active in the world", Color.White);
    //    DrawStatRow(sb, new Rectangle(inner.X + 6, inner.Y + 38, 280, 30), 0, $"Total Kills: {totalKills}", "Total player kills", Color.White);
    //    DrawStatRow(sb, new Rectangle(inner.X + 310, inner.Y + 4, 280, 30), 0, $"Total Deaths: {totalDeaths}", "Total player deaths", Color.White);
    //    DrawStatRow(sb, new Rectangle(inner.X + 310, inner.Y + 38, 280, 30), 0, $"Top Fragger: {topFragger}", "Player with the most kills", Color.White);
    //}

    //private static void DrawTownNPCs(SpriteBatch sb, Rectangle box)
    //{
    //    Rectangle inner = Inner(box);
    //    int separatorX = inner.X + inner.Width / 2;
    //    int gridX = separatorX + 18;
    //    int gridColumns = Math.Max(1, (inner.Right - gridX + 8) / 46);
    //    List<NPC> townNpcs = WorldStatsHelper.GetTownNpcs();

    //    DrawStatRow(sb, new Rectangle(inner.X + 6, inner.Y + 4, separatorX - inner.X - 24, 30), 0, $"Town NPCs In World: {townNpcs.Count}", "Town NPCs currently alive in the world", Color.White);
    //    DrawStatRow(sb, new Rectangle(inner.X + 6, inner.Y + 38, separatorX - inner.X - 24, 30), 0, $"Town NPCs With Housing: {WorldStatsHelper.CountHousedTownNpcs(townNpcs)}", "Town NPCs assigned to valid housing", Color.White);

    //    WorldStatsHelper.DrawColumnSeparator(sb, inner, separatorX, 0, 0);
    //    WorldStatsHelper.DrawRowSeparator(sb, inner.X, inner.Y + 82, separatorX - inner.X - 8);

    //    for (int i = 0; i < townNpcs.Count; i++)
    //    {
    //        int col = i % gridColumns;
    //        int row = i / gridColumns;
    //        Rectangle slot = new(gridX + col * 46, inner.Y + 4 + row * 46, 38, 38);

    //        NPC npc = townNpcs[i];
    //        Texture2D texture = TextureAssets.Npc[npc.type].Value;
    //        int frameCount = Main.npcFrameCount[npc.type] <= 0 ? 1 : Main.npcFrameCount[npc.type];
    //        Rectangle frame = texture.Frame(1, frameCount, 0, 0);
    //        float scale = Math.Min((slot.Width - 8f) / frame.Width, (slot.Height - 8f) / frame.Height);

    //        Utils.DrawInvBG(sb, slot, new Color(83, 97, 168) * 0.80f);
    //        sb.Draw(texture, slot.Center.ToVector2(), frame, Color.White, 0f, frame.Size() * 0.5f, scale, SpriteEffects.None, 0f);
    //        ShowHover(slot, npc.FullName);
    //    }
    //}

    //private static void DrawMiscInformation(SpriteBatch sb, Rectangle box)
    //{
    //    Rectangle inner = Inner(box);
    //    Item mostValuable = WorldStatsHelper.GetMostValuableItem();

    //    DrawStatRow(sb, new Rectangle(inner.X + 6, inner.Y + 4, 280, 30), 0, $"Most Valuable Item: {(mostValuable == null ? "None" : mostValuable.Name)}", "Most valuable item currently detected", Color.White);
    //    DrawStatRow(sb, new Rectangle(inner.X + 6, inner.Y + 38, 280, 30), 0, "Items Crafted: TBD", "Total items crafted", Color.White);
    //    DrawStatRow(sb, new Rectangle(inner.X + 310, inner.Y + 4, 280, 30), 0, $"Active Beds Per Team: {WorldStatsHelper.GetBedsPerTeamText()}", "Active spawn beds grouped by team", Color.White);
    //    DrawStatRow(sb, new Rectangle(inner.X + 310, inner.Y + 38, 280, 30), 0, "Potions Used: TBD", "Total potions used", Color.White);
    //}
    #endregion
}