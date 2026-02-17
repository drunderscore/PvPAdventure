using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat.EJ;
/// <summary>
/// Makes Knockback applied to players stack, instead of just setting the player's velocity, it will add velocity onto it. This is an expirimental feature.
/// </summary>
public class KnockbackStack : ModSystem
{
    public override void Load()
    {
        IL_Player.Hurt_HurtInfo_bool += Player_Hurt_IL;
    }

    private void Player_Hurt_IL(ILContext il)
    {
        var c = new ILCursor(il);
        int replacementCount = 0;

        if (c.TryGotoNext(MoveType.Before,
            i => i.MatchLdfld<Player.HurtInfo>("Knockback"),
            i => i.MatchLdloc(1),
            i => i.MatchConvR4(),
            i => i.MatchMul(),
            i => i.MatchStfld<Microsoft.Xna.Framework.Vector2>("X")))
        {
            c.TryGotoNext(MoveType.Before, i => i.MatchStfld<Microsoft.Xna.Framework.Vector2>("X"));
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldflda, typeof(Entity).GetField("velocity"));
            c.Emit(OpCodes.Ldfld, typeof(Microsoft.Xna.Framework.Vector2).GetField("X"));
            c.Emit(OpCodes.Add);
            replacementCount++;
        }
        if (c.TryGotoNext(MoveType.Before,
            i => i.MatchLdfld<Player.HurtInfo>("Knockback"),
            i => i.MatchLdcR4(-7f),
            i => i.MatchMul(),
            i => i.MatchLdcR4(9f),
            i => i.MatchDiv(),
            i => i.MatchStfld<Microsoft.Xna.Framework.Vector2>("Y")))
        {
            c.TryGotoNext(MoveType.Before, i => i.MatchStfld<Microsoft.Xna.Framework.Vector2>("Y"));
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldflda, typeof(Entity).GetField("velocity"));
            c.Emit(OpCodes.Ldfld, typeof(Microsoft.Xna.Framework.Vector2).GetField("Y"));
            c.Emit(OpCodes.Add);
            replacementCount++;
        }

        if (replacementCount > 0)
            Mod.Logger.Info($"Successfully patched {replacementCount} knockback assignment(s) to +=");
        else
            Mod.Logger.Warn("Failed to find knockback assignments in IL!");
    }
}