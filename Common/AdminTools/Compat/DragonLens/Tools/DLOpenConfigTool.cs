using DragonLens.Core.Systems.ThemeSystem;
using DragonLens.Core.Systems.ToolSystem;
using DragonLens.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.GameTimer;
using PvPAdventure.Core.Config;
using PvPAdventure.Core.Net;
using PvPAdventure.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.UI;
using Terraria.UI;
using static PvPAdventure.Common.GameTimer.GameTimerNetHandler;

namespace PvPAdventure.Common.AdminTools.Compat.DragonLens.Tools;

[JITWhenModsEnabled("DragonLens")]
[ExtendsFromMod("DragonLens")]
public class DLOpenConfigTool : Tool
{
    public override string IconKey => DLToolIcons.OpenConfigKey;

    public override string DisplayName => Language.GetTextValue($"Mods.PvPAdventure.Tools.DLOpenConfigTool.DisplayName");

    public override string Description => Language.GetTextValue($"Mods.PvPAdventure.Tools.DLOpenConfigTool.Description");

    //string.Format(
    //    Language.GetTextValue("Mods.PvPAdventure.Tools.DLPauseTool.Description"),
    //    Language.GetTextValue($"Mods.PvPAdventure.Tools.DLPauseTool.DisplayName.{ModContent.GetInstance<PauseManager>().IsPaused}").ToLower()
    //);

    public override void OnActivate()
    {
        if (IsConfigOpen())
            CloseConfig();
        else
            OpenConfig();
    }

    private void OpenConfig()
    {
        ModConfig serverConfig = ModContent.GetInstance<ServerConfig>();

        Interface.modConfig.SetMod(
            Mod,
            serverConfig,
            openedFromModder: true,          
            onClose: OnConfigClosed,      
            scrollToOption: null,
            centerScrolledOption: true
        );

        Main.InGameUI.SetState(Interface.modConfig);
        Main.menuMode = 10024;
        Main.playerInventory = false;
    }

    private static void OnConfigClosed()
    {
        Main.InGameUI.SetState(null);
        Main.menuMode = 0;
    }

    private static bool IsConfigOpen()
    {
        return Main.menuMode == 10024 &&
               ReferenceEquals(Main.InGameUI?.CurrentState, Interface.modConfig);
    }

    private static void CloseConfig()
    {
        Main.InGameUI.SetState(null);
        Main.menuMode = 0;
    }

    public override void DrawIcon(SpriteBatch spriteBatch, Rectangle position)
    {
        //base.DrawIcon(spriteBatch, position);

        //if (IsConfigOpen())
        //{
            //spriteBatch.Draw(Ass.Icon_ConfigClose.Value, position, Color.White);
        //}
        //else
        //{
            spriteBatch.Draw(Ass.Icon_ConfigOpen.Value, position, Color.White);
        //}

        if (IsConfigOpen())
        {
            GUIHelper.DrawOutline(spriteBatch, new Rectangle(position.X - 4, position.Y - 4, 46, 46), ThemeHandler.ButtonColor.InvertColor());

            Texture2D tex = DLToolIcons.GlowAlpha.Value;
            if (tex == null) return;

            Color color = new(255, 215, 150);
            color.A = 0;
            var target = new Rectangle(position.X, position.Y, 38, 38);

            spriteBatch.Draw(tex, target, color);
        }
    }
}