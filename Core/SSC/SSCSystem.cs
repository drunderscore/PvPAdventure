using Steamworks;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent.UI.States;
using Terraria.ID;
using Terraria.IO;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Core.SSC
{
    [Autoload(Side = ModSide.Both)]
    internal class SSCSystem : ModSystem
    {
        internal enum SSCPacketType : byte { BindRequest, UploadPlayer, CreatePlayer, LoadPlayer }

        private string steamId, boundStem;
        private bool requested, applied;
        private uint saveTimer;

        private static On_NetMessage.orig_SendData s_origSend;
        private static bool s_queued;
        private static int s_t, s_r, s_i, s_n, s_n5, s_n6, s_n7, s_wait;
        private static float s_n2, s_n3, s_n4;
        private static NetworkText s_txt;

        private const int TimeoutTicks = 60 * 10;

        public override void Load()
        {
            On_NetMessage.SendData += HookSendData;
            On_Main.Update += HookUpdate;
        }

        public override void Unload()
        {
            On_NetMessage.SendData -= HookSendData;
            On_Main.Update -= HookUpdate;
            s_origSend = null;
            ClearQueue();
        }

        private void HookUpdate(On_Main.orig_Update orig, Main self, Microsoft.Xna.Framework.GameTime gameTime)
        {
            orig(self, gameTime);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                ClearQueue();
                return;
            }

            if (requested && !applied && s_queued && ++s_wait > TimeoutTicks)
            {
#if DEBUG
                Main.NewText("[SSC] Timeout waiting SSC; releasing PlayerInfo.");
                Mod.Logger.Error("SSC: Timeout waiting SSC; releasing PlayerInfo.");
#endif
                applied = true;
                FlushPlayerInfo();
            }
        }

        private void HookSendData(
            On_NetMessage.orig_SendData orig,
            int msgType,
            int remoteClient,
            int ignoreClient,
            NetworkText text,
            int number,
            float number2,
            float number3,
            float number4,
            int number5,
            int number6,
            int number7)
        {
            s_origSend ??= orig;

            // Fix the "one-time initial join identity" by blocking the FIRST PlayerInfo until SSC applied.
            if (Main.netMode == NetmodeID.MultiplayerClient &&
                msgType == MessageID.PlayerInfo &&
                !applied &&
                Netplay.Connection != null &&
                Netplay.Connection.State >= 2)
            {
                if (!s_queued)
                {
                    s_queued = true;
                    s_wait = 0;
                    s_t = msgType; s_r = remoteClient; s_i = ignoreClient; s_txt = text;
                    s_n = number; s_n2 = number2; s_n3 = number3; s_n4 = number4;
                    s_n5 = number5; s_n6 = number6; s_n7 = number7;

#if DEBUG
                    string nm = Main.player[Main.myPlayer]?.name ?? "(null)";
                    Main.NewText($"[SSC] Blocking initial PlayerInfo (selected=\"{nm}\")");
                    Mod.Logger.Debug($"SSC: Blocking initial PlayerInfo (selected=\"{nm}\") state={Netplay.Connection.State}");
#endif
                }

                if (!requested)
                {
                    SendBindRequest("PlayerInfoHook");
                }

                return;
            }

            orig(msgType, remoteClient, ignoreClient, text, number, number2, number3, number4, number5, number6, number7);
        }

        public override void OnWorldUnload()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient && Main.ActivePlayerFileData?.ServerSideCharacter == true)
            {
                SendSave("ExitSave.SSC");
            }

            requested = false;
            applied = false;
            boundStem = null;
            saveTimer = 0;
            ClearQueue();
        }

        public override void PostUpdateEverything()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient || Main.ActivePlayerFileData?.ServerSideCharacter != true)
            {
                return;
            }

            if (++saveTimer < 60u * 10u)
            {
                return;
            }

            saveTimer = 0;
            SendSave("AutoSave.SSC");
        }

        private void EnsureSteamId()
        {
            if (!string.IsNullOrEmpty(steamId))
            {
                return;
            }

            try { steamId = SteamUser.GetSteamID().m_SteamID.ToString(); }
            catch { steamId = Main.clientUUID; }
        }

        private void SendBindRequest(string reason)
        {
            requested = true;
            applied = false;
            saveTimer = 0;
            s_wait = 0;

            EnsureSteamId();

            string joinName =
                Main.ActivePlayerFileData?.Player?.name ??
                Main.player[Main.myPlayer]?.name ??
                Main.LocalPlayer?.name ??
                string.Empty;

#if DEBUG
            Main.NewText($"[SSC] BindRequest({reason}) steamId={steamId} joinName=\"{joinName}\"");
            Mod.Logger.Debug($"SSC: BindRequest({reason}) steamId={steamId} joinName=\"{joinName}\"");
#endif

            var p = Mod.GetPacket();
            p.Write((byte)AdventurePacketIdentifier.SSC);
            p.Write((byte)SSCPacketType.BindRequest);
            p.Write(steamId);
            p.Write(joinName);
            p.Send();
        }

        private void FlushPlayerInfo()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient || s_origSend == null)
            {
                ClearQueue();
                return;
            }

            if (s_queued)
            {
                s_queued = false;
                s_origSend(s_t, s_r, s_i, s_txt, s_n, s_n2, s_n3, s_n4, s_n5, s_n6, s_n7);
            }
            else
            {
                s_origSend(MessageID.PlayerInfo, -1, -1, null, Main.myPlayer, 0f, 0f, 0f, 0, 0, 0);
            }

            s_origSend(MessageID.SyncPlayer, -1, -1, null, Main.myPlayer, 0f, 0f, 0f, 0, 0, 0);

