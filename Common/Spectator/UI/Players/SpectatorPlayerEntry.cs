using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Core.Utils;
using PvPAdventure.Common.Spectator.Drawers;
using PvPAdventure.Common.Spectator.UI.NPCs;
using PvPAdventure.Common.Spectator.UI.State;
using PvPAdventure.Core.Utilities;
using PvPAdventure.UI;
using ReLogic.Content;
using ReLogic.Graphics;
using System;
using System.Text;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Graphics;
using Terraria.Graphics.Light;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI.Players;

internal sealed class SpectatorPlayerEntry : UIBrowserEntry
{
    private readonly Player player;
    private readonly UIElement listChrome;
    private readonly UIText buttonLabel;
    private bool needsLateLayout = true;
    private string hoveredStatText;
    public Player Player => player;

    public int TeamSortValue => player.team == 0 ? int.MaxValue : player.team;

    public int BiomeSortValue
    {
        get
        {
            int value = BiomeHelper.GetBiomeVisual(player).BackgroundIndex;
            return value < 0 ? int.MaxValue : value;
        }
    }

    public SpectatorPlayerEntry(Player targetPlayer) : base()
    {
        player = targetPlayer ?? new Player();
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

        ApplyLayout();
    }

    public override void SetListMode(bool value)
    {
        listMode = value;
        ApplyLayout();
    }

    public override void SetEntrySize(int size)
    {
        entrySize = size;
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
        {
            Main.LocalPlayer.mouseInterface = true;
        }
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
        BackgroundDrawer.DrawMapFullscreenBackground(spriteBatch, box, player, listMode);
        //Utils.DrawInvBG(spriteBatch, box, IsMouseHovering ? new Color(73, 94, 171, 50) : new Color(0,0,0,25));

        if (listMode)
        {
            PlayerDrawer.DrawFullPlayerPreview(spriteBatch, player, box);
            DrawListMode(spriteBatch, box);
        }
        else
            DrawGridMode(spriteBatch, box);

        if (!string.IsNullOrEmpty(hoveredStatText))
            UICommon.TooltipMouseText(hoveredStatText);
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
        hoveredStatText = StatDrawer.DrawPlayerHeadStat(spriteBatch, headStatBox, player) ?? hoveredStatText;

        int statRows = Math.Max(0, totalRows - 1);
        if (statRows <= 0)
            return;

        int top = headStatBox.Bottom + statSpacing;
        Rectangle statArea = new(box.X + outerPadding, top, box.Width - outerPadding * 2, box.Bottom - outerPadding - top);
        int columns = StatDrawer.GetGridColumns(statArea);

        hoveredStatText = StatDrawer.DrawPlayerStatGrid(spriteBatch, statArea, BuildStats(skipPlayerHead: true), columns, statRows, statHeight, statSpacing) ?? hoveredStatText;
    }

    private void DrawListMode(SpriteBatch spriteBatch, Rectangle box)
    {
        int previewWidth = box.Height - 8;
        Rectangle area = new(box.X + 4 + previewWidth + 5, box.Y + 30, box.Width - previewWidth - 22, box.Height - 50);
        if (area.Width <= 0 || area.Height <= 0)
            return;

        hoveredStatText = StatDrawer.DrawPlayerListStats(spriteBatch, area, BuildStats(skipPlayerHead: true));
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

    private PlayerStatSnapshot[] BuildStats(bool skipPlayerHead)
    {
        int start = skipPlayerHead ? 1 : 0;
        PlayerStatSnapshot[] stats = new PlayerStatSnapshot[PlayerStats.All.Count - start];

        for (int i = 0; i < stats.Length; i++)
            stats[i] = PlayerStats.All[i + start].Build(player);

        return stats;
    }

    private string BuildSearchText()
    {
        StringBuilder text = new();
        text.Append(player.name);
        text.Append(' ');
        text.Append(player.whoAmI);
        text.Append(' ');
        text.Append(player.team);

        for (int i = 0; i < PlayerStats.All.Count; i++)
        {
            PlayerStatSnapshot stat = PlayerStats.All[i].Build(player);
            //text.Append(' ');
            //text.Append(stat.Label);
            //text.Append(' ');
            text.Append(stat.Text);
        }

        return text.ToString();
    }

    private void OnSpectateClicked(UIMouseEvent evt, UIElement listeningElement)
    {
        if (player is null || !player.active)
            return;

        Player localPlayer = Main.LocalPlayer;
        Player currentTarget = SpectatorSystem.GetPlayerTarget();

        if (SpectatorSystem.IsInSpectateMode(localPlayer) &&
            SpectatorSystem.GetCurrentTargetKind() == SpectatorTargetKind.Player &&
            currentTarget != null &&
            currentTarget.whoAmI == player.whoAmI)
        {
            localPlayer.GetModPlayer<SpectatorPlayer>().ClearTarget();
            SpectatorUISystem.TogglePlayerSpectatorControls();
            Log.Chat($"Stopped spectating {player.name}");
            return;
        }

        if (!SpectatorSystem.IsInSpectateMode(localPlayer))
            SpectatorSystem.RequestSetLocalMode(PlayerMode.Spectator);

        SpectatorSystem.SetPlayerTarget(player.whoAmI);
        SpectatorUISystem.EnsurePlayerSpectatorControlsOpen();

        Log.Chat($"Now spectating {player.name}");
    }

    private void OnTeleportClicked(UIMouseEvent evt, UIElement listeningElement)
    {
        if (player is null || !player.active)
            return;

        Player localPlayer = Main.LocalPlayer;

        Vector2 telePos = player.Center - new Vector2(localPlayer.width, localPlayer.height) * 0.5f;

        if (Main.netMode == NetmodeID.SinglePlayer)
            localPlayer.Teleport(telePos, TeleportationStyleID.RodOfDiscord);
        else if (Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 2, Main.LocalPlayer.whoAmI, telePos.X, telePos.Y, TeleportationStyleID.PotionOfReturn);

        Log.Chat($"Teleported to {player.name}");
    }
}
