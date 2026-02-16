using PvPAdventure.Core.Config;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Visualization.DisableSocialSlots;

internal sealed class DisableSocialAccessoriesVisibility : ModSystem
{
    public override void Load()
    {
        if (Main.dedServ)
            return;

        On_Player.UpdateVisibleAccessories += Hook_UpdateVisibleAccessories;
    }

    public override void Unload()
    {
        On_Player.UpdateVisibleAccessories -= Hook_UpdateVisibleAccessories;
    }

    private static void Hook_UpdateVisibleAccessories(On_Player.orig_UpdateVisibleAccessories orig, Player self)
    {
        ClientConfig cfg = ModContent.GetInstance<ClientConfig>();

        bool suppressForThisPlayer = self.whoAmI != Main.myPlayer || !cfg.ShowVanityVisuals;
        if (!suppressForThisPlayer)
        {
            orig(self);
            return;
        }

        if (self.hideVisibleAccessory != null)
        {
            for (int i = 0; i < self.hideVisibleAccessory.Length; i++)
                self.hideVisibleAccessory[i] = false;
        }

        Item[] armor = self.armor;
        int dyeLen = self.dye?.Length ?? 0;

        bool vanityMerman = false;
        bool vanityWerewolf = false;
        bool functionalMerman = false;
        bool functionalWerewolf = false;

        Item[] saved = null;

        if (armor != null && dyeLen > 0)
        {
            for (int slot = 3; slot < dyeLen; slot++)
            {
                if (slot < 0 || slot >= armor.Length)
                    continue;

                int type = armor[slot].type;

                if (type == ItemID.NeptunesShell || type == ItemID.CelestialShell)
                    functionalMerman = true;

                if (type == ItemID.MoonCharm || type == ItemID.CelestialShell)
                    functionalWerewolf = true;
            }

            saved = new Item[armor.Length];

            for (int slot = 3; slot < dyeLen; slot++)
            {
                int vanityIndex = slot + dyeLen;
                if (vanityIndex < 0 || vanityIndex >= armor.Length)
                    continue;

                Item item = armor[vanityIndex];
                if (item.IsAir)
                    continue;

                int type = item.type;

                if (type == ItemID.NeptunesShell || type == ItemID.CelestialShell)
                    vanityMerman = true;

                if (type == ItemID.MoonCharm || type == ItemID.CelestialShell)
                    vanityWerewolf = true;

                saved[vanityIndex] = item;
                armor[vanityIndex] = new Item();
            }
        }

        orig(self);

        SuppressVanityShapeshifts(self, vanityMerman, vanityWerewolf, functionalMerman, functionalWerewolf);

        if (saved != null && armor != null)
        {
            for (int i = 0; i < saved.Length; i++)
            {
                if (saved[i] != null)
                    armor[i] = saved[i];
            }
        }
    }

    private static void SuppressVanityShapeshifts(Player self, bool vanityMerman, bool vanityWerewolf, bool functionalMerman, bool functionalWerewolf)
    {
        if (vanityMerman)
        {
            self.forceMerman = false;
            self.hideMerman = false;

            if (!functionalMerman)
            {
                self.accMerman = false;
                self.merman = false;
            }
        }
        else if (functionalMerman)
        {
            self.hideMerman = false;
        }

        if (vanityWerewolf)
        {
            self.forceWerewolf = false;
            self.hideWolf = false;

            if (!functionalWerewolf)
            {
                self.forceWerewolf = false;
                self.wereWolf = false;
            }
        }
        else if (functionalWerewolf)
        {
            self.hideWolf = false;
        }
    }
}
