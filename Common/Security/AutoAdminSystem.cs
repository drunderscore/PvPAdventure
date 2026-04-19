using DragonLens.Core.Systems;
using PvPAdventure.Core.Config;
using PvPAdventure.Core.Debug;
using System;
using System.Diagnostics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Net;
using Terraria.Net.Sockets;

namespace PvPAdventure.Common.Security;

/// <summary>
/// Grants DragonLens admin to specific SteamID64s from ServerConfig.AutoAdmins.
/// </summary>
[JITWhenModsEnabled("DragonLens")]
[ExtendsFromMod("DragonLens")]
[Autoload(Side = ModSide.Server)]
internal sealed class AutoAdminSystem : ModSystem
{
    public override void OnWorldLoad()
    {
        var config = ModContent.GetInstance<ServerConfig>();

        int count = 0;
        if (config?.AutoAdmins?.SteamIds != null)
        {
            count = config.AutoAdmins.SteamIds.Count;
        }

        Log.Debug($"[AutoAdmins] WorldLoad Enabled={config?.AutoAdmins?.Enabled ?? false} Count={count}");
    }
}

[JITWhenModsEnabled("DragonLens")]
[ExtendsFromMod("DragonLens")]
[Autoload(Side = ModSide.Server)]
internal sealed class AutoAdminPlayer : ModPlayer
{
    private const int CheckIntervalTicks = 60;

    private int checkCooldown;
    private bool granted;
    private bool loggedWaitingForDlId;
    private bool loggedMissingSteam;

    public override void OnEnterWorld()
    {
        checkCooldown = 0;
        granted = false;
        loggedWaitingForDlId = false;
        loggedMissingSteam = false;

        TryGrant("OnEnterWorld");
    }

    public override void PostUpdate()
    {
        if (granted)
        {
            return;
        }

        if (checkCooldown > 0)
        {
            checkCooldown--;
            return;
        }

        checkCooldown = CheckIntervalTicks;
        TryGrant("PostUpdate");
    }

    private void TryGrant(string source)
    {
        var config = ModContent.GetInstance<ServerConfig>();
        if (config?.AutoAdmins == null || !config.AutoAdmins.Enabled)
        {
            return;
        }

        if (config.AutoAdmins.SteamIds == null || config.AutoAdmins.SteamIds.Count == 0)
        {
            return;
        }

        string steamId64 = TryGetSteamId64(Player.whoAmI);
        if (string.IsNullOrEmpty(steamId64))
        {
            if (!loggedMissingSteam)
            {
                loggedMissingSteam = true;
                Log.Debug($"[AutoAdmins] ({source}) '{Player.name}' has no SteamAddress (non-Steam socket?).");
            }

            return;
        }

        if (!IsWhitelisted(config.AutoAdmins.SteamIds, steamId64))
        {
            return;
        }

        PermissionPlayer dl = Player.GetModPlayer<PermissionPlayer>();
        if (dl == null || string.IsNullOrEmpty(dl.currentServerID))
        {
            if (!loggedWaitingForDlId)
            {
                loggedWaitingForDlId = true;
                Log.Debug($"[AutoAdmins] ({source}) Matched SteamID64={steamId64} for '{Player.name}', waiting for DL ID.");
            }

            return;
        }

        if (PermissionHandler.admins != null && PermissionHandler.admins.Contains(dl.currentServerID))
        {
            granted = true;
            Log.Debug($"[AutoAdmins] ({source}) '{Player.name}' already DL admin. SteamID64={steamId64}");
            return;
        }

        Log.Debug($"[AutoAdmins] ({source}) Granting DL admin to '{Player.name}'. SteamID64={steamId64}");

        PermissionHandler.AddAdmin(Player);

        granted = true;

        Log.Debug($"[AutoAdmins] ({source}) Granted DL admin to '{Player.name}'.");
    }

    private static bool IsWhitelisted(System.Collections.Generic.List<string> steamIds, string steamId64)
    {
        for (int i = 0; i < steamIds.Count; i++)
        {
            string id = steamIds[i];
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            if (id == steamId64)
            {
                return true;
            }
        }

        return false;
    }

    private static string TryGetSteamId64(int whoAmI)
    {
        if (whoAmI < 0 || whoAmI >= Main.maxPlayers)
        {
            return null;
        }

        var client = Netplay.Clients[whoAmI];
        if (client == null || client.Socket == null)
        {
            return null;
        }

        RemoteAddress addr = client.Socket.GetRemoteAddress();
        if (addr is not SteamAddress steam)
        {
            return null;
        }

        return steam.SteamId.m_SteamID.ToString();
    }
}