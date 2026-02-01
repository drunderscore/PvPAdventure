using MonoMod.RuntimeDetour;
using PvPAdventure.Core.Config;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.UI;

[Autoload(Side = ModSide.Client)]
internal sealed class DisableSocialArmorVisibility : ModSystem
{
    public override void Load()
    {
        On_Player.PlayerFrame += Hook_PlayerFrame;
    }

    public override void Unload()
    {
        On_Player.PlayerFrame -= Hook_PlayerFrame;
    }

    private static void Hook_PlayerFrame(On_Player.orig_PlayerFrame orig, Player self)
    {
        if (Main.netMode == NetmodeID.Server)
        {
            orig(self);
            return;
        }

        var cfg = ModContent.GetInstance<ClientConfig>();

        bool suppressForThisPlayer = true;
        if (self.whoAmI == Main.myPlayer)
        {
            suppressForThisPlayer = cfg.HideVanityVisuals;
        }

        if (!suppressForThisPlayer)
        {
            orig(self);
            return;
        }

        Item[] armor = self.armor;
        Item headSaved = null;
        Item bodySaved = null;
        Item legsSaved = null;

        if (armor != null && armor.Length > 12)
        {
            if (!armor[10].IsAir)
            {
                headSaved = armor[10];
                armor[10] = new Item();
            }

            if (!armor[11].IsAir)
            {
                bodySaved = armor[11];
                armor[11] = new Item();
            }

            if (!armor[12].IsAir)
            {
                legsSaved = armor[12];
                armor[12] = new Item();
            }
        }

        orig(self);

        // restore
        if (armor != null && armor.Length > 12)
        {
            if (headSaved != null)
                armor[10] = headSaved;

            if (bodySaved != null)
                armor[11] = bodySaved;

            if (legsSaved != null)
                armor[12] = legsSaved;
        }
    }
}
