using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Spectator.Drawers;
using PvPAdventure.Common.Spectator.SpectatorMode;
using PvPAdventure.Core.Utilities;
using ReLogic.Graphics;
using System;
using System.Text;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI.Tabs.Players;

internal sealed class SpectatorPlayerEntry : SpectatorEntityEntry
{
    private readonly Player player;
    private static Player inventoryPlayer;
    protected override string EntityName => player.name;

    private readonly PlayerStatSnapshot[] cachedStats;
    private readonly PlayerStatSnapshot[] cachedStatsWithoutHead;
    private ulong cachedStatsUpdate = ulong.MaxValue;

    public Player Player => player;
    public int TeamSortValue => player.team == 0 ? int.MaxValue : player.team;

    public SpectatorPlayerEntry(Player targetPlayer)
    {
        player = targetPlayer ?? new Player();
        SearchText = BuildSearchText();

        cachedStats = new PlayerStatSnapshot[PlayerStats.All.Count];
        cachedStatsWithoutHead = new PlayerStatSnapshot[Math.Max(0, PlayerStats.All.Count - 1)];

        float left = 0f;
        AddEntityButton(TextureAssets.Item[ItemID.TeleportationPotion], ref left, "Teleport", OnTeleportClicked);
        AddEntityButton(Ass.Icon_Eye, ref left, "Spectate", OnSpectateClicked, IsSpectating);
        AddEntityButton(TextureAssets.Item[ItemID.PiggyBank], ref left, "Inventory", OnInventoryClicked, IsInventoryOpen);

        FinishSetup();
    }

    protected override void DrawListPreview(SpriteBatch sb, Rectangle area) => EntityDrawer.DrawPlayerPreview(sb, player, area);

    protected override string DrawListStats(SpriteBatch sb, Rectangle area) => StatDrawer.DrawPlayerListStats(sb, area, BuildStats(skipPlayerHead: true));

    protected override string DrawHeadStat(SpriteBatch sb, Rectangle area) => EntityDrawer.DrawPlayerHeadStat(sb, area, player);

    protected override string DrawGridStats(SpriteBatch sb, Rectangle area, int columns, int rows, int statHeight, int statSpacing) =>
        StatDrawer.DrawPlayerStatGrid(sb, area, BuildStats(skipPlayerHead: true), columns, rows, statHeight, statSpacing);

    private bool IsInventoryOpen()
    {
        return SpectatorInventoryOverlay.IsOpen(player);
    }

    private bool IsSpectating()
    {
        return SpectatorTargetSystem.IsTargeting(player);
    }

    private void OnInventoryClicked(UIMouseEvent evt, UIElement listeningElement)
    {
        SpectatorInventoryOverlay.Toggle(player);
    }

    internal static void DrawSelectedInventory(SpriteBatch spriteBatch)
    {
        if (inventoryPlayer?.active != true)
        {
            inventoryPlayer = null;
            return;
        }

        Rectangle viewport = new(0, 0, Main.screenWidth, Main.screenHeight);
        InventoryDrawer.DrawInventory(spriteBatch, new Vector2(20f, 20f), inventoryPlayer, viewport);
        InventoryDrawer.DrawEquipment(spriteBatch, inventoryPlayer, viewport);
    }

    internal static void ClearSelectedInventory()
    {
        inventoryPlayer = null;
    }

    private PlayerStatSnapshot[] BuildStats(bool skipPlayerHead)
    {
        if (cachedStatsUpdate != Main.GameUpdateCount)
        {
            for (int i = 0; i < PlayerStats.All.Count; i++)
                cachedStats[i] = PlayerStats.All[i].Build(player);

            for (int i = 0; i < cachedStatsWithoutHead.Length; i++)
                cachedStatsWithoutHead[i] = cachedStats[i + 1];

            cachedStatsUpdate = Main.GameUpdateCount;
        }

        return skipPlayerHead ? cachedStatsWithoutHead : cachedStats;
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
            text.Append(' ');
            text.Append(stat.Text);
        }

        return text.ToString();
    }

    private void OnSpectateClicked(UIMouseEvent evt, UIElement listeningElement)
    {
        if (player?.active != true)
            return;

        Player local = Main.LocalPlayer;

        if (local?.active != true || player.whoAmI == local.whoAmI)
            return;

        if (IsSpectating())
        {
            SpectatorTargetSystem.ClearTarget();
            Log.Chat("Stopped spectating " + player.name);
            return;
        }

        SpectatorTargetSystem.SetPlayerTarget(player.whoAmI);
        SpectatorUISystem.EnsurePlayerSpectatorControlsOpen();

        Log.Chat($"Now spectating {player.name}");
    }

    private void OnTeleportClicked(UIMouseEvent evt, UIElement listeningElement)
    {
        if (player?.active != true)
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