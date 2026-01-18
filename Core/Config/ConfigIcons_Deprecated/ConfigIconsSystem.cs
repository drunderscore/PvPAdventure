//using Microsoft.Xna.Framework.Graphics;
//using MonoMod.RuntimeDetour;
//using PvPAdventure.Core.Assets;
//using PvPAdventure.Core.Config.ConfigIcons;
//using ReLogic.Content;
//using System;
//using System.Reflection;
//using Terraria;
//using Terraria.GameContent.UI.Elements;
//using Terraria.ModLoader;
//using Terraria.ModLoader.Config;
//using Terraria.ModLoader.Config.UI;
//using Terraria.UI;

//namespace PvPAdventure.Core.Config.ConfigIcons;

//[Autoload(Side = ModSide.Client)]
//public sealed class ConfigIconsSystem : ModSystem
//{
//    private const float IconSize = 18f;
//    private const float Gap = 6f;
//    private const float LeftInset = 2f;
//    private const float IconPad = IconSize + Gap;

//    private Hook handleHeaderHook;
//    private Hook wrapItHook;

//    private static Asset<Texture2D> pendingHeaderIcon;

//    private delegate void HandleHeaderOrig(
//        UIElement parent,
//        ref int top,
//        ref int order,
//        PropertyFieldWrapper variable);

//    private delegate Tuple<UIElement, UIElement> WrapItOrig(
//        UIElement parent,
//        ref int top,
//        PropertyFieldWrapper memberInfo,
//        object item,
//        int order,
//        object list,
//        Type arrayType,
//        int index);

//    public override void Load()
//    {
//        if (Main.dedServ)
//        {
//            return;
//        }

//        _ = Ass.Initialized;

//        MethodInfo handleHeader = typeof(UIModConfig).GetMethod(
//            "HandleHeader",
//            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

//        MethodInfo wrapIt = typeof(UIModConfig).GetMethod(
//            "WrapIt",
//            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

//        if (handleHeader == null || wrapIt == null)
//        {
//            return;
//        }

//        handleHeaderHook = new Hook(handleHeader, HandleHeader_Hook);
//        wrapItHook = new Hook(wrapIt, WrapIt_Hook);
//    }

//    public override void Unload()
//    {
//        handleHeaderHook?.Dispose();
//        wrapItHook?.Dispose();
//        handleHeaderHook = null;
//        wrapItHook = null;
//        pendingHeaderIcon = null;
//    }

//    private static void HandleHeader_Hook(
//        HandleHeaderOrig orig,
//        UIElement parent,
//        ref int top,
//        ref int order,
//        PropertyFieldWrapper variable)
//    {
//        pendingHeaderIcon = null;

//        if (variable?.MemberInfo != null)
//        {
//            var iconAttr = (ConfigIconAttribute)Attribute.GetCustomAttribute(
//                variable.MemberInfo,
//                typeof(ConfigIconAttribute),
//                inherit: true);

//            if (iconAttr != null)
//            {
//                FieldInfo field = typeof(Ass).GetField(
//                    iconAttr.AssFieldName,
//                    BindingFlags.Public | BindingFlags.Static);

//                if (field?.GetValue(null) is Asset<Texture2D> asset)
//                {
//                    pendingHeaderIcon = asset;
//                }
//            }
//        }

//        try
//        {
//            orig(parent, ref top, ref order, variable);
//        }
//        finally
//        {
//            pendingHeaderIcon = null;
//        }
//    }

//    private static Tuple<UIElement, UIElement> WrapIt_Hook(
//        WrapItOrig orig,
//        UIElement parent,
//        ref int top,
//        PropertyFieldWrapper memberInfo,
//        object item,
//        int order,
//        object list,
//        Type arrayType,
//        int index)
//    {
//        var result = orig(parent, ref top, memberInfo, item, order, list, arrayType, index);
//        if (result == null)
//        {
//            return null;
//        }

//        UIElement element = result.Item2;

//        // Header row
//        if (item is HeaderAttribute && pendingHeaderIcon != null)
//        {
//            AttachIcon(element, pendingHeaderIcon);
//            return result;
//        }

//        // Normal config row
//        if (memberInfo?.MemberInfo != null)
//        {
//            var iconAttr = (ConfigIconAttribute)Attribute.GetCustomAttribute(
//                memberInfo.MemberInfo,
//                typeof(ConfigIconAttribute),
//                inherit: true);

//            if (iconAttr != null)
//            {
//                FieldInfo field = typeof(Ass).GetField(
//                    iconAttr.AssFieldName,
//                    BindingFlags.Public | BindingFlags.Static);

//                if (field?.GetValue(null) is Asset<Texture2D> asset)
//                {
//                    AttachIcon(element, asset);
//                }
//            }
//        }

//        return result;
//    }

//    private static void AttachIcon(UIElement element, Asset<Texture2D> asset)
//    {
//        if (element == null || asset == null)
//        {
//            return;
//        }

//        foreach (var child in element.Children)
//        {
//            if (child is ConfigIconImage)
//            {
//                return;
//            }
//        }

//        // Shift content right to make room for icon
//        element.PaddingLeft += IconPad;

//        var icon = new ConfigIconImage(asset);
//        icon.Width.Set(IconSize, 0f);
//        icon.Height.Set(IconSize, 0f);
//        icon.VAlign = 0.5f;

//        // Place icon inside the padding
//        icon.Left.Set(-IconPad + LeftInset, 0f);

//        element.Append(icon);
//        element.Recalculate();
//    }

//    private sealed class ConfigIconImage : UIImage
//    {
//        public ConfigIconImage(Asset<Texture2D> texture) : base(texture)
//        {
//            IgnoresMouseInteraction = true;
//        }

//        public override void Draw(SpriteBatch spriteBatch)
//        {
//            base.Draw(spriteBatch);
//        }
//    }
//}
