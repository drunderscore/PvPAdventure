using DragonLens.Core.Systems.ThemeSystem;
using DragonLens.Core.Systems.ToolbarSystem;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Integrations.DragonLens;

[JITWhenModsEnabled("DragonLens")]
[ExtendsFromMod("DragonLens")]
public class DLIntegration : ModSystem
{
    // Keys
    public static string StartGameKey => "StartGame";
    public static string EndGameKey => "EndGame";
    public static string PauseKey => "Pause";
    public static string TeamAssignerKey => "TeamAssigner";
    public static string PointsSetterKey => "PointsSetter";
    public static string AdminManagerKey => "AdminManager";

    // Assets
    public static Asset<Texture2D> GlowAlpha = ModContent.Request<Texture2D>("DragonLens/Assets/Misc/GlowAlpha");

    public override void PostSetupContent()
    {
        AddIcons();
    }

    private void AddIcons()
    {
        if (ModLoader.TryGetMod("DragonLens", out _))
        {
            foreach (var provider in ThemeHandler.allIconProviders.Values)
            {
                provider.icons[StartGameKey] = Ass.Icon_StartGame.Value;
                provider.icons[EndGameKey] = Ass.Icon_EndGame.Value;
                provider.icons[PauseKey] = Ass.Icon_PauseGame.Value;
                provider.icons[TeamAssignerKey] = Ass.Icon_TeamAssigner.Value;
                provider.icons[PointsSetterKey] = Ass.Icon_PointsSetter.Value;
                provider.icons[AdminManagerKey] = Ass.Icon_AdminManager.Value;
            }

            // rebuild toolbars *after* icons (and tools) have been injected
            ModContent.GetInstance<ToolbarHandler>().OnModLoad();
        }
    }
}

