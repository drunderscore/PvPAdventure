using PvPAdventure.Core.Net;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Travel.Beds;

internal sealed class TeamBedPlayer : ModPlayer
{
    private int lastSpawnX = int.MinValue;
    private int lastSpawnY = int.MinValue;

    public override void PostUpdate()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient || Player.whoAmI != Main.myPlayer)
            return;

        if (Player.SpawnX == lastSpawnX && Player.SpawnY == lastSpawnY)
            return;

        lastSpawnX = Player.SpawnX;
        lastSpawnY = Player.SpawnY;

        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.TeamBed);
        packet.Write((byte)Main.myPlayer);
        packet.Write(Player.SpawnX);
        packet.Write(Player.SpawnY);
        packet.Send();
    }
}