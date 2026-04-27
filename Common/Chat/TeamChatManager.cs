using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.Chat.Commands;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace PvPAdventure.Common.Chat;

[Autoload(Side = ModSide.Client)]
public class TeamChatManager : ModSystem
{
    public enum Channel
    {
        All,
        Team
    }

    // Toggle channels.
    private Channel _channel = Channel.Team; // default channel, then remembers the last selected channel.
    private string _savedChatText = ""; // save and restore that chat text if switching channels with text.
    private bool _tabLatch; // latch to tab
    private Channel? _forcedNextOpen; // close and re-open next tick.


    public override void Load()
    {
        // Pick a channel when you open the chat.
        On_Main.OpenPlayerChat += OnMainOpenPlayerChat;
        // Visualize which channel your message will be sent to.
        On_Main.DrawPlayerChat += OnMainDrawPlayerChat;
        // Route your message to the correct channel.
        On_ChatCommandProcessor.CreateOutgoingMessage += OnChatCommandProcessorCreateOutgoingMessage;
    }

    public override void PostUpdateInput()
    {
        if (Main.dedServ)
            return;

        // release latch when tab released
        if (!Main.keyState.IsKeyDown(Keys.Tab))
            _tabLatch = false;

        if (!Main.drawingPlayerChat)
            return;

        if (_tabLatch || !JustPressed(Keys.Tab))
            return;

        _tabLatch = true;

        Channel next = _channel == Channel.Team ? Channel.All : Channel.Team;

        if (next == Channel.Team && Main.LocalPlayer.team == 0)
        {
            TryPlayMenuTick();
            return;
        }

        _savedChatText = Main.chatText ?? "";
        _forcedNextOpen = next;

        // close + reopen same tick (no flicker)
        Main.ClosePlayerChat();
        Main.drawingPlayerChat = false; // make sure it is actually closed right now

        TryPlayMenuTick();
        Main.OpenPlayerChat(); // OnMainOpenPlayerChat consumes _forcedNextOpen

        if (Main.drawingPlayerChat)
            Main.chatText = _savedChatText;

        _savedChatText = "";
    }

    private void OnMainOpenPlayerChat(On_Main.orig_OpenPlayerChat orig)
    {
        if (_forcedNextOpen.HasValue)
        {
            _channel = _forcedNextOpen.Value;
            _forcedNextOpen = null;

            if (_channel == Channel.Team && Main.LocalPlayer.team == 0)
                _channel = Channel.All;

            orig();
            return;
        }

        //_channel = Main.keyState.PressingShift() ? Channel.All : Channel.Team;

        if (_channel == Channel.Team && Main.LocalPlayer.team == 0)
            _channel = Channel.All;

        orig();
    }

    private void OnMainDrawPlayerChat(On_Main.orig_DrawPlayerChat orig, Main self)
    {
        orig(self);

        if (Main.netMode == NetmodeID.SinglePlayer || !Main.drawingPlayerChat)
            return;

        string channelString = $"({_channel.ToString().ToUpper()})";
        Color color = _channel == Channel.Team ? Main.teamColor[Main.LocalPlayer.team] : new Color(220, 220, 220);
        Vector2 size = ChatManager.GetStringSize(FontAssets.MouseText.Value, channelString, Vector2.One);

        // Draw (TEAM) or (ALL)
        ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.MouseText.Value,
            channelString,
            new Vector2((int)(39f - size.X * 0.5f), (int)(Main.screenHeight - 31f)),
            color,
            0f,
            Vector2.Zero,
            Vector2.One);
    }

    private ChatMessage OnChatCommandProcessorCreateOutgoingMessage(
        On_ChatCommandProcessor.orig_CreateOutgoingMessage orig, ChatCommandProcessor self, string text)
    {
        var chatMessage = orig(self, text);

        // FIXME: The parent function here will invoke ProcessOutgoingMessage for it's original ChatCommandId, which
        //        probably isn't good. Worse, we don't invoke ProcessOutgoingMessage for the new ChatCommandId.
        //        For our purposes (the say command and the party command), this is probably fine.
        // NOTE: Need to check starting with '/' because TML shoves it's commands into the SayChatCommand and handles
        //       it in some obtuse manner. Even this is not correct, because we don't assert that a leading slash
        //       leads to any command being handled -- but it's the best we have.
        if (!text.StartsWith('/') &&
            _channel == Channel.Team &&
            chatMessage.CommandId._name == ChatCommandId.FromType<SayChatCommand>()._name)
        {
            chatMessage.SetCommand<PartyChatCommand>();
        }

        return chatMessage;
    }

    #region Helpers
    /// <summary>
    /// Keybind to open the chat in all chat mode, regardless of team status. Copied from Main.DoUpdate_Enter_ToggleChat.
    /// </summary>
    public void OpenAllChat()
    {
        if (!Main.keyState.IsKeyDown(Keys.LeftAlt) &&
            !Main.keyState.IsKeyDown(Keys.RightAlt) &&
            Main.hasFocus)
        {
            if (!Main.InGameUI.IsVisible &&
                !Main.ingameOptionsWindow &&
                Main.chatRelease &&
                !Main.drawingPlayerChat &&
                !Main.editSign &&
                !Main.editChest &&
                !Main.gameMenu &&
                !Main.keyState.IsKeyDown(Keys.Escape))
            {
                SoundEngine.PlaySound(10);
                Main.OpenPlayerChat();
                _channel = Channel.All;
                Main.chatText = "";
            }

            Main.chatRelease = false;
        }
        else
        {
            Main.chatRelease = true;
        }
    }
    private static bool JustPressed(Keys k) => Main.keyState.IsKeyDown(k) && !Main.oldKeyState.IsKeyDown(k);
    private static void TryPlayMenuTick() => SoundEngine.PlaySound(10, -1, -1, 1, 1f, 0f);
    #endregion
}