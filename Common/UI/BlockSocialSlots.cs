using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.UI;

internal class BlockSocialSlots : ModSystem
{
    public override void Load()
    {
        // Prevent social armor slots from being drawn.
        IL_Player.PlayerFrame += EditPlayerFrame;
    }
    public override void Unload()
    {
        IL_Player.PlayerFrame -= EditPlayerFrame;
    }
    private void EditPlayerFrame(ILContext il)
    {
        var cursor = new ILCursor(il);

        RemoveSocialArmor(10);
        RemoveSocialArmor(11);
        RemoveSocialArmor(12);

        return;

        void RemoveSocialArmor(int slot)
        {
            cursor.Index = 0;

            cursor.GotoNext(i => i.MatchLdfld<Player>("armor") && i.Next.MatchLdcI4(slot));
            cursor.Index -= 1;
            cursor.RemoveRange(14);
        }
    }
}
