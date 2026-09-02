using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using PvPAdventure.Common.Combat.EJ;

namespace PvPAdventure.Content.Buffs;
/// <summary>
/// Handles the Godsprotection Buff, which gives an outline to the player who has it (based on erkyssc's god outline)
/// </summary>

public class GodsProtection : ModBuff
{
    public override string Texture => $"PvPAdventure/Assets/Buff/GodsProtection";

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = false;
        Main.buffNoTimeDisplay[Type] = true;
        Main.persistentBuff[Type] = false;
    }
}

public class GodsProtectionPlayer : ModPlayer
{
    private static readonly Vector2 RenderSize = new(16 * 6, 16 * 6);

    private RenderTarget2D renderTarget;
    private DrawData outlineDrawData;
    private bool ready;

    public bool IsAuraActive { get; private set; }

    public bool IsAuraGold { get; private set; }

    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        if (Player.HasBuff<GodsProtection>())
        {
            modifiers.FinalDamage *= 0.5f;
        }
    }

    public override void OnEnterWorld()
    {
        ready = false;
    }

    public override void Unload()
    {
        DisposeRenderTarget();
    }

    public override void PostUpdate()
    {
        bool hasBuff = Player.HasBuff<GodsProtection>();
        bool wearingCrossItem = Player.GetModPlayer<CrossNecklaceChanges>().IsWearingCrossItem;

        IsAuraActive = hasBuff || wearingCrossItem;
        IsAuraGold = hasBuff;

        if (Main.dedServ)
            return;

        if (!IsAuraActive || !Player.active || Player.dead)
        {
            ready = false;
            return;
        }

        EnsureRenderTarget();
        RenderPlayerToTarget();
        BuildOutlineDrawData();
        ready = true;
    }

    public bool TryGetOutlineDrawData(out DrawData drawData)
    {
        drawData = outlineDrawData;
        return ready && IsAuraActive && renderTarget != null && !renderTarget.IsDisposed;
    }

    private void EnsureRenderTarget()
    {
        int width = (int)RenderSize.X;
        int height = (int)RenderSize.Y;

        if (renderTarget != null && !renderTarget.IsDisposed && renderTarget.Width == width && renderTarget.Height == height)
            return;

        DisposeRenderTarget();
        renderTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, width, height);
    }

    private void DisposeRenderTarget()
    {
        if (renderTarget != null && !renderTarget.IsDisposed)
            renderTarget.Dispose();

        renderTarget = null;
        ready = false;
    }

    private void RenderPlayerToTarget()
    {
        List<DrawData> drawData = [];
        List<int> dust = [];
        List<int> gore = [];

        PlayerDrawSet drawInfo = new();
        drawInfo.BoringSetup(Player, drawData, dust, gore, Player.position, 0f, 0f, Vector2.Zero);
        DrawPlayerOnlyDrawLayers(ref drawInfo);

        GraphicsDevice device = Main.graphics.GraphicsDevice;
        SpriteBatch spriteBatch = Main.spriteBatch;

        device.SetRenderTarget(renderTarget);
        device.Clear(Color.Transparent);

        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.EffectMatrix);

        Vector2 screenPlayerPosition = Player.position - Main.screenPosition;
        Vector2 targetOffset = RenderSize * 0.5f - Player.Size * 0.5f - screenPlayerPosition;

        for (int i = 0; i < drawData.Count; i++)
        {
            DrawData data = drawData[i];
            data.position += targetOffset;
            data.Draw(spriteBatch);
        }

        spriteBatch.End();
        device.SetRenderTarget(null);
    }

    private void BuildOutlineDrawData()
    {
        outlineDrawData = new DrawData(
            renderTarget,
            Vector2.Zero,
            null,
            Color.White,
            0f,
            (RenderSize * 0.5f).Floor(),
            1f,
            SpriteEffects.None,
            0
        );
    }

    private static void DrawPlayerOnlyDrawLayers(ref PlayerDrawSet drawInfo)
    {
        PlayerDrawLayers.DrawPlayer_extra_TorsoPlus(ref drawInfo);
        PlayerDrawLayers.DrawPlayer_01_2_JimsCloak(ref drawInfo);
        PlayerDrawLayers.DrawPlayer_extra_TorsoMinus(ref drawInfo);
        PlayerDrawLayers.DrawPlayer_03_Carpet(ref drawInfo);
        PlayerDrawLayers.DrawPlayer_03_PortableStool(ref drawInfo);
        PlayerDrawLayers.DrawPlayer_extra_TorsoPlus(ref drawInfo);
        PlayerDrawLayers.DrawPlayer_extra_TorsoMinus(ref drawInfo);
        PlayerDrawLayers.DrawPlayer_08_Backpacks(ref drawInfo);
        PlayerDrawLayers.DrawPlayer_extra_TorsoPlus(ref drawInfo);
        PlayerDrawLayers.DrawPlayer_08_1_Tails(ref drawInfo);
        PlayerDrawLayers.DrawPlayer_extra_TorsoMinus(ref drawInfo);
        PlayerDrawLayers.DrawPlayer_extra_TorsoPlus(ref drawInfo);
        PlayerDrawLayers.DrawPlayer_01_BackHair(ref drawInfo);
        PlayerDrawLayers.DrawPlayer_10_BackAcc(ref drawInfo);
        PlayerDrawLayers.DrawPlayer_01_3_BackHead(ref drawInfo);
        PlayerDrawLayers.DrawPlayer_extra_TorsoMinus(ref drawInfo);
        PlayerDrawLayers.DrawPlayer_12_Skin(ref drawInfo);

        if (drawInfo.drawPlayer.wearsRobe && drawInfo.drawPlayer.body != 166)
        {
            PlayerDrawLayers.DrawPlayer_14_Shoes(ref drawInfo);
            PlayerDrawLayers.DrawPlayer_13_Leggings(ref drawInfo);
        }
        else
        {
            PlayerDrawLayers.DrawPlayer_13_Leggings(ref drawInfo);
            PlayerDrawLayers.DrawPlayer_14_Shoes(ref drawInfo);
        }

        PlayerDrawLayers.DrawPlayer_extra_TorsoPlus(ref drawInfo);
        PlayerDrawLayers.DrawPlayer_15_SkinLongCoat(ref drawInfo);
        PlayerDrawLayers.DrawPlayer_16_ArmorLongCoat(ref drawInfo);
        PlayerDrawLayers.DrawPlayer_17_Torso(ref drawInfo);
        PlayerDrawLayers.DrawPlayer_18_OffhandAcc(ref drawInfo);
        PlayerDrawLayers.DrawPlayer_19_WaistAcc(ref drawInfo);
        PlayerDrawLayers.DrawPlayer_20_NeckAcc(ref drawInfo);
        PlayerDrawLayers.DrawPlayer_21_Head(ref drawInfo);
        PlayerDrawLayers.DrawPlayer_21_1_Magiluminescence(ref drawInfo);
        PlayerDrawLayers.DrawPlayer_22_FaceAcc(ref drawInfo);

        if (drawInfo.drawFrontAccInNeckAccLayer)
        {
            PlayerDrawLayers.DrawPlayer_extra_TorsoMinus(ref drawInfo);
            PlayerDrawLayers.DrawPlayer_32_FrontAcc_FrontPart(ref drawInfo);
            PlayerDrawLayers.DrawPlayer_extra_TorsoPlus(ref drawInfo);
        }

        PlayerDrawLayers.DrawPlayer_32_FrontAcc_BackPart(ref drawInfo);
        PlayerDrawLayers.DrawPlayer_25_Shield(ref drawInfo);
        PlayerDrawLayers.DrawPlayer_extra_MountPlus(ref drawInfo);
        PlayerDrawLayers.DrawPlayer_extra_MountMinus(ref drawInfo);
        PlayerDrawLayers.DrawPlayer_28_ArmOverItem(ref drawInfo);
        PlayerDrawLayers.DrawPlayer_29_OnhandAcc(ref drawInfo);
        PlayerDrawLayers.DrawPlayer_30_BladedGlove(ref drawInfo);

        if (!drawInfo.drawFrontAccInNeckAccLayer)
            PlayerDrawLayers.DrawPlayer_32_FrontAcc_FrontPart(ref drawInfo);

        PlayerDrawLayers.DrawPlayer_extra_TorsoMinus(ref drawInfo);
    }
}

