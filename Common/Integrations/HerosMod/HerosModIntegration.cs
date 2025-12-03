using Microsoft.Xna.Framework;
using PvPAdventure.Common.Integrations.HerosMod.StartGame;
using PvPAdventure.Common.Integrations.HerosMod.TeamSelector;
using PvPAdventure.Core.Helpers;
using PvPAdventure.System;
using System;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Integrations.HerosMod;

[JITWhenModsEnabled("HEROsMod")]
public sealed class HerosModIntegration : ModSystem
{
    private const string PauseGamePermissionKey = "PauseGame";
    private const string PlayGamePermissionKey = "PlayGame";
    private const string TeamSelectorPermissionKey = "TeamSelector";

    public override void PostSetupContent()
    {
        if (ModLoader.TryGetMod("HEROsMod", out Mod herosMod))
        {
            // Add permissions
            herosMod.Call("AddPermission",PauseGamePermissionKey,"Pause / resume game",(Action<bool>)(hasPerm => PermissionChanged(hasPerm, PauseGamePermissionKey)));
            herosMod.Call("AddPermission",PlayGamePermissionKey,"Start / end game",(Action<bool>)(hasPerm => PermissionChanged(hasPerm, PlayGamePermissionKey)));
            herosMod.Call("AddPermission",TeamSelectorPermissionKey,"Open team assigner",(Action<bool>)(hasPerm => PermissionChanged(hasPerm, TeamSelectorPermissionKey)));

            // Add buttons
            AddPauseButton(herosMod);
            AddPlayButton(herosMod);
            AddTeamSelectorButton(herosMod);
        }
    }

    private void AddPauseButton(Mod herosMod)
    {
        // Pause game
        herosMod.Call("AddSimpleButton",
            PauseGamePermissionKey,
            Ass.Pause,
            (Action)(() =>
            {
                var pm = ModContent.GetInstance<PauseManager>();
                pm.PauseGame();
            }),
            (Action<bool>)(hasPerm => PermissionChanged(hasPerm, PauseGamePermissionKey)),
            (Func<string>)(() =>
            {
                var pm = ModContent.GetInstance<PauseManager>();
                return (pm != null && pm.IsPaused)
                    ? "Resume"
                    : "Pause";
            })
        );
    }
    private void AddPlayButton(Mod herosMod)
    {
        herosMod.Call("AddSimpleButton",
            PlayGamePermissionKey,
            Ass.Play,
            (Action)(() =>
            {
                var gm = ModContent.GetInstance<GameManager>();
                if (gm.CurrentPhase == GameManager.Phase.Playing)
                {
                    ModContent.GetInstance<StartGameSystem>().ShowEndDialog();
                }
                else if (gm._startGameCountdown.HasValue && Main.netMode == NetmodeID.SinglePlayer)
                {
                    gm._startGameCountdown = null;
                    gm.TimeRemaining = 0;
                    gm.CurrentPhase = GameManager.Phase.Waiting;
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral("Cancelled countdown."), Color.Red);
                }
                else
                {
                    var sgs = ModContent.GetInstance<StartGameSystem>();
                    if (sgs.IsActive())
                    {
                        sgs.Hide();
                    }
                    else
                    {
                        sgs.ShowStartDialog();
                    }
                }
            }),
            (Action<bool>)(hasPerm => PermissionChanged(hasPerm, PlayGamePermissionKey)),
            (Func<string>)(() =>
            {
                var gm = ModContent.GetInstance<GameManager>();
                if (gm.CurrentPhase == GameManager.Phase.Playing)
                {
                    return "End game";
                }
                else if (gm._startGameCountdown.HasValue)
                {
                    return "Cancel countdown";
                }
                else
                {
                    var sgs = ModContent.GetInstance<StartGameSystem>();
                    if (sgs.IsActive())
                    {
                        return "Close game starter";
                    }
                    else
                    {
                        return "Open game starter";
                    }
                }
            })
        );
    }
    private void AddTeamSelectorButton(Mod herosMod)
    {
        herosMod.Call("AddSimpleButton",
            TeamSelectorPermissionKey,
            Ass.TeamSelectorIcon,
            () =>
            {
                // Toggle active state of team selector
                var tss = ModContent.GetInstance<TeamSelectorSystem>();
                tss.ToggleActive();
            },
            (Action<bool>)(hasPerm => PermissionChanged(hasPerm, TeamSelectorPermissionKey)),
            () =>
            {
                // Update text depending on state
                var tss = ModContent.GetInstance<TeamSelectorSystem>();
                if (!tss.IsActive())
                {
                    return "Open team assigner";
                }
                else
                {
                    return "Close team assigner";
                }
            }
        );
    }
    /// <summary>
    /// Called when the player's permission changes
    /// </summary>
    private static void PermissionChanged(bool hasPerm, string permissionName)
    {
        if (!hasPerm)
        {
            //Main.NewText($"⛔ You lost permission to use the {permissionName} button!", Color.Red);
            Log.Info($"You lost permission for {permissionName} button. You cannot use it anymore.");
        }
        else
        {
            //Main.NewText($"✅ You regained permission to use the {permissionName} button!", Color.Green);
            Log.Info($"You regained permission for {permissionName} button. You can use it again.");
        }
    }
}