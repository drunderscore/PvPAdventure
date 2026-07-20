using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Content.Buffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Content.Mounts;

/// <summary>
/// This is the bunny mount (same as vanilla) used in the race period
/// </summary>
public class RacePeriodMount : ModMount
{
    public override string Texture => "PvPAdventure/Assets/Mounts/Mount_Bunny";

    public override void SetStaticDefaults()
    {
        // Exact same as vanilla bunny
        MountData.buff = ModContent.BuffType<RacePeriodBuff>();
        MountData.spawnDust = 15;
        MountData.heightBoost = 20;
        MountData.fallDamage = 0.8f;
        MountData.runSpeed = 4f;
        MountData.dashSpeed = 7.8f;
        MountData.acceleration = 0.13f;
        MountData.jumpHeight = 15;
        MountData.jumpSpeed = 5.01f;
        MountData.textureWidth = 62;
        MountData.textureHeight = 434;
        MountData.totalFrames = 7;
        int[] offsets = new int[MountData.totalFrames];
        for (int i = 0; i < offsets.Length; i++)
            offsets[i] = 14;
        offsets[2] += 2;
        offsets[3] += 4;
        offsets[4] += 8;
        offsets[5] += 8;
        MountData.playerYOffsets = offsets;

        MountData.xOffset = 1;
        MountData.bodyFrame = 3;
        MountData.yOffset = 4;
        MountData.playerHeadOffset = 22;

        MountData.standingFrameCount = 1;
        MountData.standingFrameDelay = 12;
        MountData.standingFrameStart = 0;

        MountData.runningFrameCount = 7;
        MountData.runningFrameDelay = 12;
        MountData.runningFrameStart = 0;

        MountData.flyingFrameCount = 6;
        MountData.flyingFrameDelay = 6;
        MountData.flyingFrameStart = 1;

        MountData.inAirFrameCount = 1;
        MountData.inAirFrameDelay = 12;
        MountData.inAirFrameStart = 5;

        MountData.swimFrameCount = MountData.inAirFrameCount;
        MountData.swimFrameDelay = MountData.inAirFrameDelay;
        MountData.swimFrameStart = MountData.inAirFrameStart;

        if (Main.netMode != NetmodeID.Server)
            MountData.backTexture = ModContent.Request<Texture2D>(Texture);
    }

    public override void SetMount(Player player, ref bool skipDust) => skipDust = true;
    public override void Dismount(Player player, ref bool skipDust) => skipDust = true;
}