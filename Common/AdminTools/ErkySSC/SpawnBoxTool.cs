using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Spawnbox.SpawnBoxTool;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.AdminTools.ErkySSC;

[Autoload(Side = ModSide.Client)]
internal sealed class SpawnBoxTool : ModSystem
{
    private const string Owner = "PvPAdventure.SpawnBox";

    public override void PostSetupContent() => RegisterErkySSCQuickbarEntry();
    public override void OnWorldLoad() => RegisterErkySSCQuickbarEntry();

    public override void Unload()
    {
        if (ModLoader.TryGetMod("ErkySSC", out Mod erky))
            erky.Call("ClearAdminQuickbarEntries", Owner);
    }

    private static void RegisterErkySSCQuickbarEntry()
    {
        if (!ModLoader.TryGetMod("ErkySSC", out Mod erky))
            return;

        Asset<Texture2D> icon = Ass.IconSpawnbox;
        erky.Call(
            "RegisterAdminQuickbarEntry",
            Owner,
            "spawnbox_tool",
            "PvPAdventure : Spawnbox",
            "Adjust spawnbox protection",
            icon,
            new Action(ToggleDialog),
            new Func<string>(MainActionText),
            new Func<Color>(() => Color.White),
            true,
            23);
    }

    private static string MainActionText() =>
        ModContent.GetInstance<SpawnBoxToolSystem>()?.IsActive() == true ? "Close" : "Open";

    private static void ToggleDialog() => ModContent.GetInstance<SpawnBoxToolSystem>()?.ToggleActive();
}
