using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Spectator.Drawers;
using PvPAdventure.Common.Spectator.Hooks;
using PvPAdventure.Common.Spectator.SpectatorMode;
using PvPAdventure.Common.Spectator.UI.Tabs;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI.Tabs.World;

internal sealed class SpectatorWorldTab : UIElement, ISpectatorTab
{
    public SpectatorTab Tab => SpectatorTab.World;
    public string HeaderText => "World";
    public string TooltipText => "World stats";
    public Asset<Texture2D> Icon => Ass.Icon_World;

    public SpectatorWorldTab()
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
        scrollbar.Left.Set(-24f, 1f);
        scrollbar.Top.Set(14f, 0f);
        scrollbar.Height.Set(-66f, 1f);
        Append(scrollbar);

        UIList sectionList = new()
        {
            ListPadding = 12f,
            ManualSortMethod = _ => { }
        };

        sectionList.Top.Set(10f, 0f);
        sectionList.Left.Set(8f, 0f);
        sectionList.Width.Set(-36f, 1f);
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
        sectionList.Add(new SpectatorSettingsInfoSection());
        sectionList.Add(new SpectatorWorldSection("World info", 314f, static (_, sb, box) => DrawWorldInformation(sb, box)));
        sectionList.Add(new SpectatorWorldSection("Bosses defeated", 226f, static (_, sb, box) => DrawBossInformation(sb, box)));
        sectionList.Recalculate();
    }

    private static void DrawWorldInformation(SpriteBatch sb, Rectangle box)
    {
        Rectangle inner = Inner(box);
        int leftX = inner.X + 3;
        int width = inner.Width - 6;
        int startY = inner.Y + 4;

        const int rowHeight = 30;
        const int rowStep = 34;

        StatDrawer.DrawWorldStatPanel(sb, new Rectangle(leftX, startY + 0 * rowStep, width, rowHeight), WorldInfoHelper.GetWorldSignTexture(), WorldInfoHelper.GetNameText(), WorldInfoHelper.GetNameText());
        StatDrawer.DrawWorldStatPanel(sb, new Rectangle(leftX, startY + 1 * rowStep, width, rowHeight), WorldInfoHelper.GetWorldSizeIcon(), WorldInfoHelper.GetWorldSizeText(), WorldInfoHelper.GetWorldSizeText());
        StatDrawer.DrawWorldStatPanel(sb, new Rectangle(leftX, startY + 2 * rowStep, width, rowHeight), WorldInfoHelper.GetWorldDifficultyIcon(), WorldInfoHelper.GetDifficultyText(), WorldInfoHelper.GetDifficultyText(), textColor: WorldInfoHelper.GetDifficultyColor());
        StatDrawer.DrawWorldStatPanel(sb, new Rectangle(leftX, startY + 3 * rowStep, width, rowHeight), WorldInfoHelper.GetWorldEvilIcon(), WorldInfoHelper.GetEvilText(), WorldInfoHelper.GetEvilText(), textColor: WorldInfoHelper.GetEvilColor());
        StatDrawer.DrawWorldStatPanel(sb, new Rectangle(leftX, startY + 4 * rowStep, width, rowHeight), WorldInfoHelper.GetWorldSeedIcon(), WorldInfoHelper.GetSeedText(), WorldInfoHelper.GetSeedText());
        StatDrawer.DrawWorldStatPanel(sb, new Rectangle(leftX, startY + 5 * rowStep, width, rowHeight), TextureAssets.Item[ItemID.GoldWatch].Value, WorldInfoHelper.GetTimeText(), WorldInfoHelper.GetTimeText(), iconSize: 18);
        StatDrawer.DrawWorldStatPanel(sb, new Rectangle(leftX, startY + 6 * rowStep, width, rowHeight), TextureAssets.Item[ItemID.WeatherRadio].Value, WorldInfoHelper.GetWeatherText(), WorldInfoHelper.GetWeatherText(), iconSize: 18);
        StatDrawer.DrawWorldStatPanel(sb, new Rectangle(leftX, startY + 7 * rowStep, width, rowHeight), WorldInfoHelper.GetSextantIcon(), WorldInfoHelper.GetMoonText(), WorldInfoHelper.GetMoonText(), iconSize: 18);
    }

    private static void DrawBossInformation(SpriteBatch sb, Rectangle box)
    {
        Rectangle inner = Inner(box);
        int gridX = inner.X + 3;
        int gridY = inner.Y + 44;
        int gridColumns = Math.Max(1, (inner.Right - gridX + 8) / 46);
        WorldBossInfoHelper.BossEntry[] bosses = WorldBossInfoHelper.GetBossEntries();
        Texture2D checkTexture = Ass.Icon_CheckmarkGreen.Value;

        StatDrawer.DrawWorldStatPanel(sb, new Rectangle(inner.X + 3, inner.Y + 4, inner.Width - 6, 30), Ass.Icon_CheckmarkGreen.Value, $"Bosses Defeated: {WorldBossInfoHelper.GetBossesDefeatedText()}", "Bosses Defeated:");

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

    private static void DrawColumnSeparator(SpriteBatch sb, Rectangle inner, int x, int topOffset = 0, int bottomOffset = 0)
    {
        sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(x, inner.Y + topOffset, 2, inner.Height - topOffset - bottomOffset), Color.White * 0.08f);
    }

    private static void ShowHover(Rectangle area, string text)
    {
        if (!area.Contains(Main.MouseScreen.ToPoint()))
            return;

        Main.LocalPlayer.mouseInterface = true;
        Main.instance.MouseText(text);
    }

    private static Rectangle Inner(Rectangle box)
    {
        return new Rectangle(box.X + 6, box.Y + 34, box.Width - 12, box.Height - 44);
    }
}

