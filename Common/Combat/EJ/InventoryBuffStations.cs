using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat.EJ;
/// <summary>
/// Respawning with a specific buff station in your inventory automatically gives it's specific buff
/// </summary>
public class InventoryBuffStations : ModPlayer
{
    private static readonly (int ItemId, int[] BuffIds)[] BuffStationItems =
    [
        (ItemID.SharpeningStation, new[] { BuffID.Sharpened }),
        (ItemID.BewitchingTable, new[] { BuffID.Bewitched }),
        (ItemID.CrystalBall, new[] { BuffID.Clairvoyance }),
        (ItemID.AmmoBox, new[] { BuffID.AmmoBox }),
        (ItemID.WarTable, new[] { BuffID.WarTable }),
        (ItemID.LunarCraftingStation, new[] {
            BuffID.Sharpened,
            BuffID.Bewitched,
            BuffID.Clairvoyance,
            BuffID.AmmoBox,
            BuffID.SugarRush,
        }),
    ];

    public override void OnRespawn()
    {
        foreach (var (itemId, buffIds) in BuffStationItems)
        {
            if (Player.HasItem(itemId))
            {
                foreach (int buffId in buffIds)
                    Player.AddBuff(buffId, int.MaxValue);
            }
        }
    }
}