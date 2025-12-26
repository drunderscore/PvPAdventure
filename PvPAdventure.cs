using Microsoft.Xna.Framework;
using MonoMod.Cil;
using PvPAdventure.Content.Items;
using PvPAdventure.Core.DashKeybind;
using PvPAdventure.Core.SSC;
using PvPAdventure.System;
using Steamworks;
using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Chat;
using Terraria.Enums;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

using PvPAdventure.Core.AdminTools.TeamAssigner;
using PvPAdventure.Core.SpawnAndSpectate;

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
                        ModPacket packet = (ModPacket)GetPacket();
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
            case AdventurePacketIdentifier.NpcStrikeTeam:
                {
                    var npcIndex = reader.ReadInt16();
                    var team = reader.ReadByte();

                    if (npcIndex < 0 || npcIndex >= Main.maxNPCs)
                        return;

                    if (team >= Enum.GetValues<Team>().Length)
                        return;

                    var npc = Main.npc[npcIndex];
                    npc.GetGlobalNPC<AdventureNpc>().MarkNextStrikeForTeam(npc, (Team)team);

                    break;
                }
            case AdventurePacketIdentifier.PlayerTeam:
                {
                    var team = AdventurePlayer.Team.Deserialize(reader);

                    if (Main.netMode == NetmodeID.Server)
                    {
                        if (team.Player < 0 || team.Player >= Main.maxPlayers)
                            return;

                        Player target = Main.player[team.Player];
                        if (target == null || !target.active)
                            return;

                        target.team = (int)team.Value;

                        ModPacket packet = (ModPacket)GetPacket();
                        packet.Write((byte)AdventurePacketIdentifier.PlayerTeam);
                        team.Serialize(packet);
                        packet.Send();
                        return;
                    }

                    if (team.Player < Main.maxPlayers)
                    {
                        Player target = Main.player[team.Player];
                        if (target != null && target.active)
                            target.team = (int)team.Value;
                    }

                    // Update scoreboard
                    if (!Main.dedServ)
                        ModContent.GetInstance<PointsManager>().UiScoreboard?.Invalidate();

                    // Update team assigner
                    var ts = ModContent.GetInstance<TeamAssignerSystem>();
                    if (ts?.teamAssignerState != null)
                    {
                        foreach (var child in ts.teamAssignerState.Children)
                        {
                            if (child is TeamAssignerElement panel)
                            {
                                panel.needsRebuild = true;
                                break;
                            }
                        }
                    }

                    break;
                }
            case AdventurePacketIdentifier.PauseGame:
                {
                    bool isPaused = reader.ReadBoolean();

                    if (Main.netMode == NetmodeID.Server)
                    {
                        var pm = ModContent.GetInstance<PauseManager>();
                        pm.PauseGame();
                    }

                    break;
                }
            case AdventurePacketIdentifier.StartGame:
                {
                    int time = reader.ReadInt32();
                    int countdown = reader.ReadInt32();

                    if (Main.netMode == NetmodeID.Server)
                    {
                        var gm = ModContent.GetInstance<GameManager>();

                        if (gm.CurrentPhase == GameManager.Phase.Playing || gm._startGameCountdown.HasValue)
                            break;

                        gm.StartGame(time, countdown);
                    }

                    break;
                }
            case AdventurePacketIdentifier.EndGame:
                {
                    if (Main.netMode == NetmodeID.Server)
                    {
                        var gm = ModContent.GetInstance<GameManager>();
                        gm.EndGame();
                    }

                    break;
                }
            case AdventurePacketIdentifier.Dash:
                DashKeybindSystem.HandlePacket(reader, whoAmI);
                break;
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

                        ModPacket p = (ModPacket)GetPacket();
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
            case AdventurePacketIdentifier.BedTeleport:
                {
                    BedsOnMap.HandlePacket(reader, whoAmI);
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
                        ModPacket packet = (ModPacket)GetPacket();
                        packet.Write((byte)AdventurePacketIdentifier.PlayerBed);
                        packet.Write(playerId);
                        packet.Write(spawnX);
                        packet.Write(spawnY);
                        packet.Send(-1, whoAmI);
#if DEBUG
                        if (p != null && p.name != string.Empty)
                        {
                            ChatHelper.BroadcastChatMessage(
                            NetworkText.FromLiteral($"[DEBUG/SERVER] Player {p.name} set spawn to ({spawnX}, {spawnY})"), Color.White);
                        }
#endif
                    }

                    break;
                }
            case AdventurePacketIdentifier.SetPointsRequest:
                {
                    var team = (Team)reader.ReadByte();
                    var value = reader.ReadInt32();

                    var pointsManager = ModContent.GetInstance<PointsManager>();
                    pointsManager._points[team] = value;

                    if (Main.dedServ)
                    {
                        NetMessage.SendData(MessageID.WorldData);
                    }
                    else
                    {
                        // Refresh scoreboard
                        ModContent.GetInstance<PointsManager>().UiScoreboard.Invalidate();
                    }

                    break;
                }
            case AdventurePacketIdentifier.SSC:
                {
                    ModContent.GetInstance<SSCSystem>().HandlePacket(reader, whoAmI);
                    break;
                }
            case AdventurePacketIdentifier.RespawnCommit:
                {
                    if (Main.netMode != NetmodeID.Server)
                        break;

                    var commit = (RespawnPlayer.RespawnCommit)reader.ReadByte();
                    int teammateIndex = reader.ReadInt32();

                    Player p = Main.player[whoAmI];
                    if (p != null && p.active)
                    {
                        p.GetModPlayer<RespawnPlayer>().ApplyCommitFromNet(commit, teammateIndex);
                    }

                    break;
                }
        }
    }
}
