using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Chat;
using PvPAdventure.Common.Spectator.Drawers;
using PvPAdventure.Common.Spectator.UI.Tabs.Players;
using PvPAdventure.Common.Teams;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.SpawnSelector;

/// <summary>
/// Manage all portals in the world.
/// </summary>
[Autoload(Side = ModSide.Client)]
public sealed class PortalSystem : ModSystem
{
    public static int PortalMaxHealth => NPC.downedPlantBoss ? 420 : Main.hardMode ? 69 : 27;
    public const float PortalUseRangeTiles = 8f;
    public const float PortalUseRangeWorld = PortalUseRangeTiles * 16f;
    public static int PortalCreateAnimationTicks => ModContent.GetInstance<Core.Config.ServerConfig>().AdventureMirrorRecallSeconds * 60;

    public static bool HasPortal(Player player) => SpawnPlayer.HasPortal(player);

    public static bool TryGetPortalWorldPos(Player player, out Vector2 worldPos) =>
        SpawnPlayer.TryGetPortalWorldPos(player, out worldPos);

    public static void CreatePortalAtPosition(Player player, Vector2 position)
    {
        if (player == null || !player.active)
            return;

        player.GetModPlayer<SpawnPlayer>().SetPortal(position);

        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            string biome = PlayerStats.GetBiomeText(player);
            int distance = (int)(Vector2.Distance(player.Center, position) / 16f);

            TeleportChat.SendSystemTeamMessage(
                player,
                GetPortalMessage(player, biome, distance),
                Main.OurFavoriteColor,
                GetOwnPortalMessage(player, biome, distance));
        }
    }

    internal static string GetOwnPortalMessage(Player player, string biome, int distance)
    {
        return $"{player.name} opened a portal in {biome} ({distance} tiles away)";
    }

    internal static string GetPortalMessage(Player player, string biome, int distance)
    {
        return $"{player.name} opened a portal in {biome} ({distance} tiles away)";
    }

    public static void ClearPortal(Player player)
    {
        if (player == null || !player.active)
            return;

        player.GetModPlayer<SpawnPlayer>().ClearPortal();
    }

    public static bool TryDamagePortal(Player attacker, int ownerIndex, int damage, string source)
    {
        if (ownerIndex < 0 || ownerIndex >= Main.maxPlayers || Main.player[ownerIndex] is not { active: true } owner)
            return false;

        if (attacker?.active == true && attacker.whoAmI != ownerIndex && attacker.team != 0 && attacker.team == owner.team)
            return false;

        return owner.GetModPlayer<SpawnPlayer>().DamagePortal(attacker, damage, source);
    }

    public static Rectangle GetPortalHitbox(Vector2 worldPos) =>
        new((int)worldPos.X - 24, (int)worldPos.Y - 72, 48, 72);

    public static bool IsWithinPortalUseRange(Player player, Vector2 worldPos)
    {
        return player?.active == true &&
               Vector2.DistanceSquared(player.Center, worldPos) <= PortalUseRangeWorld * PortalUseRangeWorld;
    }

    public static void PlayPortalFx(Vector2 worldPos, bool killed, int damage = 0)
    {
        if (Main.dedServ)
            return;

        if (damage > 0)
        {
            CombatText.NewText(GetPortalHitbox(worldPos), CombatText.DamagedHostile, damage);
        }

        if (!killed)
        {
            SoundEngine.PlaySound(SoundID.NPCHit4, worldPos);
            return;
        }

        SoundEngine.PlaySound(SoundID.NPCDeath6, worldPos);

        for (int i = 0; i < 28; i++)
        {
            Vector2 velocity = Main.rand.NextVector2Circular(3.5f, 3.5f);
            Dust.NewDustPerfect(worldPos + Main.rand.NextVector2Circular(24f, 36f), DustID.MagicMirror, velocity, 120, Color.White, Main.rand.NextFloat(1.1f, 1.8f));
        }
    }

    #region Clear hooks on load
    public override void OnWorldLoad()
    {
        ClearAllPortals();
    }

    public override void OnWorldUnload()
    {
        ClearAllPortals();
    }

    private static void ClearAllPortals()
    {
        for (int i = 0; i < Main.maxPlayers; i++)
            if (Main.player[i] is { active: true } player)
                player.GetModPlayer<SpawnPlayer>().ClearPortal(sync: false);
    }
    #endregion

    #region Drawing
    public override void PostDrawTiles()
    {
        if (Main.dedServ)
            return;

        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
        PortalDrawer.DrawAllPortals(Main.spriteBatch);
        Main.spriteBatch.End();
    }

    public override void PostUpdateInput()
    {
        if (Main.dedServ)
            return;

        if (!Main.mouseRight || !Main.mouseRightRelease)
            return;

        if (Main.LocalPlayer == null || !Main.LocalPlayer.active)
            return;

        if (Main.LocalPlayer.mouseInterface || Main.drawingPlayerChat || Main.editSign || Main.editChest || Main.blockInput)
            return;

        if (!TryGetHoveredPortal(Main.MouseWorld, out _))
            return;

        OpenFullscreenMap();
        Main.mouseRightRelease = false;
    }

    private static bool TryGetHoveredPortal(Vector2 mouseWorld, out int ownerIndex)
    {
        ownerIndex = -1;

        Point mousePoint = mouseWorld.ToPoint();

        for (int i = 0; i < Main.maxPlayers; i++)
            if (Main.player[i] is { active: true } player &&
                SpawnPlayer.TryGetPortalWorldPos(player, out Vector2 worldPos) &&
                GetPortalHitbox(worldPos).Contains(mousePoint))
            {
                ownerIndex = i;
                return true;
            }

        return false;
    }

    private static void OpenFullscreenMap()
    {
        Main.playerInventory = false;
        Main.LocalPlayer.talkNPC = -1;
        Main.npcChatCornerItem = 0;
        Main.mapFullscreen = true;
        Main.resetMapFull = true;

        SoundEngine.PlaySound(SoundID.MenuOpen);
    }

    #endregion
}
