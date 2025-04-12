using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;

namespace PvPAdventure
{
    class WeaponChanges
    {   
    public class Tsunami : GlobalItem
        {
            public override void SetDefaults(Item item)
            {
                if (item.type == ItemID.Tsunami)
                {
                    item.damage = 45;
                    item.useTime = 24;
                    item.useAnimation = 24;
                    //+20%damagepvp
                }
            }
        }

        public class BubbleGun : GlobalItem
        {
            public override void SetDefaults(Item item)
            {
                if (item.type == ItemID.BubbleGun)
                {
                    item.mana = 11;
                    //projectile changes restored
                }
            }
        }

        public class Flairon : GlobalItem
        {
            public override void SetDefaults(Item item)
            {
                if (item.type == ItemID.Flairon)
                {
                    item.damage = 59;
                }
            }
        }

        public class DaedalusStormbow : GlobalItem
        {
            public override void SetDefaults(Item item)
            {
                if (item.type == ItemID.DaedalusStormbow)
                {
                    item.damage = 29;
                    item.useTime = 19;
                    item.useAnimation = 19;
                    //mr sevai please increase pvp damage of this item from 40% to 60%
                }
            }
        }
        public class TrueNightsEdge : GlobalItem
        {
            public override void SetDefaults(Item item)
            {
                if (item.type == ItemID.TrueNightsEdge)
                {
                    item.damage = 150;
                    item.shootSpeed = 50;
                    item.useTime = 35;
                    item.useAnimation = 35;
                }
            }
        }

        public class Terrablade : GlobalItem
        {
            public override void SetDefaults(Item item)
            {
                if (item.type == ItemID.TerraBlade)
                {
                    item.shootSpeed = 4; // Adjust shoot speed as you already have
                }
            }
        }
        // Add a GlobalProjectile class to modify the TerraBlade2Shot projectile
    }
}
