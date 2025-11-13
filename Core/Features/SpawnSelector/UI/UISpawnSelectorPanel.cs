using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Features.SpawnSelector.Systems;
using PvPAdventure.Core.Helpers;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;

namespace PvPAdventure.Core.Features.SpawnSelector.UI
{
    internal class UISpawnSelectorPanel : UIPanel
    {
        private UITextPanel<string> randomButton;

        public override void OnInitialize()
        {
            Width.Set(400f, 0f);
            Height.Set(300f, 0f);
            HAlign = 0.5f;
            VAlign = 0.0f;
            Top.Set(10 + 32 - 10, 0);
            BackgroundColor = new Color(33, 43, 79) * 0.8f;
            SetPadding(6f);

            randomButton = new UITextPanel<string>("Random");
            randomButton.Width.Set(-20f, 1f);
            randomButton.Left.Set(10f, 0f);
            randomButton.Top.Set(30f, 0f);
            randomButton.BackgroundColor = new Color(63, 82, 151) * 0.8f;
            randomButton.BorderColor = Color.Black;
            randomButton.OnMouseOver += (_, _) =>
            {
                randomButton.BackgroundColor = new Color(73, 92, 161);
            };
            randomButton.OnMouseOut += (_, _) =>
                randomButton.BackgroundColor = new Color(63, 82, 151) * 0.8f;
            randomButton.OnLeftClick += (_, _) => PerformRandomTeleport();
            Append(randomButton);

            var p1 = new UISpawnSelectorCharacterListItem(Main.LocalPlayer, isSelf: true);
            p1.Width.Set(-20f, 1f);
            p1.Left.Set(10, 0f);
            p1.Top.Set(74, 0f);
            Append(p1);

            var players = new List<Player>();
            if (Main.LocalPlayer.team != 0)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player p = Main.player[i];
                    if (p != null && p.active && p.whoAmI != Main.LocalPlayer.whoAmI && p.team == Main.LocalPlayer.team)
                    {
                        Main.NewText("Adding teammate: " + p.name);
                        players.Add(p);
                    }
                }
            }

            for (int i = 0; i < players.Count; i++)
            {
                var row = new UISpawnSelectorCharacterListItem(players[i], isSelf: false);
                row.Width.Set(-20f, 1f);
                row.Left.Set(10, 0f);
                row.Top.Set(74 + i * 70, 0f);
                Append(row);
            }

            Log.Info($"UISpawnSelectorPanel.OnInitialize: team={Main.LocalPlayer.team}, teammates={players.Count}");
        }

        public void Rebuild()
        {
            RemoveAllChildren();
            OnInitialize();
            Log.Info("UISpawnSelectorPanel.Rebuild: UI rebuilt");
        }

        private void PerformRandomTeleport()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.RequestTeleportationByServer);
            else
                Main.LocalPlayer.TeleportationPotion();

            CloseAndExitMap();
        }

        private void CloseAndExitMap()
        {
            Main.mapFullscreen = false;
            SpawnSelectorSystem.SetEnabled(false);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
        }
    }
}
