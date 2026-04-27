using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Spectator.Drawers;
using System.Text;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI.Tabs.NPCs;

internal sealed class SpectatorNPCEntry : SpectatorEntityEntry
{
    public NPC NPC => npc;
    private readonly NPC npc;   
    protected override string EntityName => npc.FullName;

    public SpectatorNPCEntry(NPC targetNpc)
    {
        npc = targetNpc ?? new NPC();
        SearchText = BuildSearchText();

        float left = 0f;
        AddEntityButton(TextureAssets.Item[ItemID.TeleportationPotion], ref left, "Teleport", OnTeleportClicked);
        //AddEntityButton(Ass.Icon_Eye, ref left, "Placeholder");

        FinishSetup();
    }

    protected override void DrawListPreview(SpriteBatch sb, Rectangle area) => EntityDrawer.DrawNPCPreview(sb, npc, area);

    protected override string DrawListStats(SpriteBatch sb, Rectangle area) => StatDrawer.DrawNPCListStats(sb, area, BuildStats(skipNpcHead: true));

    protected override string DrawHeadStat(SpriteBatch sb, Rectangle area) => EntityDrawer.DrawNPCHeadStat(sb, area, npc);

    protected override string DrawGridStats(SpriteBatch sb, Rectangle area, int columns, int rows, int statHeight, int statSpacing) =>
        StatDrawer.DrawNPCStatGrid(sb, area, BuildStats(skipNpcHead: true), columns, rows, statHeight, statSpacing);
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

    private void OnTeleportClicked(UIMouseEvent evt, UIElement listeningElement)
    {
        if (npc?.active != true)
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