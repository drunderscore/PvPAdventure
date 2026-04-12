using System;
using System.IO;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using PvPAdventure.Core.Config;
using Steamworks;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Authentication;

public class AuthenticationManager : ModSystem
{
    private bool didServerRequestToAuthenticate;

    /// <summary>
    /// whoAmI is CLAIMING they are id.
    /// <returns>true to proceed with the authentication attempt, false to stop right now and do nothing.</returns>
    /// </summary>
    public delegate bool ShouldAttemptAuthenticationCallback(byte whoAmI, ulong id);

    public ShouldAttemptAuthenticationCallback ShouldAttemptAuthentication { get; set; }

    public override void Load()
    {
        if (Main.dedServ)
        {
            ShouldAttemptAuthentication = (whoAmI, id) =>
            {
                var config = ModContent.GetInstance<ServerConfig>();

                if (!config.WhitelistPlayers.AllowAnyPlayerToJoin &&
                    !config.WhitelistPlayers.AllowedPlayerSteamIds.Contains(id.ToString()))
                {
                    Log.Debug($"{id} not whitelisted, booting {whoAmI} without attempting authentication");
                    NetMessage.BootPlayer(whoAmI,
                        NetworkText.FromKey("Mods.PvPAdventure.Authentication.NotWhitelisted"));
                    return false;
                }

                return true;
            };

            IL_MessageBuffer.GetData += OnGetDataIL;
        }
        else
        {
            Netplay.OnDisconnect += OnDisconnect;
        }
    }

    public override void Unload()
    {
        if (!Main.dedServ)
        {
            Netplay.OnDisconnect -= OnDisconnect;
        }
    }

    public override bool HijackGetData(ref byte messageType, ref BinaryReader reader, int playerNumber)
    {
        if (!Main.dedServ)
        {
            if (messageType == MessageID.RequestPassword)
            {
                didServerRequestToAuthenticate = true;

                var previous = Netplay.ServerPassword;
                try
                {
                    var ticket = ModContent.GetInstance<SteamAuthentication>().BeginMultiplayerSession();
                    if (ticket != null)
                    {
                        Netplay.ServerPassword = SteamAuthentication.ClientSteamId + "_" + Convert.ToHexString(ticket);
                        NetMessage.SendData(MessageID.SendPassword);

                        Main.statusText = Language.GetTextValue("Mods.PvPAdventure.Authentication.WaitingForServer");
                    }
                    else
                    {
                        Netplay.Disconnect = true;
                        Main.statusText = Language.GetTextValue("Mods.PvPAdventure.Authentication.SteamInvalidTicket");
                        Main.menuMode = MenuID.MultiplayerJoining;
                    }
                }
                finally
                {
                    Netplay.ServerPassword = previous;
                }

                return true;
            }

            if (messageType == MessageID.PlayerInfo && !didServerRequestToAuthenticate)
            {
                Netplay.Disconnect = true;
                Main.statusText =
                    Language.GetTextValue("Mods.PvPAdventure.Authentication.ServerDidntRequestAuthentication");
                Main.menuMode = MenuID.MultiplayerJoining;

                return true;
            }
        }

        if (Main.dedServ && messageType == MessageID.SendPassword)
        {
            var value = reader.ReadString();
            var parts = value.Split('_');

            if (parts.Length != 2 || !ulong.TryParse(parts[0], out var id))
            {
                NetMessage.BootPlayer(playerNumber, NetworkText.FromLiteral("Invalid password syntax!"));
                return true;
            }

            // oh, we shouldn't attempt authentication? bail now.
            if (ShouldAttemptAuthentication != null && !ShouldAttemptAuthentication((byte)playerNumber, id))
                return true;

            var ticket = parts[1];

            ModContent.GetInstance<SteamAuthentication>()
                .BeginMultiplayerSessionWith((byte)playerNumber, id, ticket,
                    (authedId, whoAmI, response, alreadyOk) =>
                    {
                        if (response == EAuthSessionResponse.k_EAuthSessionResponseOK)
                        {
                            var client = Netplay.Clients[whoAmI];

                            if (!alreadyOk && client.IsActive && client.State == -1)
                            {
                                Log.Info(
                                    $"{playerNumber}/{client.Socket.GetRemoteAddress().GetIdentifier()} successfully authenticated as {authedId}");
                                Netplay.Clients[playerNumber].State = 1;

                                NetMessage.SendData(MessageID.PlayerInfo, playerNumber);
                            }
                        }
                        else
                        {
                            // non-OK response at any point is a disconnect, even after successful authentication
                            var msg = $"Steam invalidated session ticket for {authedId}/{playerNumber}: {response}";
                            Log.Warn(msg);
                            if (Netplay.Clients[playerNumber].IsActive && Netplay.Clients[playerNumber].State == -1)
                                Console.WriteLine(msg);

                            NetMessage.BootPlayer(playerNumber,
                                NetworkText.FromKey("Mods.PvPAdventure.Authentication.SteamTicketResponseFailure"));
                        }
                    });

            return true;
        }

        return false;
    }

    // We can't require a password before the net mods sync obviously -- so we'll wait for the server to sync the mods, and then request a
    // password, by using Aang's IL modification.
    private void OnGetDataIL(ILContext il)
    {
        var cursor = new ILCursor(il);
        cursor.GotoNext(MoveType.After, i => i.MatchCall(typeof(ModNet), "SendNetIDs"))
            .Emit(OpCodes.Ldarg_0)
            .Emit<MessageBuffer>(OpCodes.Ldfld, "whoAmI")
            .EmitDelegate((byte whoAmI) =>
            {
                Netplay.Clients[whoAmI].State = -1;
                NetMessage.SendData(MessageID.RequestPassword, whoAmI);
            });

        cursor.Emit(OpCodes.Ret);
    }

    private void OnDisconnect()
    {
        didServerRequestToAuthenticate = false;
    }
}