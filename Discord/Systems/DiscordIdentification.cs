using Mono.Cecil.Cil;
using MonoMod.Cil;
using PvPAdventure.Core.Config;
using PvPAdventure.Discord.GameSdk;
using System;
using System.IO;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Discord.Systems;

[Autoload(Side = ModSide.Both)]
public sealed class DiscordIdentification : ModSystem
{
    private static ulong?[] verifiedUserIds;

    public static bool IsEnabled
    {
        get
        {
            var config = ModContent.GetInstance<SSCConfig>();
            if (config == null)
            {
                Log.Error("No config found, disabling Discord identification.");
                return false;
            }

            return config.SSCPlayerNames == SSCConfig.SSCPlayerNameType.Discord;
        }
    }

    public override void Load()
    {
        if (!IsEnabled)
            return;

        if (Main.dedServ)
        {
            verifiedUserIds = new ulong?[Main.maxPlayers];
            IL_MessageBuffer.GetData += OnGetDataIL;
        }
    }

    public override void OnWorldUnload()
    {
        if (!Main.dedServ || verifiedUserIds == null)
            return;

        Array.Clear(verifiedUserIds, 0, verifiedUserIds.Length);
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

    public override bool HijackGetData(ref byte messageType, ref BinaryReader reader, int playerNumber)
    {
        if (!IsEnabled)
            return false;

        // Helper: fixes statusText showing localization keys.
        string L(string suffix) => Language.GetTextValue(Mod.GetLocalizationKey(suffix));

        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            // FIXME: The user should be prompted if they wish to share their account with the server.
            if (messageType == MessageID.RequestPassword)
            {
                Log.Debug("Server requested Discord identity");

                Main.statusText = L("DiscordIdentification.WaitingForIdentityFromDiscord");

                // Although this is a callback, we don't have to jump back onto the main thread, because we'll already
                // be on it, as we pump these from the main thread, from the RunCallbacks call in Main.Update.
                ModContent.GetInstance<DiscordSdk>().GetToken(resultOrToken =>
                {
                    if (resultOrToken is Result result)
                    {
                        Log.Debug("Discord identity fetch failed: " + result);

                        Main.statusText = string.Format(
                            L("DiscordIdentification.FailedToGetIdentityFromDiscord").Replace("\\n", "\n"),
                            result);

                        Main.menuMode = MenuID.MultiplayerJoining;
                        Netplay.Disconnect = true;
                        return;
                    }

                    string token = ((OAuth2Token)resultOrToken).AccessToken;

                    Log.Debug("Discord Identity fetched, sending to server");
                    try
                    {
                        Netplay.ServerPassword = token;
                        NetMessage.SendData(MessageID.SendPassword);
                    }
                    finally
                    {
                        Netplay.ServerPassword = null;
                    }

                    // Cache locally for SSC naming
                    _ = DiscordIdentity.CacheAsync(token);

                    Main.statusText = L("DiscordIdentification.WaitingForServerToAcceptIdentity");
                });

                return true;
            }
        }
        else
        {
            // Server is requesting the player's Discord identity -- let's read the token and attempt to verify it, and then respond with success or failure.
            if (messageType == MessageID.SendPassword && Netplay.Clients[playerNumber].State == -1)
            {
                Log.Debug("Identity received from player " + playerNumber);

                string token = reader.ReadString();
                _ = VerifyAndAcceptAsync(playerNumber, token);
                return true;
            }
        }

        return false;
    }

    private static async Task VerifyAndAcceptAsync(int playerNumber, string token)
    {
        var identity = await DiscordIdentity.FetchAsync(token).ConfigureAwait(false);

        Main.QueueMainThreadAction(() =>
        {
            if (!Netplay.Clients[playerNumber].IsActive)
                return;

            if (!identity.HasValue || identity.Value.UserId == 0)
            {
                Log.Debug("Discord verify failed for " + playerNumber);
                NetMessage.BootPlayer(playerNumber,
                    NetworkText.FromKey("Mods.PvPAdventure.DiscordIdentification.UnableToVerifyIdentity"));
                return;
            }

            ulong userId = identity.Value.UserId;

            for (int i = 0; i < verifiedUserIds.Length; i++)
            {
                if (i == playerNumber)
                    continue;

                if (!Netplay.Clients[i].IsActive)
                    continue;

                if (verifiedUserIds[i].HasValue && verifiedUserIds[i].Value == userId)
                {
                    Log.Debug("Duplicate Discord user for " + playerNumber);
                    NetMessage.BootPlayer(playerNumber,
                        NetworkText.FromKey("Mods.PvPAdventure.DiscordIdentification.LoggedInElsewhere"));
                    return;
                }
            }

            verifiedUserIds[playerNumber] = userId;
            Netplay.Clients[playerNumber].State = 1;

            Log.Debug("Discord verify OK for " + playerNumber);
            NetMessage.SendData(MessageID.PlayerInfo, playerNumber);
        });
    }
}
