using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Travel.Portals;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader;

namespace PvPAdventure.Content.Portals;

public sealed class PortalNPC : ModNPC
{
    public static int PortalMaxHealth => NPC.downedPlantBoss ? 300 : Main.hardMode ? 150 : 50;
    public const int PortalWidth = 36;
    public const int PortalHeight = 46;
    private string ownerName = string.Empty;

    public override string Texture => "PvPAdventure/Assets/Portals/Portal_NoTeam";
    public override LocalizedText DisplayName => base.DisplayName;
    public override bool CheckActive() => false;

    private string GetOwnerName()
    {
        if (!string.IsNullOrWhiteSpace(ownerName))
            return ownerName;

        if (TryGetOwner(out Player owner) && !string.IsNullOrWhiteSpace(owner.name))
            return owner.name;

        return "Unknown Player";
    }

    public int OwnerIndex
    {
        get => (int)NPC.ai[0];
        private set => NPC.ai[0] = value;
    }

    public int OwnerTeam
    {
        get => Math.Clamp((int)NPC.ai[1], 0, Main.teamColor.Length - 1);
        private set => NPC.ai[1] = Math.Clamp(value, 0, Main.teamColor.Length - 1);
    }

    public Vector2 WorldPosition => NPC.Bottom;

    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[Type] = 8;
        NPCID.Sets.ImmuneToAllBuffs[Type] = true;
    }

    public override void SetDefaults()
    {
        NPC.width = PortalWidth;
        NPC.height = PortalHeight;
        NPC.lifeMax = PortalMaxHealth;
        NPC.life = NPC.lifeMax;
        NPC.damage = 0;
        NPC.defense = 0;
        NPC.knockBackResist = 0f;
        NPC.aiStyle = -1;
        NPC.noGravity = true;
        NPC.noTileCollide = true;
        NPC.npcSlots = 0f;
        NPC.value = 0f;
        NPC.chaseable = false;
        NPC.friendly = false;
        NPC.netAlways = true;
        NPC.ShowNameOnHover = true;
    }

    internal void Initialize(Player owner, Vector2 worldPos)
    {
        OwnerIndex = owner.whoAmI;
        OwnerTeam = owner.team;
        ownerName = owner.name ?? string.Empty;

        NPC.GivenName = $"{GetOwnerName()}'s Portal";
        NPC.lifeMax = PortalMaxHealth;
        NPC.life = NPC.lifeMax;
        NPC.position = worldPos - new Vector2(NPC.width * 0.5f, NPC.height);
        NPC.velocity = Vector2.Zero;
        NPC.netUpdate = true;
    }

    public override void AI()
    {
        NPC.velocity = Vector2.Zero;

        if (Main.netMode != NetmodeID.Server)
        {
            int totalFrames = PortalCreatorItem.GetCreationTimeFrames();
            //float progress = totalFrames <= 0 ? 1f : 1f - CreateTicksRemaining / (float)totalFrames;
            PortalDrawer.SpawnPortalDust(NPC.Bottom, 1);

            // Right click
            Player player = Main.LocalPlayer;

            if (NPC.Hitbox.Contains(Main.MouseWorld.ToPoint()) && player.Distance(NPC.Center) < 200f)
            {
                player.noThrow = 2;
                player.cursorItemIconEnabled = true;
                player.cursorItemIconID = -1;

                if (Main.mouseRight && Main.mouseRightRelease)
                {
                    Main.mouseRightRelease = false;
                    player.TryOpeningFullscreenMap();
                }
            }
        }

        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        if (!TryGetOwner(out Player owner))
        {
            PortalSystem.ClearPortal(OwnerIndex);
            return;
        }

        if (ownerName != owner.name)
        {
            ownerName = owner.name ?? string.Empty;
            NPC.GivenName = $"{GetOwnerName()}'s Portal";
            NPC.netUpdate = true;
        }

        if (owner.dead)
        {
            PortalSystem.ClearPortal(owner.whoAmI);
            return;
        }

        if (OwnerTeam != owner.team)
        {
            OwnerTeam = owner.team;
            NPC.netUpdate = true;
        }

        int currentMaxHealth = PortalMaxHealth;
        if (NPC.lifeMax != currentMaxHealth)
        {
            NPC.lifeMax = currentMaxHealth;
            NPC.life = NPC.lifeMax;
            NPC.netUpdate = true;
        }
    }

    public override void FindFrame(int frameHeight)
    {
        int frameCount = Math.Max(1, Main.npcFrameCount[Type]);
        int frame = (int)(Main.GameUpdateCount / 5 % frameCount);
        NPC.frame = new Rectangle(0, frame * frameHeight, NPC.width, frameHeight);
    }

    public override void OnKill()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            PortalSystem.ClearCreationProjectiles(OwnerIndex);
    }

    private bool TryGetOwner(out Player owner)
    {
        owner = OwnerIndex >= 0 && OwnerIndex < Main.maxPlayers ? Main.player[OwnerIndex] : null;
        return owner?.active == true;
    }

    #region Hit / damage from melee & projectiles
    public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;

    private const int MeleeHitCooldown = 20;
    private const int ProjectileHitCooldown = 10;

    private static void NormalizePortalHit(ref NPC.HitModifiers modifiers)
    {
        modifiers.DefenseEffectiveness *= 0f;
        modifiers.DamageVariationScale *= 0f;
        modifiers.DisableCrit();
        modifiers.DisableKnockback();
        modifiers.HideCombatText();
    }

    public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
    {
        NormalizePortalHit(ref modifiers);
    }

    public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
    {
        NormalizePortalHit(ref modifiers);
    }

    public override bool? CanBeHitByItem(Player player, Item item)
    {
        if (item == null || item.IsAir || item.damage <= 0 || item.noMelee)
            return false;

        if (player == null || player.whoAmI < 0 || player.whoAmI >= Main.maxPlayers)
            return false;

        return NPC.immune[player.whoAmI] <= 0;
    }
    public override bool? CanBeHitByProjectile(Projectile projectile)
    {
        if (projectile == null || !projectile.active || !projectile.friendly || projectile.hostile || projectile.damage <= 0)
            return false;

        if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
            return false;

        return NPC.immune[projectile.owner] <= 0;
    }
    public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
    {
        NPC.immune[player.whoAmI] = MeleeHitCooldown;
        PlayPortalFx(WorldPosition, NPC.life <= 0, hit.Damage);
    }

    public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
    {
        NPC.immune[projectile.owner] = ProjectileHitCooldown;
        PlayPortalFx(WorldPosition, NPC.life <= 0, hit.Damage);
    }

    #endregion

    #region Netcode
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(ownerName ?? string.Empty);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        ownerName = reader.ReadString();
        NPC.GivenName = $"{GetOwnerName()}'s Portal";
    }
    #endregion

    #region Drawing
    public override bool PreDraw(SpriteBatch sb, Vector2 screenPos, Color drawColor)
    {
        Texture2D texture = PortalAssets.GetPortalTexture(OwnerTeam);

        Rectangle frame = NPC.frame;
        Vector2 origin = frame.Size() * 0.5f;
        Vector2 position = NPC.Center - screenPos;

        // Draw portal with the team texture. Portals are gameplay markers, so keep them visible in darkness.
        sb.Draw(texture, position, frame, Color.White, NPC.rotation, origin, NPC.scale, NPC.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);

        // Draw healthbar
        PortalDrawer.DrawPortalHealthBar(sb, NPC.Center + new Vector2(0f, 24f * NPC.scale), NPC.life, NPC.lifeMax, NPC.scale, 1f);

        // Draw hover icon if player is within range
        if (NPC.Hitbox.Contains(Main.MouseWorld.ToPoint()))
        {
            // Define 'in range' (usually around 200 pixels for interaction)
            float distance = Vector2.Distance(Main.LocalPlayer.Center, NPC.Center);
            bool inRange = distance < 200f;

            if (inRange && TryGetOwner(out Player owner))
            {
                PortalDrawer.DrawHoverIcon(sb, owner, NPC.Center, Main.teamColor[OwnerTeam], 1f);
            }
        }

        return false;
    }

    public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position) => false;

    #endregion

    #region Visual effects and sounds
    public static void PlayPortalFx(Vector2 worldPos, bool killed, int damage = 0)
    {
        if (Main.dedServ)
            return;

        //if (damage > 0)
        //{
        //    CombatText.NewText(GetPortalHitbox(worldPos), CombatText.DamagedHostile, damage);
        //}

        if (!killed)
        {
            SoundEngine.PlaySound(SoundID.NPCHit4, worldPos);
            return;
        }

        SoundEngine.PlaySound(SoundID.NPCDeath6, worldPos);

        for (int i = 0; i < 42; i++)
        {
            Vector2 velocity = Main.rand.NextVector2Circular(3.5f, 3.5f);
            Dust.NewDustPerfect(worldPos + Main.rand.NextVector2Circular(24f, 36f), DustID.MagicMirror, velocity, 120, Color.White, Main.rand.NextFloat(1.1f, 1.8f));
        }
    }

    public static Rectangle GetPortalHitbox(Vector2 worldPos) =>
        new((int)worldPos.X - 24, (int)worldPos.Y - 72, 48, 72);
    #endregion
}
