using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.SpawnAndSpectate.UI;

public class WorldSpawnPanel : UIPanel
{
    public WorldSpawnPanel(float size)
    {
        Width.Set(size, 0f);
        Height.Set(size, 0f);

        BackgroundColor = new Color(63, 82, 151) * 0.8f;
        BorderColor = Color.Black;
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);

        var gm = ModContent.GetInstance<GameManager>();
        if (gm.CurrentPhase != GameManager.Phase.Playing)
            return;

        var respawnPlayer = Main.LocalPlayer.GetModPlayer<RespawnPlayer>();

        if (SpawnAndSpectateSystem.IsAliveSpawnRegionInstant)
        {
            if (SpawnAndSpectateSystem.ShouldCommitMapTeleport)
            {
                SpawnAndSpectateSystem.ToggleMapCommitWorldSpawn();
                return;
            }

            Main.LocalPlayer.Spawn(PlayerSpawnContext.SpawningIntoWorld);
            return;
        }

        if (Main.LocalPlayer.dead)
        {
            respawnPlayer.ToggleCommitWorldSpawn();
        }
    }

    public override void Draw(SpriteBatch sb)
    {
        base.Draw(sb);

        if (IsMouseHovering)
        {
            bool dead = Main.LocalPlayer.dead;
            bool ready = dead ? !SpawnAndSpectateSystem.CanRespawn : SpawnAndSpectateSystem.ShouldCommitMapTeleport;

            bool committed = dead
                ? Main.LocalPlayer.GetModPlayer<RespawnPlayer>().IsWorldSpawnCommitted
                : SpawnAndSpectateSystem.IsMapWorldSpawnCommitted;

            string text;

            if (ready)
            {
                text = committed
                    ? Language.GetTextValue("Mods.PvPAdventure.SpawnAndSpectate.CancelWorldSpawn")
                    : Language.GetTextValue("Mods.PvPAdventure.SpawnAndSpectate.SelectWorldSpawn");
            }
            else
            {
                text = Language.GetTextValue("Mods.PvPAdventure.SpawnAndSpectate.WorldSpawn");
            }

            Main.instance.MouseText(text);
        }

        var d = GetDimensions();
        var tex = TextureAssets.SpawnPoint.Value;

        Vector2 pos = new(
            d.X + d.Width * 0.5f,
            d.Y + d.Height * 0.5f
        );

        float scale = 1.6f;

        sb.Draw(
            tex,
            pos,
            sourceRectangle: null,
            color: Color.White,
            rotation: 0f,
            origin: tex.Size() * 0.5f,
            scale: scale,
            effects: SpriteEffects.None,
            layerDepth: 0f
        );
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        SpawnAndSpectateSystem.HoveringWorldSpawn = IsMouseHovering;

        bool dead = Main.LocalPlayer.dead;
        bool gated = SpawnAndSpectateSystem.IsMapTeleportGated;

        if (!dead && !gated)
        {
            BorderColor = Color.Black;
            BackgroundColor = IsMouseHovering
                ? new Color(73, 92, 161, 150)
                : new Color(63, 82, 151) * 0.8f;

            return;
        }

        bool committed = dead
            ? Main.LocalPlayer.GetModPlayer<RespawnPlayer>().IsWorldSpawnCommitted
            : SpawnAndSpectateSystem.IsMapWorldSpawnCommitted;

        if (committed)
            BackgroundColor = Color.Yellow;
        else if (IsMouseHovering)
            BackgroundColor = new Color(73, 92, 161, 150);
        else
            BackgroundColor = new Color(63, 82, 151);
    }
}
