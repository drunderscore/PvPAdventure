using PvPAdventure.Core.Config.ConfigElements;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PvPAdventure.Common.Travel.UI;
using System;
using System.ComponentModel;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace PvPAdventure.Core.Config;

public class ClientConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;

    public enum TravelUIPosition
    {
        Top,
        Bottom,
    }

    public enum TravelUISize
    {
        VerySmall,
        Small,
        Medium,
        Big,
    }

    [Header("Visualization")]
    [HeaderIcon(nameof(Ass.ConfigPlayerOutline))]

    [BackgroundColor(126, 62, 88)]
    [DefaultValue(false)] 
    [ConfigIcon(ItemID.PlumbersShirt)]
    public bool ShowVanityVisuals = false;

    [Header("UI")]
    [BackgroundColor(36, 104, 118)]
    [DefaultValue(TravelUIPosition.Top)]
    [JsonConverter(typeof(StringEnumConverter))]
    public TravelUIPosition PortalTravelUIPosition = TravelUIPosition.Top;

    [BackgroundColor(36, 104, 118)]
    [DefaultValue(TravelUISize.Small)]
    [JsonConverter(typeof(StringEnumConverter))]
    public TravelUISize PortalTravelUISize = TravelUISize.Small;

    [Header("Chat")]
    [HeaderIcon(nameof(Ass.ConfigChat))]

    [BackgroundColor(70, 92, 126)]
    [DefaultValue(true)]
    public bool ShowTeleportPlayerMessages = true;

    [BackgroundColor(70, 92, 126)]
    [DefaultValue(false)]
    public bool ShowDebugMessages = false;

    #region NestedConfigTypes
    #endregion

    #region Methods
    public override void OnChanged()
    {
        base.OnChanged();
        Log.Chat("Client config changed");

        // Rebuild travel UI
        var travelUISystem = ModContent.GetInstance<TravelUISystem>();
        if (travelUISystem != null)
        {
            travelUISystem?.travelUIState?.ForceRebuildNextUpdate();
        }
    }
    #endregion
}
