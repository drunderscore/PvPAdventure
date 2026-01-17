using PvPAdventure.Core.Net;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.SpawnSelector;

public static class TeleportFxNetHandler
{
    public static void Send(int who)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        ModPacket p = ModContent.GetInstance<PvPAdventure>().GetPacket();
        p.Write((byte)AdventurePacketIdentifier.TeleportFx);
        p.Write((byte)who);
        p.Send(); // to all clients
    }

    public static void Receive(BinaryReader r)
    {
        if (Main.dedServ)
            return;

        int who = r.ReadByte();
        if (who < 0 || who >= Main.maxPlayers)
            return;

        // Guarantee local hears it
        if (who == Main.myPlayer)
        {
            SoundEngine.PlaySound(SoundID.Item6);
            return;
        }

        Player plr = Main.player[who];
        if (plr != null && plr.active)
            SoundEngine.PlaySound(SoundID.Item6, plr.Center);
    }
}
