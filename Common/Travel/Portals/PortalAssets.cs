using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Travel.Portals;

internal static class PortalAssets
{
    public static Texture2D GetCreatorTexture(int team) =>
        ModContent.Request<Texture2D>(GetCreatorTexturePath(team), AssetRequestMode.ImmediateLoad).Value;

    public static Texture2D GetPortalTexture(int team) =>
        ModContent.Request<Texture2D>(GetPortalTexturePath(team), AssetRequestMode.ImmediateLoad).Value;

    public static Texture2D GetPortalMinimapTexture(int team) =>
        ModContent.Request<Texture2D>(GetPortalMinimapTexturePath(team), AssetRequestMode.ImmediateLoad).Value;

    private static string GetCreatorTexturePath(int team)
    {
        //return "PvPAdventure/Assets/Items/AdventureMirror";

        return team switch
        {
            1 => "PvPAdventure/Assets/Portals/PortalCreator_Red",
            2 => "PvPAdventure/Assets/Portals/PortalCreator_Green",
            3 => "PvPAdventure/Assets/Portals/PortalCreator_Blue",
            4 => "PvPAdventure/Assets/Portals/PortalCreator_Yellow",
            5 => "PvPAdventure/Assets/Portals/PortalCreator_Pink",
            _ => "PvPAdventure/Assets/Portals/PortalCreator_NoTeam"
        };
    }

    private static string GetPortalTexturePath(int team)
    {
        return team switch
        {
            1 => "PvPAdventure/Assets/Portals/Portal_Red",
            2 => "PvPAdventure/Assets/Portals/Portal_Green",
            3 => "PvPAdventure/Assets/Portals/Portal_Blue",
            4 => "PvPAdventure/Assets/Portals/Portal_Yellow",
            5 => "PvPAdventure/Assets/Portals/Portal_Pink",
            _ => "PvPAdventure/Assets/Portals/Portal_NoTeam"
        };
    }

    private static string GetPortalMinimapTexturePath(int team)
    {
        return team switch
        {
            1 => "PvPAdventure/Assets/Portals/PortalMinimap_Red",
            2 => "PvPAdventure/Assets/Portals/PortalMinimap_Green",
            3 => "PvPAdventure/Assets/Portals/PortalMinimap_Blue",
            4 => "PvPAdventure/Assets/Portals/PortalMinimap_Yellow",
            5 => "PvPAdventure/Assets/Portals/PortalMinimap_Pink",
            _ => "PvPAdventure/Assets/Portals/PortalMinimap_NoTeam"
        };
    }
}