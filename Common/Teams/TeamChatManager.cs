using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Core.Config;
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

    public void ToggleChatChannel()
    {
        if (_channel == Channel.All)
        {
            if (Main.LocalPlayer.team == 0)
            {
                _channel = Channel.All;
                Main.NewText("You must join a team to use TEAM chat.", Color.OrangeRed);
                return;
            }

            _channel = Channel.Team;
        }
        else
        {
            _channel = Channel.All;
        }

        TryPlayMenuTick();
    }

    private void TryPlayMenuTick()
    {
        if (_soundEnginePlaySoundLegacy != null)
            _soundEnginePlaySoundLegacy.Invoke(null, [10, -1, -1, 1, 1.0f, 0.0f]);
    }

    private void OnMainOpenPlayerChat(On_Main.orig_OpenPlayerChat orig)
    {
        _channel = Channel.Team;

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
        Color channelColor = _channel == Channel.Team ? Main.teamColor[Main.LocalPlayer.team] : Color.White;

        Vector2 channelSize = ChatManager.GetStringSize(FontAssets.MouseText.Value, channelString, Vector2.One);

        Vector2 channelPos = new(
            (int)((78.0f / 2.0f) - (channelSize.X / 2.0f)),
            (int)(Main.screenHeight - 36.0f + 5.0f)
        );

        ChatManager.DrawColorCodedStringWithShadow(
            Main.spriteBatch,
            FontAssets.MouseText.Value,
            channelString,
            channelPos,
            channelColor,
            0.0f,
            Vector2.Zero,
            Vector2.One
        );

        string next = _channel == Channel.Team ? "ALL" : "TEAM";
        string prompt = $"TAB: {next}";

        Color promptColor = Color.Gray;
        Vector2 promptSize = ChatManager.GetStringSize(FontAssets.MouseText.Value, prompt, Vector2.One);

        Vector2 promptPos = new(
            (int)((78.0f / 2.0f) - (promptSize.X / 2.0f)),
            (int)(channelPos.Y + channelSize.Y + 2.0f)
        );

        ChatManager.DrawColorCodedStringWithShadow(
            Main.spriteBatch,
            FontAssets.MouseText.Value,
            prompt,
            promptPos,
            promptColor,
            0.0f,
            Vector2.Zero,
            Vector2.One
        );
    }

    private ChatMessage OnChatCommandProcessorCreateOutgoingMessage(
        On_ChatCommandProcessor.orig_CreateOutgoingMessage orig, ChatCommandProcessor self, string text)
    {
        ChatMessage chatMessage = orig(self, text);

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
                TryPlayMenuTick();
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
}
