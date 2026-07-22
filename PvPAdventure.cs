using PvPAdventure.Core.Net;
using PvPAdventure.Common.Game;
using PvPFramework.Common.Scoreboard;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PvPAdventure;

public class PvPAdventure : Mod
{
    private const string ImportSscStatsCall = "ErkySSC.ImportStats";
    private const string ExportSscStatsCall = "ErkySSC.ExportStats";

    /// <summary>
    /// Packet handler for PvP Adventure mod packets.
    /// See <see cref="AdventurePacketIdentifier"/> for packet types.
    /// </summary>
    public override void HandlePacket(BinaryReader reader, int whoAmI)
    {
        base.HandlePacket(reader, whoAmI);

        // This causes read underflow and bogus logs, dont do it!
        //long packetStart = reader.BaseStream.Position;
        //long packetLength = reader.BaseStream.Length;

        var id = (AdventurePacketIdentifier)reader.ReadByte();

        //Log.Debug($"[Packet] Start id={(byte)id} ({id}), whoAmI={whoAmI}, bytes={packetLength - packetStart}");

        switch (id)
        {
            case AdventurePacketIdentifier.BountyTransaction:
                Common.Bounties.BountyNetHandler.HandlePacket(reader, whoAmI);
                break;

            case AdventurePacketIdentifier.TeamBed:
                Common.Travel.Beds.TeamBedNetHandler.HandlePacket(reader, whoAmI);
                break;

            case AdventurePacketIdentifier.GameManager:
                Common.Game.GameManagerNetHandler.HandlePacket(reader, whoAmI);
                break;

            case AdventurePacketIdentifier.TravelTeleport:
                Common.Travel.TravelTeleportNetHandler.HandlePacket(reader, whoAmI);
                break;

            case AdventurePacketIdentifier.UsePortal:
                Common.Travel.Portals.PortalNetHandler.HandlePacket(reader, whoAmI);
                break;

            case AdventurePacketIdentifier.MatchStatDelta:
                Common.Game.StatTrackers.MatchStatsNetHandler.HandlePacket(reader, whoAmI);
                break;

            default:
                Log.Warn($"[Packet] Unknown packet id: {(byte)id} ({id})");
                break;
        }

        //long bytesLeft = reader.BaseStream.Length - reader.BaseStream.Position;

        //if (bytesLeft != 0)
        //{
        //    Log.Warn($"[Packet] Handler left unread bytes: id={(byte)id} ({id}), left={bytesLeft}, total={packetLength - packetStart}");
        //    reader.BaseStream.Position = reader.BaseStream.Length;
        //}
    }

    public override object Call(params object[] args)
    {
        if (args.Length < 4 || args[0] is not string operation ||
            args[1] is not int whoAmI || args[2] is not string characterKey ||
            args[3] is not TagCompound root || whoAmI is < 0 or >= Main.maxPlayers)
            return false;

        Player player = Main.player[whoAmI];
        if (player == null)
            return false;

        return operation switch
        {
            ImportSscStatsCall => ImportSscStats(player, characterKey, root),
            ExportSscStatsCall => ExportSscStats(player, characterKey, root),
            _ => false
        };
    }

    private static bool ImportSscStats(Player player, string characterKey, TagCompound root)
    {
        if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient || !root.ContainsKey("ErkySSC"))
            return true;

        TagCompound ssc = root.GetCompound("ErkySSC");
        if (!ssc.ContainsKey("PvPAdventure"))
            return true;

        TagCompound saved = ssc.GetCompound("PvPAdventure");
        string savedCharacter = saved.ContainsKey("characterKey") ? saved.GetString("characterKey") : "";
        string savedMatch = saved.ContainsKey("matchToken") ? saved.GetString("matchToken") : "";
        if ((!string.IsNullOrEmpty(savedCharacter) && savedCharacter != characterKey) || savedMatch != CurrentMatchToken())
            return true;

        ScoreboardService.SetPlayerStats(player, saved.GetInt("kills"), saved.GetInt("deaths"), saved.Get<long>("damage"));
        return true;
    }

    private static bool ExportSscStats(Player player, string characterKey, TagCompound root)
    {
        if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient)
            return true;

        ScoreboardEntry stats = ScoreboardService.GetPlayerStats(player);
        TagCompound ssc = root.ContainsKey("ErkySSC") ? root.GetCompound("ErkySSC") : [];
        ssc["PvPAdventure"] = new TagCompound
        {
            ["version"] = 1,
            ["characterKey"] = characterKey ?? "",
            ["matchToken"] = CurrentMatchToken(),
            ["kills"] = stats.Kills,
            ["deaths"] = stats.Deaths,
            ["damage"] = stats.Damage
        };
        root["ErkySSC"] = ssc;
        return true;
    }

    private static string CurrentMatchToken()
    {
        GameManager game = ModContent.GetInstance<GameManager>();
        return game.CurrentPhase == GameManager.Phase.Playing ? game.CurrentMatchToken : "";
    }
}
