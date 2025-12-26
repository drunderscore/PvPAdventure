using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Core.SpawnAndSpectate;

/// <summary>
/// Keeps respawn timer frozen until player selects a spawn point.
/// </summary>
internal class RespawnPlayer : ModPlayer
{
    internal enum RespawnCommit : byte
    {
        None,
        Random,
        Teammate
    }

    private RespawnCommit committedSpawn = RespawnCommit.None;
    private int committedTeammateIndex = -1;

    public bool HasCommit => committedSpawn != RespawnCommit.None;
    public bool IsRandomCommitted => committedSpawn == RespawnCommit.Random;
    public bool IsTeammateCommitted(int idx) => committedSpawn == RespawnCommit.Teammate && committedTeammateIndex == idx;

    public void ToggleCommitRandom()
    {
        if (!Player.dead)
            return;

        if (committedSpawn == RespawnCommit.Random)
        {
            SetCommit(RespawnCommit.None, -1);
            return;
        }

        SetCommit(RespawnCommit.Random, -1);
    }

    public void ToggleCommitTeammate(int idx)
    {
        if (!Player.dead)
            return;

        if (committedSpawn == RespawnCommit.Teammate && committedTeammateIndex == idx)
        {
            SetCommit(RespawnCommit.None, -1);
            return;
        }

        SetCommit(RespawnCommit.Teammate, idx);
    }

    private void ClearCommit()
    {
        SetCommit(RespawnCommit.None, -1);
    }

    private void SetCommit(RespawnCommit commit, int teammateIndex)
    {
        if (commit == RespawnCommit.Teammate && !IsValidCommittedTeammate(teammateIndex))
        {
            commit = RespawnCommit.None;
            teammateIndex = -1;
        }

        int normalizedTeammateIndex = commit == RespawnCommit.Teammate ? teammateIndex : -1;
        if (committedSpawn == commit && committedTeammateIndex == normalizedTeammateIndex)
            return;

        committedSpawn = commit;
        committedTeammateIndex = normalizedTeammateIndex;

        if (commit != RespawnCommit.None)
        {
            SpawnAndSpectateSystem.SetCanRespawn(false);
        }

        KickFrozenRespawnIfNeeded();
        SendCommitToServer();
    }

    // If the timer is frozen at 2, selecting now should “insta-respawn” (next tick) and teleport via OnRespawn.
    private void KickFrozenRespawnIfNeeded()
    {
        if (Player.dead && Player.respawnTimer == 2)
        {
            Player.respawnTimer = 1;
        }
    }

    private bool IsValidCommittedTeammate(int idx)
    {
        if (idx < 0 || idx >= Main.maxPlayers)
            return false;

        Player t = Main.player[idx];
        if (t == null || !t.active || t.dead)
            return false;

        if (t.whoAmI == Player.whoAmI)
            return false;

        if (Player.team == 0 || t.team != Player.team)
            return false;

        return true;
    }

    public override void UpdateDead()
    {
        bool isLocalPlayer = Player.whoAmI == Main.myPlayer;

        if (isLocalPlayer)
            SpawnAndSpectateSystem.SetCanRespawn(false);

        // Let vanilla count down until it reaches 2.
        if (Player.respawnTimer > 2)
        {
            base.UpdateDead();
            return;
        }

        // We are at (or below) 2: this is the selection/execution gate.
        // If nothing is selected, freeze at 2 until the player chooses.
        if (!HasCommit)
        {
            Player.respawnTimer = 2;

            if (isLocalPlayer)
                SpawnAndSpectateSystem.SetCanRespawn(true);

            return;
        }

        // If something is selected and we just hit 2, immediately execute:
        // force the timer below 2 so vanilla completes the respawn right now.
        if (Player.respawnTimer == 2)
        {
            Player.respawnTimer = 1;
        }

        base.UpdateDead();
    }

    private void SendCommitToServer()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        var packet = Mod.GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.RespawnCommit);
        packet.Write((byte)committedSpawn);
        packet.Write(committedTeammateIndex);
        packet.Send();
    }

    internal void ApplyCommitFromNet(RespawnCommit commit, int teammateIndex)
    {
        // Server validates teammate selection.
        if (commit == RespawnCommit.Teammate && !IsValidCommittedTeammate(teammateIndex))
        {
            commit = RespawnCommit.None;
            teammateIndex = -1;
        }

        committedSpawn = commit;
        committedTeammateIndex = commit == RespawnCommit.Teammate ? teammateIndex : -1;

        // If server was frozen at 2, allow it to proceed immediately.
        KickFrozenRespawnIfNeeded();
    }

    public override void OnRespawn()
    {
        if (committedSpawn == RespawnCommit.Random)
        {
            RandomTeleport();
        }
        else if (committedSpawn == RespawnCommit.Teammate && IsValidCommittedTeammate(committedTeammateIndex))
        {
            TeammateTeleport(committedTeammateIndex);
        }

        ClearCommit();

        if (Player.whoAmI == Main.myPlayer)
            SpawnAndSpectateSystem.SetCanRespawn(false);
    }

    #region Teleport Methods
    public void RandomTeleport()
    {
        // Instantly execute random respawn (on both client and server)
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            NetMessage.SendData(MessageID.RequestTeleportationByServer);
        }
        else
        {
            Player.TeleportationPotion();
        }
    }
    public void TeammateTeleport(int teammateIndex)
    {
        Player.UnityTeleport(Main.player[teammateIndex].position);
    }
    #endregion
}
