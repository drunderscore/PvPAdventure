using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Travel.Portals;
using PvPAdventure.Core.Config;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Content.Portals;

public sealed class PortalCreationProjectile : ModProjectile
{
    public override string Texture => "PvPAdventure/Assets/Portals/Portal_NoTeam";

    private int CreationFrames
    {
        get => Math.Max(0, (int)Projectile.ai[0]);
        set => Projectile.ai[0] = Math.Max(0, value);
    }

    private int OwnerTeam
    {
        get => Math.Clamp((int)Projectile.ai[1], 0, Main.teamColor.Length - 1);
        set => Projectile.ai[1] = Math.Clamp(value, 0, Main.teamColor.Length - 1);
    }

    private float ElapsedFrames
    {
        get => Projectile.localAI[0];
        set => Projectile.localAI[0] = value;
    }

    private float Progress => CreationFrames <= 0 ? 1f : MathHelper.Clamp(ElapsedFrames / CreationFrames, 0f, 1f);
    private float Opacity => MathHelper.Lerp(0f, 1f, Progress);

    public void Initialize(Vector2 worldPos, int creationFrames, int ownerTeam)
    {
        CreationFrames = creationFrames;
        OwnerTeam = ownerTeam;
        ElapsedFrames = 0f;

        Projectile.position = worldPos - new Vector2(Projectile.width * 0.5f, Projectile.height);
        Projectile.netUpdate = true;
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(CreationFrames);
        writer.Write(OwnerTeam);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        CreationFrames = reader.ReadInt32();
        OwnerTeam = reader.ReadInt32();
    }

    public override void SetDefaults()
    {
        Projectile.width = PortalNPC.PortalWidth;
        Projectile.height = PortalNPC.PortalHeight;
        Projectile.aiStyle = -1;
        Projectile.timeLeft = 60 * 60;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.friendly = false;
        Projectile.hostile = false;
        Projectile.penetrate = -1;
        Projectile.netImportant = true;
    }

    public override void AI()
    {
        var config = ModContent.GetInstance<ServerConfig>();

        Projectile.velocity = Vector2.Zero;

        if (ElapsedFrames < CreationFrames)
            ElapsedFrames++;

        if (config.ShowPortalCreationProjectile && Main.netMode != NetmodeID.Server)
            PortalDrawer.SpawnPortalDust(Projectile.Bottom, Progress);

        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        if (!TryGetOwner(out Player owner) || owner.dead || owner.ghost || owner.velocity.LengthSquared() > 0f)
        {
            Projectile.Kill();
            //Log.Chat("Portal cancelled");
            return;
        }

        if (ElapsedFrames < CreationFrames)
            return;

        if (!PortalSystem.CreateOrReplacePortal(owner, Projectile.Bottom))
        {
            Projectile.Kill();
            return;
        }

        //Log.Chat("Portal creation projectile killed");
        Projectile.Kill();

        if (Main.netMode == NetmodeID.Server)
            NetMessage.SendData(MessageID.KillProjectile, -1, -1, null, Projectile.identity, Projectile.owner);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        var config = ModContent.GetInstance<ServerConfig>();
        if (!config.ShowPortalCreationProjectile)
            return false;

        Texture2D texture = PortalAssets.GetPortalTexture(OwnerTeam);

        int frameCount = Math.Max(1, Main.npcFrameCount[ModContent.NPCType<PortalNPC>()]);
        int frameIndex = (int)(Main.GameUpdateCount / 5 % frameCount);
        Rectangle frame = texture.Frame(1, frameCount, 0, frameIndex);

        Vector2 origin = frame.Size() * 0.5f;
        Vector2 position = Projectile.Center - Main.screenPosition;

        Main.spriteBatch.Draw(texture, position, frame, Color.White * Opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);

        int visibleHealth = Math.Max(1, (int)(PortalNPC.PortalMaxHealth * Progress));
        PortalDrawer.DrawPortalHealthBar(
            Main.spriteBatch,
            Projectile.Center + new Vector2(0f, 24f * Projectile.scale),
            visibleHealth,
            PortalNPC.PortalMaxHealth,
            Projectile.scale,
            Opacity
        );

        return false;
    }

    public void Initialize(Vector2 worldPos)
    {
        Projectile.position = worldPos - new Vector2(Projectile.width * 0.5f, Projectile.height);
        Projectile.netUpdate = true;
    }

    private bool TryGetOwner(out Player owner)
    {
        owner = Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers ? Main.player[Projectile.owner] : null;
        return owner?.active == true;
    }

}
