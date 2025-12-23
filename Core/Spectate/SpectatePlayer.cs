using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Spectate;

internal class SpectatePlayer : ModPlayer
{
    public int? TargetPlayerIndex;
    private bool openSpectateUiNextTick;

    public override void ModifyScreenPosition()
    {
        if (TargetPlayerIndex is null or -1)
            return;

        Player target = Main.player[TargetPlayerIndex.Value];

        if (target == null || !target.active || target.dead)
        {
            TargetPlayerIndex = null;
            SnapBackToSelf();
            return;
        }

        Main.screenPosition = target.position - new Vector2(Main.screenWidth, Main.screenHeight) / 2f;
    }

    public override void OnRespawn()
    {
        base.OnRespawn();

        SetTarget(null);

        var ss = ModContent.GetInstance<SpectateSystem>();
        if (ss != null && ss.IsActive())
            ss.ExitSpectateUI();
    }

    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        base.Kill(damage, hitDirection, pvp, damageSource);

        var cfg = ModContent.GetInstance<AdventureClientConfig>();
        if (!cfg.SpectateTeammatesOnDeath || Player.whoAmI != Main.myPlayer || Main.dedServ)
            return;

        List<int> ids = GetTeammateIds();
        if (ids.Count == 0)
            return;

        int bestId = -1;
        float best = float.MaxValue;

        for (int i = 0; i < ids.Count; i++)
        {
            Player p = Main.player[ids[i]];
            float d = Vector2.DistanceSquared(p.Center, Player.Center);

            if (d < best)
            {
                best = d;
                bestId = p.whoAmI;
            }
        }

        SetTarget(bestId);
        openSpectateUiNextTick = true;
    }

    public override void PostUpdate()
    {
        if (openSpectateUiNextTick)
        {
            openSpectateUiNextTick = false;

            if (TargetPlayerIndex is not null)
                ModContent.GetInstance<SpectateSystem>()?.EnterSpectateUI(clearTarget: false);
        }
    }

    public void SetTarget(int? target) => TargetPlayerIndex = target;

    public void SnapBackToSelf() =>
        Main.screenPosition = Player.position - new Vector2(Main.screenWidth, Main.screenHeight) / 2f;

    public List<int> GetTeammateIds(bool includeAllPlayers = false)
    {
        List<int> ids = [];

#if DEBUG
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            int me = Player.whoAmI;
            ids.Add(me);
            ids.Add(me);
            ids.Add(me);
            ids.Add(me);
            return ids;
        }
#endif

        Player mePlayer = Player;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player p = Main.player[i];

            if (p == null || !p.active || p.dead || p.whoAmI == mePlayer.whoAmI)
                continue;

            if (!includeAllPlayers)
            {
                if (mePlayer.team != 0 && p.team != mePlayer.team)
                    continue;
            }

            ids.Add(p.whoAmI);
        }

        return ids;
    }

    public void Cycle(int dir)
    {
        List<int> ids = GetTeammateIds();
        if (ids.Count == 0)
            return;

        if (TargetPlayerIndex is null)
        {
            SetTarget(dir < 0 ? ids[^1] : ids[0]);
            return;
        }

        int cur = ids.IndexOf(TargetPlayerIndex.Value);
        if (cur == -1)
        {
            SetTarget(ids[0]);
            return;
        }

        cur = (cur + dir + ids.Count) % ids.Count;
        SetTarget(ids[cur]);
    }
}