internal sealed class SpectatorWorldSection : UIPanel
{
    private readonly string title;
    private readonly Action<SpectatorWorldSection, SpriteBatch, Rectangle> drawContent;

    public SpectatorWorldSection(string title, float height, Action<SpectatorWorldSection, SpriteBatch, Rectangle> drawContent)
    {
        this.title = title;
        this.drawContent = drawContent;

        Width.Set(0f, 1f);
        Height.Set(height, 0f);
        SetPadding(0f);
        BackgroundColor = new Color(28, 36, 76) * 0.92f;
        BorderColor = new Color(116, 154, 255) * 0.75f;
    }

    protected override void DrawSelf(SpriteBatch sb)
    {
        base.DrawSelf(sb);

        Rectangle box = GetDimensions().ToRectangle();
        sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(box.X + 10, box.Y + 28, box.Width - 20, 2), Color.White * 0.10f);
        Utils.DrawBorderString(sb, title, new Vector2(box.X + 10, box.Y + 6), new Color(255, 228, 140), 0.9f);
        drawContent?.Invoke(this, sb, box);
    }
}

internal sealed class SpectatorSettingsInfoSection : UIPanel
{
    private const float HeaderHeight = 34f;
    private const float RowHeight = 28f;

    private readonly SettingField[] rows;

    public SpectatorSettingsInfoSection()
    {
        rows = GetRows();

        Width.Set(0f, 1f);
        Height.Set(HeaderHeight + rows.Length * RowHeight + 10f, 0f);
        SetPadding(0f);
        BackgroundColor = new Color(28, 36, 76) * 0.92f;
        BorderColor = new Color(116, 154, 255) * 0.75f;

        for (int i = 0; i < rows.Length; i++)
        {
            SettingTextRow row = new(rows[i]);
            row.Top.Set(HeaderHeight + i * RowHeight, 0f);
            row.Width.Set(0f, 1f);
            row.Height.Set(RowHeight, 0f);
            Append(row);
        }
    }

