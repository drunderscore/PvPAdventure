using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Teams;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
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
    public const int PortalMaxHealth = 27;

    public static bool HasPortal(Player player)
    {
        return SpawnPlayer.HasPortal(player);
    }

    public static bool TryGetPortalWorldPos(Player player, out Vector2 worldPos)
    {
        return SpawnPlayer.TryGetPortalWorldPos(player, out worldPos);
    }

    public static void CreatePortalAtPosition(Player player, Vector2 position)
    {
        if (player == null || !player.active)
            return;

        player.GetModPlayer<SpawnPlayer>().SetPortal(position);

        if (Main.netMode != NetmodeID.MultiplayerClient)
            TeamChatManager.SendSystemTeamMessage(player, $"{player.name} has created a portal", Color.Yellow);
    }

    public static void ClearPortal(Player player)
    {
        if (player == null || !player.active)
            return;

        player.GetModPlayer<SpawnPlayer>().ClearPortal();
    }

    public static bool TryDamagePortal(Player attacker, int ownerIndex, int damage, string source)
    {
        if (ownerIndex < 0 || ownerIndex >= Main.maxPlayers)
            return false;

        Player owner = Main.player[ownerIndex];
        if (owner == null || !owner.active)
            return false;

        if (attacker != null &&
            attacker.active &&
            attacker.whoAmI != ownerIndex &&
            attacker.team != 0 &&
            attacker.team == owner.team)
        {
            return false;
        }

        return owner.GetModPlayer<SpawnPlayer>().DamagePortal(attacker, damage, source);
    }

    public static Rectangle GetPortalHitbox(Vector2 worldPos)
    {
        return new Rectangle((int)worldPos.X - 24, (int)worldPos.Y - 72, 48, 72);
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
        {
            Player player = Main.player[i];
            if (player == null || !player.active)
                continue;

            player.GetModPlayer<SpawnPlayer>().ClearPortal(sync: false);
        }
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

    #endregion
}
