using System.IO;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.DropRates;

internal class BossDropPerTeamSystem : ModSystem
{
    public override void Load()
    {
        On_CommonCode._DropItemFromNPC += DropItemsBasedOnTeam;
    }

    public override void Unload()
    {
        On_CommonCode._DropItemFromNPC -= DropItemsBasedOnTeam;
    }

    private void DropItemsBasedOnTeam(On_CommonCode.orig__DropItemFromNPC orig, NPC npc, int id, int stack, bool scattered)
    {
        // If we are the server, check if the drops should be instanced instead.
        if (Main.dedServ && npc.boss)
        {
            var number = Item.NewItem(npc.GetSource_Loot(), npc.Hitbox, id, stack, true, -1);

            // FIXME: Not consistent with how the rest of the codebase determines the last hit.
            if (npc.lastInteraction != 255)
            {
                var player = Main.player[npc.lastInteraction];
                Main.item[number].GetGlobalItem<BossDropPerTeamGlobalItem>()._team = (Team)player.team;
            }

            NetMessage.SendData(MessageID.SyncItem, number: number);
        }
        else
        {
            orig(npc, id, stack, scattered);
        }
    }
}

public class BossDropPerTeamGlobalItem : GlobalItem
{
    public override bool InstancePerEntity => true;
    public Team? _team;

    public override void NetSend(Item item, BinaryWriter writer)
    {
        var adventureItem = item.GetGlobalItem<BossDropPerTeamGlobalItem>();
        var has = adventureItem._team != null;

        writer.Write(has);

        if (has)
            writer.Write7BitEncodedInt((int)adventureItem._team);
    }

    public override void NetReceive(Item item, BinaryReader reader)
    {
        var has = reader.ReadBoolean();
        var adventureItem = item.GetGlobalItem<BossDropPerTeamGlobalItem>();

        adventureItem._team = has ? (Team)reader.Read7BitEncodedInt() : null;
    }

    public override bool CanPickup(Item item, Player player)
    {
        var team = item.GetGlobalItem<BossDropPerTeamGlobalItem>()._team;

        return team == null || team == (Team)player.team;
    }

    public override bool OnPickup(Item item, Player player)
    {
        item.GetGlobalItem<BossDropPerTeamGlobalItem>()._team = null;

        return true;
    }
}
