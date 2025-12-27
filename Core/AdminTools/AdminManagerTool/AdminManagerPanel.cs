using DragonLens.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common;
using PvPAdventure.Core.AdminTools.UI;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.AdminTools.AdminManagerTool;

internal class AdminManagerPanel : DraggablePanel
{
    private readonly List<int> playerIndices = [];

    private UIText summaryTextElement;

    private int lastPlayerSetHash;
    private int lastAdminsHash;

    public AdminManagerPanel()
        : base(title: Language.GetTextValue("Mods.PvPAdventure.Tools.DLAdminManagerTool.DisplayName"))
    {
        Width.Set(350, 0);
        Height.Set(460, 0);
        VAlign = 0.7f;
        HAlign = 0.9f;

        Rebuild();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        RecomputePlayerIndices();

        int playerHash = ComputePlayerSetHash(playerIndices);
        int adminsHash = ComputeAdminsHash();

        if (playerHash != lastPlayerSetHash || adminsHash != lastAdminsHash)
        {
            lastPlayerSetHash = playerHash;
            lastAdminsHash = adminsHash;
            Rebuild();
        }
        else
        {
            // If nothing structural changed, still keep summary correct.
            UpdateSummaryText();
        }
    }

    private void RecomputePlayerIndices()
    {
        playerIndices.Clear();

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player p = Main.player[i];
            if (!p.active)
            {
                continue;
            }

            playerIndices.Add(i);
        }
    }

    private static int ComputePlayerSetHash(List<int> indices)
    {
        unchecked
        {
            int hash = 17;
            for (int i = 0; i < indices.Count; i++)
            {
                hash = (hash * 31) + indices[i];
            }

            return hash;
        }
    }
    private static int ComputeAdminsHash()
    {
        unchecked
        {
            int hash = 17;

            if (PermissionHandler.admins == null)
            {
                return hash;
            }

            foreach (var admin in PermissionHandler.admins)
            {
                if (admin == null)
                {
                    continue;
                }

                hash = (hash * 31) + admin.GetHashCode();
            }

            return hash;
        }
    }

    private void Rebuild()
    {
        ContentPanel.RemoveAllChildren();

        BuildSummaryRow();

        const int rowH = 30;
        const int rowTopStart = 24 + 6;

        for (int i = 0; i < playerIndices.Count; i++)
        {
            int playerIndex = playerIndices[i];
            Player player = Main.player[playerIndex];

            UIPanel playerRow = new UIPanel
            {
                Top = { Pixels = rowTopStart + (i * rowH) },
                Width = { Percent = 1f },
                Height = { Pixels = rowH },
                BackgroundColor = Color.White * 0.95f,
                BorderColor = Color.Black
            };
            playerRow.SetPadding(0f);

            string playerName = player.name;

            if (player == Main.LocalPlayer)
            {
                playerName += " (you)";
            }

            if (IsAdmin(player))
            {
                playerName += " [admin]";
            }

            playerRow.Append(new UIText(playerName, textScale: 1.0f)
            {
                Left = { Pixels = 10f },
                VAlign = 0.5f,
                TextColor = Color.White
            });

            AdminToggleIconElement adminToggleIcon = new(
                player: player,
                getIsOn: () => IsAdmin(player),
                setIsOn: isOn => SetAdmin(player, isOn),
                onToggled: isOn =>
                {
                    if (player.whoAmI == Main.LocalPlayer.whoAmI)
                    {
                        return;
                    }

                    UpdateSummaryText();

#if DEBUG
                    Main.NewText(player.name + " admin: " + (isOn ? "yes" : "no"));
#endif
                })
            {
                HAlign = 1f,
                VAlign = 0.5f
            };

            adminToggleIcon.Left.Set(-10f - AdminToggleIconElement.IconSizePixels, 0f);

            playerRow.Append(adminToggleIcon);
            ContentPanel.Append(playerRow);
        }

        UpdateSummaryText();
        ContentPanel.Recalculate();
    }

    private void BuildSummaryRow()
    {
        UIPanel summaryRow = new()
        {
            Width = { Percent = 1f },
            Height = { Pixels = 24f },
            BackgroundColor = new Color(10, 10, 10) * 0.9f,
            BorderColor = Color.Black
        };
        summaryRow.SetPadding(4f);

        summaryTextElement = new UIText("Admins: (none)", textScale: 0.8f)
        {
            VAlign = 0.5f,
            Left = { Pixels = 6f }
        };

        summaryRow.Append(summaryTextElement);
        ContentPanel.Append(summaryRow);
    }

    private void UpdateSummaryText()
    {
        if (summaryTextElement == null)
        {
            return;
        }

        List<string> adminNames = [];

        for (int i = 0; i < playerIndices.Count; i++)
        {
            int idx = playerIndices[i];
            Player p = Main.player[idx];

            if (p == null || !p.active)
            {
                continue;
            }

            if (!IsAdmin(p))
            {
                continue;
            }

            string name = p.name;

            if (p == Main.LocalPlayer)
            {
                name += " (you)";
            }

            adminNames.Add(name);
        }

        if (adminNames.Count == 0)
        {
            summaryTextElement.SetText("Admins: (none)");
            return;
        }

        summaryTextElement.SetText("Admins: " + string.Join(", ", adminNames));
    }

    private static bool IsAdmin(Player player)
    {
        // DragonLens canonical check.
        return PermissionHandler.LooksLikeAdmin(player);
    }

    private static void SetAdmin(Player player, bool isAdmin)
    {
        if (player.whoAmI == Main.LocalPlayer.whoAmI)
        {
            Main.NewText(Language.GetTextValue("Mods.PvPAdventure.Tools.DLAdminManagerTool.CannotRemoveYourOwnAdminPermissions"), Color.Red);
            return;
        }

        if (isAdmin)
        {
            PermissionHandler.AddAdmin(player);
        }
        else
        {
            PermissionHandler.RemoveAdmin(player);
        }

        // DragonLens networking: only broadcast on dedicated server.
        if (Main.netMode == NetmodeID.Server)
        {
            PermissionHandler.SendVisualAdmins();
        }
    }

    protected override void OnClosePanelLeftClick()
    {
        ModContent.GetInstance<AdminManagerSystem>().ToggleActive();
    }

    protected override void OnRefreshPanelLeftClick()
    {
        // Optional: sorting, etc.
    }

    public sealed class AdminToggleIconElement : UIElement
    {
        internal const float IconSizePixels = 20f;

        private readonly Player player;
        private readonly Func<bool> getIsOn;
        private readonly Action<bool> setIsOn;
        private readonly Action<bool> onToggled;

        public AdminToggleIconElement(
            Player player,
            Func<bool> getIsOn,
            Action<bool> setIsOn,
            Action<bool> onToggled)
        {
            this.player = player;
            this.getIsOn = getIsOn;
            this.setIsOn = setIsOn;
            this.onToggled = onToggled;

            Width.Set(IconSizePixels, 0f);
            Height.Set(IconSizePixels, 0f);
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);

            bool newValue = !getIsOn();
            setIsOn(newValue);
            onToggled?.Invoke(newValue);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            Texture2D tex =
                getIsOn()
                    ? (IsMouseHovering ? Ass.Icon_On_Hover.Value : Ass.Icon_On.Value)
                    : (IsMouseHovering ? Ass.Icon_Off_Hover.Value : Ass.Icon_Off.Value);

            if (tex == null)
            {
                return;
            }

            CalculatedStyle dims = GetDimensions();

            // Draw centered with 2x scale
            spriteBatch.Draw(
                tex,
                position: new(dims.X + (dims.Width * 0.5f), dims.Y + (dims.Height * 0.5f)),
                sourceRectangle: null,
                color: Color.White,
                rotation: 0f,
                origin: new(tex.Width * 0.5f, tex.Height * 0.5f),
                scale: 1.4f,
                effects: SpriteEffects.None,
                layerDepth: 0f);
        }
    }
}
