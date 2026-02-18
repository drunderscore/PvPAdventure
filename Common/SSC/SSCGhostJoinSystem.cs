using Mono.Cecil.Cil;
using MonoMod.Cil;
using PvPAdventure.Core.Config;
using Steamworks;
using System;
using System.IO;
using Terraria;
using Terraria.IO;
using Terraria.ModLoader;

namespace PvPAdventure.Common.SSC;

internal class SSCGhostJoinSystem : ModSystem
{
    public static Player JoinPlayerSnapshot { get; private set; }
    public override void Load()
    {
        IL_MessageBuffer.GetData += ILHook2;
    }
    void ILHook2(ILContext il)
    {
        //    if (Netplay.Connection.State == 1)
        //        Netplay.Connection.State = 2;
        //    int index1 = (int) this.reader.ReadByte();
        //    bool flag1 = this.reader.ReadBoolean();
        // -> byte gameMode = this.reader.ReadByte();
        var cur = new ILCursor(il);
        cur.GotoNext(MoveType.After,
            i => i.MatchLdarg(0),
            i => i.MatchLdfld(typeof(MessageBuffer), nameof(MessageBuffer.reader)),
            i => i.MatchCallvirt(typeof(BinaryReader), nameof(BinaryReader.ReadByte)),
            i => i.MatchStloc(out _),
            i => i.MatchLdarg(0),
            i => i.MatchLdfld(typeof(MessageBuffer), nameof(MessageBuffer.reader)),
            i => i.MatchCallvirt(typeof(BinaryReader), nameof(BinaryReader.ReadBoolean)),
            i => i.MatchStloc(out _)
        );
        cur.Emit(OpCodes.Ldarg_0);
        cur.Emit(OpCodes.Ldfld, typeof(MessageBuffer).GetField(nameof(MessageBuffer.reader))!);
        cur.EmitDelegate<Action<BinaryReader>>(i =>
        {
            var game_mode = i.ReadByte();
            if (Netplay.Connection.State != 2)
            {
                return;
            }

            // Save joined player for appearance (skin color, etc)
            JoinPlayerSnapshot ??= Main.ActivePlayerFileData?.Player?.SerializedClone();

            CreateGhostPlayerWithSteamID();
        });
    }

    private static void CreateGhostPlayerWithSteamID()
    {
        var config = ModContent.GetInstance<SSCConfig>();
        if (!config.IsSSCEnabled)
            return;

        var JoinPlayer = (Player)Main.ActivePlayerFileData.Player.Clone();

        // UUID会影响本地的map数据,同一个UUID的不同角色会拥有相同的地图探索
        var PID = SteamUser.GetSteamID().m_SteamID.ToString();

        // Get the desired name based on config setting
        string desiredName = SSCDelayJoinSystem.GetDesiredPlayerName();

        var fileData = new PlayerFileData(Path.Combine(Main.PlayerPath, $"{PID}.plr"), false)
        {
            Metadata = FileMetadata.FromCurrentSettings(FileType.Player),
            Player = new Player
            {
                name = desiredName,
                //difficulty = game_mode,
                // MessageID -> StatLife:16  Ghost:13  Dead:12&16
                statLife = 0,
                statMana = 0,
                dead = true,
                ghost = true,
                // 避免因为进入世界的自动复活,导致客户端与服务端失去同步
                respawnTimer = 0, // instantly respawn I guess
                lastTimePlayerWasSaved = long.MaxValue,
                savedPerPlayerFieldsThatArentInThePlayerClass = new Player.SavedPlayerDataWithAnnoyingRules()
            }
        };
        // 正常情况下不会拥有此标记,区别与Main.SSC,这个只会影响本地角色的保存,不会更改游戏流程
        fileData.MarkAsServerSide();
        fileData.SetAsActive();
    }
}
