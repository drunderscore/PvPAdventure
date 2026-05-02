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
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI.Gamepad;

namespace PvPAdventure.Common.Visualization;

/// <summary>
/// Always draws vanilla PvP/team icons, but blocks player-side PvP/team changes when server rules disallow them.
/// </summary>
[Autoload(Side = ModSide.Client)]
internal sealed class DisableTeamSelect : ModSystem
{
    private const int RedTeam = 1;

    public override void Load()
    {
        On_Main.DrawPVPIcons += ModifyDrawPvPIcons;
    }

    public override void PostUpdateEverything()
    {
#if DEBUG
        if (Main.netMode == NetmodeID.SinglePlayer && Main.LocalPlayer is not null)
            Main.LocalPlayer.team = RedTeam;
#endif
    }

    private static bool CanChangeTeam()
    {
#if DEBUG
        if (Main.netMode == NetmodeID.SinglePlayer)
            return true;
#endif

        ServerConfig config = ModContent.GetInstance<ServerConfig>();

        if (config.AllowPlayersToChangeTeam == ServerConfig.AllowMode.Never)
            return false;

        bool hasGameStarted = ModContent.GetInstance<GameManager>().CurrentPhase == GameManager.Phase.Playing;

        if (config.AllowPlayersToChangeTeam == ServerConfig.AllowMode.BeforeGameStart && hasGameStarted)
            return false;

        return true;
    }

    public static bool CanChangeTeams()
    {
#if DEBUG
        if (Main.netMode == NetmodeID.SinglePlayer)
            return true;
#endif

        ServerConfig config = ModContent.GetInstance<ServerConfig>();

        if (config.AllowPlayersToChangeTeam == ServerConfig.AllowMode.Never)
            return false;

        bool hasGameStarted = ModContent.GetInstance<GameManager>().CurrentPhase == GameManager.Phase.Playing;

        if (config.AllowPlayersToChangeTeam == ServerConfig.AllowMode.BeforeGameStart && hasGameStarted)
            return false;

        return true;
    }

    private static bool CanTogglePvp()
    {
        return false;
    }

    private static void ShowTeamChangeBlockedText()
    {
        //Main.NewText(Lang.misc[84].Value, Color.Yellow);
        Main.NewText("You can't change teams while game is playing!", Color.Yellow);
    }

    private static void ShowPvpChangeBlockedText()
    {
        Main.NewText("PvP cannot be changed while game is playing.", Color.Yellow);
    }

    private static void ModifyDrawPvPIcons(On_Main.orig_DrawPVPIcons orig)
    {
        if (!CanChangeTeams())
            return;

        if (Main.EquipPage == 1)
        {
            if (Main.hidePVPIcons)
                return;
        }
        else
        {
            Main.hidePVPIcons = false;
        }

        Main.inventoryScale = 0.6f;

        int size = (int)(52f * Main.inventoryScale);
        int x = 707 - size * 4 + Main.screenWidth - 800;
        int y = 114 + Main.mH + size * 2 + size / 2 - 12;

        if (Main.EquipPage == 2)
            x += size + size / 2;

        DrawPvpIcon(x, y);
        DrawTeamIcons(x - 10, y + 60);
    }

    private static void DrawPvpIcon(int x, int y)
    {
        Player player = Main.LocalPlayer;
        int frame = player.hostile ? 2 : 0;
        Rectangle hitbox = new(x - 7, y - 2, 32, 39);

        if (hitbox.Contains(Main.MouseScreen.ToPoint()) && !PlayerInput.IgnoreMouseInterface)
        {
            player.mouseInterface = true;

            if (Main.teamCooldown == 0)
                frame++;

            if (Main.mouseLeft && Main.mouseLeftRelease && Main.teamCooldown == 0)
            {
                Main.mouseLeftRelease = false;
                Main.teamCooldown = Main.teamCooldownLen;
                SoundEngine.PlaySound(SoundID.MenuTick);

                if (!CanTogglePvp())
                {
                    ShowPvpChangeBlockedText();
                    return;
                }

                player.hostile = !player.hostile;
                NetMessage.SendData(MessageID.TogglePVP, -1, -1, null, Main.myPlayer);
            }
        }

        Rectangle frameRect = TextureAssets.Pvp[0].Frame(4, 6);
        frameRect.Location = new Point(frameRect.Width * frame, frameRect.Height * player.team);
        frameRect.Width -= 2;
        frameRect.Height--;

        Main.spriteBatch.Draw(TextureAssets.Pvp[0].Value, new Vector2(x - 10, y), frameRect, Color.White);
        UILinkPointNavigator.SetPosition(1550, new Vector2(x - 10, y) + frameRect.Size() * 0.75f);
    }

    private static void DrawTeamIcons(int x, int y)
    {
        Player player = Main.LocalPlayer;
        Rectangle source = TextureAssets.Pvp[1].Frame(6);
        Rectangle hitbox = source;

        for (int i = 0; i < 6; i++)
        {
            hitbox.Location = new Point(x + i % 2 * 20, y + i / 2 * 20);
            source.X = source.Width * i;

            bool hovered = hitbox.Contains(Main.MouseScreen.ToPoint()) && !PlayerInput.IgnoreMouseInterface;
            bool highlight = hovered && Main.teamCooldown == 0;

            if (hovered)
            {
                player.mouseInterface = true;

                if (Main.mouseLeft && Main.mouseLeftRelease && player.team != i && Main.teamCooldown == 0)
                {
                    Main.mouseLeftRelease = false;
                    Main.teamCooldown = Main.teamCooldownLen;
                    SoundEngine.PlaySound(SoundID.MenuTick);

                    if (!CanChangeTeams() || !player.TeamChangeAllowed())
                    {
                        ShowTeamChangeBlockedText();
                    }
                    else
                    {
                        player.team = i;
                        NetMessage.SendData(MessageID.PlayerTeam, -1, -1, null, Main.myPlayer);
                    }
                }
            }

            hitbox.Width = source.Width - 2;

            if (highlight)
                Main.spriteBatch.Draw(TextureAssets.Pvp[2].Value, hitbox.Location.ToVector2() + new Vector2(-2f), Color.White);

            Rectangle drawSource = source;
            drawSource.Width -= 2;

            Main.spriteBatch.Draw(TextureAssets.Pvp[1].Value, hitbox.Location.ToVector2(), drawSource, Color.White);
            UILinkPointNavigator.SetPosition(1550 + i + 1, hitbox.Location.ToVector2() + hitbox.Size() * 0.75f);
        }
    }

    public override bool HijackGetData(ref byte messageType, ref BinaryReader reader, int playerNumber)
    {
        return Main.dedServ && (messageType is MessageID.TogglePVP or MessageID.PlayerTeam);
    }
}