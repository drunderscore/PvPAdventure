using System;
using Discord;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Discord;

[Autoload(Side = ModSide.Client)]
public class DiscordSdk : ModSystem
{
    private const LogLevel DiscordSdkLogLevel = LogLevel.Info;
    private const long DiscordSdkClientId = 1298376502999646238;
    private Discord _discord;
    private OAuth2Token? _token;

    public override void OnModLoad()
    {
        // FIXME: Apparently, loading mod content is not protected and throwing here will just close the game, unlike throwing in Mod.Load
        //        Which will handle it properly and communicate to the end user... but why? This needs to be propagated somehow to indicate
        //        that something failed to the user.
        try
        {
            _discord = new Discord(DiscordSdkClientId, (ulong)CreateFlags.Default);
        }
        catch (ResultException e)
        {
            if (e.Result == Result.NotRunning)
                throw new Exception("Discord could not be found running", e);

            throw;
        }

        _discord.SetLogHook(DiscordSdkLogLevel, (level, message) =>
        {
            Action<object> loggerFunction = level switch
            {
                LogLevel.Error => Mod.Logger.Error,
                LogLevel.Warn => Mod.Logger.Warn,
                LogLevel.Info => Mod.Logger.Info,
                LogLevel.Debug => Mod.Logger.Debug,
                _ => null
            };

            if (loggerFunction == null)
                return;

            loggerFunction($"[Discord]: {message}");
        });

        On_Main.Update += (orig, self, time) =>
        {
            orig(self, time);

            // FIXME: What if this does throw? We should inform the user, or can we somehow have TML handle it?
            try
            {
                _discord.RunCallbacks();
            }
            catch (Exception e)
            {
                Mod.Logger.Error("Failed to RunCallbacks for Discord", e);
            }
        };
    }

    public override void OnModUnload()
    {
        _discord?.Dispose();
    }

    // Comes back to you with either a Discord.Result that is non-Ok, or a Discord.OAuth2Token
    public void GetToken(Action<object> callback)
    {
        if (_token != null)
        {
            callback(_token);
            return;
        }

        _discord.GetApplicationManager().GetOAuth2Token((Result result, ref OAuth2Token token) =>
        {
            if (result != Result.Ok)
                callback(result);
            else
                callback(_token = token);
        });
    }
}