using Microsoft.Xna.Framework.Input;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Steamworks;
using System;
using System.IO;
using System.Runtime.Intrinsics.X86;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Core.SSC_v3;

[Autoload(Side = ModSide.Client)]
internal class SSC_Join_As_Ghost : ModSystem
{
    private static bool _pendingJoinDelay;
    private static int _joinDelayTicks;

    public static bool HasTwoSecondsPassed { get; private set; }

    public override void Load()
    {
        IL_MessageBuffer.GetData += ILHook2;
    }

    public override void Unload()
    {
        IL_MessageBuffer.GetData -= ILHook2;
    }

    public override void OnWorldLoad()
    {
        _pendingJoinDelay = true;
        _joinDelayTicks = 240; // 2 seconds
        HasTwoSecondsPassed = false;
    }

    public override void OnWorldUnload()
    {
        _pendingJoinDelay = false;
        _joinDelayTicks = 0;
        HasTwoSecondsPassed = false;
    }

    public override void PostUpdateEverything()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        // Save when pressing I
        if (Main.keyState.IsKeyDown(Keys.I) && Main.oldKeyState.IsKeyUp(Keys.I))
        {
            var steamID = SteamUser.GetSteamID().m_SteamID.ToString();
            var fileData = Main.ActivePlayerFileData;
            var name = fileData.Player.name;
            var plr = Player.SavePlayerFile_Vanilla(fileData);
            var tplr = PlayerIO.SaveData(fileData.Player);

            var packet = Mod.GetPacket();
            packet.Write((byte)AdventurePacketIdentifier.SSC);
            packet.Write((byte)SSCPacketType.ServerSavePlayer);
            packet.Write(steamID);
            packet.Write(name);
            packet.Write(plr.Length);
            packet.Write(plr);
            TagIO.Write(tplr, packet);
            packet.Write(fileData.Path == "Create.SSC");
            packet.Send();

            Log.Chat("Client sent packet to save " + fileData.Player.name);
        }

        if (!_pendingJoinDelay)
            return;

        if (_joinDelayTicks > 0)
        {
            _joinDelayTicks--;
            return;
        }

        // Transition once
        HasTwoSecondsPassed = true;
        _pendingJoinDelay = false;

        // This is typically what you actually want after the delay:
         SSC_v3.SendJoinRequestOnce();
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

            var steamID = SteamUser.GetSteamID().m_SteamID.ToString();

            var fileData = new PlayerFileData(Path.Combine(Main.PlayerPath, $"{steamID}.plr"), false)
            {
                Metadata = FileMetadata.FromCurrentSettings(FileType.Player),
                Player = new Player
                {
                    name = Main.LocalPlayer.name,
                    difficulty = game_mode,
                    statLife = 0,
                    statMana = 0,
                    dead = true,
                    ghost = true,
                    respawnTimer = int.MaxValue,
                    lastTimePlayerWasSaved = long.MaxValue,
                    savedPerPlayerFieldsThatArentInThePlayerClass = new Player.SavedPlayerDataWithAnnoyingRules()
                }
            };
            fileData.MarkAsServerSide();
            fileData.SetAsActive();
            Log.Chat("Set active player to ghost temporarily");

            //ModContent.GetInstance<ServerSystem>().UI?.SetState(new ServerViewer()); // 唯一设置界面的地方
        });
    }
}
