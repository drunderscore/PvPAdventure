using DragonLens.Core.Systems.ThemeSystem;
using DragonLens.Core.Systems.ToolbarSystem;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Helpers;
using ReLogic.Content;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Integrations.DragonLens;

[JITWhenModsEnabled("DragonLens")]
[ExtendsFromMod("DragonLens")]
public class DLIntegration : ModSystem
{
    // Keys
    public static string StartGameKey => "StartGame";
    public static string TeamAssignerKey => "TeamAssigner";
    public static string PauseKey => "Pause";

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
                provider.icons[StartGameKey] = Ass.Play.Value;
                provider.icons[TeamAssignerKey] = Ass.TeamAssignerIcon.Value;
                provider.icons[PauseKey] = Ass.Pause.Value;
            }

            // rebuild toolbars *after* icons (and tools) have been injected
            ModContent.GetInstance<ToolbarHandler>().OnModLoad();
        }
    }
}