#if DEBUG
            string nm = Main.player[Main.myPlayer]?.name ?? "(null)";
            Main.NewText($"[SSC] Released PlayerInfo name=\"{nm}\"");
            Mod.Logger.Debug($"SSC: Released PlayerInfo name=\"{nm}\"");
#endif
        }

        private static void ClearQueue()
        {
            s_queued = false;
            s_wait = 0;
        }

        private void SendSave(string fileName)
        {
            var plr = Main.LocalPlayer;
            if (plr == null)
            {
                return;
            }

            EnsureSteamId();

            var fd = new PlayerFileData(fileName, false)
            {
                Metadata = FileMetadata.FromCurrentSettings(FileType.Player),
                Player = plr
            };
            fd.MarkAsServerSide();

            byte[] plrBytes = Player.SavePlayerFile_Vanilla(fd);
            TagCompound tplr;

            try { tplr = PlayerIO.SaveData(plr); }
            catch (Exception ex)
            {
#if DEBUG
                Main.NewText("[SSC] SaveData failed; sending empty mod data.");
                Mod.Logger.Error($"SSC: SaveData exception: {ex}");
#endif
                tplr = new TagCompound();
            }

            var p = Mod.GetPacket();
            p.Write((byte)AdventurePacketIdentifier.SSC);
            p.Write((byte)SSCPacketType.UploadPlayer);
            p.Write(steamId);
            p.Write(plr.name);
            p.Write(plrBytes.Length);
            p.Write(plrBytes);
            TagIO.Write(tplr, p);
            p.Send();
        }

        public void HandlePacket(BinaryReader r, int whoAmI)
        {
            var t = (SSCPacketType)r.ReadByte();

            if (t == SSCPacketType.BindRequest)
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    return;
                }

                string id = r.ReadString();
                string joinName = r.ReadString();

                string worldId = Main.ActiveWorldFileData.UniqueId.ToString();
                string root = Path.Combine(Main.SavePath, "PvPAdventureSSC", worldId, id);
                string stem = GetBoundCharacterStem(root, joinName);

#if DEBUG
                Main.NewText($"[SSC] BindRequest server who={whoAmI} steamId={id} joinName=\"{joinName}\" stem=\"{stem}\"");
                Mod.Logger.Debug($"SSC: BindRequest server who={whoAmI} steamId={id} joinName=\"{joinName}\" stem=\"{stem}\"");
