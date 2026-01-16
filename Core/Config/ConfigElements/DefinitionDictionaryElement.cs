using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Debug;
using ReLogic.Content;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace PvPAdventure.Core.Config.ConfigElements;

/// <summary>
/// Improves the DictionaryElement used in ModConfig with better display of text:
/// ItemDefinition,NPCDefinition and ProjectileDefinition now have a text added after their index in the dictionary list.
/// If a config item is defined as Dictionary<SomeDefinition,SomeDataType>, 
/// then their text will now display <Index>: Icon and DisplayName instead of just the index of the element <Index>.
/// </summary>

internal class DefinitionDictionaryElement : DictionaryElement
{
    protected override void SetupList()
    {
        base.DataList.Clear();
        int top = 0;
        dataWrapperList = [];
        Type type = typeof(DictionaryElementWrapper<,>).MakeGenericType(keyType, valueType);
        if (base.Data == null)
        {
            return;
        }

        ICollection keys = ((IDictionary)base.Data).Keys;
        ICollection values = ((IDictionary)base.Data).Values;
        IEnumerator enumerator = keys.GetEnumerator();
        IEnumerator enumerator2 = values.GetEnumerator();
        int num = 0;
        while (enumerator.MoveNext())
        {
            enumerator2.MoveNext();
            object keyObj = enumerator.Current;
            IDictionaryElementWrapper item = (IDictionaryElementWrapper)Activator.CreateInstance(type, keyObj, enumerator2.Current, (IDictionary)base.Data);
            dataWrapperList.Add(item);
            _ = base.MemberInfo.Type.GetGenericArguments()[0];
            PropertyFieldWrapper memberInfo = ConfigManager.GetFieldsAndProperties(this).ToList()[0];
            Tuple<UIElement, UIElement> tuple = UIModConfig.WrapIt(base.DataList, ref top, memberInfo, this, 0, dataWrapperList, type, num);
            tuple.Item2.Left.Pixels += 24f;
            tuple.Item2.Width.Pixels -= 24f;

            // Collapse!!! Both these probably are overkill but it works, so, oh well.
            CollapseEntry(tuple.Item2);
            expanded = false;

            if (TryCreateKeyIcon(keyObj, out UIElement icon))
            {
                icon.Left.Set(24f, 0f);
                icon.VAlign = 0.5f;
                tuple.Item1.Append(icon);

                tuple.Item2.Left.Pixels += 20f;
                tuple.Item2.Width.Pixels -= 20f;
            }

            // --- Add to the text, displaying 1: <extra text> instead of just the raw key ---
            int displayIndex = num + 1;
            object capturedKey = keyObj;

            if (tuple.Item2 is ConfigElement configElement)
            {
                configElement.TextDisplayFunction = () => $"{displayIndex}: {FormatKeyWithItemTag(capturedKey)}";
            }

            UIModConfigHoverImage uIModConfigHoverImage = new(base.DeleteTexture, Language.GetTextValue("tModLoader.ModConfigRemove"))
            {
                VAlign = 0.5f
            };
            object o = keyObj;
            uIModConfigHoverImage.OnLeftClick += delegate
            {
                ((IDictionary)base.Data).Remove(o);
                SetupList();
                Interface.modConfig.SetPendingChanges();
            };
            tuple.Item1.Append(uIModConfigHoverImage);
            num++;
        }
    }

    public override void OnBind()
    {
        base.OnBind();
        expanded = false;
        pendingChanges = true;
        //Log.Debug("onbind() expanded=false");
    }

    private static void CollapseEntry(UIElement element)
    {
        var t = element.GetType();
        var expandedField = t.GetField("expanded", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (expandedField != null && expandedField.FieldType == typeof(bool))
        {
            Log.Debug("UIElement expanded=false");
            expandedField.SetValue(element, false);
        }

        var pendingField = t.GetField("pendingChanges", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (pendingField != null && pendingField.FieldType == typeof(bool))
        {
            Log.Debug("UIElement pendingChanges=true");
            pendingField.SetValue(element, true);
        }
    }

    private static string FormatKeyWithItemTag(object keyObj)
    {
        if (keyObj == null)
            return "null";

        // ItemDefinition
        if (keyObj is ItemDefinition itemDef)
        {
            if (itemDef.Type > 0)
                return $"[i:{itemDef.Type}] {Lang.GetItemNameValue(itemDef.Type)}";
        }

        // NPCDefinition
        if (keyObj is NPCDefinition npcDef)
        {
            if (npcDef.Type > 0)
                return $"{Lang.GetNPCNameValue(npcDef.Type)}";
        }

        // ProjectileDefinition
        if (keyObj is ProjectileDefinition projectileDef)
        {
            if (projectileDef.Type > 0)
                return $"{Lang.GetProjectileName(projectileDef.Type)}";
        }

        // Fallback to return the default keyObj.
        return keyObj.ToString();
    }

    // Create an icon of the first frame of a NPC or Projectile texture.
    private static bool TryCreateKeyIcon(object keyObj, out UIElement icon)
    {
        icon = null;

        if (keyObj is NPCDefinition npcDef && npcDef.Type > 0)
        {
            var asset = TextureAssets.Npc[npcDef.Type];
            int frames = Main.npcFrameCount[npcDef.Type];
            if (frames < 1)
                frames = 1;

            int frameHeight = asset.Value.Height / frames;
            var src = new Rectangle(0, 0, asset.Value.Width, frameHeight);

            icon = new UIDefinitionIcon(asset, src);
            return true;
        }

        if (keyObj is ProjectileDefinition projDef && projDef.Type > 0)
        {
            var asset = TextureAssets.Projectile[projDef.Type];
            int frames = Main.projFrames[projDef.Type];
            if (frames < 1)
                frames = 1;

            int frameHeight = asset.Value.Height / frames;
            var src = new Rectangle(0, 0, asset.Value.Width, frameHeight);

            icon = new UIDefinitionIcon(asset, src);
            return true;
        }

        return false;
    }
}

/// <summary>
/// Icons of NPC's and projectiles.
/// Used in <see cref="DefinitionDictionaryElement"/> to display Projectiles and NPC's
/// </summary>
internal sealed class UIDefinitionIcon : UIElement
{
    private readonly Asset<Texture2D> _texture;
    private readonly Rectangle _source;

    public UIDefinitionIcon(Asset<Texture2D> texture, Rectangle source)
    {
        _texture = texture;
        _source = source;
        Width.Set(16f, 0f);
        Height.Set(16f, 0f);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        if (_texture == null || !_texture.IsLoaded)
            return;

        var tex = _texture.Value;
        var dims = GetDimensions();

        float scaleX = dims.Width / _source.Width;
        float scaleY = dims.Height / _source.Height;
        float scale = MathHelper.Min(scaleX, scaleY);

        var drawSize = new Vector2(_source.Width, _source.Height) * scale;
        var pos = new Vector2(dims.X, dims.Y) + (new Vector2(dims.Width, dims.Height) - drawSize) * 0.5f;

        spriteBatch.Draw(tex, pos, _source, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }
}
