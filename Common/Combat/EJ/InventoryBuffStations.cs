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
    private static readonly (int ItemId, int BuffId)[] BuffStationItems =
    [
        (ItemID.SharpeningStation, BuffID.Sharpened),
        (ItemID.BewitchingTable, BuffID.Bewitched),
        (ItemID.CrystalBall, BuffID.Clairvoyance),
        (ItemID.AmmoBox, BuffID.AmmoBox),
        (ItemID.WarTable, BuffID.WarTable),
    ];

    public override void OnRespawn()
    {
        foreach (var (itemId, buffId) in BuffStationItems)
        {
            if (Player.HasItem(itemId))
            {
                Player.AddBuff(buffId, int.MaxValue);
            }
        }
    }
}
