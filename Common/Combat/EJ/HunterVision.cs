using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat.EJ;

/// <summary>
/// While the player has the Hunter buff, this forces players to be fully visible.
/// </summary>
public class HunterVision : ModPlayer
{
    public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
    {
        Player target = Player;
        Player viewer = Main.LocalPlayer;

        if (target.whoAmI == Main.myPlayer || target.dead)
            return;

        if (!viewer.HasBuff(BuffID.Hunter))
            return;

        if (target.team == 0 && viewer.team == 0)
            return;

        fullBright = true;
    }
}
 
/// <summary>
/// This replicates the Red and Green color overlays from the hunter potion
/// </summary>
public class HunterVisionLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition() => PlayerDrawLayers.AfterLastVanillaLayer;

    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
    {
        Player target = drawInfo.drawPlayer;
        Player viewer = Main.LocalPlayer;

        if (target.whoAmI == Main.myPlayer || target.dead)
            return false;

        if (!viewer.HasBuff(BuffID.Hunter))
            return false;

        if (target.team == 0 && viewer.team == 0)
            return false;

        return true;
    }

    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        Player target = drawInfo.drawPlayer;
        Player viewer = Main.LocalPlayer;

        Color tint = target.team == viewer.team
            ? new Color(60, 255, 60)  
            : new Color(255, 60, 60);  
        int count = drawInfo.DrawDataCache.Count;
        for (int i = 0; i < count; i++)
        {
            DrawData dd = drawInfo.DrawDataCache[i];
            dd.color = tint;
            drawInfo.DrawDataCache.Add(dd);
        }
    }
}