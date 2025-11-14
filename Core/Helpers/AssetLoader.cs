using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Helpers;

/// <summary>
/// Static class to hold all assets used in the mod.
/// </summary>
public static class Ass
{
    // Add assets here
    public static Asset<Texture2D> Question_Mark;

    // This bool automatically initializes all specified assets
    public static bool Initialized { get; set; }

    static Ass()
    {
        foreach (FieldInfo field in typeof(Ass).GetFields())
        {
            if (field.FieldType == typeof(Asset<Texture2D>))
            {
                var asset = ModContent.Request<Texture2D>($"PvPAdventure/Assets/SpawnSelector/{field.Name}", AssetRequestMode.AsyncLoad);
                field.SetValue(null, asset);
            }
        }
    }
}

/// <summary>
/// System that automatically initializes assets
/// </summary>
public class LoadAssets : ModSystem
{
    public override void Load()
    {
        _ = Ass.Initialized;
    }
}