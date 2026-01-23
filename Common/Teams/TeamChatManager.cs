using System.Reflection;
using Microsoft.Xna.Framework;
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

    private void OnMainOpenPlayerChat(On_Main.orig_OpenPlayerChat orig)
    {
        //if (ModContent.GetInstance<ClientConfig>().ShiftEnterOpensAllChat && Main.keyState.PressingShift())
            //_channel = Channel.All;
        //else
            _channel = Channel.Team;

        orig();
    }

    private void OnMainDrawPlayerChat(On_Main.orig_DrawPlayerChat orig, Main self)
    {
        orig(self);

        if (Main.netMode == NetmodeID.SinglePlayer || !Main.drawingPlayerChat)
            return;

        var channelString = $"({_channel.ToString().ToUpper()})";
        var color = _channel == Channel.Team ? Main.teamColor[Main.LocalPlayer.team] : Color.White;

        var size = ChatManager.GetStringSize(FontAssets.MouseText.Value, channelString, Vector2.One);

        ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.MouseText.Value,
            $"({_channel.ToString().ToUpper()})",
            new((int)((78.0f / 2.0f) - (size.X / 2.0f)), (int)(Main.screenHeight - 36.0f + 5.0f)),
            color,
            0.0f,
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
}