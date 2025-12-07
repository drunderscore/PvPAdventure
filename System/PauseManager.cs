using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace PvPAdventure.System;

public class PauseManager : ModSystem
{
    private bool _paused;
    public bool IsPaused => _paused;
    private Interface _interface;

    public override void Load()
    {
        // Modify the pause state on the client and the server.
        On_Main.CanPauseGame += OnCanPauseGame;
        // Prevent drops whilst the game is paused (right-click out of inventory specifically is problematic).
        On_Player.DropSelectedItem_int_refItem += OnDropSelectedItem;

        if (!Main.dedServ)
            _interface = new Interface(this);
    }

    private void OnDropSelectedItem(On_Player.orig_DropSelectedItem_int_refItem orig, Player self, int slot,
        ref Item theitemwedrop)
    {
        // The drop bind doesn't function whilst paused, but right-clicking out of your inventory does, locks up your
        // cursor and inventory interactions, and is generally buggy and likely unintended.
        if (Main.gamePaused)
            return;

        orig(self, slot, ref theitemwedrop);
    }

    private bool OnCanPauseGame(On_Main.orig_CanPauseGame orig)
    {
        var value = orig();
        value |= _paused;
        return value;
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if (!layers.Contains(_interface))
        {
            var layerIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Inventory");

            if (layerIndex != -1)
                layers.Insert(layerIndex + 1, _interface);
        }
    }

    public override void NetSend(BinaryWriter writer)
    {
        writer.Write(_paused);
    }

    public override void NetReceive(BinaryReader reader)
    {
        _paused = reader.ReadBoolean();
    }

    public sealed class Interface(PauseManager pauseManager)
        : GameInterfaceLayer("PvPAdventure: Pause", InterfaceScaleType.UI)
    {
        protected override bool DrawSelf()
        {
            // Only display this if our mod specifically has paused the game.
            if (!pauseManager._paused)
                return true;

            var size = ChatManager.GetStringSize(FontAssets.DeathText.Value, "PAUSED", Vector2.One);

            ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.DeathText.Value, "PAUSED",
                new Vector2((int)((Main.screenWidth / 2.0f) - (size.X / 2.0f)),
                    (int)(Main.screenHeight / 2.0f) - (size.Y / 2.0f)), Color.Red, 0.0f,
                Vector2.Zero,
                Vector2.One);

            return true;
        }
    }

    public class PauseCommand : ModCommand
    {
        public override void Action(CommandCaller caller, string input, string[] args)
        {
            var pause = ModContent.GetInstance<PauseManager>();
            pause._paused = !pause._paused;

            ChatHelper.BroadcastChatMessage(NetworkText.FromKey($"Mods.PvPAdventure.Pause.{pause._paused}"),
                Color.White);

            NetMessage.SendData(MessageID.WorldData);
        }

        public override string Command => "pause";
        public override CommandType Type => CommandType.Console;
    }

    public void PauseGame()
    {
        var pause = ModContent.GetInstance<PauseManager>();
        pause._paused = !pause._paused;

        ChatHelper.BroadcastChatMessage(NetworkText.FromKey($"Mods.PvPAdventure.Pause.{pause._paused}"),
            Color.White);

        NetMessage.SendData(MessageID.WorldData);
    }
}