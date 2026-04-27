using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using PvPAdventure.Content.Items;
using PvPAdventure.Content.NPCs;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace PvPAdventure.Common.SpawnSelector;

public static class PortalDrawer
{
    public static Color GetPortalColor(Player player)
    {
        return GetPortalColor(player?.team ?? 0);
    }

    public static Color GetPortalColor(int team)
    {
        if (team <= 0 || team >= Main.teamColor.Length)
            return Color.White;

        return Main.teamColor[team];
    }

    public static Asset<Texture2D> GetPortalAsset(Player player)
    {
        return GetPortalAsset(player?.team ?? 0);
    }

    public static Asset<Texture2D> GetPortalAsset(int team)
    {
        return team switch
        {
            (int)Terraria.Enums.Team.Red => Ass.Portal_Red,
            (int)Terraria.Enums.Team.Green => Ass.Portal_Green,
            (int)Terraria.Enums.Team.Blue => Ass.Portal_Blue,
            (int)Terraria.Enums.Team.Yellow => Ass.Portal_Yellow,
            (int)Terraria.Enums.Team.Pink => Ass.Portal_Pink,
            _ => Ass.Portal,
        };
    }

    public static Asset<Texture2D> GetPortalMinimapAsset(Player player)
    {
        return GetPortalMinimapAsset(player?.team ?? 0);
    }

    public static Asset<Texture2D> GetPortalMinimapAsset(int team)
    {
        return team switch
        {
            (int)Terraria.Enums.Team.Red => Ass.Portal_Red_Minimap,
            (int)Terraria.Enums.Team.Green => Ass.Portal_Green_Minimap,
            (int)Terraria.Enums.Team.Blue => Ass.Portal_Blue_Minimap,
            (int)Terraria.Enums.Team.Yellow => Ass.Portal_Yellow_Minimap,
            (int)Terraria.Enums.Team.Pink => Ass.Portal_Pink_Minimap,
            _ => Ass.Portal_Minimap,
        };
    }

    public static Rectangle GetPortalFrameRectangle(Texture2D texture, int frameCount = 8)
    {
        int frame = (int)(Main.GameUpdateCount / 5 % frameCount);
        int frameHeight = texture.Height / frameCount;
        return new Rectangle(0, frame * frameHeight, texture.Width, frameHeight);
    }

    public static void DrawAllPortals(SpriteBatch spriteBatch)
    {
        if (Main.dedServ)
            return;

        foreach (NPC npc in PortalSystem.EnumeratePortalNpcs())
        {
            if (npc.ModNPC is not PortalNPC portal)
                continue;

            Vector2 worldPos = portal.WorldPosition;
            int team = portal.OwnerTeam;
            int health = npc.life;
            int maxHealth = npc.lifeMax;
            int createTicksRemaining = portal.CreateTicksRemaining;

            float progress = GetCreateProgress(createTicksRemaining);
            bool inRange = PortalSystem.IsWithinPortalUseRange(Main.LocalPlayer, worldPos);
            //Log.Chat(inRange);
            SpawnPortalDust(worldPos, progress, dustMultiplier: inRange ? 3 : 1); // always spawn portal dust. we could gate it to only show if in range, etc.

            Texture2D texture = GetPortalAsset(team).Value;
            Rectangle source = GetPortalFrameRectangle(texture);
            Vector2 origin = new(source.Width * 0.5f, source.Height);
            Vector2 drawPos = worldPos - Main.screenPosition;
            bool hovered = PortalSystem.GetPortalHitbox(worldPos).Contains(Main.MouseWorld.ToPoint());
            Color borderColor = GetPortalColor(team);
            //float rangeAlpha = inRange ? 1f : 0.5f;
            float rangeAlpha = 1f;
            int visualHealth = (int)MathHelper.Lerp(0f, health, progress);

            DrawPortal(spriteBatch, texture, drawPos, source, origin, 1f, Color.White * (progress * rangeAlpha), borderColor, outline: inRange);
            DrawPortalHealthBar(spriteBatch, worldPos + new Vector2(0f, 8f), visualHealth, maxHealth, 1f, progress);

            if (hovered)
            {
                if (inRange)
                    DrawHoverIcon(spriteBatch, team, worldPos, borderColor, progress);

                //string healthText = $"Portal: {health}/{PortalSystem.PortalMaxHealth}";
                //float textScale = 0.9f;
                //Vector2 textSize = FontAssets.MouseText.Value.MeasureString(healthText) * textScale;
                //Vector2 textPos = worldPos + new Vector2(0f, 30f) - Main.screenPosition - new Vector2(textSize.X * 0.5f, 0f);

                //Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, healthText, textPos.X, textPos.Y, Color.White * progress, Color.Black * progress, Vector2.Zero, textScale);
            }
        }

        DrawLocalFormingPortal(spriteBatch);
    }

