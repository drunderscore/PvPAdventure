using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.Integrations.TeamAssigner;

/// <summary>
/// A selectable team icon element.
/// </summary>
public class TeamIconElement : UIElement
{
    private readonly Texture2D sheet;
    private readonly Texture2D hover;
    private readonly Rectangle src;
    private readonly Player player;
    private readonly int teamIndex;

    public TeamIconElement(Asset<Texture2D> sheetAsset, Asset<Texture2D> hoverAsset, Rectangle src, Player player, int teamIndex)
    {
        sheet = sheetAsset.Value;
        hover = hoverAsset.Value;
        this.src = src;
        this.player = player;
        this.teamIndex = teamIndex;

        Width.Set(src.Width, 0f);
        Height.Set(src.Height, 0f);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        var d = GetDimensions();
        var dest = new Rectangle((int)d.X, (int)d.Y, src.Width, src.Height);

        spriteBatch.Draw(sheet, dest, src, Color.White);

        bool selected = player.team == teamIndex;
        if (IsMouseHovering || selected)
        {
            int hoverSize = hover.Width;
            var hoverDest = new Rectangle(
                dest.Center.X - hoverSize / 2,
                dest.Center.Y - hoverSize / 2,
                hoverSize,
                hoverSize
            );
            spriteBatch.Draw(hover, hoverDest, Color.White);
        }
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);

        // Update local view
        player.team = teamIndex;

        // Multiplayer
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            var packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
            packet.Write((byte)AdventurePacketIdentifier.PlayerTeam);
            new AdventurePlayer.Team((byte)player.whoAmI, (Terraria.Enums.Team)teamIndex).Serialize(packet);
            packet.Send();
        }

        // Always rebuild UI
        if (Parent?.Parent is TeamAssignerElement panel)
            panel.needsRebuild = true;
    }

}
