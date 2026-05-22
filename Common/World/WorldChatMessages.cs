using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.GameContent.Events;
using Terraria.Localization;
using Terraria.ModLoader;
using PvPAdventure.Core.Config;


namespace PvPAdventure.Common.World;

internal class WorldChatMessages : ModSystem
{
    public override void Load()
    {
        // Broadcast a message when rain starts.
        On_Main.StartRain += OnMainStartRain;

        // Broadcast a message when a sandstorm starts.
        On_Sandstorm.StartSandstorm += OnSandstormStartSandstorm;
    }

    public override void Unload()
    {
        On_Main.StartRain -= OnMainStartRain;
        On_Sandstorm.StartSandstorm -= OnSandstormStartSandstorm;
    }

    private void OnSandstormStartSandstorm(On_Sandstorm.orig_StartSandstorm orig)
    {
        orig();
        if (ModContent.GetInstance<ServerConfig>().BroadcastWeatherMessages)
        {
            ChatHelper.BroadcastChatMessage(NetworkText.FromKey("Mods.PvPAdventure.Sandstorm"), Color.White);
        }
    }

    private void OnMainStartRain(On_Main.orig_StartRain orig)
    {
        orig();
        if (ModContent.GetInstance<ServerConfig>().BroadcastWeatherMessages)
        {
            ChatHelper.BroadcastChatMessage(NetworkText.FromKey("Mods.PvPAdventure.Rain"), Color.White);
        }
    }
}