    private static void SpawnPortalDust(Vector2 worldPos, float progress = 1f, int dustMultiplier = 1)
    {
        progress = MathHelper.Clamp(progress, 0f, 1f);

        for (int i = 0; i < dustMultiplier; i++)
        {
            PotionOfReturnGateHelper gate = new(
                PotionOfReturnGateHelper.GateType.EntryPoint,
                worldPos,
                progress
            );

            gate.SpawnReturnPortalDust();
        }
    }

    public static void SpawnPortalMapDust(Vector2 worldPos, bool emphasized)
    {
        int dustCount = emphasized ? 3 : 1;

        for (int i = 0; i < dustCount; i++)
        {
            PotionOfReturnGateHelper gate = new(
                PotionOfReturnGateHelper.GateType.EntryPoint,
                worldPos,
                1f
            );

            gate.SpawnReturnPortalDust();
        }
    }

    public static void DrawPortalPreview(SpriteBatch sb, Player player, Vector2 position, float scale, bool outline = true, Color drawColor = default, float blackOutlineDistance = 4f, float colorOutlineDistance = 3f)
    {
        Texture2D texture = GetPortalAsset(player).Value;
        Rectangle source = GetPortalFrameRectangle(texture);
        Vector2 origin = source.Size() * 0.5f;
        Color borderColor = GetPortalColor(player);

        if (drawColor == default)
            drawColor = Color.White;

        DrawPortal(sb, texture, position, source, origin, scale, drawColor, borderColor, outline, blackOutlineDistance, colorOutlineDistance);
    }

    public static void DrawPortal(SpriteBatch sb, Texture2D texture, Vector2 position, Rectangle source, Vector2 origin, float scale, Color drawColor, Color borderColor, bool outline, float blackOutlineDistance = 4f, float colorOutlineDistance = 3f)
    {
        if (outline)
        {
            float alphaScale = 0.9f * drawColor.A / 255f;
            //DrawTextureOutline(sb, texture, position, source, origin, Color.Yellow * alphaScale, scale, blackOutlineDistance);
            DrawTextureOutline(sb, texture, position, source, origin, borderColor * alphaScale, scale, colorOutlineDistance);
        }

        sb.Draw(texture, position, source, drawColor, 0f, origin, scale, SpriteEffects.None, 0f);
    }

    private static float GetCreateProgress(int createTicksRemaining)
    {
        int total = PortalSystem.PortalCreateAnimationTicks;
        if (total <= 0)
            return 1f;

        return MathHelper.Clamp(1f - createTicksRemaining / (float)total, 0f, 1f);
    }

    private static void DrawLocalFormingPortal(SpriteBatch spriteBatch)
    {
        Player player = Main.LocalPlayer;
        if (player == null || !player.active)
            return;

        SpawnPlayer sp = player.GetModPlayer<SpawnPlayer>();
        if (sp.SelectedType != SpawnType.None)
            return;

        if (!IsMirrorPortalBuildActive(player, out float progress))
            return;

        Vector2 worldPos = player.Bottom;
        SpawnPortalDust(worldPos, progress, 1);

        Texture2D texture = GetPortalAsset(player).Value;
        Rectangle source = GetPortalFrameRectangle(texture);
        Vector2 origin = new(source.Width * 0.5f, source.Height);
        Vector2 drawPos = worldPos - Main.screenPosition;
        Color borderColor = GetPortalColor(player);

        DrawPortal(spriteBatch, texture, drawPos, source, origin, 1f, Color.White * progress, borderColor, outline: true);
        DrawPortalHealthBar(spriteBatch, worldPos + new Vector2(0f, 8f), (int)(PortalSystem.PortalMaxHealth * progress), PortalSystem.PortalMaxHealth, 1f, progress);
    }

    private static bool IsMirrorPortalBuildActive(Player player, out float progress)
    {
        progress = 0f;

        int mirrorType = ModContent.ItemType<AdventureMirror>();
        if (player.HeldItem == null || player.HeldItem.type != mirrorType)
            return false;

        SpawnPlayer sp = player.GetModPlayer<SpawnPlayer>();
        if (!sp.AdventureMirrorCountdownStartedThisUse)
            return false;

        int total = PortalSystem.PortalCreateAnimationTicks;
        if (total <= 0)
        {
            progress = 1f;
            return true;
        }

        int framesLeft = Math.Max(0, sp.AdventureMirrorTicksLeft);
        progress = MathHelper.Clamp(1f - framesLeft / (float)total, 0f, 1f);
        return true;
    }

