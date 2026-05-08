using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.World.Outlines.ItemOutlines;
using PvPAdventure.Common.World.Outlines.TileOutlines;
using PvPAdventure.Core.Config;
using PvPAdventure.Core.Net;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ObjectData;
using TTeam = Terraria.Enums.Team;

namespace PvPAdventure.Common.Teams;

internal sealed class TeamItemSystem : ModSystem
{
    private const string SaveKey = "teamItems";
    private readonly Dictionary<Point, TTeam> teamItems = [];

    public bool TryGetTeam(Point origin, out TTeam team) => teamItems.TryGetValue(origin, out team);

    public override void OnWorldUnload() => teamItems.Clear();

    public override void SaveWorldData(TagCompound tag) => tag[SaveKey] = teamItems.Select(kv => new TagCompound
    {
        ["x"] = kv.Key.X,
        ["y"] = kv.Key.Y,
        ["team"] = (int)kv.Value
    }).ToList();

    public override void LoadWorldData(TagCompound tag)
    {
        teamItems.Clear();
        foreach (TagCompound item in tag.GetList<TagCompound>(SaveKey))
            teamItems[new Point(item.GetInt("x"), item.GetInt("y"))] = (TTeam)item.GetInt("team");
    }

    public override void NetSend(BinaryWriter writer)
    {
        writer.Write(teamItems.Count);
        foreach (var (origin, team) in teamItems)
        {
            writer.Write(origin.X);
            writer.Write(origin.Y);
            writer.Write((byte)team);
        }
    }

    public override void NetReceive(BinaryReader reader)
    {
        teamItems.Clear();
        for (int i = reader.ReadInt32(); i > 0; i--)
            teamItems[new Point(reader.ReadInt32(), reader.ReadInt32())] = (TTeam)reader.ReadByte();
    }

    public void Claim(Point origin, TTeam team)
    {
        if (team == TTeam.None)
            return;

        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
            packet.Write((byte)AdventurePacketIdentifier.TeamItem);
            packet.Write(origin.X);
            packet.Write(origin.Y);
            packet.Send();
            return;
        }

        teamItems[origin] = team;
        if (Main.netMode == NetmodeID.Server)
            NetMessage.SendData(MessageID.WorldData);
    }

    public void Clear(Point origin)
    {
        if (!teamItems.Remove(origin) || Main.netMode != NetmodeID.Server)
            return;

        NetMessage.SendData(MessageID.WorldData);
    }

    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        Point origin = new(reader.ReadInt32(), reader.ReadInt32());

        if (Main.netMode != NetmodeID.Server || whoAmI < 0 || whoAmI >= Main.maxPlayers || (uint)origin.X >= Main.maxTilesX || (uint)origin.Y >= Main.maxTilesY)
            return;

        Player player = Main.player[whoAmI];
        if (player?.active != true)
            return;

        ModContent.GetInstance<TeamItemSystem>().Claim(origin, (TTeam)player.team);
    }

    public static Point Origin(int i, int j)
    {
        Point16 p = TileObjectData.TopLeft(i, j);
        return p == Point16.NegativeOne ? new Point(i, j) : new Point(p.X, p.Y);
    }

    public static bool IsTeamItem(int type) => type is TileID.Campfire or TileID.HangingLanterns or TileID.WaterCandle or TileID.Sunflower or TileID.CatBast or TileID.Banners;

    public static bool IsTeamItem(Tile tile) => IsTeamItem(tile.TileType) && (tile.TileType != TileID.HangingLanterns || tile.TileFrameY is >= 324 and <= 358);

    public static bool IsTeamItem(Item item) => item.type == ItemID.HeartLantern || item.createTile is TileID.Campfire or TileID.WaterCandle or TileID.Sunflower or TileID.CatBast or TileID.Banners;

    public static int Buff(int type) => type switch
    {
        TileID.Campfire => BuffID.Campfire,
        TileID.HangingLanterns => BuffID.HeartLamp,
        TileID.WaterCandle => BuffID.WaterCandle,
        TileID.Sunflower => BuffID.Sunflower,
        TileID.CatBast => BuffID.CatBast,
        _ => 0
    };
}

internal sealed class TeamItemTile : GlobalTile
{
    public override bool PreDraw(int i, int j, int type, SpriteBatch sb)
    {
        if (!ModContent.GetInstance<ClientConfig>().TeamItemOutlines || !TeamItemSystem.IsTeamItem(type))
            return true;

        Point origin = TeamItemSystem.Origin(i, j);
        Tile tile = Main.tile[i, j];
        if (i != origin.X || j != origin.Y || !TeamItemSystem.IsTeamItem(tile) || !ModContent.GetInstance<TeamItemSystem>().TryGetTeam(origin, out TTeam team) || team == TTeam.None)
            return true;

        TileObjectData data = TileObjectData.GetTileData(tile);
        Color border = Main.teamColor[(int)team];
        border.A = 255;

        int w = data?.Width ?? 1;
        int h = data?.Height ?? 1;
        if (ModContent.GetInstance<TileOutlineSystem>().TryGet(type, origin, w, h, border, out RenderTarget2D target, out Vector2 targetOrigin))
            sb.Draw(target, new Vector2(origin.X * 16 + w * 8, origin.Y * 16 + h * 8) - Main.screenPosition + (Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange)), null, Color.White, 0f, targetOrigin, 1f, SpriteEffects.None, 0f);

