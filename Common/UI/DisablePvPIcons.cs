using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.GameTimer;
using PvPAdventure.Core.Config;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI.Gamepad;

namespace PvPAdventure.Common.UI;

/// <summary>
/// Disables drawing and handling of PvP and team icons (these are drawn next to accessories when inventory is open).
/// </summary>
[Autoload(Side =ModSide.Client)]
internal class DisablePvPIcons : ModSystem
{
    public override void Load()
    {
        // Do not draw the PvP or team icons -- the server has full control over your PvP and team.
        // TODO: In the future, the server should send a packet relaying if the player can toggle hostile and which teams they may join.
        //       For now, let's just totally disable it.


        // Update: We're overriding vanilla manually and disallowing clicks only, we're still allowing players to choose teams.
        // player.hostile is continually set to true in CombatPlayer.
        On_Main.DrawPVPIcons += ModifyDrawPvPIcons;
    }

    private bool Enabled()
    {
        var config = ModContent.GetInstance<ServerConfig>();

        bool allow = true;

        if (config.AllowPlayersToChangeTeam == ServerConfig.AllowMode.Never)
            allow = false;

        bool hasGameStarted = ModContent.GetInstance<GameManager>().CurrentPhase == GameManager.Phase.Playing;

        if (config.AllowPlayersToChangeTeam == ServerConfig.AllowMode.BeforeGameStart && hasGameStarted)
            allow = false;

        if (!allow)
            return false;

        return true;
    }

    private void ModifyDrawPvPIcons(On_Main.orig_DrawPVPIcons orig)
    {
        if (!Enabled())
            return;

        // Full vanilla implementation below,
        // except the PvP toggle is disabled.
        if (Main.EquipPage == 1)
        {
            if (Main.hidePVPIcons)
            {
                return;
            }
        }
        else
        {
            Main.hidePVPIcons = false;
        }
        Main.inventoryScale = 0.6f;
        int num = (int)(52f * Main.inventoryScale);
        int num2 = 707 - num * 4 + Main.screenWidth - 800;
        int num3 = 114 + Main.mH + num * 2 + num / 2 - 12;
        if (Main.EquipPage == 2)
        {
            num2 += num + num / 2;
        }
        int num4 = (Main.player[Main.myPlayer].hostile ? 2 : 0);
        if (Main.mouseX > num2 - 7 && Main.mouseX < num2 + 25 && Main.mouseY > num3 - 2 && Main.mouseY < num3 + 37 && !PlayerInput.IgnoreMouseInterface)
        {
            Main.player[Main.myPlayer].mouseInterface = true;
            if (Main.teamCooldown == 0)
            {
                num4++;
            }

            // Disallow toggling PvP.
            if (Main.mouseLeft && Main.mouseLeftRelease && Main.teamCooldown == 0)
            {
                Main.teamCooldown = Main.teamCooldownLen;
                SoundEngine.PlaySound(12);
                //Main.player[Main.myPlayer].hostile = !Main.player[Main.myPlayer].hostile;
                //NetMessage.SendData(30, -1, -1, null, Main.myPlayer);
                bool hasGameStarted = ModContent.GetInstance<GameManager>().CurrentPhase == GameManager.Phase.Playing;
                if (!hasGameStarted)
                {
                    Main.NewText("PvP disabled until the game starts.", Color.Yellow);
                }
                else
                {
                    Main.NewText("PvP enabled!", Color.Yellow);
                }

            }
        }
        Rectangle rectangle = TextureAssets.Pvp[0].Frame(4, 6);
        rectangle.Location = new Point(rectangle.Width * num4, rectangle.Height * Main.player[Main.myPlayer].team);
        rectangle.Width -= 2;
        rectangle.Height--;
        Main.spriteBatch.Draw(TextureAssets.Pvp[0].Value, new Vector2(num2 - 10, num3), rectangle, Color.White, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
        UILinkPointNavigator.SetPosition(1550, new Vector2(num2 - 10, num3) + rectangle.Size() * 0.75f);
        num3 += 60;
        num2 -= 10;
        rectangle = TextureAssets.Pvp[1].Frame(6);
        Rectangle r = rectangle;
        for (int i = 0; i < 6; i++)
        {
            r.Location = new Point(num2 + i % 2 * 20, num3 + i / 2 * 20);
            rectangle.X = rectangle.Width * i;
            bool flag = false;
            if (r.Contains(Main.MouseScreen.ToPoint()) && !PlayerInput.IgnoreMouseInterface)
            {
                Main.player[Main.myPlayer].mouseInterface = true;
                if (Main.teamCooldown == 0)
                {
                    flag = true;
                }
                if (Main.mouseLeft && Main.mouseLeftRelease && Main.player[Main.myPlayer].team != i && Main.teamCooldown == 0)
                {
                    if (!Main.player[Main.myPlayer].TeamChangeAllowed())
                    {
                        Main.NewText(Lang.misc[84].Value, byte.MaxValue, 240, 20);
                    }
                    else
                    {
                        Main.teamCooldown = Main.teamCooldownLen;
                        SoundEngine.PlaySound(12);
                        Main.player[Main.myPlayer].team = i;
                        NetMessage.SendData(MessageID.PlayerTeam, -1, -1, null, Main.myPlayer);
                    }
                }
            }
            r.Width = rectangle.Width - 2;
            if (flag)
            {
                Main.spriteBatch.Draw(TextureAssets.Pvp[2].Value, r.Location.ToVector2() + new Vector2(-2f), Color.White);
            }
            Rectangle value = rectangle;
            value.Width -= 2;
            Main.spriteBatch.Draw(TextureAssets.Pvp[1].Value, r.Location.ToVector2(), value, Color.White);
            UILinkPointNavigator.SetPosition(1550 + i + 1, r.Location.ToVector2() + r.Size() * 0.75f);
        }
    }

    public override bool HijackGetData(ref byte messageType, ref BinaryReader reader, int playerNumber)
    {
        return Main.dedServ && (messageType is MessageID.TogglePVP or MessageID.PlayerTeam);
    }
}
