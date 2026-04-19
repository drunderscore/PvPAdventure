//using PvPAdventure.Common.Skins.Net;
//using Terraria;
//using Terraria.ID;
//using Terraria.ModLoader;

//namespace PvPAdventure.Common.Skins;

///// <summary>
///// Server-side system that sends existing players' skin data to newly joined clients.
///// When a player enters the world in multiplayer, we send them every other player's skins
///// so they can see the correct textures/names immediately.
///// </summary>
//internal sealed class SkinSyncSystem : ModSystem
//{
//    /// <summary>
//    /// Not a ModSystem hook — we use a ModPlayer hook instead.
//    /// See <see cref="SkinSyncPlayer"/>.
//    /// </summary>
//}

///// <summary>
///// Handles syncing all existing player skins to a newly joined player.
///// </summary>
//internal sealed class SkinSyncPlayer : ModPlayer
//{
//    private bool _hasSyncedOnJoin;

//    public override void Initialize()
//    {
//        _hasSyncedOnJoin = false;
//    }

//    public override void OnEnterWorld()
//    {
//        _hasSyncedOnJoin = false;
//        Log.Debug($"[SkinSyncPlayer] OnEnterWorld player={Player.whoAmI} netMode={Main.netMode}");
//    }

//    public override void PostUpdate()
//    {
//        // Server-side: when a new player becomes active and we haven't synced them yet,
//        // send them all other players' skin data.
//        if (Main.netMode == NetmodeID.Server && Player.active && !_hasSyncedOnJoin)
//        {
//            _hasSyncedOnJoin = true;

//            Log.Debug($"[SkinSyncPlayer] First PostUpdate for player={Player.whoAmI} on server — sending existing skins");

//            for (int i = 0; i < Main.maxPlayers; i++)
//            {
//                if (i == Player.whoAmI)
//                    continue;

//                if (Main.player[i] == null || !Main.player[i].active)
//                    continue;

//                Log.Debug($"[SkinSyncPlayer] Sending player {i}'s skins to new player {Player.whoAmI}");
//                SkinNetHandler.SendAllSkins(i, Player.whoAmI);
//            }
//        }
//    }
//}
