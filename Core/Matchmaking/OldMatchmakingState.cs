//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;
//using Microsoft.Xna.Framework.Input;
//using PvPAdventure.Core.Matchmaking.UI;
//using System;
//using System.Linq;
//using Terraria;
//using Terraria.Audio;
//using Terraria.GameContent.UI.Elements;
//using Terraria.ID;
//using Terraria.UI;

//namespace PvPAdventure.Core.Matchmaking;

//public sealed class OldMatchmakingState : UIState
//{
//    private const string Hostname = "Tpvpa.terraria.sh";
//    private const int Port = 7777;
//    private const int QueueTarget = 3;
//    private const float ConnectDelaySeconds = 1f;

//    private UIText title;
//    private HoverButton matchmakingButton;
//    private HoverButton backButton;
//    private readonly Action onBackButtonPressed;
//    private readonly Action onCloseUi;
//    private UIText debugText;

//    private bool isQueuing;
//    private bool isConnecting;
//    private float connectTimer = -1f;
//    private string host;
//    private int port;
//    private int playersOnlineCount = 1;
//    private int playersQueuingCount = 0;

//    public OldMatchmakingState(Action onBack, Action onCloseUi)
//    {
//        onBackButtonPressed = onBack;
//        this.onCloseUi = onCloseUi;
//    }

//    public override void OnInitialize()
//    {
//        // Add title button
//        title = new("PvP Adventure", 1.1f, true) { HAlign = 0.5f };
//        title.Top.Set(200, 0f);
//        Append(title);

//        // Add matchmaking button
//        matchmakingButton = new("Play Ranked");
//        matchmakingButton.HAlign = 0.5f;
//        matchmakingButton.Top.Set(400, 0f);
//        Append(matchmakingButton);

//        // Add matchmaking button
//        backButton = new("Back");
//        backButton.HAlign = 0.5f;
//        backButton.Top.Set(700, 0f);
//        Append(backButton);

//        matchmakingButton.OnLeftClick += (_, __) => OnMatchmakingPressed();
//        backButton.OnLeftClick += (_, __) => OnBackPressed();

//        debugText = new UIText("PvP Adventure Info", 1, false) { HAlign = 0.5f };
//        debugText.Top.Set(255, 0);
//        Append(debugText);

//        UpdateDebug();
//    }

//    private void OnMatchmakingPressed()
//    {
//        if (isConnecting)
//        {
//            isConnecting = false;
//            connectTimer = -1f;

//            if (isQueuing)
//            {
//                isQueuing = false;
//                playersQueuingCount--;
//                if (playersQueuingCount < 0) playersQueuingCount = 0;
//                //MatchmakingClient.SendToggle(false);
//            }

//            matchmakingButton.SetLabel("Play Ranked");
//            SoundEngine.PlaySound(SoundID.MenuClose);
//            UpdateDebug();
//            return;
//        }

//        isQueuing = !isQueuing;
//        matchmakingButton.SetLabel(isQueuing ? "Cancel" : "Play Ranked");
//        SoundEngine.PlaySound(isQueuing ? SoundID.MenuOpen : SoundID.MenuClose);

//        playersQueuingCount += isQueuing ? 1 : -1;
//        if (playersQueuingCount < 0) playersQueuingCount = 0;

//        //MatchmakingClient.SendToggle(isQueuing);
//        UpdateDebug();

//        if (isQueuing && playersQueuingCount >= QueueTarget)
//        {
//            isConnecting = true;
//            connectTimer = ConnectDelaySeconds;
//            host = Hostname;

//            // TODO temp fix
//            host = "94.130.143.111";

//            port = Port;
//        }
//    }

//    public override void Draw(SpriteBatch sb)
//    {
//        base.Draw(sb);
//    }

//    public override void Update(GameTime gameTime)
//    {
//        base.Update(gameTime);

//        if (Main.menuMode == 14 || Main.menuMode == 10)
//            onCloseUi?.Invoke();

//        if (Main.hasFocus && Main.keyState.IsKeyDown(Keys.Escape) && !Main.oldKeyState.IsKeyDown(Keys.Escape))
//        {
//            OnBackPressed();
//            return;
//        }

//        if (isConnecting && connectTimer > 0f)
//        {
//            connectTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
//            if (connectTimer <= 0f)
//            {
//                connectTimer = 0f;
//                isConnecting = false;
//                matchmakingButton.SetLabel("Play Ranked");
//                AutoConnect(host, port);
//            }
//            UpdateDebug();
//        }
//    }

//    private void UpdateDebug()
//    {
//        var phase = isConnecting && connectTimer > 0f
//            ? $"Connecting in {(int)Math.Ceiling(connectTimer)}…"
//            : (isQueuing ? "Searching…" : "Idle");

//        debugText?.SetText(
//            $"Server: {Hostname}:{Port}\n" +
//            $"Players Online: {playersOnlineCount}\n" +
//            $"Players Queuing: {playersQueuingCount} / {QueueTarget}\n" +
//            phase
//        );
//    }

//    private void OnBackPressed()
//    {
//        if (isQueuing)
//        {
//            isQueuing = false;
//            matchmakingButton.SetLabel("Play Ranked");
//            playersQueuingCount--;
//            if (playersQueuingCount < 0) playersQueuingCount = 0;
//            //MatchmakingClient.SendToggle(false);
//            UpdateDebug();
//        }

//        isConnecting = false;
//        connectTimer = -1f;
//        SoundEngine.PlaySound(SoundID.MenuClose);
//        onBackButtonPressed?.Invoke();
//    }

//    private void AutoConnect(string h, int p)
//    {
//        onCloseUi?.Invoke();

//        Main.LoadPlayers();
//        var player = Main.PlayerList.FirstOrDefault();
//        if (player != null) Main.SelectPlayer(player);

//        Main.menuMultiplayer = true;
//        Main.menuServer = false;
//        Main.autoPass = true;

//        Netplay.ListenPort = p;
//        Main.getIP = (h ?? "").Trim();

//        Netplay.SetRemoteIPAsync(Main.getIP, () =>
//        {
//            Main.menuMode = 14;
//            Main.statusText = $"Connecting to {h}:{p}";
//            Netplay.StartTcpClient();
//        });
//    }
//}