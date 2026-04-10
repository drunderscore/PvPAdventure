using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Spectator.Drawers;
using PvPAdventure.Common.Spectator.UI.Players;
using PvPAdventure.Common.Spectator.UI.State;
using PvPAdventure.Core.Utilities;
using PvPAdventure.UI;
using ReLogic.Content;
using System;
using System.Text;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI.NPCs;

internal sealed class SpectatorNPCEntry : UIBrowserEntry
{
    private readonly NPC npc;
    private readonly UIElement listChrome;
    private readonly UIText buttonLabel;
    private bool initialized;
    private bool needsLateLayout = true;
    private string hoveredStatText;

    public SpectatorNPCEntry(NPC targetNpc) : base()
    {
        npc = targetNpc ?? new NPC();
        SearchText = BuildSearchText();

        listChrome = new UIElement();
        listChrome.Width.Set(0f, 1f);
        listChrome.Height.Set(0f, 1f);
        Append(listChrome);

        float right = -8f;

        buttonLabel = new UIText("", 0.8f)
        {
            HAlign = 1f,
            IgnoresMouseInteraction = true
        };
        buttonLabel.Left.Set(-180f, 0f);
        buttonLabel.Top.Set(7f, 0f);
        listChrome.Append(buttonLabel);

        AddTopRightButton(Ass.ButtonTeleport, ref right, "Teleport", OnTeleportClicked);
        AddTopRightButton(Ass.ButtonEye, ref right, "Spectate", OnSpectateClicked);

        buttonLabel.Left.Set(right - 4f, 0f);

        initialized = true;
        ApplyLayout();
    }

    public override void SetListMode(bool value)
    {
        listMode = value;

        if (!initialized)
            return;

        ApplyLayout();
    }

    public override void SetEntrySize(int size)
    {
        entrySize = size;

        if (!initialized)
            return;

        ApplyLayout();
    }

    private void ApplyLayout()
    {
        if (listMode)
        {
            Width.Set(0f, 1f);
            Height.Set(entrySize, 0f);

            if (listChrome.Parent is null)
                Append(listChrome);

        }
        else
        {
            Width.Set(entrySize, 0f);
            Height.Set(entrySize, 0f);

            listChrome.Remove();
            buttonLabel.SetText("");
        }

        Recalculate();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        ApplyLateLayout();

        if (IsMouseHovering)
            Main.LocalPlayer.mouseInterface = true;
    }

    private void ApplyLateLayout()
    {
        if (!needsLateLayout || Parent == null || GetDimensions().Width <= 0f)
            return;

        ApplyLayout();
        listChrome.Recalculate();
        buttonLabel.Recalculate();
        Recalculate();

        needsLateLayout = false;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        Rectangle box = GetDimensions().ToRectangle();
        hoveredStatText = null;

        Utils.DrawInvBG(spriteBatch, box, Color.Black * 0.35f);
        BackgroundDrawer.DrawMapFullscreenBackground(spriteBatch, box, npc, listMode);
        Utils.DrawInvBG(spriteBatch, box, IsMouseHovering ? new Color(73, 94, 171, 185) : new Color(63, 82, 151, 145));

        if (listMode)
        {
            NPCDrawer.DrawFullNPC(spriteBatch, npc, new Rectangle(box.X + 4, box.Y + 4, box.Height - 8, box.Height - 8));
            DrawListMode(spriteBatch, box);
        }
        else
            DrawGridMode(spriteBatch, box);

        if (!string.IsNullOrEmpty(hoveredStatText))
            UICommon.TooltipMouseText(hoveredStatText);
    }

    private void DrawListMode(SpriteBatch spriteBatch, Rectangle box)
    {
        int previewWidth = box.Height - 8;
        Rectangle area = new(box.X + 4 + previewWidth + 5, box.Y + 30, box.Width - previewWidth - 22, box.Height - 50);
        if (area.Width <= 0 || area.Height <= 0)
            return;

        hoveredStatText = StatDrawer.DrawNPCListStats(spriteBatch, area, BuildStats(skipNpcHead: true)) ?? hoveredStatText;
    }

    private void DrawGridMode(SpriteBatch spriteBatch, Rectangle box)
    {
        const int outerPadding = 6;
        const int statSpacing = 2;
        const int statHeight = 27;
        int availableHeight = box.Height - outerPadding * 2;
        int totalRows = Math.Max(0, (availableHeight + statSpacing) / (statHeight + statSpacing));
        if (totalRows <= 0)
            return;

        Rectangle headStatBox = new(box.X + outerPadding, box.Y + outerPadding, box.Width - outerPadding * 2, statHeight);
        DrawNPCHeadStat(spriteBatch, headStatBox);

        int statRows = Math.Max(0, totalRows - 1);
        if (statRows <= 0)
            return;

        int top = headStatBox.Bottom + statSpacing;
        Rectangle statArea = new(box.X + outerPadding, top, box.Width - outerPadding * 2, box.Bottom - outerPadding - top);
        int columns = StatDrawer.GetGridColumns(statArea);
        DrawStatGrid(spriteBatch, statArea, BuildStats(skipNpcHead: true), columns, statRows, statHeight, statSpacing);
    }

