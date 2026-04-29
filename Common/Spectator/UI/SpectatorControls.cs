using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Common.Spectator.SpectatorMode;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI;

internal sealed class SpectatorControls : UIElement
{
    private const int Slot = 52;
    private const int TextHeight = 26;
    private const int BottomPadding = 18;

    private int locked = -1;
    private int hovered = -1;
    private string shownTargets = "";
    private UIText status;

    private int GetSlot() => 52;
    private int GetTextHeight() => 26;
    private int GetBottomPadding() => 40;

    public SpectatorControls()
    {
        Height.Set(GetSlot() + GetTextHeight(), 0f);
        HAlign = 0.5f;
        VAlign = 1f;
        Top.Set(-GetBottomPadding(), 0f);
        Rebuild();
    }

    public void Rebuild()
    {
        RemoveAllChildren();

        List<int> targets = SpectatorTargetSystem.GetTargets(Main.myPlayer);
        shownTargets = string.Join(",", targets);

        if (!targets.Contains(locked))
            locked = -1;

        if (!targets.Contains(hovered))
            hovered = -1;

        if (targets.Count == 0)
        {
            Width.Set(Slot * 4f, 0f);
            locked = hovered = -1;
            SpectatorTargetSystem.ClearTarget();

            status = new UIText(Status()) { HAlign = 0.5f, VAlign = 0.5f };
            Append(status);
            Recalculate();
            return;
        }

        int slotSize = GetSlot();
        Width.Set(targets.Count * slotSize, 0f);

        for (int i = 0; i < targets.Count; i++)
        {
            SlotElement slot = new(targets[i], i);
            slot.Width.Set(slotSize, 0f);
            slot.Height.Set(slotSize, 0f);
            slot.Left.Set(i * slotSize, 0f);
            Append(slot);
        }

        status = new UIText("") { HAlign = 0.5f, Top = StyleDimension.FromPixels(slotSize + 6f) };

        Append(status);
        UpdateStatus();
        Recalculate();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

#if DEBUG
        if (Pressed(Keys.NumPad1))
        {
            AddDebugPlayer();
            Rebuild();
        }

        if (Pressed(Keys.NumPad2))
        {
            RemoveDebugPlayer();
            Rebuild();
        }
#endif

        List<int> targets = SpectatorTargetSystem.GetTargets(Main.myPlayer);

        if (string.Join(",", targets) != shownTargets)
            Rebuild();

        if (Pressed(Keys.Left))
            CycleTarget(-1);

        if (Pressed(Keys.Right))
            CycleTarget(1);

        int nextHover = GetHoveredSlot();

        if (nextHover != hovered)
        {
            if (hovered >= 0)
                EndHover();

            if (nextHover >= 0)
                BeginHover(nextHover);
        }

        UpdateStatus();

        if (IsMouseHovering)
            Main.LocalPlayer.mouseInterface = true;
    }

    public void UpdateTarget()
    {
        if (hovered >= 0)
            return;

        Player target = SpectatorTargetSystem.GetPlayerTarget();
        locked = target?.active == true ? target.whoAmI : -1;
        Rebuild();
    }

    private int GetHoveredSlot()
    {
        if (!ContainsPoint(Main.MouseScreen))
            return -1;

        for (int i = 0; i < Elements.Count; i++)
            if (Elements[i] is SlotElement slot && slot.ContainsPoint(Main.MouseScreen))
                return slot.PlayerIndex;

        return -1;
    }

    private void BeginHover(int playerIndex)
    {
        if (!CanUse(playerIndex))
            return;

        hovered = playerIndex;
        SpectatorTargetSystem.SetPlayerTarget(playerIndex);
    }

    private void EndHover()
    {
        hovered = -1;

        if (CanUse(locked))
            SpectatorTargetSystem.SetPlayerTarget(locked);
        else
        {
            locked = -1;
            SpectatorTargetSystem.ClearTarget();
        }
    }

    private void ToggleLock(int playerIndex)
    {
        if (!CanUse(playerIndex))
            return;

        if (locked == playerIndex)
        {
            locked = -1;
            hovered = -1;
            SpectatorTargetSystem.ClearTarget();
            Rebuild();
            return;
        }

        locked = playerIndex;
        SpectatorTargetSystem.SetPlayerTarget(playerIndex);
        Rebuild();
    }

    private string Status()
    {
        if (hovered >= 0 && Main.player[hovered]?.active == true)
            return $"Spectating: {Main.player[hovered].name}";

        if (locked >= 0 && Main.player[locked]?.active == true)
            return $"Spectating: {Main.player[locked].name}";

        return "Hover to preview, click to spectate";
    }

    private void UpdateStatus()
    {
        status?.SetText(Status());
    }

    private static bool CanUse(int playerIndex)
    {
        return playerIndex >= 0 && SpectatorTargetSystem.GetTargets(Main.myPlayer).Contains(playerIndex);
    }

    #region Custom UI Elements
    private sealed class SlotElement : UIElement
    {
        private readonly int playerIndex;
        private readonly Bg bg = new();

        public int PlayerIndex => playerIndex;

        public SlotElement(int playerIndex, int order)
        {
            this.playerIndex = playerIndex;

            Width.Set(64, 0f);
            Height.Set(64, 0f);
            Left.Set(order * Slot, 0f);

            Append(bg);
            Append(new Head(playerIndex));
            OnLeftClick += Click;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Parent is not SpectatorControls controls)
                return;

            Player player = Main.player[playerIndex];
            bool selected = controls.locked == playerIndex || controls.hovered == playerIndex;

