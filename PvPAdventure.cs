using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using PvPAdventure.System;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.Map;
using Terraria.ModLoader;

namespace PvPAdventure;

public class PvPAdventure : Mod
{
    internal static Color NpcChatColor = new(245, 133, 34);

    public override void Load()
    {
        Array.Resize(ref Main.teamColor, Main.teamColor.Length + 2);
        // This is Rebecca Purple, the favorite color of Rebecca Alison Meyer.
        Main.teamColor[^1] = new(102, 51, 153);
        Main.teamColor[^2] = Color.Orange;

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

    public override void Unload()
    {
        Array.Resize(ref Main.teamColor, 6);
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
                var statistics = AdventurePlayer.Statistics.Deserialize(reader);
                var player = Main.player[Main.dedServ ? whoAmI : statistics.Player];

                statistics.Apply(player.GetModPlayer<AdventurePlayer>());

                // FIXME: bruh thats a little dumb maybe
                if (!Main.dedServ)
                    ModContent.GetInstance<PointsManager>().UiScoreboard.Invalidate();

                break;
            }
            case AdventurePacketIdentifier.WorldMapLighting:
            {
                var worldMapSyncLighting = WorldMapSyncManager.Lighting.Deserialize(reader);

                if (!ModContent.GetInstance<AdventureConfig>().ShareWorldMap)
                    break;

                // On the server, we just forward this to everyone else.
                if (Main.dedServ)
                {
                    var packet = GetPacket();
                    packet.Write((byte)AdventurePacketIdentifier.WorldMapLighting);
                    worldMapSyncLighting.Serialize(packet);

                    var team = Main.player[whoAmI].team;
                    foreach (var player in Main.ActivePlayers)
                    {
                        // Map will sync to teammates and everyone without a team.
                        if (player.team == (int)Team.None || player.team == team)
                            packet.Send(player.whoAmI);
                    }
                }
                // On the client, we use it to update the lighting of the world map tiles.
                else
                {
                    try
                    {
                        ModContent.GetInstance<WorldMapSyncManager>().ignore = true;
                        foreach (var (point, light) in worldMapSyncLighting.TileLight)
                        {
                            Main.Map.UpdateLighting(point.X, point.Y, light);

                            if (MapHelper.numUpdateTile < MapHelper.maxUpdateTile - 1)
                            {
                                MapHelper.updateTileX[MapHelper.numUpdateTile] = point.X;
                                MapHelper.updateTileY[MapHelper.numUpdateTile] = point.Y;
                                MapHelper.numUpdateTile++;
                            }
                            else
                            {
                                Main.refreshMap = true;
                            }
                        }
                    }
                    finally
                    {
                        ModContent.GetInstance<WorldMapSyncManager>().ignore = false;
                    }
                }

                break;
            }
            case AdventurePacketIdentifier.PingPong:
            {
                var pingPong = AdventurePlayer.PingPong.Deserialize(reader);
                if (Main.dedServ)
                {
                    Main.player[whoAmI].GetModPlayer<AdventurePlayer>().OnPingPongReceived(pingPong);
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
            case AdventurePacketIdentifier.PlayerItemPickups:
            {
                var itemPickup = AdventurePlayer.ItemPickups.Deserialize(reader);
                if (Main.dedServ)
                {
                    var player = Main.player[whoAmI];
                    itemPickup.Apply(player.GetModPlayer<AdventurePlayer>());
                    ModContent.GetInstance<BountyManager>()
                        .OnPlayerItemPickupsUpdated(player, itemPickup.Items.ToHashSet());
                }

                break;
            }
        }
    }

    internal void DisableOurselfAndReload()
    {
        var modLoaderType = typeof(ModLoader);
        modLoaderType.GetMethod("DisableMod", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [Name]);
        modLoaderType.GetMethod("Reload", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, null);
    }
}