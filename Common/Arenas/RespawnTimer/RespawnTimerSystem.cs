using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Arenas.RespawnTimer;

internal sealed class RespawnTimerSystem : ModSystem
{
    public override void Load()
    {
        IL_Player.UpdateDead += EditUpdateDead;
    }

    public override void Unload()
    {
        IL_Player.UpdateDead -= EditUpdateDead;
    }

    private static void EditUpdateDead(ILContext il)
    {
        ILCursor c = new(il);
        int edits = 0;

        while (c.TryGotoNext(
            MoveType.Before,
            i => i.MatchLdcI4(3600),
            i => i.MatchCall(typeof(Utils), nameof(Utils.Clamp))))
        {
            c.Remove();
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(GetRespawnClamp);
            edits++;
        }

        if (edits != 2)
            throw new InvalidOperationException($"Expected 2 respawn clamp edits, found {edits}.");
    }

    private static int GetRespawnClamp(Player player)
    {
        RespawnTimerPlayer modPlayer = player.GetModPlayer<RespawnTimerPlayer>();
        return modPlayer.CustomRespawnTicks > 0 ? modPlayer.CustomRespawnTicks : 3600;
    }
}
