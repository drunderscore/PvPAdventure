using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Spectator.UI.Tabs;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI.Tabs.NPCs;

internal sealed class SpectatorNPCTab : UIElement, ISpectatorTab
{
    private readonly UIList npcList;
    private readonly List<(int WhoAmI, int Type, string Name)> npcSnapshot = [];

    public SpectatorTab Tab => SpectatorTab.NPCs;
    public string HeaderText => "NPCs";
    public string TooltipText => "Spectate NPCs";
    public Asset<Texture2D> Icon => Ass.Icon_NPC;

    public SpectatorNPCTab()
    {
        Width.Set(0f, 1f);
        Height.Set(0f, 1f);
        SetPadding(0f);

        UIScrollbar scrollbar = new();
        scrollbar.Left.Set(-24f, 1f);
        scrollbar.Top.Set(8f, 0f);
        scrollbar.Height.Set(-16f, 1f);
        Append(scrollbar);

        npcList = new UIList
        {
            ListPadding = 8f,
            ManualSortMethod = _ => { }
        };
        npcList.Top.Set(8f, 0f);
        npcList.Left.Set(8f, 0f);
        npcList.Width.Set(-40f, 1f);
        npcList.Height.Set(-16f, 1f);
        npcList.SetScrollbar(scrollbar);
        Append(npcList);
    }

    public void Refresh()
    {
        RememberNPCList();
        Build();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        RefreshIfNPCListChanged();
    }

    private void Build()
    {
        npcList.Clear();

        int filteredCount = CountFilteredNPCs();
        int listIndex = 0;

        npcList.Add(new NPCFilterSummaryPanel(filteredCount));

        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (!ShouldShowNPC(npc))
                continue;

            UINPCCard card = new(i, listIndex++);
            card.Width.Set(UINPCCard.CardWidth, 0f);
            card.Height.Set(UINPCCard.CardHeight, 0f);
            npcList.Add(card);
        }

        if (listIndex == 0)
        {
            UIText emptyText = new("No NPCs are available to spectate.")
            {
                TextColor = Color.LightGray
            };
            emptyText.Width.Set(0f, 1f);
            emptyText.Height.Set(40f, 0f);
            npcList.Add(emptyText);
        }

        npcList.Recalculate();
        Recalculate();
    }

    private void RefreshIfNPCListChanged()
    {
        List<(int WhoAmI, int Type, string Name)> current = [];
        BuildNPCSnapshot(current);

        if (MatchesNPCSnapshot(current))
            return;

        npcSnapshot.Clear();
        npcSnapshot.AddRange(current);
        Build();
    }

    private void RememberNPCList()
    {
        BuildNPCSnapshot(npcSnapshot);
    }

    private static void BuildNPCSnapshot(List<(int WhoAmI, int Type, string Name)> snapshot)
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

    private static int CountFilteredNPCs()
    {
        int count = 0;

        for (int i = 0; i < Main.maxNPCs; i++)
        {
            if (ShouldShowNPC(Main.npc[i]))
                count++;
        }

        return count;
    }

    private sealed class NPCFilterSummaryPanel : UIPanel
    {
        public NPCFilterSummaryPanel(int count)
        {
            Width.Set(UINPCCard.CardWidth, 0f);
            Height.Set(32f, 0f);
            SetPadding(0f);
            BackgroundColor = new Color(28, 36, 76) * 0.92f;
            BorderColor = Color.Black;

            Append(new UIText($"{count} NPCs filtered", textScale: 0.85f)
            {
                HAlign = 0.5f,
                VAlign = 0.5f,
                TextColor = Color.White
            });
        }
    }
}
