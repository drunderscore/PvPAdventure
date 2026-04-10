using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Spectator.UI.State;
using PvPAdventure.Core.Utilities;
using PvPAdventure.UI;
using ReLogic.Content;
using Terraria;

namespace PvPAdventure.Common.Spectator.UI.NPCs;

internal sealed class SpectatorNPCPanel : UIBrowserPanel
{
    public SpectatorNPCPanel() : base("NPCs")
    {
        Width.Set(560f, 0f);
        Height.Set(560f, 0f);
        HAlign = 0.6f;
        VAlign = 0.45f;
    }

    protected override float MinResizeH => base.MinResizeH;
    protected override float MaxResizeH => base.MaxResizeH;
    protected override float MinResizeW => base.MinResizeW;
    protected override float MaxResizeW => base.MaxResizeW;
    protected override Asset<Texture2D> ActionPanelIconAsset => Ass.Icon_Eye;
    protected override string ActionPanelHoverText => "Open NPC spectate controls";

    protected override void OnActionPanelLeftClick()
    {
        SpectatorUISystem.ToggleNpcSpectatorControls();
    }

    protected override void PopulateEntries()
    {
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (npc == null || !npc.active)
                continue;

            AddEntry(new SpectatorNPCEntry(npc));
        }
    }
}
