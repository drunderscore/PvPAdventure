using Microsoft.Xna.Framework;
using PvPAdventure.Common.SpawnSelector;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Content.NPCs;

public sealed class PortalNPC : ModNPC
{
    public const int PortalWidth = 48;
    public const int PortalHeight = 72;
    private const int OwnerMissingGraceTicks = 120;

    private string ownerName = string.Empty;
    private int ownerMissingTicks;

    public override string Texture => "PvPAdventure/Assets/Custom/Portal";
    public override LocalizedText DisplayName => base.DisplayName;
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
        get => (int)NPC.ai[1];
        private set => NPC.ai[1] = value;
    }

    public int CreateTicksRemaining
    {
        get => Math.Max(0, (int)NPC.ai[3]);
        private set => NPC.ai[3] = Math.Max(0, value);
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
        NPC.lifeMax = PortalSystem.PortalMaxHealth;
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
        NPC.lifeMax = PortalSystem.PortalMaxHealth;
        NPC.life = NPC.lifeMax;
        NPC.position = PortalSystem.GetPortalTopLeft(worldPos);
        NPC.velocity = Vector2.Zero;
        CreateTicksRemaining = 0;
        NPC.netUpdate = true;
    }

    public override void AI()
    {
        NPC.velocity = Vector2.Zero;

        if (CreateTicksRemaining > 0)
            CreateTicksRemaining--;

        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        if (!TryGetOwner(out Player owner))
        {
            if (++ownerMissingTicks >= OwnerMissingGraceTicks)
                PortalSystem.RemovePortalNpc(NPC, silent: true);

            return;
        }

        if (ownerName != owner.name)
        {
            ownerName = owner.name ?? string.Empty;
            NPC.GivenName = $"{GetOwnerName()}'s Portal";
            NPC.netUpdate = true;
        }

        ownerMissingTicks = 0;

        if (owner.dead)
        {
            PortalSystem.RemovePortalNpc(NPC, silent: true);
            return;
        }

        if (OwnerTeam != owner.team)
        {
            OwnerTeam = owner.team;
            NPC.netUpdate = true;
        }

        int currentMaxHealth = PortalSystem.PortalMaxHealth;
        if (NPC.lifeMax != currentMaxHealth)
        {
            NPC.lifeMax = currentMaxHealth;
            NPC.life = NPC.lifeMax;
            NPC.netUpdate = true;
        }
    }

    public override bool CheckActive() => false;

    public override bool? CanBeHitByItem(Player player, Item item)
    {
        if (item == null || item.IsAir || item.damage <= 0 || item.noMelee)
            return false;

        return PortalSystem.CanPlayerDamagePortal(player, OwnerIndex);
    }

    public override bool? CanBeHitByProjectile(Projectile projectile)
    {
        if (projectile == null || !projectile.active || !projectile.friendly || projectile.hostile || projectile.damage <= 0)
            return false;

        if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
            return false;

        return PortalSystem.CanPlayerDamagePortal(Main.player[projectile.owner], OwnerIndex);
    }

    public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;

    public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
    {
        NormalizePortalHit(ref modifiers);
    }

    public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
    {
        NormalizePortalHit(ref modifiers);
    }

    private static void NormalizePortalHit(ref NPC.HitModifiers modifiers)
    {
        modifiers.DefenseEffectiveness *= 0f;
        modifiers.DamageVariationScale *= 0f;
        modifiers.DisableCrit();
        modifiers.DisableKnockback();
        modifiers.HideCombatText();
    }

    public override void HitEffect(NPC.HitInfo hit)
    {
        PortalSystem.PlayPortalFx(WorldPosition, NPC.life <= 0, hit.Damage);
    }

    public override void OnKill()
    {
        PortalSystem.HandlePortalKilled(NPC, this);
    }

    public override bool PreDraw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) => false;

    public override void FindFrame(int frameHeight)
    {
        int frame = (int)(Main.GameUpdateCount / 5 % Main.npcFrameCount[Type]);
        NPC.frame = new Rectangle(0, frame * frameHeight, NPC.width, frameHeight);
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(ownerName ?? string.Empty);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        ownerName = reader.ReadString();
        NPC.GivenName = $"{GetOwnerName()}'s Portal";
    }

    private bool TryGetOwner(out Player owner)
    {
        owner = OwnerIndex >= 0 && OwnerIndex < Main.maxPlayers ? Main.player[OwnerIndex] : null;
        return owner?.active == true;
    }
}
