using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace PvPAdventure.Common.MatchHistory.Achievements;

internal sealed class UIAchievementsPanel : UIPanel
{
    public UIAchievementsPanel()
    {
        //SetPadding(0f);
        BorderColor = Color.Black;
        BackgroundColor = new Color(33, 43, 79) * 0.8f;

        Refresh();
    }

    public void Refresh()
    {
        RemoveAllChildren();

        Append(new UITextPanel<string>("Achievements", 0.9f, true)
        {
            Top = new StyleDimension(-48f, 0f),
            HAlign = 0.5f,
            BackgroundColor = UICommon.DefaultUIBlue
        });

        UIElement body = new()
        {
            Top = new StyleDimension(6f, 0f),
            Width = StyleDimension.Fill,
            Height = StyleDimension.Fill
        };
        body.SetPadding(10f);
        Append(body);

        AchievementSaveData data = AchievementStorage.Data;
        if (data == null || data.Achievements == null)
        {
            body.Append(new UIText("No achievements data.", 0.9f)
            {
                HAlign = 0.5f,
                VAlign = 0.5f,
                TextColor = Color.Gray
            });
            return;
        }

        AddRow(body, data, AchievementId.TotalKills100, 0f);
        AddRow(body, data, AchievementId.TotalTeamPoints100, 20f);
    }

    private static void AddRow(UIElement parent, AchievementSaveData data, AchievementId id, float topPx)
    {
        if (!data.Achievements.TryGetValue(id, out AchievementProgress p) || p == null)
            return;

        bool completed = p.CompletedUtc != null;
        Color c = completed ? new Color(120, 220, 120) : new Color(220, 220, 220);

        string text = $"{p.Name}: {p.Current}/{p.Target}";
        if (completed)
            text += " (done)";

        parent.Append(new UIText(text, 0.85f)
        {
            Top = new StyleDimension(topPx, 0f),
            Width = StyleDimension.Fill,
            Height = new StyleDimension(24f, 0f),
            TextOriginX = 0f,
            TextOriginY = 0.5f,
            TextColor = c,
            IsWrapped = false
        });
    }
}
