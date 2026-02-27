using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Net;

[Autoload(Side = ModSide.Both)]
public class NetworkInspector : ModSystem
{
    public override void Load()
    {
        // Complain when the server is behind on ticking.
        if (Main.dedServ)
            IL_Main.DedServ_PostModLoad += EditMainDedServ_PostModLoad;
    }

    private void EditMainDedServ_PostModLoad(ILContext il)
    {
        var cursor = new ILCursor(il);

        // Find the expression: target = now + delta;
        cursor.GotoNext(i =>
            i.MatchLdloc(28) &&
            i.Next.MatchLdloc2() &&
            i.Next.Next.MatchAdd() &&
            i.Next.Next.Next.MatchStloc3()
        );

        // Ensure our instructions are placed within the label.
        cursor.MoveAfterLabels();
        cursor
            .EmitLdloc(28)
            .EmitLdloc2()
            .EmitLdloc3()
            .EmitDelegate((double now, double delta, double target) =>
            {
                var missed = now - target;
                //Mod.Logger.Warn($"Can't keep up! Missed {missed:F2}ms, {(missed / delta):F2} ticks");
            });
    }
}