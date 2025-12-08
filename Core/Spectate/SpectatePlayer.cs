using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Spectate;
internal class SpectatePlayer : ModPlayer
{
    // The index of the player being spectated. Initially null. Set this to a valid player index to spectate that player.
    public int? TargetPlayerIndex;

    public override void ModifyScreenPosition()
    {
        if (TargetPlayerIndex is null or -1)
        {
            return;
        }

        // spectate target player
        Main.screenPosition = Main.player[TargetPlayerIndex.Value].position - new Vector2(Main.screenWidth, Main.screenHeight) / 2;
    }
    public override void OnRespawn()
    {
        base.OnRespawn();

        // reset spectate target on respawn
        TargetPlayerIndex = null;
    }
    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        base.Kill(damage, hitDirection, pvp, damageSource);

        Player? closest = Main.player.Where(x => x != Main.LocalPlayer).MinBy(x => x.position.Distance(Main.LocalPlayer.position));
        if (closest is null) return;

        TargetPlayerIndex = closest.whoAmI;
    }

    public override void PostUpdate()
    {
        // Send update to server about screen position every 10 ticks
        if (Player.whoAmI == Main.myPlayer && Main.netMode == NetmodeID.MultiplayerClient && Main.GameUpdateCount % 10 == 0)
        {
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)AdventurePacketIdentifier.TeamSpectate);
            packet.WriteVector2(Main.screenPosition);

            packet.Send();
        }
    }

    /// Wrapped list of active players except yourself
    public List<Player> GetValidPlayers()
    {
        List<Player> list = new();
        foreach (var p in Main.player)
        {
            if (p != null && p.active && p != Player)
                list.Add(p);
        }
        return list;
    }

    public void GetNextPrev(out Player prev, out Player next)
    {
        prev = next = null;

        var list = GetValidPlayers();
        if (list.Count == 0 || TargetPlayerIndex is null)
            return;

        int current = list.FindIndex(p => p.whoAmI == TargetPlayerIndex);
        if (current == -1)
            return;

        // wrap-around
        prev = list[(current - 1 + list.Count) % list.Count];
        next = list[(current + 1) % list.Count];
    }

    public void SelectNext()
    {
        var list = GetValidPlayers();
        if (list.Count == 0)
            return;

        if (TargetPlayerIndex is null)
            TargetPlayerIndex = list[0].whoAmI;
        else
        {
            int i = list.FindIndex(p => p.whoAmI == TargetPlayerIndex);
            TargetPlayerIndex = list[(i + 1) % list.Count].whoAmI;
        }
    }

    public void SelectPrev()
    {
        var list = GetValidPlayers();
        if (list.Count == 0)
            return;

        if (TargetPlayerIndex is null)
            TargetPlayerIndex = list[0].whoAmI;
        else
        {
            int i = list.FindIndex(p => p.whoAmI == TargetPlayerIndex);
            TargetPlayerIndex = list[(i - 1 + list.Count) % list.Count].whoAmI;
        }
    }

}
