using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Core.Config;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.Chat.Commands;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace PvPAdventure.Common.Teams;

[Autoload(Side = ModSide.Client)]
public class TeamChatManager : ModSystem
{
    public enum Channel
    {
        All,
        Team
    }

    private Channel _channel = Channel.All;
    private FieldInfo _chatCommandIdName;
    private MethodInfo _soundEnginePlaySoundLegacy;

    // Toggle channels.
    private string _savedChatText = ""; // save and restore that chat text if switching channels with text.
    private bool _tabLatch; // latch to tab
    private Channel? _forcedNextOpen; // close and re-open next tick.

    // Toggle text fade
    private int _chatOpenedTick = -1;

    private static bool JustPressed(Keys k) => Main.keyState.IsKeyDown(k) && !Main.oldKeyState.IsKeyDown(k);

    public override void Load()
    {
        _chatCommandIdName = typeof(ChatCommandId).GetField("_name", BindingFlags.Instance | BindingFlags.NonPublic);
        _soundEnginePlaySoundLegacy =
            typeof(SoundEngine).GetMethod("PlaySound", BindingFlags.Static | BindingFlags.NonPublic,
                [typeof(int), typeof(int), typeof(int), typeof(int), typeof(float), typeof(float)]);

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

        // Comment this out! Always allow tab
        //var cfg = ModContent.GetInstance<ClientConfig>();
        //if (!cfg.TabToSwitchChannel)
        //    return;

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

            _chatOpenedTick = (int)Main.GameUpdateCount;
            orig();
            return;
        }

        if (Main.keyState.PressingShift())
            _channel = Channel.All;
        else
            _channel = Channel.Team;

        if (_channel == Channel.Team && Main.LocalPlayer.team == 0)
            _channel = Channel.All;

        _chatOpenedTick = (int)Main.GameUpdateCount;
        orig();
    }

    private void OnMainDrawPlayerChat(On_Main.orig_DrawPlayerChat orig, Main self)
    {
        orig(self);

        if (Main.netMode == NetmodeID.SinglePlayer || !Main.drawingPlayerChat)
            return;

        var channelString = $"({_channel.ToString().ToUpper()})";
        var color = _channel == Channel.Team ? Main.teamColor[Main.LocalPlayer.team] : new Color(220, 220, 220);

        var size = ChatManager.GetStringSize(FontAssets.MouseText.Value, channelString, Vector2.One);

        // Draw (TEAM) or (ALL)
        ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.MouseText.Value,
            $"({_channel.ToString().ToUpper()})",
            new((int)((78.0f / 2.0f) - (size.X / 2.0f)), (int)(Main.screenHeight - 36.0f + 5.0f)),
            color,
            0.0f,
            Vector2.Zero,
            Vector2.One);

        // Draw Tab to switch (for half a second)
        var config = ModContent.GetInstance<ClientConfig>();
        if (!config.TabToSwitchChannel)
            return;

        if (Main.chatText != "")
            return;

        int openedTick = _chatOpenedTick;
        if (openedTick < 0)
            return;

        float t = ((int)Main.GameUpdateCount - openedTick) / 60f; // seconds

        // alpha: 0.5 until 0.5s, then fade to 0 by 1.0s
        float alpha = 1.0f;
        if (t >= 1.0f)
            alpha = 0.0f;
        else if (t > 0.5f)
            alpha = 1.0f * (1f - (t - 0.5f) / 0.5f);

        // debug: always draw
        alpha = 1.0f;

        if (alpha <= 0f)
            return;

        size = ChatManager.GetStringSize(FontAssets.MouseText.Value, "(TEAM)", Vector2.One);

        //color = _channel == Channel.All ? Main.teamColor[Main.LocalPlayer.team] : new Color(220, 220, 220);
        color = new Color(220,220,220);
        color = Color.LightGray;

        if (Main.LocalPlayer.team == 0)
        {
            return;
        }

        ChatManager.DrawColorCodedStringWithShadow(
            Main.spriteBatch, FontAssets.MouseText.Value,
            "Press [Tab] \n to Switch",
            new((int)((78.0f / 2.0f) - (size.X / 2.0f) -1f), (int)(Main.screenHeight - 78)),
            color * alpha,
            0.0f,
            Vector2.Zero,
            new Vector2(0.82f)
        );
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
            (string)_chatCommandIdName.GetValue(chatMessage.CommandId) ==
            (string)_chatCommandIdName.GetValue(ChatCommandId.FromType<SayChatCommand>()))
        {
            chatMessage.SetCommand<PartyChatCommand>();
        }

        return chatMessage;
    }

    public void OpenAllChat()
    {
        // Copied from Main.DoUpdate_Enter_ToggleChat
        if (!Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftAlt) &&
            !Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightAlt) &&
            Main.hasFocus)
        {
            if (!Main.InGameUI.IsVisible &&
                !Main.ingameOptionsWindow &&
                Main.chatRelease &&
                !Main.drawingPlayerChat &&
                !Main.editSign &&
                !Main.editChest &&
                !Main.gameMenu &&
                !Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape))
            {
                // SoundEngine.PlaySound(10);
                _soundEnginePlaySoundLegacy.Invoke(null, [10, -1, -1, 1, 1.0f, 0.0f]);
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

    private void TryPlayMenuTick()
    {
        if (_soundEnginePlaySoundLegacy != null)
            _soundEnginePlaySoundLegacy.Invoke(null, [10, -1, -1, 1, 1.0f, 0.0f]);
    }
}