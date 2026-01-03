using Microsoft.Build.Tasks;
using Microsoft.Xna.Framework;
using PvPAdventure.Core.SSC.UI;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace PvPAdventure.Core.SSC;

public class ServerSystem : ModSystem
{
    internal UserInterface UI;
    internal uint Timer;
    internal Task SaveTask;
    internal TagCompound LastCharacterList;
    public static int Count;
    const int AutoSaveSeconds = 10;

    public override void Load()
    {
        if (!SSCBuild.Enabled)
            return;

        if (!Main.dedServ)
        {
            UI = new UserInterface();
        }
    }

    public override void UpdateUI(GameTime gameTime)
    {
        if (UI?.CurrentState != null)
        {
            UI.Update(gameTime);

            // Debug
            //(UI?.CurrentState as ServerViewer)?.Calc(LastCharacterList);
        }
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if (!SSCBuild.Enabled)
            return;

        var index = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
        if (index != -1)
        {
            layers.Insert(index, new LegacyGameInterfaceLayer("Vanilla: SSC UI", () =>
            {
                if (UI?.CurrentState != null)
                {
                    bool old = ModifyPlayerDrawInfo.ForceFullBrightOnce;
                    ModifyPlayerDrawInfo.ForceFullBrightOnce = true;

                    UI.Draw(Main.spriteBatch, Main.gameTimeCache);

                    ModifyPlayerDrawInfo.ForceFullBrightOnce = old;
                }

                return true;
            }, InterfaceScaleType.UI));
        }
    }
    public override void NetSend(BinaryWriter writer)
    {
        if (!SSCBuild.Enabled)
        {
            TagIO.Write(new TagCompound(), writer);
            return;
        }

        var root = new TagCompound();

        // SSC/ or SSC/<WORLD-ID>/, where the first level contains SteamID folders
        // and the second level contains .plr files.
        // When only SSC/ exists, the world folder is read, but since it only contains
        // SteamID directories and no .plr files, there is no performance impact.
        Utils.TryCreatingDirectory(Path.Combine(SSC.PATH, SSC.MapID));

        var users = new DirectoryInfo(Path.Combine(SSC.PATH, SSC.MapID)).GetDirectories(); // Get first-level directories
        foreach (var user in users)
        {
            root.Set(user.Name, new List<TagCompound>());

            foreach (var fileInfo in user.GetFiles("*.plr")) // Get player files
            {
                var plrBytes = File.ReadAllBytes(fileInfo.FullName);
                var fileData = Player.LoadPlayer(fileInfo.FullName, false);
                root.Get<List<TagCompound>>(user.Name).Add(new TagCompound
            {
                { "plr", plrBytes },
                { "name", fileData.Player.name },
                { "play_time", fileData.GetPlayTime().Ticks },
                { "lifeMax", fileData.Player.statLifeMax2 },
                { "manaMax", fileData.Player.statManaMax2 },
            });
            }
        }

        TagIO.Write(root, writer);
    }

    public override void NetReceive(BinaryReader reader)
    {
        if (!SSCBuild.Enabled)
        {
            _ = TagIO.Read(reader); // consume bytes
            LastCharacterList = null;
            return;
        }

        var root = TagIO.Read(reader);
        LastCharacterList = root;
        (UI?.CurrentState as ServerViewer)?.Calc(root);
    }

    public override void PostUpdateEverything()
    {
        if (!SSCBuild.Enabled)
            return;

        //// Ghost leash
        //var Player = Main.LocalPlayer;
        //if (!Player.ghost)
        //{
        //    return;
        //}

        //// Match your RegionManager default spawn region: 50x50 tiles centered on spawn.
        //int leftTile = Main.spawnTileX - 25;
        //int topTile = Main.spawnTileY - 25;
        //int widthTiles = 50;
        //int heightTiles = 50;

        //Rectangle bounds = new(leftTile * 16, topTile * 16, widthTiles * 16, heightTiles * 16);

        //Rectangle hitbox = Player.getRect();
        //Vector2 newPos = Player.position;

        //// Clamp X (Player.position is top-left)
        //if (hitbox.Left < bounds.Left)
        //{
        //    newPos.X += bounds.Left - hitbox.Left;
        //    Player.velocity.X = 0f;
        //}
        //else if (hitbox.Right > bounds.Right)
        //{
        //    newPos.X -= hitbox.Right - bounds.Right;
        //    Player.velocity.X = 0f;
        //}

        //// Clamp Y
        //if (hitbox.Top < bounds.Top)
        //{
        //    newPos.Y += bounds.Top - hitbox.Top;
        //    Player.velocity.Y = 0f;
        //}
        //else if (hitbox.Bottom > bounds.Bottom)
        //{
        //    newPos.Y -= hitbox.Bottom - bounds.Bottom;
        //    Player.velocity.Y = 0f;
        //}

        // Teleport ghost players to world spawn every second
        if (Main.LocalPlayer != null && Main.LocalPlayer.ghost)
        {
            Count++;
            if (Count > 60)
            {
                Count = 0;

                int floorX = Main.spawnTileX;
                int floorY = Main.spawnTileY;
                Main.LocalPlayer.Spawn_GetPositionAtWorldSpawn(ref floorX, ref floorY);

                var spawnPosition = new Vector2(floorX * 16, floorY * 16);
                if (Vector2.Distance(Main.LocalPlayer.position, spawnPosition) > 60)
                {
                    Main.LocalPlayer.position = spawnPosition;
                }
            }
        }

        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            return;
        }

        // Auto-save every AutoSaveSeconds seconds
        Timer++;
        if (Timer > AutoSaveSeconds * 60)
        {
            Timer = 0;
            if (SaveTask is not { Status: TaskStatus.Running })
            {
                SaveTask = Task.Run(() =>
                {
                    var fileData = Main.ActivePlayerFileData;
                    if (fileData != null)
                    {
                        Player.InternalSavePlayerFile(fileData);
                    }
                });
            }
        }
    }

    public override void PreSaveAndQuit()
    {
        base.PreSaveAndQuit();
        // Save once before exiting the game
        Player.InternalSavePlayerFile(Main.ActivePlayerFileData);
    }

    public override void Unload()
    {
        UI = null;
    }
}