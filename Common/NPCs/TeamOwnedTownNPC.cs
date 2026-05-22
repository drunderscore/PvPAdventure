using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.World.Outlines.BoundNPCs;
using PvPAdventure.Content.NPCs;
using PvPAdventure.Core.Config;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Team = Terraria.Enums.Team;

namespace PvPAdventure.Common.NPCs;

public sealed class TeamOwnedTownNPC : GlobalNPC
{
    private const string OwnerTeamKey = "OwnerTeam";
    //private const string DeniedInteractionText = "Your team is not allowed to interact with this NPC!";

    public override bool InstancePerEntity => true;

    public Team OwnerTeam { get; private set; } = Team.None;

    public override bool AppliesToEntity(NPC entity, bool lateInstantiation) =>
        entity.ModNPC is BoundNPC || entity.isLikeATownNPC;

    public void SetOwnerTeam(NPC npc, Team team, bool sync = true)
    {
        OwnerTeam = NormalizeTeam(team);
        npc.netUpdate = true;

        if (sync && Main.netMode == NetmodeID.Server)
            NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
    }

    public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        bool hasOwner = OwnerTeam != Team.None;
        bitWriter.WriteBit(hasOwner);

        if (hasOwner)
            binaryWriter.Write((byte)OwnerTeam);
    }

    public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
    {
        OwnerTeam = bitReader.ReadBit() ? NormalizeTeam((Team)binaryReader.ReadByte()) : Team.None;
    }

    public override void SaveData(NPC npc, TagCompound tag)
    {
        if (OwnerTeam != Team.None)
            tag[OwnerTeamKey] = (int)OwnerTeam;
    }

    public override void LoadData(NPC npc, TagCompound tag)
    {
        OwnerTeam = tag.ContainsKey(OwnerTeamKey)
            ? NormalizeTeam((Team)tag.GetInt(OwnerTeamKey))
            : Team.None;
    }

    public override bool? CanChat(NPC npc)
    {
        // Always allow.
        return true;

        // Keep this commented out. Uncomment this if we ever wanna gate town NPCs on teams again.
        //if (IsLockedForLocalPlayer())
        //    return false;

        //return null;
    }

    public override bool PreHoverInteract(NPC npc, bool mouseIntersects)
    {
        // Always allow.
        return true;

        // Keep this commented out. Uncomment this if we ever wanna gate town NPCs on teams again.
        //if (!IsLockedForLocalPlayer())
        //    return true;

        //if (Main.mouseRight && Main.mouseRightRelease)
        //{
        //    ShowDeniedInteractionText();
        //    Main.mouseRightRelease = false;
        //}

        //return false;
    }

    public override bool PreChatButtonClicked(NPC npc, bool firstButton)
    {
        // Always allow.
        return true;

        // Keep this commented out. Uncomment this if we ever wanna gate town NPCs on teams again.
        //if (!IsLockedForLocalPlayer())
        //    return true;

        //ShowDeniedInteractionText();
        //return false;
    }

    private bool IsLockedForLocalPlayer()
    {
        if (OwnerTeam == Team.None || Main.dedServ)
            return false;

        Player player = Main.LocalPlayer;
        return player == null || !player.active || (Team)player.team != OwnerTeam;
    }

    //private static void ShowDeniedInteractionText()
    //{
    //    if (Main.netMode != NetmodeID.Server)
    //        Main.NewText(DeniedInteractionText, Color.Red);
    //}

    private static Team NormalizeTeam(Team team)
    {
        int teamIndex = (int)team;
        return teamIndex > 0 && teamIndex < Main.teamColor.Length ? team : Team.None;
    }

    #region Drawing

    public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        if (OwnerTeam == Team.None || !npc.active)
            return true;

        var config = ModContent.GetInstance<ClientConfig>();

        if (!config.Outlines.DrawOutlines || !config.Outlines.TownNPCOutlines)
            return true;

        Rectangle screenBounds = new(
            (int)Main.screenPosition.X,
            (int)Main.screenPosition.Y,
            Main.screenWidth,
            Main.screenHeight);

        if (!npc.getRect().Intersects(screenBounds))
            return true;

        Color borderColor = Main.teamColor[(int)OwnerTeam];
        borderColor.A = 255;

        DrawTeamOutline(npc, spriteBatch, screenPos, borderColor);

        return true;
    }

    private static void DrawTeamOutline(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color borderColor)
    {
        var outlineSystem = ModContent.GetInstance<BoundNpcOutlineSystem>();

        if (!outlineSystem.TryGet(npc, borderColor, out RenderTarget2D renderTarget, out Vector2 origin))
            return;

        Rectangle frame = npc.frame;

        if (frame.Width <= 0 || frame.Height <= 0)
        {
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            int frameCount = Main.npcFrameCount[npc.type];
            frame = texture.Frame(1, frameCount > 0 ? frameCount : 1);
        }

        float visualCenterY = npc.Center.Y + (npc.height - frame.Height) * 0.25f;

        Vector2 drawPosition = new(
            npc.Center.X - screenPos.X,
            visualCenterY - screenPos.Y + npc.gfxOffY);

        Color lightColor = Lighting.GetColor(npc.Center.ToTileCoordinates());
        Color finalColor = lightColor * npc.Opacity;
        finalColor.A = (byte)(255f * npc.Opacity);

        SpriteEffects effects = npc.spriteDirection == 1
            ? SpriteEffects.FlipHorizontally
            : SpriteEffects.None;

        spriteBatch.Draw(
            renderTarget,
            drawPosition,
            null,
            finalColor,
            npc.rotation,
            origin,
            npc.scale,
            effects,
            0f);
    }
    #endregion

    //private static Terraria.DataStructures.DrawData CreateTownNpcDrawData(NPC npc, Vector2 screenPos)
    //{
    //    Texture2D texture = TextureAssets.Npc[npc.type].Value;
    //    Rectangle frame = npc.frame;

    //    if (frame.Width <= 0 || frame.Height <= 0)
    //    {
    //        int frameCount = Main.npcFrameCount[npc.type];
    //        frame = texture.Frame(1, frameCount > 0 ? frameCount : 1);
    //    }

    //    Vector2 origin = frame.Size() * 0.5f;
    //    Vector2 position = npc.Center - screenPos;
    //    position.Y += npc.gfxOffY;

    //    SpriteEffects effects = npc.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
    //    Color color = npc.GetAlpha(Color.White) * npc.Opacity;

    //    return new Terraria.DataStructures.DrawData(texture, position, frame, color, npc.rotation, origin, npc.scale, effects, 0);
    //}

    //private static void DrawOutlineCopies(
    //    SpriteBatch spriteBatch,
    //    Terraria.DataStructures.DrawData[] source,
    //    float alpha,
    //    float scale,
    //    Color borderColor)
    //{
    //    if (borderColor == Color.Transparent)
    //        return;

    //    float distance = 2f * scale;
    //    float opacity = alpha * alpha;

    //    Color outerColor = Color.Black * opacity;
    //    Color innerColor = borderColor * opacity;

    //    int shader = ContentSamples.CommonlyUsedContentSamples.ColorOnlyShaderIndex;

    //    DrawOutlineRing(spriteBatch, source, shader, outerColor, distance, 2);
    //    DrawOutlineRing(spriteBatch, source, shader, innerColor, distance, 1);
    //}

    //private static void DrawOutlineRing(
    //    SpriteBatch spriteBatch,
    //    Terraria.DataStructures.DrawData[] source,
    //    int shader,
    //    Color color,
    //    float distance,
    //    int radius)
    //{
    //    for (int x = -radius; x <= radius; x++)
    //    {
    //        for (int y = -radius; y <= radius; y++)
    //        {
    //            if (System.Math.Abs(x) + System.Math.Abs(y) != radius)
    //                continue;

    //            Vector2 offset = new(x * distance, y * distance);

    //            for (int i = 0; i < source.Length; i++)
    //            {
    //                Terraria.DataStructures.DrawData drawData = source[i];
    //                drawData.position += offset;
    //                drawData.color = color;
    //                drawData.shader = shader;
    //                drawData.Draw(spriteBatch);
    //            }
    //        }
    //    }
    //}
}
