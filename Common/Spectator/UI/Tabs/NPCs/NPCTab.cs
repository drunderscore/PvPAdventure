using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI.Tabs.NPCs;

internal sealed class NPCTab : UIElement, ISpectatorTab
{
    private readonly UIBrowser browser;

    public SpectatorTab Tab => SpectatorTab.NPCs;
    public string HeaderText => "NPCs";
    public string TooltipText => "Spectate NPCs";
    public Asset<Texture2D> Icon => Ass.Icon_NPC;
    public string ActionHoverText => null;
    public bool HasAction => false;
    private readonly List<(int WhoAmI, int Type, string Name)> npcSnapshot = [];

    public NPCTab()
    {
        Width.Set(0f, 1f);
        Height.Set(0f, 1f);
        SetPadding(0f);

        browser = new UIBrowser(PopulateEntries, GetSorts, GetFilters, GetHintText);
        browser.Width.Set(0f, 1f);
        browser.Height.Set(0f, 1f);

        Append(browser);
    }

    public void Refresh()
    {
        RememberNPCList();
        browser.Rebuild();
    }

    public void OnAction()
    {
    }

    public override void Update(Microsoft.Xna.Framework.GameTime gameTime)
    {
        base.Update(gameTime);
        RefreshIfNPCListChanged();
    }

    private void PopulateEntries(List<UIBrowserEntry> entries)
    {
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];

            if (ShouldShowNPC(npc))
                entries.Add(new SpectatorNPCEntry(npc));
        }
    }

    private static List<UIBrowserFilter> GetFilters()
    {
        int guideHeadIndex = NPC.TypeToDefaultHeadIndex(NPCID.Guide);

        return
        [
            new("TownNPCs", "Filter Town NPCs", TextureAssets.NpcHead[guideHeadIndex], entry => entry is SpectatorNPCEntry npcEntry && npcEntry.NPC.townNPC),
            new("Bosses", "Filter Bosses", TextureAssets.Item[ItemID.SuspiciousLookingEye], entry => entry is SpectatorNPCEntry npcEntry && npcEntry.NPC.boss)
        ];
    }

    private static string GetHintText(int count)
    {
#if !DEBUG
    if (count <= 1) return "";
#endif
        return $"Search {count} NPCs...";
    }

    private void RefreshIfNPCListChanged()
    {
        List<(int WhoAmI, int Type, string Name)> current = [];
        BuildNPCSnapshot(current);

        if (MatchesNPCSnapshot(current))
            return;

        npcSnapshot.Clear();
        npcSnapshot.AddRange(current);
        browser.Rebuild();
    }

    private void RememberNPCList()
    {
        BuildNPCSnapshot(npcSnapshot);
    }

    private void BuildNPCSnapshot(List<(int WhoAmI, int Type, string Name)> snapshot)
    {
        snapshot.Clear();

        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];

            if (ShouldShowNPC(npc))
                snapshot.Add((npc.whoAmI, npc.type, npc.FullName));
        }
    }

    private bool MatchesNPCSnapshot(List<(int WhoAmI, int Type, string Name)> current)
    {
        if (current.Count != npcSnapshot.Count)
            return false;

        for (int i = 0; i < current.Count; i++)
        {
            if (!current[i].Equals(npcSnapshot[i]))
                return false;
        }

        return true;
    }

    private static bool ShouldShowNPC(NPC npc)
    {
        return npc?.active == true;
    }

    private List<UIBrowserSort> GetSorts()
    {
        return
        [
            new("Sort A-Z", static (a, b) =>
        {
            SpectatorNPCEntry left = (SpectatorNPCEntry)a;
            SpectatorNPCEntry right = (SpectatorNPCEntry)b;
            return string.Compare(left.NPC.FullName, right.NPC.FullName, StringComparison.OrdinalIgnoreCase);
        }),

        new("Sort Z-A", static (a, b) =>
        {
            SpectatorNPCEntry left = (SpectatorNPCEntry)a;
            SpectatorNPCEntry right = (SpectatorNPCEntry)b;
            return string.Compare(right.NPC.FullName, left.NPC.FullName, StringComparison.OrdinalIgnoreCase);
        })
        ];
    }
}