    protected override void DrawSelf(SpriteBatch sb)
    {
        base.DrawSelf(sb);

        Rectangle box = GetDimensions().ToRectangle();
        sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(box.X + 10, box.Y + 28, box.Width - 20, 2), Color.White * 0.10f);
        Utils.DrawBorderString(sb, "Spectator Settings", new Vector2(box.X + 10, box.Y + 6), new Color(255, 228, 140), 0.9f);
    }

    private static SettingField[] GetRows()
    {
        return
        [
            new("Players online", () => SpectatorModeSystem.GetPlayersOnlineCount().ToString()),
            new("Spectators", () => SpectatorModeSystem.GetSpectatorCount().ToString()),
            new("Fullbright", () => OnOff(FloodlightSpectatorSystem.Enabled), () => FloodlightSpectatorSystem.Enabled = !FloodlightSpectatorSystem.Enabled),
            new("Reveal Map", () => OnOff(MapRevealHelper.Revealed), () => MapRevealHelper.SetRevealed(!MapRevealHelper.Revealed)),
            new("Draw Players", () => SpectatorClientSettings.DrawPlayersLabel, SpectatorClientSettings.CycleDrawPlayers),
            new(
                "Player Cards",
                () => SpectatorControlsPanel.ShownPlayerCardCount.ToString(),
                () => SpectatorControlsPanel.ChangeShownPlayerCards(1),
                () => SpectatorControlsPanel.ChangeShownPlayerCards(-1),
                "Left click: to increase\nRight click to decrease"),
            new("Auto Director", () => OnOff(AutoDirectorSystem.Enabled), () => AutoDirectorSystem.Enabled = !AutoDirectorSystem.Enabled)
        ];
    }

    private static string OnOff(bool value) => value ? "On" : "Off";

    private readonly struct SettingField
    {
        public readonly string Label;
        public readonly Func<string> GetValue;
        public readonly Action OnLeftClick;
        public readonly Action OnRightClick;
        public readonly string Tooltip;

        public SettingField(string label, Func<string> getValue, Action onLeftClick = null, Action onRightClick = null, string tooltip = null)
        {
            Label = label;
            GetValue = getValue;
            OnLeftClick = onLeftClick;
            OnRightClick = onRightClick;
            Tooltip = tooltip;
        }
    }

    private sealed class SettingTextRow : UIElement
    {
        private readonly SettingField field;
        private readonly UIText text;

        public SettingTextRow(SettingField field)
        {
            this.field = field;

            text = new UIText("", textScale: 0.85f)
            {
                HAlign = 0f,
                VAlign = 0.5f,
                Left = new StyleDimension(10f, 0f),
                TextColor = Color.Gray
            };

            Append(text);

            if (field.OnLeftClick is not null)
                OnLeftClick += (_, _) => field.OnLeftClick();

            if (field.OnRightClick is not null)
                OnRightClick += (_, _) => field.OnRightClick();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            text.SetText($"{field.Label}: {field.GetValue()}");
            text.TextColor = IsMouseHovering ? Color.White : Color.Gray;

            if (IsMouseHovering)
            {
                Main.LocalPlayer.mouseInterface = true;

                if (!string.IsNullOrEmpty(field.Tooltip))
                    Main.instance.MouseText(field.Tooltip);
            }
        }
    }
}

internal static class WorldInfoHelper
{
    public static Texture2D GetWorldSignTexture()
    {
        return Main.Assets.Request<Texture2D>("Images/UI/WorldCreation/IconRandomName").Value;
    }

    public static string GetNameText()
    {
        return $"Name: {Main.worldName}";
    }

    public static Texture2D GetWorldSizeIcon()
    {
        string path = Main.maxTilesX switch
        {
            <= 4200 => "Images/UI/WorldCreation/IconSizeSmall",
            <= 6400 => "Images/UI/WorldCreation/IconSizeMedium",
            _ => "Images/UI/WorldCreation/IconSizeLarge"
        };

        return Main.Assets.Request<Texture2D>(path).Value;
    }

    public static string GetWorldSizeText()
    {
        if (Main.maxTilesX <= 4200)
            return "Size: Small";

        if (Main.maxTilesX <= 6400)
            return "Size: Medium";

        if (Main.maxTilesX <= 8400)
            return "Size: Large";

        return "Size: Custom";
    }

    public static Texture2D GetWorldDifficultyIcon()
    {
        string path = Main.GameMode switch
        {
            1 => "Images/UI/WorldCreation/IconDifficultyExpert",
            2 => "Images/UI/WorldCreation/IconDifficultyMaster",
            3 => "Images/UI/WorldCreation/IconDifficultyCreative",
            _ => "Images/UI/WorldCreation/IconDifficultyNormal"
        };

        return Main.Assets.Request<Texture2D>(path).Value;
    }

