//using Terraria;
//using Terraria.ID;
//using Terraria.ModLoader;

//namespace PvPAdventure.Common.SSC;

//[Autoload(Side = ModSide.Both)]
//internal sealed class SpectatorPlayer : ModPlayer
//{
//    // For testing: easiest “switch” is a special name.
//    // Replace with config / command / IP check as you prefer.
//    public bool Enabled;

//    public Vector2 AnchorPosition;
//    public Vector2 CameraCenter;

//    public override void OnEnterWorld()
//    {
//        Enabled = Player.name == "Spectator";

//        if (!Enabled)
//        {
//            return;
//        }

//        AnchorPosition = Player.position;
//        CameraCenter = Player.Center;
//    }

//    public override void ResetEffects()
//    {
//        if (!Enabled)
//        {
//            return;
//        }

//        // Keep the player visually “not there”.
//        Player.AddBuff(BuffID.Invisibility, 2, quiet: true);

//        // Avoid annoying incidental mechanics.
//        Player.hostile = false;
//        Player.noKnockback = true;
//    }

//    public override void PreUpdate()
//    {
//        if (!Enabled)
//        {
//            return;
//        }

//        // Server-authoritative freeze so the spectator cannot move even if the client sends controls.
//        if (Main.netMode == NetmodeID.Server)
//        {
//            Player.velocity = Vector2.Zero;
//            Player.position = AnchorPosition;
//        }
//    }

//    public override bool CanUseItem(Item item)
//    {
//        if (Enabled)
//        {
//            return false;
//        }

//        return true;
//    }

//    public override bool? CanHitNPCWithProj(Projectile proj, NPC target)
//    {
//        if (Enabled)
//        {
//            return false;
//        }

//        return null;
//    }

//    public override bool CanHitPvp(Item item, Player target)
//    {
//        if (Enabled)
//        {
//            return false;
//        }

//        return true;
//    }

//    public override bool CanHitPvpWithProj(Projectile proj, Player target)
//    {
//        if (Enabled)
//        {
//            return false;
//        }

//        return true;
//    }

//    public override bool CanBeHitByProjectile(Projectile proj)
//    {
//        if (Enabled)
//        {
//            return false;
//        }

//        return true;
//    }

//    public override bool CanBeHitByNPC(NPC npc, ref int cooldownSlot)
//    {
//        if (Enabled)
//        {
//            return false;
//        }

//        return true;
//    }
//}
