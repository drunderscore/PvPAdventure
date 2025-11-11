using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.UI;

namespace PvPAdventure.Core.Features.AdventureTeleport;

public class PlayerHeadElement : UIElement
{
    private readonly Player player;
    private readonly bool isSelf;

    public PlayerHeadElement(Player player, bool isSelf)
    {
        this.player = player;
        this.isSelf = isSelf;
        Width.Set(24f, 0f);
        Height.Set(24f, 0f);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        // Only draw if we have a player
        if (player == null || !player.active) return;

        // Where to draw (top-left of this element)
        var dims = GetDimensions();
        Vector2 pos = new Vector2(dims.X + dims.Width * 0.5f, dims.Y + dims.Height * 0.5f); // center on the element
        float scale = 0.75f;  
        float alpha = 1f;
        Main.MapPlayerRenderer.DrawPlayerHead(
            Main.Camera, player, pos, alpha, scale, Color.White);
    }
}
