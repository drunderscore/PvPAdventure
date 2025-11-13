using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Features.SpawnSelector.Systems;
using System;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader.UI;
using Terraria.UI;
using static System.Net.Mime.MediaTypeNames;

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

            // --- Random teleport button ---
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

            var item1 = new UISpawnSelectorCharacterListItem();
            item1.Width.Set(-20f, 1f);
            item1.Left.Set(10, 0f);
            item1.Top.Set(74, 0f);
            Append(item1);

            var item2 = new UISpawnSelectorCharacterListItem();
            item2.Width.Set(-20f, 1f);
            item2.Left.Set(10, 0f);
            item2.Top.Set(74 + 72 + 4, 0f);
            Append(item2);
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
            //RemoveAllChildren();
            //OnInitialize();

            base.Draw(spriteBatch);
        }
    }
}
