using System;
using System.IO;
using System.Text.RegularExpressions;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;
using Terraria.Utilities;

namespace PvPAdventure.Core.SSC;

public class HookManager : ModSystem
{
    public static Player JoinPlayer;

    public override void Load()
    {
        IL_MessageBuffer.GetData += ILHook0;
        IL_NetMessage.SendData += ILHook1;
        IL_MessageBuffer.GetData += ILHook2;
        On_FileUtilities.Exists += OnHook1;
        On_FileUtilities.ReadAllBytes += OnHook2;
        On_Player.InternalSavePlayerFile += OnHook3;
    }

    public override void Unload()
    {
        IL_MessageBuffer.GetData -= ILHook0;
        IL_NetMessage.SendData -= ILHook1;
        IL_MessageBuffer.GetData -= ILHook2;

        On_FileUtilities.Exists -= OnHook1;
        On_FileUtilities.ReadAllBytes -= OnHook2;
        On_Player.InternalSavePlayerFile -= OnHook3;

        JoinPlayer = null;
    }

    void ILHook0(ILContext il)
    {
        // Main.ServerSideCharacter = true;
        var cur = new ILCursor(il);
        cur.GotoNext(MoveType.Before, i => i.MatchStsfld<Main>(nameof(Main.ServerSideCharacter)));
        cur.EmitDelegate<Func<bool, bool>>(_ => true);
    }

    // Server sends the game mode to the client during connection.
    void ILHook1(ILContext il)
    {
        // case 3:
        //    writer.Write((byte) remoteClient);
        //    writer.Write(false);
        // -> writer.Write((byte) Main.GameMode);
        //    break;
        var cur = new ILCursor(il);
        cur.GotoNext(MoveType.After,
            i => i.MatchLdloc(3), i => i.MatchLdarg(1), i => i.MatchConvU1(),
            i => i.MatchCallvirt(typeof(BinaryWriter), nameof(BinaryWriter.Write)),
            i => i.MatchLdloc(3), i => i.MatchLdcI4(0),
            i => i.MatchCallvirt(typeof(BinaryWriter), nameof(BinaryWriter.Write))
        );
        cur.Emit(OpCodes.Ldloc_3);
        cur.EmitDelegate<Action<BinaryWriter>>(i => i.Write((byte)Main.GameMode));
    }

    // Client initializes based on the incoming game mode; this also means character selection/initialization only happens in multiplayer.
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
                return; // Hook above ensures initialization only on the first entry into a world
            }

            // JoinPlayer = Main.ActivePlayerFileData.Player.SerializedClone();

            JoinPlayer = (Player)Main.ActivePlayerFileData.Player.Clone();

            // UUID affects local map data; different characters with the same UUID share map exploration
            var PID = SSC.GetPID();
            var fileData = new PlayerFileData(Path.Combine(Main.PlayerPath, $"{PID}.plr"), false)
            {
                Metadata = FileMetadata.FromCurrentSettings(FileType.Player),
                Player = new Player
                {
                    name = PID, difficulty = game_mode,
                    // MessageID -> StatLife:16  Ghost:13  Dead:12&16
                    statLife = 0, statMana = 0, dead = true, ghost = true,
                    // Prevent the automatic revive on entry from desyncing client and server
                    respawnTimer = int.MaxValue, lastTimePlayerWasSaved = long.MaxValue,
                    savedPerPlayerFieldsThatArentInThePlayerClass = new Player.SavedPlayerDataWithAnnoyingRules()
                }
            };
            // Normally this flag is absent; unlike Main.SSC, this only affects local save behavior and not gameplay flow
            fileData.MarkAsServerSide();
            fileData.SetAsActive();

            ModContent.GetInstance<ServerSystem>().UI?.SetState(new ServerViewer()); // Only place the UI is assigned
        });
    }

    // Allow specific data formats
    bool OnHook1(On_FileUtilities.orig_Exists orig, string path, bool cloud)
    {
        if (path == null)
        {
            return false;
        }

        return path.StartsWith("SSC@") || orig(path, cloud);
    }

    // Original paths end with plr or tplr; return content for the special SSC format based on suffix
    byte[] OnHook2(On_FileUtilities.orig_ReadAllBytes orig, string path, bool cloud)
    {
        if (path.StartsWith("SSC@"))
        {
            var regex = new Regex(@"^SSC@(?<plr>[0-9A-F]+)@(?<tplr>[0-9A-F]+)@\.(?<type>plr|tplr)$");
            var match = regex.Match(path);
            if (match.Success)
            {
                return Convert.FromHexString(match.Groups[match.Groups["type"].Value].Value);
            }

            throw new ArgumentException("SSC regex match failed.");
        }

        return orig(path, cloud);
    }

    // Only SSC saves whose paths end with .SSC are uploaded to the cloud; using the SSC flag and suffix cleanly separates them.
    // Paths are one of: PS/[SteamID].plr, Create.SSC, PS/[SteamID].SSC
    void OnHook3(On_Player.orig_InternalSavePlayerFile orig, PlayerFileData fileData)
    {
        // Not a vanilla feature; can't use TerraHook, but this still doesn't break the interface call.
        if (Main.netMode == NetmodeID.MultiplayerClient &&
            fileData.ServerSideCharacter && fileData.Path.EndsWith("SSC"))
        {
            try
            {
                var plr = Player.SavePlayerFile_Vanilla(fileData);
                var tplr = GetTPLR(fileData);

                var mp = Mod.GetPacket();
                mp.Write((byte)AdventurePacketIdentifier.SSC);
                mp.Write((byte)SSCMessageID.SaveSSC);
                mp.Write(SSC.GetPID());
                mp.Write(fileData.Player.name);
                mp.Write(plr.Length);
                mp.Write(plr);
                TagIO.Write(tplr, mp);
                mp.Write(fileData.Path == "Create.SSC");
                MessageManager.FrameSend(mp);
            }
            catch (Exception e)
            {
                Mod.Logger.Error(e);
            }

            return;
        }

        orig(fileData);
    }

    TagCompound GetTPLR(PlayerFileData fileData)
    {
        return PlayerIO.SaveData(fileData.Player);
    }
}