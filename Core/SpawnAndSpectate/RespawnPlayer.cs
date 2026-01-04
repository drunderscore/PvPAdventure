//using Terraria;
//using Terraria.ID;
//using Terraria.ModLoader;

//namespace PvPAdventure.Core.SpawnAndSpectate;

///// <summary>
///// Keeps respawn timer frozen until player selects a spawn point.
///// </summary>
//internal class RespawnPlayer : ModPlayer
//{
//    //internal enum RespawnCommit : byte
//    //{
//    //    None,
//    //    Random,
//    //    Teammate,
//    //    WorldSpawn
//    //}

//    //private bool _autoCommittedThisDeath; // when we die, we instantly select world spawn
//    //private RespawnCommit committedSpawn = RespawnCommit.None;
//    //private int committedTeammateIndex = -1;

//    //public bool HasCommit => committedSpawn != RespawnCommit.None;
//    //public bool IsRandomCommitted => committedSpawn == RespawnCommit.Random;
//    //public bool IsTeammateCommitted(int idx) => committedSpawn == RespawnCommit.Teammate && committedTeammateIndex == idx;
//    //public bool IsWorldSpawnCommitted => committedSpawn == RespawnCommit.WorldSpawn;

//    //public void ToggleCommitRandom()
//    //{
//    //    if (!Player.dead)
//    //        return;

//    //    if (committedSpawn == RespawnCommit.Random)
//    //    {
//    //        SetCommit(RespawnCommit.None, -1);
//    //        return;
//    //    }

//    //    SetCommit(RespawnCommit.Random, -1);
//    //}

//    //public void ToggleCommitTeammate(int idx)
//    //{
//    //    if (!Player.dead)
//    //        return;

//    //    if (committedSpawn == RespawnCommit.Teammate && committedTeammateIndex == idx)
//    //    {
//    //        SetCommit(RespawnCommit.None, -1);
//    //        return;
//    //    }

//    //    SetCommit(RespawnCommit.Teammate, idx);
//    //}

//    //public void ToggleCommitWorldSpawn()
//    //{
//    //    if (!Player.dead)
//    //        return;

//    //    if (committedSpawn == RespawnCommit.WorldSpawn)
//    //    {
//    //        SetCommit(RespawnCommit.None, -1);
//    //        return;
//    //    }

//    //    SetCommit(RespawnCommit.WorldSpawn, -1);
//    //}

//    //private void ClearCommit()
//    //{
//    //    SetCommit(RespawnCommit.None, -1);
//    //}

//    //private void SetCommit(RespawnCommit commit, int teammateIndex)
//    //{
//    //    if (commit == RespawnCommit.Teammate && !IsValidCommittedTeammate(teammateIndex))
//    //    {
//    //        commit = RespawnCommit.None;
//    //        teammateIndex = -1;
//    //    }

//    //    int normalizedTeammateIndex = commit == RespawnCommit.Teammate ? teammateIndex : -1;
//    //    if (committedSpawn == commit && committedTeammateIndex == normalizedTeammateIndex)
//    //        return;

//    //    committedSpawn = commit;
//    //    committedTeammateIndex = normalizedTeammateIndex;

//    //    if (commit != RespawnCommit.None)
//    //    {
//    //        SpawnSystem_v2.SetCanTeleport(false);
//    //    }

//    //    KickFrozenRespawnIfNeeded();
//    //    SendCommitToServer();
//    //}

//    // If the timer is frozen at 2, selecting now should “insta-respawn” (next tick) and teleport via OnRespawn.
//    private void KickFrozenRespawnIfNeeded()
//    {
//        if (Player.dead && Player.respawnTimer == 2)
//        {
//            Player.respawnTimer = 1;
//        }
//    }

//    private bool IsValidCommittedTeammate(int idx)
//    {
//        if (idx < 0 || idx >= Main.maxPlayers)
//            return false;

//        Player t = Main.player[idx];
//        if (t == null || !t.active || t.dead)
//            return false;

//        if (t.whoAmI == Player.whoAmI)
//            return false;

//        if (Player.team == 0 || t.team != Player.team)
//            return false;

//        return true;
//    }

//    public override void UpdateDead()
//    {
//        if (Player.whoAmI == Main.myPlayer)
//        {
//            SpawnSystem_v2.SetCanTeleport(false);

//            var config = ModContent.GetInstance<AdventureClientConfig>();
//            bool autoSelectWorldSpawn = config.AutoSelectWorldSpawnWhenRespawning;

//            if (!_autoCommittedThisDeath && !HasCommit && Player.respawnTimer > 2 && autoSelectWorldSpawn)
//            {
//                SetCommit(RespawnCommit.WorldSpawn, -1);
//                _autoCommittedThisDeath = true;
//            }
//        }

//        // Let vanilla count down until it reaches 2.
//        if (Player.respawnTimer > 2)
//        {
//            base.UpdateDead();
//            return;
//        }

//        if (!HasCommit)
//        {
//            Player.respawnTimer = 2;

//            if (Player.whoAmI == Main.myPlayer)
//            {
//                SpawnSystem_v2.SetCanTeleport(true);
//            }

//            return;
//        }

//        if (Player.respawnTimer == 2)
//        {
//            Player.respawnTimer = 1;
//        }

//        base.UpdateDead();
//    }

//    public override void PostUpdate()
//    {
//        if (!Player.dead)
//        {
//            _autoCommittedThisDeath = false;
//        }
//    }

//    //private void SendCommitToServer()
//    //{
//    //    if (Main.netMode != NetmodeID.MultiplayerClient)
//    //        return;

//    //    var packet = Mod.GetPacket();
//    //    packet.Write((byte)AdventurePacketIdentifier.RespawnCommit);
//    //    packet.Write((byte)committedSpawn);
//    //    packet.Write(committedTeammateIndex);
//    //    packet.Send();
//    //}

//    //internal void ApplyCommitFromNet(RespawnCommit commit, int teammateIndex)
//    //{
//    //    // Server validates teammate selection.
//    //    if (commit == RespawnCommit.Teammate && !IsValidCommittedTeammate(teammateIndex))
//    //    {
//    //        commit = RespawnCommit.None;
//    //        teammateIndex = -1;
//    //    }

//    //    committedSpawn = commit;
//    //    committedTeammateIndex = commit == RespawnCommit.Teammate ? teammateIndex : -1;

//    //    // If server was frozen at 2, allow it to proceed immediately.
//    //    KickFrozenRespawnIfNeeded();
//    //}

//    public override void OnRespawn()
//    {
//        //if (committedSpawn == RespawnCommit.Random)
//        //{
//        //    RandomTeleport();
//        //}
//        //else if (committedSpawn == RespawnCommit.Teammate && IsValidCommittedTeammate(committedTeammateIndex))
//        //{
//        //    TeammateTeleport(committedTeammateIndex);
//        //}

//        //ClearCommit();

//        //if (Player.whoAmI == Main.myPlayer)
//        //    SpawnSystem_v2.SetCanTeleport(false);
//    }

//    #region Teleport Methods
//    public void RandomTeleport()
//    {
//        // Instantly execute random respawn (on both client and server)
//        if (Main.netMode == NetmodeID.MultiplayerClient)
//        {
//            NetMessage.SendData(MessageID.RequestTeleportationByServer);
//        }
//        else
//        {
//            Player.TeleportationPotion();
//        }
//    }
//    public void TeleportToTeammate(int teammateIndex)
//    {
//        Player.UnityTeleport(Main.player[teammateIndex].position);
//    }
//    #endregion
//}
