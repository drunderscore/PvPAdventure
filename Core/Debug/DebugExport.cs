using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.IO;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Debug;

#if DEBUG
public class ExportHbCommand : ModCommand
{
    public override CommandType Type => CommandType.Chat;
    public override string Command => "exporthb";
    public override string Usage => "/exporthb";
    public override string Description => "Export files.";

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (Main.dedServ)
        {
            caller.Reply("This command can only be used on a client (not on a dedicated server).", Color.Red);
            return;
        }

        string rootDir = Path.Combine(Main.SavePath, "TextureExports", "TextureAssets");
        Directory.CreateDirectory(rootDir);

        int exported = 0;
        int failed = 0;

        Type taType = typeof(TextureAssets);
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        foreach (FieldInfo field in taType.GetFields(flags))
        {
            try
            {
                Type fieldType = field.FieldType;

                if (fieldType == typeof(Asset<Texture2D>))
                {
                    var asset = field.GetValue(null) as Asset<Texture2D>;
                    ExportAssetTexture(asset, rootDir, field.Name, ref exported, ref failed);
                }
                else if (fieldType.IsArray && fieldType.GetElementType() == typeof(Asset<Texture2D>))
                {
                    var array = field.GetValue(null) as Asset<Texture2D>[];
                    if (array == null)
                        continue;

                    for (int i = 0; i < array.Length; i++)
                    {
                        var asset = array[i];
                        string name = $"{field.Name}_{i}";
                        ExportAssetTexture(asset, rootDir, name, ref exported, ref failed);
                    }
                }
            }
            catch (Exception ex)
            {
                failed++;
                ModContent.GetInstance<PvPAdventure>().Logger.Warn($"Failed exporting field {field.Name}: {ex}");
            }
        }

        caller.Reply($"Export complete. Exported {exported} textures, {failed} failures. Folder: {rootDir}", Color.LimeGreen);
    }

    private static void ExportAssetTexture(Asset<Texture2D> asset, string rootDir, string name, ref int exported, ref int failed)
    {
        try
        {
            if (asset == null)
                return;

            Texture2D tex = asset.Value;
            if (tex == null)
                return;

            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            string path = Path.Combine(rootDir, name + ".png");

            using (FileStream stream = File.Create(path))
            {
                tex.SaveAsPng(stream, tex.Width, tex.Height);
            }

            exported++;
        }
        catch
        {
            failed++;
        }
    }
}
#endif
