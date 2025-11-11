using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace PvPAdventure.Core.Features.AdventureTeleport;

public class AdventureTeleportState : UIState
{
    private UIPanel _panel;
    private UIText _title;
    private UITextPanel<string> _btnRandom;
    private UITextPanel<string> _btnTeammates;
    private UITextPanel<string> _btnBeds;

    public bool IsMouseOverUI { get; private set; }

    // Team menu 
    private UIList _teammateList;
    private bool _teammatesOpen;
    private int _selectedPlayerId = -1;

    private const float RowHeight = 28f;
    private const float RowPadding = 4f;
    private const float ListTop = 146f;
    private const float PanelInnerPadding = 10f; // left/right padding inside _panel

    public override void OnInitialize()
    {
        _panel = new UIPanel();
        _panel.Width.Set(420f, 0f);
        _panel.Height.Set(180f, 0f);
        _panel.Top.Set(20f, 0f);
        _panel.HAlign = 0.5f;
        Append(_panel);

        _title = new UIText("PvP Adventure Teleport");
        _title.HAlign = 0.5f;
        _title.Top.Set(8f, 0f);
        _panel.Append(_title);

        _btnRandom = MakeButton("Random Teleport", 44f);
        _btnRandom.OnLeftClick += (_, __) => RequestRandomTeleport();

        _btnTeammates = MakeButton("Teleport to Teammate", 78f);
        _btnTeammates.OnLeftClick += (_, __) => ToggleTeammatesMenu();

        _btnBeds = MakeButton("Teleport to Bed", 112f);
        _btnBeds.OnLeftClick += (_, __) => OpenBedsMenu();

        _panel.Append(_btnRandom);
        _panel.Append(_btnTeammates);
        _panel.Append(_btnBeds);
    }

    private UITextPanel<string> MakeButton(string label, float top)
    {
        var b = new UITextPanel<string>(label, 0.9f, large: false);
        b.Width.Set(-20f, 1f);
        b.Left.Set(10f, 0f);
        b.Top.Set(top, 0f);
        b.BackgroundColor = new Color(63, 82, 151) * 0.8f;
        b.BorderColor = new Color(89, 116, 213) * 0.8f;
        b.OnMouseOver += (evt, element) => b.BackgroundColor = new Color(73, 92, 161);
        b.OnMouseOut += (evt, element) => b.BackgroundColor = new Color(63, 82, 151) * 0.8f;
        return b;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        // Update mouse over state
        IsMouseOverUI = _panel.IsMouseHovering
                 || _btnRandom.IsMouseHovering
                 || _btnTeammates.IsMouseHovering
                 || _btnBeds.IsMouseHovering
                 || (_teammateList?.IsMouseHovering ?? false);
    }

    private void RequestRandomTeleport()
    {
        SoundEngine.PlaySound(SoundID.Item6); // MAGIC MIRROR SOUND

        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            NetMessage.SendData(MessageID.RequestTeleportationByServer);
        }
        else
        {
            Main.LocalPlayer.TeleportationPotion();
        }

        CloseAndExitMap();
    }

    private void ToggleTeammatesMenu()
    {
        _teammatesOpen = !_teammatesOpen;

        // Remove previous list if exists
        _teammateList?.Remove();
        _teammateList = null;

        // If closing, restore compact panel height and reset Beds button
        if (!_teammatesOpen)
        {
            _panel.Height.Set(180f, 0f);
            _btnBeds.Top.Set(112f, 0f);
            _btnBeds.Recalculate();
            SoundEngine.PlaySound(SoundID.MenuClose);
            return;
        }
        SoundEngine.PlaySound(SoundID.MenuOpen);

        // Build a list
        _teammateList = new UIList
        {
            ListPadding = RowPadding,
            OverflowHidden = true
        };
        _teammateList.Left.Set(PanelInnerPadding, 0f);
        _teammateList.Top.Set(ListTop-RowHeight, 0f);
        _teammateList.Width.Set(-PanelInnerPadding * 2f, 1f); // fill width minus padding
        _panel.Append(_teammateList);

        // Gather teammates on same team (excluding self)
        int myId = Main.myPlayer;
        var me = Main.player[myId];
        int myTeam = me.team;

        var teammates = new List<Player>();
        if (myTeam != 0)
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                var p = Main.player[i];
                if (p != null && p.active && i != myId && p.team == myTeam)
                    teammates.Add(p);
            }
        }

        // If none, show yourself as a single entry instead
        if (teammates.Count == 0)
        {
            _teammateList.Add(MakeTeammateRow(me, isSelf: true));
        }
        else
        {
            foreach (var p in teammates)
                _teammateList.Add(MakeTeammateRow(p, isSelf: false));
        }

        int rowCount = Math.Max(1, teammates.Count);
        float listHeight = rowCount * RowHeight + (rowCount - 1) * RowPadding + 6f;

        _teammateList.Height.Set(listHeight, 0f);
        _teammateList.Recalculate();

        // Beds button just below list
        float bedsTop = ListTop + listHeight + 8f;
        _btnBeds.Top.Set(bedsTop, 0f);
        _btnBeds.Recalculate();
    }

    private UIElement MakeTeammateRow(Player p, bool isSelf)
    {
        var row = new UIElement { OverflowHidden = true };
        row.OnMouseOver += (_, _) => { SoundEngine.PlaySound(SoundID.MenuTick); };
        row.Width.Set(0f, 1f);
        row.Height.Set(RowHeight, 0f);

        var bg = new UIPanel();
        bg.Left.Set(0f, 0f);
        bg.Top.Set(0f, 0f);
        bg.Width.Set(0f, 1f);
        bg.Height.Set(RowHeight, 0f);
        bg.PaddingTop = bg.PaddingLeft = bg.PaddingRight = bg.PaddingBottom = 0;
        bg.BackgroundColor = new Color(63, 82, 151) * 0.65f;
        bg.BorderColor = new Color(89, 116, 213) * 0.90f;

        // 24x24 head at left
        var head = new PlayerHeadElement(p, isSelf);
        head.Left.Set(2f, 0f);
        head.Top.Set(0f, 0f); // 2px padding inside 28px row
        bg.Append(head);

        // Name text
        var name = new UIText(isSelf ? $"You ({p?.name ?? "Player"})" : (p?.name ?? "Player"), 0.9f);
        name.Left.Set(36, 0f);
        name.Top.Set(5f, 0f);
        name.TextColor = Color.White;

        row.OnMouseOver += (_, __) => { name.TextColor = Color.Yellow; };
        row.OnMouseOut += (_, __) => { name.TextColor = Color.White; };

        row.OnLeftClick += (_, __) =>
        {
            if (isSelf)
            {
                CloseAndExitMap();
                return;
            }
            if (p == null) return;

            Main.LocalPlayer.UnityTeleport(p.Center);
            CloseAndExitMap();
        };

        bg.Append(name);
        row.Append(bg);
        return row;
    }

    private void OpenBedsMenu()
    {
        Main.NewText("Bed menu is still in construction (WIP)!");
    }

    private void CloseAndExitMap()
    {
        Main.mapFullscreen = false;
        AdventureTeleportStateSettings.SetIsEnabled(false);
    }
}
