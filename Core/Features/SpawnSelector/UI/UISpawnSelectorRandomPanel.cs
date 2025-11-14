using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Features.SpawnSelector.Systems;
using PvPAdventure.Core.Helpers;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.Features.SpawnSelector.UI
{
    internal class UISpawnSelectorRandomPanel : UIPanel
    {
        public UISpawnSelectorRandomPanel(float startX, float itemHeight, int playerCount, float itemWidth, float Spacing, float y)
        {
            Width.Set(itemHeight, 0f);
            Height.Set(itemHeight, 0f);
            Top.Set(y, 0f);

            float randomX = startX + playerCount * (itemWidth + Spacing);
            Left.Set(randomX, 0f);

            BackgroundColor = new Color(63, 82, 151) * 0.8f;
            BorderColor = Color.Black;
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendData(MessageID.RequestTeleportationByServer);
            }
            else
            {
                Main.LocalPlayer.TeleportationPotion();
            }

            Main.mapFullscreen = false;
            SpawnSelectorSystem.SetEnabled(false);
        }

        public override void MouseOver(UIMouseEvent evt)
        {
            base.MouseOver(evt);
            BackgroundColor = new Color(73, 92, 161);

        }

        public override void MouseOut(UIMouseEvent evt)
        {
            base.MouseOut(evt);
            BackgroundColor = new Color(63, 82, 151) * 0.8f;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

            if (IsMouseHovering)
            {
                Main.instance.MouseText("Random");
            }

            // Draw question mark
            var dims = GetDimensions();
            var rect = dims.ToRectangle();
            spriteBatch.Draw(Ass.Question_Mark.Value, rect, Color.White);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }
    }
}
