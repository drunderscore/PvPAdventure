using PvPAdventure.Core.Config;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.World;

internal class ModifyInvasionSizes : ModSystem
{
    public override void Load()
    {
        On_Main.StartInvasion += OnMainStartInvasion;
    }
    private void OnMainStartInvasion(On_Main.orig_StartInvasion orig, int type)
    {
        orig(type);

        if (Main.invasionType == InvasionID.None)
            return;

        var adventureConfig = ModContent.GetInstance<ServerConfig>();
        if (!adventureConfig.InvasionSizes.TryGetValue(type, out var invasionSize))
            return;

        // We shouldn't increase the invasion size, only ever decrease it.
        if (Main.invasionSize > invasionSize.Value)
        {
            Mod.Logger.Info($"Reducing invasion {type} size from {Main.invasionSize} to {invasionSize}");
            Main.invasionSize = Main.invasionSizeStart = Main.invasionProgressMax = invasionSize.Value;
        }
    }
}
