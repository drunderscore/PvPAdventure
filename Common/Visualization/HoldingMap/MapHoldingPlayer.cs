using PvPAdventure.Core.Net;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Visualization.HoldingMap;

public sealed class MapHoldingPlayer : ModPlayer
{
    // A state where this player currently has the fullscreen map open
    public bool HoldingMap;

    private bool _lastLocalState;

    public override void PostUpdate()
    {
        // Only the owning client can decide its own UI-driven map state.
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        if (Player.whoAmI != Main.myPlayer)
            return;

        bool newState = Main.mapFullscreen && !Player.dead;

        // Update new state.
        if (HoldingMap == newState)
        {
            _lastLocalState = newState;
            return;
        }

        _lastLocalState = newState;
        HoldingMap = newState;

        SendHoldingMapState(Player.whoAmI, HoldingMap, toWho: -1, ignoreClient: -1);
    }

    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
    {
        // When a client joins / requests sync, ensure they receive current state.
        //SendHoldingMapState(Player.whoAmI, HoldingMap, toWho, ignoreClient: -1);
    }

    public override void CopyClientState(ModPlayer targetCopy)
    {
        var copy = (MapHoldingPlayer)targetCopy;
        copy.HoldingMap = HoldingMap;
    }

    public override void SendClientChanges(ModPlayer clientPlayer)
    {
        // This runs client-side; use it as a safety net for state drift.
        //var old = (MapHoldingPlayer)clientPlayer;

        //if (old.HoldingMap == HoldingMap)
            //return;

        //SendHoldingMapState(Player.whoAmI, HoldingMap, toWho: -1, ignoreClient: Player.whoAmI);
    }

    public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
    {
        if (!HoldingMap)
            return;

        Player p = drawInfo.drawPlayer;

        float rotation;
        if (p.direction == 1)
            rotation = -1.9f;
        else
            rotation = 1.9f;

        // Hold out the arms.
        p.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);
        p.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, rotation);
    }

    private void SendHoldingMapState(int playerIndex, bool holding, int toWho, int ignoreClient)
    {
        ModPacket packet = Mod.GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.HoldingMap);
        packet.Write((byte)VisualsPacketType.MapHoldingState);
        packet.Write((byte)playerIndex);
        packet.Write(holding);
        packet.Send(toWho, ignoreClient);
    }

    internal enum VisualsPacketType : byte
    {
        MapHoldingState = 1
    }
}
