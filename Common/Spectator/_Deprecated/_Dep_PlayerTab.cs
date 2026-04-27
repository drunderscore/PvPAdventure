//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;
//using Microsoft.Xna.Framework.Input;
//using PvPAdventure.Common.Spectator.UI;
//using PvPAdventure.Common.Spectator.UI.Tabs;
//using PvPAdventure.Common.Spectator.UI.Tabs.Players;
//using PvPAdventure.Core.Utilities;
//using ReLogic.Content;
//using System;
//using System.Collections.Generic;
//using Terraria;
//using Terraria.GameContent;
//using Terraria.UI;

//namespace PvPAdventure.Common.Spectator._Deprecated;

//internal sealed class PlayerTab : UIElement, ISpectatorTab
//{
//    private readonly List<Player> debugPlayers = [];
//    private readonly UIBrowser browser;

//    public SpectatorTab Tab => SpectatorTab.Player;
//    public string HeaderText => "Players";
//    public string TooltipText => "Spectate players";
//    public Asset<Texture2D> Icon => Ass.Icon_Player;
//    private readonly List<(int WhoAmI, string Name, int Team, bool Ghost)> playerSnapshot = [];

//    public PlayerTab()
//    {
//        Width.Set(0f, 1f);
//        Height.Set(0f, 1f);
//        SetPadding(0f);

//        browser = new UIBrowser(PopulateEntries, GetSorts, GetFilters, GetHintText);

//        browser.Width.Set(0f, 1f);
//        browser.Height.Set(0f, 1f);

//        Append(browser);
//    }

//    private static List<UIBrowserFilter> GetFilters()
//    {
//        return
//        [
//            new("LowHealth", "Players below 50% health", TextureAssets.Heart, entry => entry is SpectatorPlayerEntry playerEntry && playerEntry.Player.statLife < playerEntry.Player.statLifeMax2 * 0.5f),
//        new("HighHealth", "Players at or above 50% health", TextureAssets.Heart2, entry => entry is SpectatorPlayerEntry playerEntry && playerEntry.Player.statLife >= playerEntry.Player.statLifeMax2 * 0.5f)
//        ];
//    }

//    public void Refresh()
//    {
//        RememberPlayerList();
//        browser.Rebuild();
//    }

//    public override void Update(GameTime gameTime)
//    {
//        base.Update(gameTime);

//#if DEBUG
//        PopulateDebugPlayers();
//#endif

//        RefreshIfPlayerListChanged();
//    }

//    private void PopulateEntries(List<UIBrowserEntry> entries)
//    {
//        for (int i = 0; i < Main.maxPlayers; i++)
//        {
//            Player player = Main.player[i];

//            if (ShouldShowPlayer(player))
//                entries.Add(new SpectatorPlayerEntry(player));
//        }

//        foreach (Player debugPlayer in debugPlayers)
//            entries.Add(new SpectatorPlayerEntry(debugPlayer));
//    }

//    private static string GetHintText(int count)
//    {
//        return $"Search {count} players...";
//    }

//    private void RefreshIfPlayerListChanged()
//    {
//        List<(int WhoAmI, string Name, int Team, bool Ghost)> current = [];
//        BuildPlayerSnapshot(current);

//        if (MatchesPlayerSnapshot(current))
//            return;

//        playerSnapshot.Clear();
//        playerSnapshot.AddRange(current);
//        browser.Rebuild();
//    }

//    private void RememberPlayerList()
//    {
//        BuildPlayerSnapshot(playerSnapshot);
//    }

//    private void BuildPlayerSnapshot(List<(int WhoAmI, string Name, int Team, bool Ghost)> snapshot)
//    {
//        snapshot.Clear();

//        for (int i = 0; i < Main.maxPlayers; i++)
//        {
//            Player player = Main.player[i];

//            if (ShouldShowPlayer(player))
//                snapshot.Add((player.whoAmI, player.name, player.team, player.ghost));
//        }

//        for (int i = 0; i < debugPlayers.Count; i++)
//            snapshot.Add((-i - 1, debugPlayers[i].name, debugPlayers[i].team, debugPlayers[i].ghost));
//    }

//    private bool MatchesPlayerSnapshot(List<(int WhoAmI, string Name, int Team, bool Ghost)> current)
//    {
//        if (current.Count != playerSnapshot.Count)
//            return false;

//        for (int i = 0; i < current.Count; i++)
//        {
//            if (!current[i].Equals(playerSnapshot[i]))
//                return false;
//        }

//        return true;
//    }

//    private static bool ShouldShowPlayer(Player player)
//    {
//        if (player?.active != true)
//            return false;

//        //if (player.ghost)
//            //return false;

//        // Skip myself but not in debug builds for testing
//#if !DEBUG
//    if (player.whoAmI == Main.myPlayer)
//        return false;
//#endif

//        return true;
//    }

//    private List<UIBrowserSort> GetSorts()
//    {
//        return
//        [
//            new("Sort A-Z", static (a, b) =>
//        {
//            SpectatorPlayerEntry left = (SpectatorPlayerEntry)a;
//            SpectatorPlayerEntry right = (SpectatorPlayerEntry)b;
//            return string.Compare(left.Player.name, right.Player.name, StringComparison.OrdinalIgnoreCase);
//        }),

//        new("Sort Z-A", static (a, b) =>
//        {
//            SpectatorPlayerEntry left = (SpectatorPlayerEntry)a;
//            SpectatorPlayerEntry right = (SpectatorPlayerEntry)b;
//            return string.Compare(right.Player.name, left.Player.name, StringComparison.OrdinalIgnoreCase);
//        })
//        ];
//    }

//    private void PopulateDebugPlayers()
//    {
//        string GetNextDebugName()
//        {
//            int count = debugPlayers.Count + 1;

//            if (count <= 9)
//            {
//                string seq = "";

//                for (int i = 1; i <= count; i++)
//                    seq += i;

//                return $"Debug{seq}";
//            }

//            return $"Debug{Main.rand.Next(10000, 99999999)}";
//        }

//        if (Main.keyState.IsKeyDown(Keys.NumPad1) && !Main.oldKeyState.IsKeyDown(Keys.NumPad1))
//        {
//            Player clonedPlayer = (Player)Main.LocalPlayer.Clone();
//            clonedPlayer.name = GetNextDebugName();

//            debugPlayers.Add(clonedPlayer);
//            Log.Chat($"Added debug player: {clonedPlayer.name}");

//            Refresh();
//        }

//        if (Main.keyState.IsKeyDown(Keys.NumPad2) && !Main.oldKeyState.IsKeyDown(Keys.NumPad2))
//        {
//            if (debugPlayers.Count <= 0)
//                return;

//            string removedName = debugPlayers[^1].name;
//            debugPlayers.RemoveAt(debugPlayers.Count - 1);

//            Log.Chat($"Removed debug player: {removedName}");

//            Refresh();
//        }
//    }
//}