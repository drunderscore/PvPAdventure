using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Authentication;

public class AuthenticatedPlayer : ModPlayer
{
    public ulong? SteamId =>
        ModContent.GetInstance<SteamAuthentication>().GetAuthenticatedIdentity((byte)Player.whoAmI);

    public override void PlayerDisconnect()
    {
        if (Main.dedServ)
            ModContent.GetInstance<SteamAuthentication>().EndMultiplayerSessionWith((byte)Player.whoAmI);
    }
}