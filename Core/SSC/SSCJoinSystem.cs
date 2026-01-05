using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Core.SSC;

[Autoload(Side = ModSide.Client)]
public class SSCJoinSystem : ModSystem
{
    private bool _sent;
    private int _delayTicks;

    public override void OnWorldLoad()
    {
        _sent = false;
        _delayTicks = 60; // 1 seconds
    }

    public override void PostUpdateEverything()
    {
        if (_sent)
            return;

        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        if (_delayTicks > 0)
        {
            _delayTicks--;
            return;
        }

        _sent = true;
        SSC.SendJoinRequest();
    }

    public override void OnWorldUnload()
    {
        _sent = false;
        _delayTicks = 0;
    }
}
