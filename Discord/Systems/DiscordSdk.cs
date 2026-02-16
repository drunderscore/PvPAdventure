using PvPAdventure.Discord.GameSdk;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using GameSdk = PvPAdventure.Discord.GameSdk;

namespace PvPAdventure.Discord.Systems;

[Autoload(Side = ModSide.Client)]
public sealed class DiscordSdk : ModSystem
{
    private const long ClientId = 1298376502999646238;

    private GameSdk.Discord discord;
    private OAuth2Token? token;

    private readonly List<Action<object>> pending = new();
    private bool requesting;
    private int retryTicks;

    public override void OnModLoad()
    {
        if (!DiscordIdentification.IsEnabled)
            return;

        // FIXME: Apparently, loading mod content is not protected and throwing here will just close the game, unlike throwing in Mod.Load
        //        Which will handle it properly and communicate to the end user... but why? This needs to be propagated somehow to indicate
        //        that something failed to the user.
        try
        {
            discord = new GameSdk.Discord(ClientId, (ulong)CreateFlags.Default);
        }
        catch (ResultException e)
        {
            if (e.Result == Result.NotRunning)
                throw new Exception("Discord could not be found running", e);

            throw;
        }

        On_Main.Update += (orig, self, time) =>
        {
            orig(self, time);

            if (discord == null)
                return;

            try
            {
                discord.RunCallbacks();
            }
            catch (Exception e)
            {
                Log.Error("Failed to RunCallbacks for Discord: " + e);
            }

            TickTokenRequest();
        };
    }

    public override void OnModUnload()
    {
        discord?.Dispose();
        discord = null;
        token = null;

        pending.Clear();
        requesting = false;
        retryTicks = 0;
    }

    public void GetToken(Action<object> callback)
    {
        if (!DiscordIdentification.IsEnabled)
            return;

        Log.Debug("Discord token requested");

        if (token != null)
        {
            callback(token);
            return;
        }

        pending.Add(callback);
        TickTokenRequest();
    }

    private void TickTokenRequest()
    {
        if (token != null || pending.Count == 0 || requesting)
            return;

        if (retryTicks > 0)
        {
            retryTicks--;
            return;
        }

        if (discord == null)
        {
            try
            {
                discord = new GameSdk.Discord(ClientId, (ulong)CreateFlags.Default);
                Log.Debug("Discord SDK initialized");
            }
            catch (ResultException e)
            {
                Log.Debug("Discord SDK init failed: " + e.Result);
                Flush(e.Result);
                return;
            }
        }

        requesting = true;
        Log.Debug("Requesting OAuth token");

        discord.GetApplicationManager().GetOAuth2Token((Result result, ref OAuth2Token sdkToken) =>
        {
            requesting = false;

            Log.Debug("OAuth token result: " + result);

            if (result == Result.Ok)
            {
                token = sdkToken;
                Flush(token);
                return;
            }

            if (result == Result.InvalidCommand)
            {
                // Discord sometimes isn't ready yet; retry shortly
                retryTicks = 15;
                return;
            }

            Flush(result);
        });
    }

    private void Flush(object value)
    {
        for (int i = 0; i < pending.Count; i++)
            pending[i](value);

        pending.Clear();
    }
}
