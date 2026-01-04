using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Core.SSC_v3;

public sealed class SSC_v3Player : ModPlayer
{
    private bool _sentJoin;

    public override void OnEnterWorld()
    {
        Log.Chat("OnEnterWorld with " + Main.LocalPlayer.name);

        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        if (_sentJoin)
            return;

        SSC_v3.SendJoinRequestOnce();

        _sentJoin = true;
    }

    public override void SaveData(TagCompound tag)
    {
        base.SaveData(tag);
    }

    public override void LoadData(TagCompound tag)
    {
        base.LoadData(tag);
    }
}