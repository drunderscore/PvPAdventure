//using PvPAdventure.Common.Spectator.SpectatorMode;
//using Terraria;
//using Terraria.ModLoader;

//namespace PvPAdventure.Common.Spectator.Visualization;

//[Autoload(Side = ModSide.Client)]
//internal sealed class DisableInventorySpectatorSystem : ModSystem
//{
//    public override void PostUpdateInput()
//    {
//        if (!SpectatorModeSystem.IsInSpectateMode(Main.LocalPlayer))
//            return;

//        if (!Main.playerInventory)
//            return;

//        Main.playerInventory = false;
//        Main.LocalPlayer.chest = -1;
//        Main.InGuideCraftMenu = false;
//        Main.InReforgeMenu = false;
//        Recipe.FindRecipes();
//    }
//}