    public static string GetDifficultyText()
    {
        int mode = GetEffectiveDifficultyMode();

        if (mode == 0)
            return "Difficulty: Classic";

        if (mode == 1)
            return "Difficulty: Expert";

        if (mode == 2)
            return "Difficulty: Master";

        if (mode == 3)
            return "Difficulty: Journey";

        if (mode == 4)
            return "Difficulty: Legendary";

        return $"Difficulty: Mode {mode}";
    }

    public static Color GetDifficultyColor()
    {
        int mode = GetEffectiveDifficultyMode();

        if (mode == 1)
            return Main.mcColor;

        if (mode == 2)
            return Main.hcColor;

        if (mode == 3)
            return Main.creativeModeColor;

        if (mode == 4)
            return Main.hcColor;

        return Color.White;
    }

    public static Texture2D GetWorldEvilIcon()
    {
        return Main.Assets.Request<Texture2D>(WorldGen.crimson ? "Images/UI/WorldCreation/IconEvilCrimson" : "Images/UI/WorldCreation/IconEvilCorruption").Value;
    }

    public static string GetEvilText()
    {
        return $"Evil: {(WorldGen.crimson ? "Crimson" : "Corruption")}";
    }

    public static Color GetEvilColor()
    {
        return WorldGen.crimson ? new Color(255, 120, 120) : new Color(170, 120, 255);
    }

    public static Texture2D GetWorldSeedIcon()
    {
        return Main.Assets.Request<Texture2D>("Images/UI/WorldCreation/IconRandomSeed").Value;
    }

    public static string GetSeedText()
    {
        string seed = Main.ActiveWorldFileData?.SeedText;
        return $"Seed: {(string.IsNullOrWhiteSpace(seed) ? "-" : seed)}";
    }

    public static string GetTimeText()
    {
        string amPm = Language.GetTextValue("GameUI.TimeAtMorning");
        double time = Main.time;
        if (!Main.dayTime)
            time += 54000.0;

        time = time / 86400.0 * 24.0;
        time = time - 7.5 - 12.0;
        if (time < 0.0)
            time += 24.0;

        if (time >= 12.0)
            amPm = Language.GetTextValue("GameUI.TimePastMorning");

        int hoursString = (int)time;
        double secondRemainder = time - hoursString;
        secondRemainder = (int)(secondRemainder * 60.0);
        string minutesString = secondRemainder.ToString();
        if (secondRemainder < 10.0)
            minutesString = "0" + minutesString;

        if (hoursString > 12)
            hoursString -= 12;

        if (hoursString == 0)
            hoursString = 12;

        return Language.GetTextValue("CLI.Time", hoursString + ":" + minutesString + " " + amPm);
    }

    public static string GetWeatherText()
    {
        string name = GetWeatherName();
        int wind = (int)Math.Round(Math.Abs(Main.windSpeedCurrent) * 60f);
        string direction = Main.windSpeedCurrent >= 0f ? "E" : "W";

        return $"Weather: {name} ({wind} mph {direction})";
    }

    public static Texture2D GetSextantIcon()
    {
        int index = (Main.bloodMoon && !Main.dayTime) || (Main.eclipse && Main.dayTime) ? 8 : 7;
        return TextureAssets.InfoIcon[index].Value;
    }

    public static string GetMoonText()
    {
        string name = Main.moonPhase switch
        {
            0 => "Full Moon",
            1 => "Waning Gibbous",
            2 => "Third Quarter",
            3 => "Waning Crescent",
            4 => "New Moon",
            5 => "Waxing Crescent",
            6 => "First Quarter",
            _ => "Waxing Gibbous"
        };

        return $"Moon Phase: {name} ({Main.moonPhase + 1}/8)";
    }

    private static int GetEffectiveDifficultyMode()
    {
        int modeNumber = Main.GameMode;

        if (Main.getGoodWorld && modeNumber > 0)
            modeNumber++;

        return modeNumber;
    }