#endif

                string plrPath = Path.Combine(root, stem + ".plr");
                string tplrPath = Path.Combine(root, stem + ".tplr");

                if (File.Exists(plrPath) && File.Exists(tplrPath))
                {
                    byte[] plrData;
                    TagCompound tplrData;

                    try { plrData = File.ReadAllBytes(plrPath); }
                    catch { SendCreate(whoAmI, stem, joinName); return; }

                    try { tplrData = TagIO.FromFile(tplrPath); }
                    catch { tplrData = new TagCompound(); }

                    var p = Mod.GetPacket();
                    p.Write((byte)AdventurePacketIdentifier.SSC);
                    p.Write((byte)SSCPacketType.LoadPlayer);
                    p.Write(stem);
                    p.Write(plrData.Length);
                    p.Write(plrData);
                    TagIO.Write(tplrData, p);
                    p.Send(toClient: whoAmI);
                    return;
                }

                SendCreate(whoAmI, stem, joinName);
                return;
            }

            if (t == SSCPacketType.UploadPlayer)
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    return;
                }

                string id = r.ReadString();
                string name = r.ReadString();
                byte[] plrData = r.ReadBytes(r.ReadInt32());
                TagCompound tplrData = TagIO.Read(r);

                string worldId = Main.ActiveWorldFileData.UniqueId.ToString();
                string root = Path.Combine(Main.SavePath, "PvPAdventureSSC", worldId, id);

                Directory.CreateDirectory(root);

                string stem = GetBoundCharacterStem(root, name);

                try
                {
                    File.WriteAllBytes(Path.Combine(root, stem + ".plr"), plrData);
                    TagIO.ToFile(tplrData, Path.Combine(root, stem + ".tplr"));
                }
                catch (Exception ex)
                {
#if DEBUG
                    Mod.Logger.Error($"SSC: Save failed steamId={id} stem=\"{stem}\": {ex}");
#endif
                }

                return;
            }

            if (t == SSCPacketType.CreatePlayer)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    return;
                }

                string stem = r.ReadString();
                string displayName = r.ReadString();

                var p = new Player();
                var ui = new UICharacterCreation(p);

                p.name = displayName;
                p.difficulty = PlayerDifficultyID.SoftCore;
                ui.SetupPlayerStatsAndInventoryBasedOnDifficulty();

                var lp = Main.LocalPlayer;
                if (lp != null)
                {
                    p.skinVariant = lp.skinVariant;
                    p.skinColor = lp.skinColor;
                    p.eyeColor = lp.eyeColor;
                    p.hair = lp.hair;
                    p.hairColor = lp.hairColor;
                    p.shirtColor = lp.shirtColor;
                    p.underShirtColor = lp.underShirtColor;
                    p.pantsColor = lp.pantsColor;
                    p.shoeColor = lp.shoeColor;
                }

                var fd = new PlayerFileData("Create.SSC", false)
                {
                    Metadata = FileMetadata.FromCurrentSettings(FileType.Player),
                    Player = p
                };
                fd.MarkAsServerSide();

                byte[] plrBytes = Player.SavePlayerFile_Vanilla(fd);
                TagCompound tplr;

                try { tplr = PlayerIO.SaveData(p); }
                catch { tplr = new TagCompound(); }

                ApplyLoadedPlayer(stem, plrBytes, tplr);

                EnsureSteamId();

                var up = Mod.GetPacket();
                up.Write((byte)AdventurePacketIdentifier.SSC);
                up.Write((byte)SSCPacketType.UploadPlayer);
                up.Write(steamId);
                up.Write(p.name);
                up.Write(plrBytes.Length);
                up.Write(plrBytes);
                TagIO.Write(tplr, up);
                up.Send();
                return;
            }

            if (t == SSCPacketType.LoadPlayer)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    return;
                }

                string stem = r.ReadString();
                byte[] plrData = r.ReadBytes(r.ReadInt32());
                TagCompound tplr;

                try { tplr = TagIO.Read(r); }
                catch { tplr = new TagCompound(); }

                ApplyLoadedPlayer(stem, plrData, tplr);
            }
        }

        private void ApplyLoadedPlayer(string stem, byte[] plr, TagCompound tplr)
        {
            boundStem = stem;

            byte[] tplrBytes;
            using (var ms = new MemoryStream())
            {
                TagIO.ToStream(tplr, ms);
                tplrBytes = ms.ToArray();
            }

            var fd = new PlayerFileData(Path.Combine(Main.PlayerPath, stem + ".SSC"), false)
            {
                Metadata = FileMetadata.FromCurrentSettings(FileType.Player)
            };

            Player.LoadPlayerFromStream(fd, plr, tplrBytes);
            fd.MarkAsServerSide();
            fd.SetAsActive();

            var loaded = fd.Player;
            loaded.whoAmI = Main.myPlayer;
            Main.player[Main.myPlayer] = loaded;

            applied = true;
            FlushPlayerInfo();

#if DEBUG
            Main.NewText($"[SSC] Applied stem=\"{stem}\" name=\"{loaded.name}\"");
            Mod.Logger.Debug($"SSC: Applied stem=\"{stem}\" name=\"{loaded.name}\"");
#endif
        }

        private void SendCreate(int toClient, string stem, string name)
        {
            var p = Mod.GetPacket();
            p.Write((byte)AdventurePacketIdentifier.SSC);
            p.Write((byte)SSCPacketType.CreatePlayer);
            p.Write(stem);
            p.Write(name);
            p.Send(toClient: toClient);
        }

        private static string GetBoundCharacterStem(string root, string fallbackName)
        {
            Directory.CreateDirectory(root);

            string bind = Path.Combine(root, "bound.txt");
            if (File.Exists(bind))
            {
                string s = File.ReadAllText(bind).Trim();
                if (!string.IsNullOrWhiteSpace(s))
                {
                    return s;
                }
            }

            string stem = GetOldestCharacterStem(root);
            if (string.IsNullOrEmpty(stem))
            {
                stem = SanitizeFileName(fallbackName);
            }

            File.WriteAllText(bind, stem);
            return stem;
        }

        private static string GetOldestCharacterStem(string root)
        {
            if (!Directory.Exists(root))
            {
                return null;
            }

            string best = null;
            DateTime bestTime = DateTime.MaxValue;

            foreach (string plr in Directory.EnumerateFiles(root, "*.plr"))
            {
                string stem = Path.GetFileNameWithoutExtension(plr);
                if (string.IsNullOrEmpty(stem))
                {
                    continue;
                }

                string tplr = Path.Combine(root, stem + ".tplr");
                if (!File.Exists(tplr))
                {
                    continue;
                }

                DateTime t = File.GetLastWriteTimeUtc(plr);
                if (t < bestTime)
                {
                    bestTime = t;
                    best = stem;
                }
            }

            return best;
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "Player";
            }

            string s = name.Trim();
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                s = s.Replace(c, '_');
            }

            return string.IsNullOrWhiteSpace(s) ? "Player" : s;
        }
    }
}
