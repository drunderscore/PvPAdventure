using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Helpers;

public static class Ass
{
    // Matchmaking assets
    public static Asset<Texture2D> Button;
    public static Asset<Texture2D> Button_Border;
    public static Asset<Texture2D> Button_Small;
    public static Asset<Texture2D> Button_Small_Border;
    public static Asset<Texture2D> Map_Icon_Guide;

    public static bool Initialized { get; set; }

    static Ass()
    {
        // Load Matchmaking files
        foreach (FieldInfo f in typeof(Ass).GetFields())
        {
            if (f.FieldType == typeof(Asset<Texture2D>))
            {
                var asset = ModContent.Request<Texture2D>(
                    $"PvPAdventure/Assets/Matchmaking/{f.Name}",
                    AssetRequestMode.AsyncLoad);
                f.SetValue(null, asset);
            }
        }
    }
}

public class LoadAssets : ModSystem
{
    public override void Load() => _ = Ass.Initialized;
}