public class GodsProtectionOutlineLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.Skin);

    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => !drawInfo.drawPlayer.dead;

    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        Player player = drawInfo.drawPlayer;
        GodsProtectionPlayer modPlayer = player.GetModPlayer<GodsProtectionPlayer>();

        if (!modPlayer.TryGetOutlineDrawData(out DrawData baseDrawData))
            return;

        Vector2 center = player.position - Main.screenPosition + player.Size * 0.5f;

        if (modPlayer.IsAuraGold)
        {
            DrawOutlineRing(ref drawInfo, baseDrawData, center, radius: 2f, new Color(255, 210, 60) * 1.00f, directions: 12);
            DrawOutlineRing(ref drawInfo, baseDrawData, center, radius: 4f, new Color(255, 195, 45) * 0.90f, directions: 14);
            DrawOutlineRing(ref drawInfo, baseDrawData, center, radius: 6f, new Color(255, 180, 35) * 0.75f, directions: 16);
            DrawOutlineRing(ref drawInfo, baseDrawData, center, radius: 8f, new Color(255, 165, 25) * 0.55f, directions: 18);
        }
        else
        {
            DrawOutlineRing(ref drawInfo, baseDrawData, center, radius: 2f, Color.White * 0.90f, directions: 12);
            DrawOutlineRing(ref drawInfo, baseDrawData, center, radius: 4f, Color.White * 0.75f, directions: 14);
            DrawOutlineRing(ref drawInfo, baseDrawData, center, radius: 6f, new Color(230, 230, 255) * 0.55f, directions: 16);
        }
    }

    private static void DrawOutlineRing(ref PlayerDrawSet drawInfo, DrawData template, Vector2 center, float radius, Color color, int directions)
    {
        for (int i = 0; i < directions; i++)
        {
            float angle = MathHelper.TwoPi * i / directions;
            Vector2 offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;

            DrawData copy = template;
            copy.position = center + offset;
            copy.color = color;

            drawInfo.DrawDataCache.Add(copy);
        }
    }
}