    private void DrawNPCHeadStat(SpriteBatch spriteBatch, Rectangle area)
    {
        hoveredStatText = NPCDrawer.DrawNPCHeadStat(spriteBatch, area, npc) ?? hoveredStatText;
    }

    private void DrawStatGrid(SpriteBatch spriteBatch, Rectangle area, NPCStatSnapshot[] stats, int columns, int rows, int statHeight, int statSpacing)
    {
        if (rows <= 0 || columns <= 0 || stats.Length == 0)
            return;

        int panelWidth = (area.Width - statSpacing * (columns - 1)) / columns;
        int count = Math.Min(stats.Length, columns * rows);
        Point mouse = Main.MouseScreen.ToPoint();

        for (int i = 0; i < count; i++)
        {
            int column = i % columns;
            int row = i / columns;
            Rectangle panel = new(area.X + column * (panelWidth + statSpacing), area.Y + row * (statHeight + statSpacing), panelWidth, statHeight);

            StatDrawer.DrawNPCStat(spriteBatch, panel, stats[i]);

            if (panel.Contains(mouse))
                hoveredStatText = stats[i].HoverText;
        }
    }

    private void AddTopRightButton(Asset<Texture2D> texture, ref float rightOffset, string label, UIElement.MouseEvent click = null)
    {
        UIImageButton button = new(texture) { HAlign = 1f };
        button.Top.Set(4f, 0f);
        button.Left.Set(rightOffset, 0f);
        button.OnMouseOver += (_, _) => buttonLabel.SetText(label);
        button.OnMouseOut += (_, _) => buttonLabel.SetText("");

        if (click != null)
            button.OnLeftClick += click;

        listChrome.Append(button);
        rightOffset -= 24f;
    }

    private NPCStatSnapshot[] BuildStats(bool skipNpcHead)
    {
        int start = skipNpcHead ? 1 : 0;
        NPCStatSnapshot[] stats = new NPCStatSnapshot[NPCStats.All.Count - start];

        for (int i = 0; i < stats.Length; i++)
            stats[i] = NPCStats.All[i + start].Build(npc);

        return stats;
    }

    private string BuildSearchText()
    {
        StringBuilder text = new();
        text.Append(npc.FullName);
        text.Append(' ');
        text.Append(npc.whoAmI);
        text.Append(' ');
        text.Append(npc.type);

        for (int i = 0; i < NPCStats.All.Count; i++)
        {
            NPCStatSnapshot stat = NPCStats.All[i].Build(npc);
            text.Append(' ');
            text.Append(stat.Text);
        }

        return text.ToString();
    }

    private void OnSpectateClicked(UIMouseEvent evt, UIElement listeningElement)
    {
        if (npc is null || !npc.active)
            return;

        Player localPlayer = Main.LocalPlayer;
        NPC currentTarget = SpectatorSystem.GetNPCTarget();

        if (SpectatorSystem.IsInSpectateMode(localPlayer) &&
            SpectatorSystem.GetCurrentTargetKind() == SpectatorTargetKind.NPC &&
            currentTarget != null &&
            currentTarget.whoAmI == npc.whoAmI)
        {
            localPlayer.GetModPlayer<SpectatorPlayer>().ClearTarget();
            SpectatorUISystem.ToggleNpcSpectatorControls();
            Log.Chat($"Stopped spectating {npc.FullName}");
            return;
        }

        if (!SpectatorSystem.IsInSpectateMode(localPlayer))
            SpectatorSystem.RequestSetLocalMode(PlayerMode.Spectator);

        SpectatorSystem.SetNPCTarget(npc.whoAmI);
        SpectatorUISystem.EnsureNpcSpectatorControlsOpen();

        Log.Chat($"Now spectating {npc.FullName}");
    }

    private void OnTeleportClicked(UIMouseEvent evt, UIElement listeningElement)
    {
        if (npc is null || !npc.active)
            return;

        Player localPlayer = Main.LocalPlayer;
        Vector2 telePos = npc.Center - new Vector2(localPlayer.width, localPlayer.height) * 0.5f;

        if (Main.netMode == NetmodeID.SinglePlayer)
            localPlayer.Teleport(telePos, TeleportationStyleID.RodOfDiscord);
        else if (Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 2, Main.LocalPlayer.whoAmI, telePos.X, telePos.Y, TeleportationStyleID.PotionOfReturn);

        Log.Chat($"Teleported to {npc.FullName}");
    }
}