    private static string GetWeatherName()
    {
        if (Main.maxRaining >= 0.6f)
            return "Heavy Rain";

        if (Main.maxRaining >= 0.2f)
            return "Rain";

        if (Main.maxRaining > 0f)
            return "Light Rain";

        if (Main.cloudAlpha >= 0.8f)
            return "Overcast";

        if (Main.cloudAlpha >= 0.6f)
            return "Mostly Cloudy";

        if (Main.cloudAlpha >= 0.35f)
            return "Cloudy";

        if (Main.cloudAlpha >= 0.15f)
            return "Partly Cloudy";

        return "Clear";
    }
}

internal static class WorldBossInfoHelper
{
    public readonly record struct BossEntry(int NpcId, string Name, bool Downed);

    public static BossEntry[] GetBossEntries()
    {
        int evilBossId;
        string evilBossName;

        if (WorldGen.crimson)
        {
            evilBossId = NPCID.BrainofCthulhu;
            evilBossName = "Brain of Cthulhu";
        }
        else
        {
            evilBossId = NPCID.EaterofWorldsHead;
            evilBossName = "Eater of Worlds";
        }

        return
        [
            new BossEntry(NPCID.KingSlime, "King Slime", NPC.downedSlimeKing),
            new BossEntry(NPCID.EyeofCthulhu, "Eye of Cthulhu", NPC.downedBoss1),
            new BossEntry(evilBossId, evilBossName, NPC.downedBoss2),
            new BossEntry(NPCID.QueenBee, "Queen Bee", NPC.downedQueenBee),
            new BossEntry(NPCID.SkeletronHead, "Skeletron", NPC.downedBoss3),
            new BossEntry(NPCID.WallofFlesh, "Wall of Flesh", Main.hardMode),
            new BossEntry(NPCID.QueenSlimeBoss, "Queen Slime", NPC.downedQueenSlime),
            new BossEntry(NPCID.TheDestroyer, "The Destroyer", NPC.downedMechBoss1),
            new BossEntry(NPCID.Retinazer, "The Twins", NPC.downedMechBoss2),
            new BossEntry(NPCID.SkeletronPrime, "Skeletron Prime", NPC.downedMechBoss3),
            new BossEntry(NPCID.Plantera, "Plantera", NPC.downedPlantBoss),
            new BossEntry(NPCID.Golem, "Golem", NPC.downedGolemBoss),
            new BossEntry(NPCID.DukeFishron, "Duke Fishron", NPC.downedFishron),
            new BossEntry(NPCID.CultistBoss, "Lunatic Cultist", NPC.downedAncientCultist),
            new BossEntry(NPCID.MoonLordCore, "Moon Lord", NPC.downedMoonlord)
        ];
    }

    public static int GetBossHeadNpcId(int npcId)
    {
        return npcId == NPCID.Golem ? NPCID.GolemHead : npcId;
    }

    public static string GetBossesDefeatedText()
    {
        int defeated = 0;
        int total = 18;

        defeated += NPC.downedBoss1 ? 1 : 0;
        defeated += NPC.downedBoss2 ? 1 : 0;
        defeated += NPC.downedQueenBee ? 1 : 0;
        defeated += NPC.downedBoss3 ? 1 : 0;
        defeated += Main.hardMode ? 1 : 0;
        defeated += NPC.downedMechBoss1 ? 1 : 0;
        defeated += NPC.downedMechBoss2 ? 1 : 0;
        defeated += NPC.downedMechBoss3 ? 1 : 0;
        defeated += NPC.downedPlantBoss ? 1 : 0;
        defeated += NPC.downedGolemBoss ? 1 : 0;
        defeated += NPC.downedFishron ? 1 : 0;
        defeated += NPC.downedAncientCultist ? 1 : 0;
        defeated += NPC.downedMoonlord ? 1 : 0;
        defeated += NPC.downedSlimeKing ? 1 : 0;
        defeated += NPC.downedQueenSlime ? 1 : 0;
        defeated += NPC.downedEmpressOfLight ? 1 : 0;
        defeated += NPC.downedDeerclops ? 1 : 0;
        defeated += NPC.downedTowerSolar && NPC.downedTowerVortex && NPC.downedTowerNebula && NPC.downedTowerStardust ? 1 : 0;

        return $"{defeated}/{total}";
    }
}