            bg.Set(selected ? TextureAssets.InventoryBack15 : TextureAssets.InventoryBack7, selected ? 1f : 0.75f);

            if (IsMouseHovering && player?.active == true)
                Main.instance.MouseText(controls.locked == playerIndex ? $"{player.name} (currently spectating)" : $"Spectate {player.name}");
        }

        private void Click(UIMouseEvent evt, UIElement element)
        {
            if (Parent is SpectatorControls controls)
                controls.ToggleLock(playerIndex);
        }

        private sealed class Bg : UIElement
        {
            private Asset<Texture2D> texture = TextureAssets.InventoryBack7;
            private float scale = 0.75f;

            public Bg()
            {
                Width.Set(0f, 1f);
                Height.Set(0f, 1f);
                IgnoresMouseInteraction = true;
            }

            public void Set(Asset<Texture2D> texture, float scale)
            {
                this.texture = texture;
                this.scale = scale;
            }

            protected override void DrawSelf(SpriteBatch sb)
            {
                if (Parent is not SlotElement slot || slot.Parent is not SpectatorControls controls)
                    return;

                bool selected = controls.locked == slot.playerIndex || controls.hovered == slot.playerIndex;
                Asset<Texture2D> texture = selected ? TextureAssets.InventoryBack15 : TextureAssets.InventoryBack7;
                Texture2D value = texture.Value;

                float size = selected ? 64 : 30f;
                float scale = size / value.Width;
                Color color = selected ? Color.Yellow : Color.White;

                sb.Draw(value, GetDimensions().Center(), null, color, 0f, value.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }
        }

        private sealed class Head : UIElement
        {
            private readonly int playerIndex;

            public Head(int playerIndex)
            {
                this.playerIndex = playerIndex;
                Width.Set(40f, 0f);
                Height.Set(40f, 0f);
                HAlign = 0.5f;
                VAlign = 0.5f;
                IgnoresMouseInteraction = true;
            }

            protected override void DrawSelf(SpriteBatch sb)
            {
                Player player = Main.player[playerIndex];

                if (player?.active != true)
                    return;

                float scale = SpectatorTargetSystem.GetPlayerTarget()?.whoAmI == playerIndex ? 1f : 0.75f;
                Vector2 position = GetDimensions().Center() + new Vector2(-3f, -2f);

                if (player.ghost || SpectatorModeSystem.IsInSpectateMode(player))
                {
                    Texture2D texture = player.direction == -1 ? Ass.GhostLeft.Value : Ass.Ghost.Value;
                    sb.Draw(texture, position, null, Color.White, 0f, texture.Size() * 0.5f, scale * 1.15f, SpriteEffects.None, 0f);
                    return;
                }

                Main.MapPlayerRenderer.DrawPlayerHead(Main.Camera, player, position, scale, scale, Color.White);
            }
        }
    }

    #endregion

    #region Cycle targets
    private void CycleTarget(int direction)
    {
        List<int> targets = SpectatorTargetSystem.GetTargets(Main.myPlayer);

        if (targets.Count == 0)
            return;

        hovered = -1;

        if (!targets.Contains(locked))
        {
            locked = direction < 0 ? targets[^1] : targets[0];
            SpectatorTargetSystem.SetPlayerTarget(locked);
            Rebuild();
            return;
        }

        int index = targets.IndexOf(locked);
        index += direction;

        if (index < 0)
            index = targets.Count - 1;

        if (index >= targets.Count)
            index = 0;

        locked = targets[index];
        SpectatorTargetSystem.SetPlayerTarget(locked);
        Rebuild();
    }
    #endregion

    private static bool Pressed(Keys key) => Main.keyState.IsKeyDown(key) && !Main.oldKeyState.IsKeyDown(key);

    #region Debug players used for UI testing
#if DEBUG
    private static readonly List<int> debugSlots = [];
    private static void AddDebugPlayer()
    {
        int slot = -1;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            if (i != Main.myPlayer && Main.player[i]?.active != true)
            {
                slot = i;
                break;
            }
        }

        if (slot < 0)
        {
            Log.Chat("No free player slots for debug spectate player.");
            return;
        }

        Player player = Main.LocalPlayer.SerializedClone();
        int number = Main.rand.NextBool() ? Main.rand.Next(1, 11) : Main.rand.Next(99999991, 100000000);

        player.whoAmI = slot;
        player.name = "Player"+number.ToString();
        player.active = true;
        player.dead = false;
        player.ghost = false;
        player.team = Main.LocalPlayer.team;
        player.statLife = Math.Max(1, player.statLife);
        player.Center = new Vector2(Main.rand.Next(100, Math.Max(101, Main.maxTilesX - 100)), Main.rand.Next(100, Math.Max(101, Main.maxTilesY - 100))) * 16f;

        Main.player[slot] = player;
        SpectatorModeSystem.Modes[slot] = PlayerMode.Player;
        debugSlots.Add(slot);

        Log.Chat($"Added debug spectate player {player.name} at slot {slot}.");
    }

    private void RemoveDebugPlayer()
    {
        if (debugSlots.Count == 0)
            return;

        int slot = debugSlots[^1];
        debugSlots.RemoveAt(debugSlots.Count - 1);

        if (hovered == slot)
            hovered = -1;

        if (locked == slot)
            locked = -1;

        Main.player[slot] = new Player { whoAmI = slot };
        SpectatorModeSystem.Modes.Remove(slot);

        if (CanUse(locked))
            SpectatorTargetSystem.SetPlayerTarget(locked);
        else
            SpectatorTargetSystem.ClearTarget();

        Log.Chat($"Removed debug spectate player from slot {slot}.");
    }
#endif
    #endregion

}