//using Microsoft.Xna.Framework;
//using PvPAdventure.Core.Config;
//using System.Collections.Generic;
//using Terraria;
//using Terraria.ModLoader;
//using Terraria.UI;

//namespace PvPAdventure.Common.MatchHistory.Recording;

//[Autoload(Side = ModSide.Client)]
//public sealed class DemoUISystem : ModSystem
//{
//    // UI
//    public static UserInterface Interface;
//    public static RecordingUIState RecordingState;

//    // Enabled check
//    public static bool IsEnabled
//    {
//        get
//        {
//            return true;
//            var config = ModContent.GetInstance<ArenasConfig>();
//            //if (config == null)
//            //{
//                //Log.Warn("ServerConfig not loaded – Arenas disabled by default");
//                //return false;
//            //}

//            //return config.IsArenasEnabled;
//        }
//    }

//    public override void OnWorldLoad()
//    {
//        Interface = new();
//        RecordingState = new();

//        if (IsEnabled)
//        {
//            Toggle();
//        }
//    }

//    /// <summary> Flips the state of the Demo UI. </summary>
//    public static void Toggle() => Interface?.SetState(Interface.CurrentState == null ? RecordingState : null);

//    public override void UpdateUI(GameTime gameTime)
//    {
//        Interface?.Update(gameTime);
//    }

//    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
//    {
//        if (Interface?.CurrentState == null)
//            return;

//        int index = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
//        if (index == -1)
//            return;

//        layers.Insert(index, new LegacyGameInterfaceLayer(
//            "PvPAdventure: RecordingUISystem",
//            () =>
//            {
//                Interface.Draw(Main.spriteBatch, new GameTime());
//                return true;
//            },
//            InterfaceScaleType.UI));
//    }
//}