    private static void DrawPortalHealthBar(SpriteBatch sb, Vector2 worldPos, int health, int maxHealth, float scale, float alpha)
    {
        if (maxHealth <= 0)
            return;

        float healthRatio = (float)health / maxHealth;
        if (healthRatio > 1f)
            healthRatio = 1f;

        int barPixels = (int)(36f * healthRatio);
        if (barPixels < 3)
            barPixels = 3;

        healthRatio -= 0.1f;
        float green = healthRatio > 0.5f ? 255f : 255f * healthRatio * 2f;
        float red = healthRatio > 0.5f ? 255f * (1f - healthRatio) * 2f : 255f;
        float colorScale = alpha * 0.95f;

        red = MathHelper.Clamp(red * colorScale, 0f, 255f);
        green = MathHelper.Clamp(green * colorScale, 0f, 255f);
        float alphaByte = MathHelper.Clamp(255f * colorScale, 0f, 255f);
        Color barColor = new((byte)red, (byte)green, 0, (byte)alphaByte);

        Vector2 screenOrigin = new(worldPos.X - 18f * scale - Main.screenPosition.X, worldPos.Y - Main.screenPosition.Y);
        Texture2D backTex = TextureAssets.Hb2.Value;
        Texture2D fillTex = TextureAssets.Hb1.Value;

        if (barPixels < 34)
        {
            if (barPixels < 36)
                sb.Draw(backTex, screenOrigin + new Vector2(barPixels * scale, 0f), new Rectangle(2, 0, 2, backTex.Height), barColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            if (barPixels < 34)
                sb.Draw(backTex, screenOrigin + new Vector2((barPixels + 2) * scale, 0f), new Rectangle(barPixels + 2, 0, 36 - barPixels - 2, backTex.Height), barColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            if (barPixels > 2)
                sb.Draw(fillTex, screenOrigin, new Rectangle(0, 0, barPixels - 2, fillTex.Height), barColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            sb.Draw(fillTex, screenOrigin + new Vector2((barPixels - 2) * scale, 0f), new Rectangle(32, 0, 2, fillTex.Height), barColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            return;
        }

        if (barPixels < 36)
            sb.Draw(backTex, screenOrigin + new Vector2(barPixels * scale, 0f), new Rectangle(barPixels, 0, 36 - barPixels, backTex.Height), barColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

        sb.Draw(fillTex, screenOrigin, new Rectangle(0, 0, barPixels, fillTex.Height), barColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    private static void DrawHoverIcon(SpriteBatch sb, int team, Vector2 worldPos, Color outlineColor, float alpha)
    {
        const float pulseSpeed = 6f; // Lower = slower pulse.
        const float pulseScaleVariance = 0.15f; // Higher = larger size change.

        // Draw animated portal. Keep this commented out for now.
        //Texture2D iconTexture = GetPortalAsset(player).Value;
        //Rectangle source = GetPortalFrameRectangle(iconTexture);

        Texture2D iconTexture = GetPortalMinimapAsset(team).Value;
        Rectangle source = iconTexture.Bounds;

        Vector2 origin = new(source.Width * 0.5f, source.Height * 0.5f); // center
        origin = Vector2.Zero;
        Vector2 drawPos = new Vector2(Main.mouseX, Main.mouseY) + new Vector2(12f, 14f);

        float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * pulseSpeed);
        float iconScale = 1f + pulse * pulseScaleVariance;
        Color drawColor = Color.White * alpha;

        //DrawTextureOutline(sb, iconTexture, drawPos, source, origin, Color.Black * alpha, iconScale, 2f);
        //DrawTextureOutline(sb, iconTexture, drawPos, source, origin, outlineColor * alpha, iconScale, 1f);
        sb.Draw(iconTexture, drawPos, source, drawColor, 0f, origin, iconScale, SpriteEffects.None, 0f);
    }

    private static void DrawTextureOutline(SpriteBatch sb, Texture2D texture, Vector2 position, Rectangle source, Vector2 origin, Color color, float scale, float distance)
    {
        Vector2[] offsets =
        [
            new Vector2(-distance, 0f),
            new Vector2(distance, 0f),
            new Vector2(0f, -distance),
            new Vector2(0f, distance),
            new Vector2(-distance, -distance),
            new Vector2(-distance, distance),
            new Vector2(distance, -distance),
            new Vector2(distance, distance)
        ];

        for (int i = 0; i < offsets.Length; i++)
            sb.Draw(texture, position + offsets[i], source, color, 0f, origin, scale, SpriteEffects.None, 0f);
    }
}
