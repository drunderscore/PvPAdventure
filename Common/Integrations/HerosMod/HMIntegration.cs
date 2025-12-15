using Microsoft.Xna.Framework;
using PvPAdventure.Common.Integrations.GameManagerIntegration;
using PvPAdventure.Common.Integrations.TeamAssigner;
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
public sealed class HMIntegration : ModSystem
{
    private const string PauseGamePermissionKey = "PauseGame";
    private const string PlayGamePermissionKey = "PlayGame";
    private const string TeamAssignerPermissionKey = "TeamAssigner";

    public override void PostSetupContent()
    {
        if (ModLoader.TryGetMod("HEROsMod", out Mod herosMod))
        {
            // Add permissions
            herosMod.Call("AddPermission",PauseGamePermissionKey,"Pause / resume game",(Action<bool>)(hasPerm => PermissionChanged(hasPerm, PauseGamePermissionKey)));
            herosMod.Call("AddPermission",PlayGamePermissionKey,"Start / end game",(Action<bool>)(hasPerm => PermissionChanged(hasPerm, PlayGamePermissionKey)));
            herosMod.Call("AddPermission",TeamAssignerPermissionKey,"Open team assigner",(Action<bool>)(hasPerm => PermissionChanged(hasPerm, TeamAssignerPermissionKey)));

            // Add buttons
            AddPauseButton(herosMod);
            AddPlayButton(herosMod);
            AddTeamAssignerButton(herosMod);
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
                    ModContent.GetInstance<GameManagerSystem>().ShowEndDialog();
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
                    var gms = ModContent.GetInstance<GameManagerSystem>();
                    if (gms.IsActive())
                    {
                        gms.Hide();
                    }
                    else
                    {
                        gms.ShowStartDialog();
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
                    var gss = ModContent.GetInstance<GameManagerSystem>();
                    if (gss.IsActive())
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
    private void AddTeamAssignerButton(Mod herosMod)
    {
        herosMod.Call("AddSimpleButton",
            TeamAssignerPermissionKey,
            Ass.TeamAssignerIcon,
            () =>
            {
                // Toggle active state of team selector
                var tas = ModContent.GetInstance<TeamAssignerSystem>();
                tas.ToggleActive();
            },
            (Action<bool>)(hasPerm => PermissionChanged(hasPerm, TeamAssignerPermissionKey)),
            () =>
            {
                // Update text depending on state
                var tas = ModContent.GetInstance<TeamAssignerSystem>();
                if (!tas.IsActive())
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