using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace PvPAdventure.System;

[Autoload(Side = ModSide.Both)]
public class BountyManager : ModSystem
{
    // Any interaction with claims will increment this, ensuring the client is interacting with the correct state.
    public int TransactionId { get; private set; }
    public bool CollectedAllMechanicalBossSouls { get; private set; }
    private readonly Dictionary<Team, IList<Page>> _bounties = new();
    public IReadOnlyDictionary<Team, IList<Page>> Bounties => _bounties;

    public UIBountyShop UiBountyShop { get; private set; }

    public sealed class Page(IList<Item[]> bounties)
    {
        public IList<Item[]> Bounties { get; } = bounties;
    }

    public sealed class Transaction(int id, byte team, byte pageIndex, byte bountyIndex)
        : IPacket<Transaction>
    {
        public int Id { get; } = id;
        public byte Team { get; } = team;
        public byte PageIndex { get; } = pageIndex;
        public byte BountyIndex { get; } = bountyIndex;

        public static Transaction Deserialize(BinaryReader reader)
        {
            var transactionId = reader.ReadInt32();
            var team = reader.ReadByte();
            var pageIndex = reader.ReadByte();
            var bountyIndex = reader.ReadByte();

            return new(transactionId, team, pageIndex, bountyIndex);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Id);
            writer.Write(Team);
            writer.Write(PageIndex);
            writer.Write(BountyIndex);
        }
    }

    public class ClaimEntitySource : IEntitySource
    {
        public string Context => null;
    }

    private class UIItemSlotScalable(Item[] itemArray, int itemIndex, int itemSlotContext)
        : UIItemSlot(itemArray, itemIndex, itemSlotContext)
    {
        public float InventoryScale { get; set; }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            var previousInventoryScale = Main.inventoryScale;

            try
            {
                Main.inventoryScale = InventoryScale;
                var item = itemArray[itemIndex];
                var position = GetDimensions().Center() + new Vector2(52f, 52f) * -0.5f * Main.inventoryScale;
                ItemSlot.Draw(spriteBatch, ref item, itemSlotContext, position);
            }
            finally
            {
                Main.inventoryScale = previousInventoryScale;
            }
        }
    }

    public class UIBountyShop(BountyManager bountyManager) : UIState
    {
        public override void OnInitialize()
        {
            Invalidate();
        }

        // FIXME: We could be smarter, but is that worth it?
        public void Invalidate()
        {
            RemoveAllChildren();

            var root = new UIElement
            {
                HAlign = 0.5f,
                Top = { Pixels = 200 },
                Width = { Percent = 0.35f },
                Height = { Percent = 0.5f }
            };

            root.Append(new UIPanel
            {
                Width = { Percent = 1.0f },
                Height = { Percent = 1.0f }
            });

            root.Append(new UITextPanel<string>("Bounty Shop", large: true)
            {
                HAlign = 0.5f,
                PaddingLeft = 15.0f,
                PaddingRight = 15.0f,
                PaddingTop = 15.0f,
                PaddingBottom = 15.0f,
                Top = { Pixels = -30.0f },
            });

            var bountyList = new UIList
            {
                Width = { Percent = 1.0f },
                Height = { Percent = 1.0f },
                PaddingTop = 60.0f, PaddingBottom = 30.0f, PaddingLeft = 30.0f, PaddingRight = 30.0f
            };

            var scrollbar = new UIScrollbar
            {
                Top = { Pixels = -5f },
                Height =
                {
                    Pixels = -20.0f,
                    Percent = 1.0f
                },
                HAlign = 1.0f,
                VAlign = 1.0f
            };
            scrollbar.SetView(100.0f, 1000.0f);
            bountyList.SetScrollbar(scrollbar);

            root.Append(bountyList);

            var teamBounties = bountyManager.Bounties[(Team)Main.LocalPlayer.team];
            // FIXME: indentation cause we append root late
            if (teamBounties.Count != 0)
            {
                var page = teamBounties[0];

                var i = 0;

                foreach (var items in page.Bounties)
                {
                    var elementForBounty = new UIElement
                    {
                        Width = { Pixels = 600.0f },
                        Height = { Pixels = 50.0f },
                        Top = { Pixels = i * 50.0f },
                    };
                    bountyList.Add(elementForBounty);

                    var button = new UIKeybindingSimpleListItem(() => "Claim", new Color(73, 94, 171, 255) * 0.9f)
                    {
                        // FIXME: Not sure why these don't look right as they do in the controls panel, scaling seems odd.
                        Width = { Pixels = 75.0f },
                        Height = { Pixels = 35.0f }
                    };

                    // FIXME: dumb
                    var i1 = (byte)i;
                    button.OnLeftClick += (evt, element) =>
                    {
                        // FIXME: wtf??
                        var packet = ModContent.GetInstance<BountyManager>().Mod.GetPacket();
                        packet.Write((byte)AdventurePacketIdentifier.BountyTransaction);
                        new Transaction(bountyManager.TransactionId, (byte)Main.LocalPlayer.team, 0, i1).Serialize(
                            packet);
                        packet.Send();
                    };

                    elementForBounty.Append(button);
                    var leftOffset = 0.0f;

                    foreach (var item in items)
                    {
                        var itemSlot = new UIItemSlotScalable([item], 0, ItemSlot.Context.ChestItem)
                        {
                            Left = { Pixels = button.Width.Pixels + leftOffset },
                            InventoryScale = 0.8f,
                        };
                        elementForBounty.Append(itemSlot);

                        leftOffset += itemSlot.Width.Pixels;
                    }

                    i++;
                }
            }

            Append(root);
        }
    }

    public override void Load()
    {
        if (!Main.dedServ)
            UiBountyShop = new UIBountyShop(this);
    }

    public override void LoadWorldData(TagCompound tag)
    {
        CollectedAllMechanicalBossSouls = tag.Get<bool>("collectedAllMechanicalBossSouls");
    }

    public override void SaveWorldData(TagCompound tag)
    {
        tag["collectedAllMechanicalBossSouls"] = CollectedAllMechanicalBossSouls;
    }

    public override void ClearWorld()
    {
        for (var i = 0; i < 8; i++)
        {
            var team = (Team)i;
            _bounties[team] = new List<Page>();
        }
    }

    public override void NetSend(BinaryWriter writer)
    {
        writer.Write(Bounties.Count);

        foreach (var (team, pages) in Bounties)
        {
            writer.Write((int)team);
            writer.Write(pages.Count);

            foreach (var page in pages)
            {
                writer.Write(page.Bounties.Count);

                foreach (var bounty in page.Bounties)
                {
                    writer.Write(bounty.Length);
                    foreach (var item in bounty)
                        ItemIO.Send(item, writer, true);
                }
            }
        }

        writer.Write(TransactionId);
    }

    public override void NetReceive(BinaryReader reader)
    {
        _bounties.Clear();

        var numberOfTeams = reader.ReadInt32();
        for (var i = 0; i < numberOfTeams; i++)
        {
            var team = (Team)reader.ReadInt32();
            var numberOfPages = reader.ReadInt32();
            var pages = new List<Page>();

            for (var j = 0; j < numberOfPages; j++)
            {
                var numberOfBounties = reader.ReadInt32();
                var page = new Page(new List<Item[]>());

                for (var k = 0; k < numberOfBounties; k++)
                {
                    var numberOfItems = reader.ReadInt32();
                    var items = new Item[numberOfItems];

                    for (var l = 0; l < numberOfItems; l++)
                        items[l] = ItemIO.Receive(reader, true);

                    page.Bounties.Add(items);
                }

                pages.Add(page);
            }

            _bounties[team] = pages;
        }

        TransactionId = reader.ReadInt32();

        UiBountyShop.Invalidate();
        ModContent.GetInstance<PointsManager>().UiScoreboard.Invalidate();
    }

    public void Award(Player killer, Player victim)
    {
        var team = (Team)killer.team;

        var eligibleBounties = ModContent.GetInstance<AdventureConfig>().Bounties
            .Where(IsBountyAvailable)
            .Select(bounty => bounty.Items)
            .Select(items => items.Select(item => new Item(item.Item.Type, item.Stack, item.Prefix.Type)).ToArray())
            .ToList();

        if (eligibleBounties.Count == 0)
            return;

        _bounties[team].Add(new Page(eligibleBounties));

        var firstPersonMessage = NetworkText.FromLiteral("+1 Bounty Shard");
        var thirdPersonMessage =
            NetworkText.FromLiteral(
                $"{team} Team awarded +1 Bounty Shard for defeating [c/{Main.teamColor[victim.team].Hex3()}:{victim.name}]!");

        NetMessage.SendData(MessageID.CombatTextString,
            text: firstPersonMessage,
            number: (int)Main.teamColor[(int)team].PackedValue,
            number2: killer.position.X,
            number3: killer.position.Y - 20.0f
        );

        NetMessage.SendData(MessageID.WorldData);

        foreach (var player in Main.ActivePlayers)
        {
            if ((Team)player.team == team)
                ChatHelper.SendChatMessageToClient(firstPersonMessage, Main.teamColor[(int)team], player.whoAmI);
            else
                ChatHelper.SendChatMessageToClient(thirdPersonMessage, Main.teamColor[(int)team], player.whoAmI);
        }
    }

    // FIXME: We could be MUCH smarter.
    public override bool HijackGetData(ref byte messageType, ref BinaryReader reader, int playerNumber)
    {
        if (!Main.dedServ && messageType is MessageID.PlayerTeam)
            Main.QueueMainThreadAction(() => UiBountyShop.Invalidate());

        return false;
    }

    // FIXME: We could be MUCH smarter.
    public override bool HijackSendData(int whoAmI, int msgType, int remoteClient, int ignoreClient, NetworkText text,
        int number,
        float number2, float number3, float number4, int number5, int number6, int number7)
    {
        if (!Main.dedServ && msgType == MessageID.PlayerTeam)
            Main.QueueMainThreadAction(() => UiBountyShop.Invalidate());

        return false;
    }

    public void OnPlayerItemPickupsUpdated(Player who, HashSet<int> updated)
    {
        // We've already reached our requirements, no need to check anymore.
        if (CollectedAllMechanicalBossSouls)
            return;

        // Unteamed players do not get a say in this.
        if (who.team == (int)Team.None)
            return;

        // A lot of this is possibly expensive (and nearly always unnecessary!), so we can pre-emptively check if
        // anything that was updated is actually significant enough for us to re-calculate everything.
        if (!(updated.Contains(ItemID.SoulofMight) ||
              updated.Contains(ItemID.SoulofSight) ||
              updated.Contains(ItemID.SoulofFright)))
            return;

        // Now, we need a set of all item pickups from all players on the same team.
        var itemPickupsForThisTeam = new HashSet<int>(Main.player
            .Where(player => player.active)
            .Where(player => player.team == who.team)
            .Select(player => player.GetModPlayer<AdventurePlayer>())
            .Select(player => player._itemPickups)
            .SelectMany(set => set));

        if (itemPickupsForThisTeam.Contains(ItemID.SoulofMight) &&
            itemPickupsForThisTeam.Contains(ItemID.SoulofSight) &&
            itemPickupsForThisTeam.Contains(ItemID.SoulofFright))
        {
            CollectedAllMechanicalBossSouls = true;
            ChatHelper.BroadcastChatMessage(
                NetworkText.FromKey("Mods.PvPAdventure.Bounty.CollectedAllMechanicalBossSouls"), Color.White);
        }
    }

    public void IncrementTransactionId() => TransactionId++;

    private bool IsBountyAvailable(AdventureConfig.Bounty bounty)
    {
        // This set requires pre-hardmode, but the world is hardmode.
        if (bounty.Conditions.WorldProgression == AdventureConfig.Condition.WorldProgressionState.PreHardmode &&
            Main.hardMode)
            return false;

        // This set requires hardmode, but the world is pre-hardmode.
        if (bounty.Conditions.WorldProgression == AdventureConfig.Condition.WorldProgressionState.Hardmode &&
            !Main.hardMode)
            return false;

        // This set requires Skeletron Prime to be defeated, but it is not.
        if (bounty.Conditions.SkeletronPrimeDefeated && !NPC.downedMechBoss3)
            return false;

        // This set requires The Twins to be defeated, but it is not.
        if (bounty.Conditions.TwinsDefeated && !NPC.downedMechBoss2)
            return false;

        // This set requires The Destroyer to be defeated, but it is not.
        if (bounty.Conditions.DestroyerDefeated && !NPC.downedMechBoss1)
            return false;

        // This set requires Plantera to be defeated, but it is not.
        if (bounty.Conditions.PlanteraDefeated && !NPC.downedPlantBoss)
            return false;

        // This set requires Golem to be defeated, but it is not.
        if (bounty.Conditions.GolemDefeated && !NPC.downedGolemBoss)
            return false;

        // This set requires Golem to be defeated, but it is not.
        if (bounty.Conditions.SkeletronDefeated && !NPC.downedBoss3)
            return false;

        // This set requires all mechanical boss souls to have been collected, but it is not.
        if (bounty.Conditions.CollectedAllMechanicalBossSouls && !CollectedAllMechanicalBossSouls)
            return false;

        return true;
    }
}