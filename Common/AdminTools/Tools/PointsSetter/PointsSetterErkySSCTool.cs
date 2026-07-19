using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.AdminTools.Tools.PointsSetter;

[Autoload(Side = ModSide.Client)]
internal sealed class PointsSetterErkySSCTool : ModSystem
{
    private const string Owner = "PvPAdventure.PointsSetter";

    public override void PostSetupContent()
    {
        RegisterErkySSCQuickbarEntries();
    }

    public override void OnWorldLoad()
    {
        RegisterErkySSCQuickbarEntries();
    }

    public override void Unload()
    {
        if (ModLoader.TryGetMod("ErkySSC", out Mod erky))
            erky.Call("ClearAdminQuickbarEntries", Owner);
    }

    private static void RegisterErkySSCQuickbarEntries()
    {
        if (!ModLoader.TryGetMod("ErkySSC", out Mod erky))
            return;

        Asset<Texture2D> icon = Ass.IconPointsSetter;

        erky.Call(
            "RegisterAdminQuickbarEntry",
            Owner,
            "points_setter",
            "PvPAdventure : Points Setter",
            "Set team points",
            icon,
            new Action(ToggleDialog),
            new Func<string>(MainActionText),
            new Func<Color>(() => Color.White),
            true,
            22
        );
    }

    private static string MainActionText()
    {
        PointsSetterSystem ui = ModContent.GetInstance<PointsSetterSystem>();

        return ui?.IsActive() == true ? "Close" : "Set";
    }

    private static void ToggleDialog()
    {
        PointsSetterSystem ui = ModContent.GetInstance<PointsSetterSystem>();
        if (ui == null)
        {
            Main.NewText("Failed to open PointsSetterSystem.", Color.Red);
            return;
        }

        ui.ToggleActive();
    }
}
