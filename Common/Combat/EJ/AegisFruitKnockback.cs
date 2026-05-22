using Terraria;
using Terraria.ModLoader;
/// <summary>
/// Grants knockback immunity to players who have consumed the Aegis Fruit.
/// </summary>
namespace PvPAdventure.Common.Combat.EJ;
public class AegisFruitKnockback : ModPlayer
{
    public override void PostUpdateEquips()
    {
        if (Player.usedAegisFruit)
            Player.noKnockback = true;
    }
}