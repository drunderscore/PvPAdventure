using System;
using System.IO;
using System.Linq;
using Discord;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using PvPAdventure.System.Client;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.System;

[Autoload(Side = ModSide.Both)]
public class DiscordIdentification : ModSystem
{
    private const bool Enabled = false;

    public delegate void PlayerJoinEventHandler(DiscordIdentification source, PlayerJoinArgs args);

    public event PlayerJoinEventHandler PlayerJoin;

    public override void Load()
    {
        if (Enabled && Main.dedServ)
            IL_MessageBuffer.GetData += OnGetDataIL;
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

    private bool OnPlayerJoin(Player player)
    {
        var args = new PlayerJoinArgs
        {
            Player = player
        };

        PlayerJoin?.Invoke(this, args);
        return args.Allowed;
    }

    public override bool HijackGetData(ref byte messageType, ref BinaryReader reader, int playerNumber)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            // FIXME: The user should be prompted if they wish to share their account with the server.
            if (messageType == MessageID.RequestPassword)
            {
                // Although this is a callback, we don't have to jump back onto the main thread, because we'll already
                // be on it, as we pump these from the main thread, from the RunCallbacks call in Main.Update.
                ModContent.GetInstance<DiscordSdk>().GetToken(resultOrToken =>
                {
                    if (resultOrToken is Result result)
                    {
                        // FIXME: I don't like that we resolve escape sequences here, wish it was elsewhere... only
                        //        instance of it so far so it's alright.
                        Main.statusText =
                            string.Format(
                                Language.GetOrRegister(
                                        Mod.GetLocalizationKey("DiscordIdentification.FailedToGetIdentityFromDiscord"))
                                    .Value
                                    .Replace("\\n", "\n"), result);
                        Main.menuMode = MenuID.MultiplayerJoining;
                        Netplay.Disconnect = true;
                    }
                    else
                    {
                        try
                        {
                            Netplay.ServerPassword = ((OAuth2Token)resultOrToken).AccessToken;
                            NetMessage.SendData(MessageID.SendPassword);
                        }
                        finally
                        {
                            Netplay.ServerPassword = null;
                        }

                        Main.statusText = Language
                            .GetOrRegister(
                                Mod.GetLocalizationKey("DiscordIdentification.WaitingForServerToAcceptIdentity")).Value;
                    }
                });

                Main.statusText = Language
                    .GetOrRegister(Mod.GetLocalizationKey("DiscordIdentification.WaitingForIdentityFromDiscord")).Value;

                return true;
            }
        }
        else
        {
            var adventurePlayer = Main.player[playerNumber].GetModPlayer<AdventurePlayer>();

            if (messageType == MessageID.SendPassword && Netplay.Clients[playerNumber].State == -1)
            {
                adventurePlayer.SetDiscordToken(reader.ReadString(), succeeded =>
                {
                    // The player may have disconnected since.
                    // FIXME: The player may also have been replaced since (original player leaves and another joins, taking this player index)
                    if (!Netplay.Clients[playerNumber].IsActive)
                        return;

                    if (!succeeded)
                    {
                        NetMessage.BootPlayer(playerNumber,
                            NetworkText.FromKey("Mods.PvPAdventure.DiscordIdentification.UnableToVerifyIdentity"));
                        return;
                    }

                    // Checking for duplicate Discord user -- let's only allow users on their currently connected player
                    if (Main.player
                        .Where(predicatePlayer => predicatePlayer != null && predicatePlayer.active &&
                                                  predicatePlayer.whoAmI != playerNumber)
                        .Select(predicatePlayer => predicatePlayer.GetModPlayer<AdventurePlayer>())
                        .Any(predicatePlayer => predicatePlayer.DiscordUser?.Id == adventurePlayer.DiscordUser.Id))
                    {
                        NetMessage.BootPlayer(playerNumber,
                            NetworkText.FromKey("Mods.PvPAdventure.DiscordIdentification.LoggedInElsewhere"));
                        return;
                    }

                    if (!OnPlayerJoin(adventurePlayer.Player))
                    {
                        NetMessage.BootPlayer(playerNumber,
                            NetworkText.FromKey("Mods.PvPAdventure.DiscordIdentification.NotPermittedToJoin"));
                        return;
                    }

                    Netplay.Clients[playerNumber].State = 1;
                    // This packet is known as SetUserSlot in Tappy
                    // It is NOT equivalent to Tappy's PlayerInfo -- it is simply named awfully.
                    NetMessage.SendData(MessageID.PlayerInfo, playerNumber);
                });
                return true;
            }
        }

        return false;
    }

    public class PlayerJoinArgs : EventArgs
    {
        public bool Allowed { get; set; }
        public Player Player { get; init; }
    }
}