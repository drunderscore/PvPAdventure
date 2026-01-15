using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using PvPAdventure.Common.Statistics;
using PvPAdventure.Core.Discord;
using PvPAdventure.Core.Net;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure;

public class PvPAdventure : Mod
{
    public override void Load()
    {
        // This mod should only ever be loaded when connecting to a server, it should never be loaded beforehand.
        // We don't use Netplay.Disconnect here, as that's not initialized to true (but rather to default value, aka false), so instead
        // we'll check the connection status of our own socket.
        if (Main.dedServ)
        {
            ModContent.GetInstance<DiscordIdentification>().PlayerJoin += (_, args) =>
            {
                // FIXME: We should allow or deny players based on proper criteria.
                //        For now, let's allow everyone.
                args.Allowed = true;
            };
        }
    }

    public override void HandlePacket(BinaryReader reader, int whoAmI)
    {
        var id = (AdventurePacketIdentifier)reader.ReadByte();

        switch (id)
        {
            case AdventurePacketIdentifier.BountyTransaction:
            {
                var bountyTransaction = BountyManager.Transaction.Deserialize(reader);

                if (!Main.dedServ)
                    break;

                var bountyManager = ModContent.GetInstance<BountyManager>();

                if (bountyTransaction.Id != ModContent.GetInstance<BountyManager>().TransactionId)
                {
                    // Transaction ID doesn't match, likely out of sync. Sync now.
                    NetMessage.SendData(MessageID.WorldData, whoAmI);
                    break;
                }

                if (bountyTransaction.Team != Main.player[whoAmI].team)
                    break;

                var teamBounties = bountyManager.Bounties[(Team)bountyTransaction.Team];

                if (bountyTransaction.PageIndex >= teamBounties.Count)
                    break;

                var page = bountyManager.Bounties[(Team)bountyTransaction.Team][
                    bountyTransaction.PageIndex];

                if (bountyTransaction.BountyIndex >= page.Bounties.Count)
                    break;

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

                break;
            }
            case AdventurePacketIdentifier.PlayerStatistics:
            {
                var statistics = StatisticsPlayer.Statistics.Deserialize(reader);
                var player = Main.player[Main.dedServ ? whoAmI : statistics.Player];

                statistics.Apply(player.GetModPlayer<StatisticsPlayer>());

                // FIXME: bruh thats a little dumb maybe
                if (!Main.dedServ)
                    ModContent.GetInstance<PointsManager>().UiScoreboard.Invalidate();

                break;
            }
            case AdventurePacketIdentifier.PingPong:
            {
                var pingPong = LatencyTrackerPlayer.PingPong.Deserialize(reader);
                if (Main.dedServ)
                {
                    Main.player[whoAmI].GetModPlayer<LatencyTrackerPlayer>().OnPingPongReceived(pingPong);
                }
                else
                {
                    var packet = GetPacket();
                    packet.Write((byte)AdventurePacketIdentifier.PingPong);
                    pingPong.Serialize(packet);
                    packet.Send();
                }

                break;
            }
            case AdventurePacketIdentifier.PlayerItemPickup:
            {
                var itemPickup = StatisticsPlayer.ItemPickup.Deserialize(reader);
                if (Main.dedServ)
                {
                    var player = Main.player[whoAmI];
                    itemPickup.Apply(player.GetModPlayer<StatisticsPlayer>());
                    ModContent.GetInstance<BountyManager>()
                        .OnPlayerItemPickupsUpdated(player, itemPickup.Items.ToHashSet());
                }

                break;
            }
            case AdventurePacketIdentifier.PlayerTeam:
            {
                var team = StatisticsPlayer.Team.Deserialize(reader);
                var player = Main.player[Main.dedServ ? whoAmI : team.Player];

                player.team = (int)team.Value;
                break;
            }
        }
    }
}