        return true;
    }

    public override void PlaceInWorld(int i, int j, int type, Item item)
    {
        if (TeamItemSystem.IsTeamItem(item))
        {
            DebugLog.Chat("Team item placed: " + item.Name + ", team: " + (Terraria.Enums.Team)Main.LocalPlayer.team);
            ModContent.GetInstance<TeamItemSystem>().Claim(TeamItemSystem.Origin(i, j), (TTeam)Main.LocalPlayer.team);
        }
    }

    public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
    {
        if (!fail && !effectOnly && TeamItemSystem.IsTeamItem(Main.tile[i, j]))
            ModContent.GetInstance<TeamItemSystem>().Clear(TeamItemSystem.Origin(i, j));
    }

    public override void NearbyEffects(int i, int j, int type, bool closer)
    {
        if (closer || !TeamItemSystem.IsTeamItem(Main.tile[i, j]))
            return;

        TeamItemPlayer p = Main.LocalPlayer.GetModPlayer<TeamItemPlayer>();
        TeamItemSystem items = ModContent.GetInstance<TeamItemSystem>();
        bool own = !items.TryGetTeam(TeamItemSystem.Origin(i, j), out TTeam team) || team == (TTeam)Main.LocalPlayer.team;

        if (type == TileID.Banners)
            p.RecordBanner(TileObjectData.GetTileStyle(Main.tile[i, j]), own);
        else
            p.RecordBuff(TeamItemSystem.Buff(type), own);
    }
}

internal sealed class TeamItemInventory : GlobalItem
{
    public override bool PreDrawInInventory(Item item, SpriteBatch sb, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        if (!ModContent.GetInstance<ClientConfig>().TeamItemOutlines || !TeamItemSystem.IsTeamItem(item))
            return true;

        TTeam team = (TTeam)Main.LocalPlayer.team;
        if (team == TTeam.None)
            return true;

        Color border = Main.teamColor[(int)team];
        border.A = 255;

        if (ModContent.GetInstance<ItemOutlineSystem>().TryGet(item.type, frame.Width, frame.Height, border, out RenderTarget2D target, out Vector2 targetOrigin))
            sb.Draw(target, position, null, Color.White, 0f, targetOrigin, scale, SpriteEffects.None, 0f);

        return true;
    }
}

internal sealed class TeamItemPlayer : ModPlayer
{
    private readonly HashSet<int> allowedBuffs = [];
    private readonly HashSet<int> deniedBuffs = [];
    private readonly HashSet<int> allowedBanners = [];
    private readonly HashSet<int> deniedBanners = [];

    public override void ResetEffects()
    {
        allowedBuffs.Clear();
        deniedBuffs.Clear();
        allowedBanners.Clear();
        deniedBanners.Clear();
    }

    public void RecordBuff(int buff, bool allowed)
    {
        if (buff > 0)
            (allowed ? allowedBuffs : deniedBuffs).Add(buff);
    }

    public void RecordBanner(int banner, bool allowed)
    {
        if (banner > 0)
            (allowed ? allowedBanners : deniedBanners).Add(banner);
    }

    private bool Denied(int buff) => deniedBuffs.Contains(buff) && !allowedBuffs.Contains(buff);

    public override void PreUpdateBuffs()
    {
        if (Denied(BuffID.Campfire)) Main.SceneMetrics.HasCampfire = false;
        if (Denied(BuffID.HeartLamp)) Main.SceneMetrics.HasHeartLantern = false;
        if (Denied(BuffID.WaterCandle)) Main.SceneMetrics.WaterCandleCount = 0;
        if (Denied(BuffID.Sunflower)) Main.SceneMetrics.HasSunflower = false;
        if (Denied(BuffID.CatBast)) Main.SceneMetrics.HasCatBast = false;

        foreach (int buff in deniedBuffs)
            if (!allowedBuffs.Contains(buff))
                Player.ClearBuff(buff);

        foreach (int banner in deniedBanners)
            if (!allowedBanners.Contains(banner) && banner < Main.SceneMetrics.NPCBannerBuff.Length)
                Main.SceneMetrics.NPCBannerBuff[banner] = false;

        if (deniedBanners.Count == 0)
            return;

        Main.SceneMetrics.hasBanner = Main.SceneMetrics.NPCBannerBuff.Any(x => x);
        if (!Main.SceneMetrics.hasBanner)
            Player.ClearBuff(BuffID.MonsterBanner);
    }
}
