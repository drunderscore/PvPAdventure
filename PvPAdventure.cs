using Microsoft.Xna.Framework;
using MonoMod.Cil;
using PvPAdventure.Content.Items;
using PvPAdventure.Core.Helpers;
using PvPAdventure.System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Chat;
using Terraria.Enums;
using Terraria.ID;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.UI.Chat;

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

        // Don't set Player.mouseInterface when mousing over buffs.
        IL_Main.DrawBuffIcon += EditMainDrawBuffIcon;
    }

    private void EditMainDrawBuffIcon(ILContext il)
    {
        var cursor = new ILCursor(il);

        // First, find a store to Player.mouseInterface...
        // NOTE: The reference we find actually relates to gamepad, which we don't touch.
        cursor.GotoNext(i => i.MatchStfld<Player>("mouseInterface"));
        // ...and go past the gamepad interactions...
        cursor.Index += 2;
        // ...to remove the loads and stores to Player.mouseInterface for non-gamepad.
        cursor.RemoveRange(5);
    }

    public override void PostSetupContent()
    {
        base.PostSetupContent();
    }

    public override void HandlePacket(BinaryReader reader, int whoAmI)
    {
        var id = (AdventurePacketIdentifier)reader.ReadByte();

        switch (id)
        {
            case AdventurePacketIdentifier.BedTeleport:
            {
                byte playerId = reader.ReadByte();
                short bedX = reader.ReadInt16();
                short bedY = reader.ReadInt16();

                if (Main.netMode != NetmodeID.Server)
                    break;

                // optional anti-cheat: ensure bedX/bedY matches what server knows for this player
                if (playerId != whoAmI)
                    return;

                Player player = Main.player[playerId];
                if (player is null || !player.active)
                    return;

                // If you trust bedX/bedY, use those:
                Vector2 spawnWorld = new Vector2(bedX, bedY - 3).ToWorldCoordinates();

                player.Teleport(spawnWorld, TeleportationStyleID.RecallPotion);

                NetMessage.SendData(
                    MessageID.TeleportEntity,
                    -1, -1, null,
                    number: 0,
                    number2: player.whoAmI,
                    number3: spawnWorld.X,
                    number4: spawnWorld.Y,
                    number5: TeleportationStyleID.RecallPotion
                );

#if DEBUG
                ChatHelper.BroadcastChatMessage(
                    NetworkText.FromLiteral($"[DEBUG/SERVER] Player {player.name} teleported to bed ({bedX}, {bedY})."),
                    Color.White
                );
#endif

                break;
            }

            case AdventurePacketIdentifier.AdventureMirrorRightClickUse:
            {
                byte playerId = reader.ReadByte();
                byte slot = reader.ReadByte();

                if (Main.netMode == NetmodeID.Server)
                {
                    if (playerId != whoAmI)
                        return;

                    if (playerId < 0 || playerId >= Main.maxPlayers)
                        return;

                    Player player = Main.player[playerId];
                    if (player is null || !player.active)
                        return;

                    if (slot < 0 || slot >= player.inventory.Length)
                        return;

                    Item item = player.inventory[slot];
                    if (item?.ModItem is not AdventureMirror)
                        return;

                    ModPacket p = GetPacket();
                    p.Write((byte)AdventurePacketIdentifier.AdventureMirrorRightClickUse);
                    p.Write(playerId);
                    p.Write(slot);
                    p.Send();
                }
                else if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    Player player = Main.player[playerId];
                    if (player is null || !player.active)
                        return;

                    if (slot < 0 || slot >= player.inventory.Length)
                        return;

                    Item item = player.inventory[slot];
                    if (item?.ModItem is not AdventureMirror)
                        return;

                    // Visual state only
                    player.selectedItem = slot;
                    player.itemAnimation = item.useAnimation;
                    player.itemAnimationMax = item.useAnimation;
                    player.itemTime = item.useTime;
                    player.itemTimeMax = item.useTime;
                }
                break;
            }
            case AdventurePacketIdentifier.PlayerBed:
            {
                byte playerId = reader.ReadByte();
                int spawnX = reader.ReadInt32();
                int spawnY = reader.ReadInt32();

                Player p = Main.player[playerId];
                p.SpawnX = spawnX;
                p.SpawnY = spawnY;

                    if (Main.dedServ)
                    {
                        var packet = GetPacket();
                        packet.Write((byte)AdventurePacketIdentifier.PlayerBed);
                        packet.Write(playerId);
                        packet.Write(spawnX);
                        packet.Write(spawnY);
                        packet.Send(-1, whoAmI);

#if DEBUG
                        ChatHelper.BroadcastChatMessage(
                            NetworkText.FromLiteral($"[DEBUG/SERVER] Player {p.name} set spawn to ({spawnX}, {spawnY})"), Color.White);
#endif
                    }

                break;
            }

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
            case AdventurePacketIdentifier.PlayerItemPickup:
            {
                var itemPickup = AdventurePlayer.ItemPickup.Deserialize(reader);
                if (Main.dedServ)
                {
                    var player = Main.player[whoAmI];
                    itemPickup.Apply(player.GetModPlayer<AdventurePlayer>());
                    ModContent.GetInstance<BountyManager>()
                        .OnPlayerItemPickupsUpdated(player, itemPickup.Items.ToHashSet());
                }

                break;
            }
            case AdventurePacketIdentifier.PlayerTeam:
            {
                var team = AdventurePlayer.Team.Deserialize(reader);
                var player = Main.player[Main.dedServ ? whoAmI : team.Player];

                player.team = (int)team.Value;
                break;
            }
        }
    }
}