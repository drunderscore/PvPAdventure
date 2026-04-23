using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Core.Config;
using System.Reflection;
using System.Text.RegularExpressions;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.Chat.Commands;
using Terraria.GameContent;
using Terraria.GameContent.UI.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace PvPAdventure.Common.Teams;

[Autoload(Side = ModSide.Client)]
public class TeamChatManager : ModSystem
{
    private const string TeamSystemMarker = "\u0002PVPA_TEAM_SYS\u0002";
    private static readonly Regex NameTagRegex = new(@"\[n:(?<name>[^\]]+)\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ColorTagRegex = new(@"\[c\/[0-9a-fA-F]{3,8}:(?<text>[^\]]*?)(?::)?\]", RegexOptions.Compiled);

    public enum Channel
    {
        All,
        Team
    }

    private Channel _channel = Channel.All;
    private FieldInfo _chatCommandIdName;
    private MethodInfo _soundEnginePlaySoundLegacy;

    internal static Channel CurrentChannel
    {
        get
        {
            TeamChatManager system = ModContent.GetInstance<TeamChatManager>();
            return system?._channel ?? Channel.All;
        }
    }

    // Toggle channels.
    private string _savedChatText = ""; // save and restore that chat text if switching channels with text.
    private bool _tabLatch; // latch to tab
    private Channel? _forcedNextOpen; // close and re-open next tick.

    // Toggle text fade
    private int _chatOpenedTick = -1;

    private static bool JustPressed(Keys k) => Main.keyState.IsKeyDown(k) && !Main.oldKeyState.IsKeyDown(k);

    public static void SendSystemTeamMessage(Player player, string text, Color color)
    {
        if (player == null || !player.active)
            return;

        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            Main.NewText(text, color);
            return;
        }

        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            if (player.whoAmI == Main.myPlayer)
                Main.NewText(text, color);

            return;
        }

        NetworkText message = NetworkText.FromLiteral(TeamSystemMarker + text);

        if (player.team == 0)
        {
            ChatHelper.SendChatMessageToClient(message, color, player.whoAmI);
            return;
        }

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player teammate = Main.player[i];
            if (teammate != null && teammate.active && teammate.team == player.team)
                ChatHelper.SendChatMessageToClient(message, color, i);
        }
    }

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
        // Route team chat and add the visual prefix to rendered chat.
        On_ChatCommandProcessor.CreateOutgoingMessage += OnChatCommandProcessorCreateOutgoingMessage;
        On_RemadeChatMonitor.AddNewMessage += OnAddNewMessage;
    }

    public override void Unload()
    {
        On_Main.OpenPlayerChat -= OnMainOpenPlayerChat;
        On_Main.DrawPlayerChat -= OnMainDrawPlayerChat;
        On_ChatCommandProcessor.CreateOutgoingMessage -= OnChatCommandProcessorCreateOutgoingMessage;
        On_RemadeChatMonitor.AddNewMessage -= OnAddNewMessage;
    }

    public override void PostUpdateInput()
    {
        if (Main.dedServ)
            return;

        if (Main.netMode == NetmodeID.SinglePlayer)
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
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            _channel = Channel.All;
            _forcedNextOpen = null;
            _chatOpenedTick = (int)Main.GameUpdateCount;
            orig();
            return;
        }

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
            "   [Tab] \n to Switch",
            new((int)((78.0f / 2.0f) - (size.X / 2.0f) -3f), (int)(Main.screenHeight - 78)),
            color * alpha,
            0.0f,
            Vector2.Zero,
            new Vector2(0.82f)
        );
    }

    private ChatMessage OnChatCommandProcessorCreateOutgoingMessage(
        On_ChatCommandProcessor.orig_CreateOutgoingMessage orig, ChatCommandProcessor self, string text)
    {
        ChatMessage chatMessage = orig(self, text);

        if (Main.netMode == NetmodeID.SinglePlayer || text.StartsWith('/'))
            return chatMessage;

        if (_channel == Channel.Team &&
            (string)_chatCommandIdName.GetValue(chatMessage.CommandId) ==
            (string)_chatCommandIdName.GetValue(ChatCommandId.FromType<SayChatCommand>()))
        {
            chatMessage.SetCommand<PartyChatCommand>();
        }

        return chatMessage;
    }

    private void OnAddNewMessage(On_RemadeChatMonitor.orig_AddNewMessage orig, RemadeChatMonitor self, string text, Color color, int widthLimitInPixels)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            orig(self, text, color, widthLimitInPixels);
            return;
        }

        bool isTeam = false;

        if (text.Contains(TeamSystemMarker))
        {
            text = text.Replace(TeamSystemMarker, string.Empty);
            isTeam = true;
        }
        else if (!IsAllChatColor(color))
        {
            isTeam = true;
        }

        if (!isTeam)
            text = NormalizeAllChatText(text);

        text = InsertPrefix(text, isTeam ? " (TEAM)" : " (ALL)");

        Color rowColor = isTeam && Main.LocalPlayer.team > 0 ? Main.teamColor[Main.LocalPlayer.team] : Color.White;
        orig(self, text, rowColor, widthLimitInPixels);
    }

    public void OpenAllChat()
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
            return;

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

    private static bool IsAllChatColor(Color color)
    {
        int delta = System.Math.Abs(color.R - color.G) + System.Math.Abs(color.G - color.B) + System.Math.Abs(color.R - color.B);
        return delta < 24 && color.R >= 180 && color.G >= 180 && color.B >= 180;
    }

    private static string NormalizeAllChatText(string text)
    {
        text = NameTagRegex.Replace(text, match =>
        {
            string name = match.Groups["name"].Value;
            return ModLoader.HasMod("ChatPlus") ? $"{name}:" : $"[n:{name}]";
        });

        while (ColorTagRegex.IsMatch(text))
            text = ColorTagRegex.Replace(text, "${text}");

        return text;
    }

    private static string InsertPrefix(string text, string prefix)
    {
        if (text.Contains("(TEAM)") || text.Contains("(ALL)"))
            return text;

        int index = 0;
        while (index < text.Length)
        {
            while (index < text.Length && char.IsWhiteSpace(text[index]))
                index++;

            if (index >= text.Length || text[index] != '[')
                break;

            int end = text.IndexOf(']', index);
            if (end < 0)
                break;

            string tag = text[index..(end + 1)];
            if (!tag.StartsWith("[m:", System.StringComparison.OrdinalIgnoreCase) &&
                !tag.StartsWith("[p:", System.StringComparison.OrdinalIgnoreCase) &&
                !tag.StartsWith("[player:", System.StringComparison.OrdinalIgnoreCase) &&
                !tag.StartsWith("[playericon:", System.StringComparison.OrdinalIgnoreCase) &&
                !tag.StartsWith("[i:", System.StringComparison.OrdinalIgnoreCase) &&
                !tag.StartsWith("[item:", System.StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            index = end + 1;
            if (index < text.Length && text[index] == ' ')
                index++;
        }

        return text.Insert(index, prefix + " ");
    }
}
