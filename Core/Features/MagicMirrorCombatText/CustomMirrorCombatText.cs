//using Terraria;
//using Terraria.ID;
//using Terraria.ModLoader;

//namespace PvPAdventure.Core.Features.MagicMirrorCombatText
//{
//    internal class CustomMirrorCombatText : GlobalItem
//    {
//        public override bool? UseItem(Item item, Player player)
//        {
//            if (item.type == ItemID.MagicMirror)
//            {
//                player.GetModPlayer<MagicMirrorPlayer>().usedMirror = true;
//                return false;
//            }

//            return base.UseItem(item, player);
//        }
//    }
//}
