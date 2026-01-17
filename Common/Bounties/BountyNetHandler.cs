using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Bounties;

public static class BountyNetHandler
{
    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        var bountyTransaction = BountyManager.Transaction.Deserialize(reader);

        if (!Main.dedServ)
            return;

        var bountyManager = ModContent.GetInstance<BountyManager>();

        if (bountyTransaction.Id != ModContent.GetInstance<BountyManager>().TransactionId)
        {
            // Transaction ID doesn't match, likely out of sync. Sync now.
            NetMessage.SendData(MessageID.WorldData, whoAmI);
            return;
        }

        if (bountyTransaction.Team != Main.player[whoAmI].team)
            return;

        var teamBounties = bountyManager.Bounties[(Team)bountyTransaction.Team];

        if (bountyTransaction.PageIndex >= teamBounties.Count)
            return;

        var page = bountyManager.Bounties[(Team)bountyTransaction.Team][
            bountyTransaction.PageIndex];

        if (bountyTransaction.BountyIndex >= page.Bounties.Count)
            return;

        try
        {
            var bounty = page.Bounties[bountyTransaction.BountyIndex];

            foreach (var item in bounty)
            {
                var index = Item.NewItem(new BountyManager.ClaimEntitySource(), Main.player[whoAmI].position,
                    Vector2.Zero, item, true, true);
                Main.timeItemSlotCannotBeReusedFor[index] = 54000;

                NetMessage.SendData(MessageID.InstancedItem, whoAmI, -1, null, index);

                Main.item[index].active = false;
            }
        }
        finally
        {
            bountyManager.Bounties[(Team)bountyTransaction.Team].Remove(page);
            bountyManager.IncrementTransactionId();
            NetMessage.SendData(MessageID.WorldData);
        }
    }
}
