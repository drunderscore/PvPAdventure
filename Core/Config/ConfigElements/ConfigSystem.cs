using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using PvPAdventure.Core.Config;
using ReLogic.Content;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config.UI;
using Terraria.UI;
using Terraria.UI.Chat;
using TMLHeaderAttribute = Terraria.ModLoader.Config.HeaderAttribute;

namespace PvPAdventure.Core.Config.ConfigElements;

[Autoload(Side = ModSide.Client)]
public sealed class ConfigSystem : ModSystem
{
    private const float IconSize = 24f;
    private const float LockIconSize = 24f;
    private const float IconLeft = 8f;
    private const float RowHeight = 30f;
    private const float TextOffset = IconLeft + IconSize + 8f;
    private const string ConfigLocalizationPrefix = "Mods.PvPAdventure.Configs";
    private const string LockedTooltipColor = "FF0000";
    private static readonly Color LockedBackgroundColor = new(80, 80, 80);
    private static readonly Color LockedIconColor = new(145, 145, 145);
    private static readonly FieldInfo BackgroundColorField = typeof(ConfigElement).GetField("backgroundColor", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly HashSet<string> Warnings = [];
    private static Asset<Texture2D> pendingHeaderIcon;
    private Hook handleHeaderHook, wrapItHook;
    private ILHook headerDrawHook;

    private delegate void HandleHeaderOrig(UIElement parent, ref int top, ref int order, PropertyFieldWrapper variable);
    private delegate Tuple<UIElement, UIElement> WrapItOrig(UIElement parent, ref int top, PropertyFieldWrapper memberInfo, object item, int order, object list, Type arrayType, int index);

    public override void Load()
    {
        if (Main.dedServ)
        {
            return;
        }

        _ = Ass.Initialized;
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        handleHeaderHook = Hook(typeof(UIModConfig).GetMethod("HandleHeader", flags), HandleHeader);
        wrapItHook = Hook(typeof(UIModConfig).GetMethod("WrapIt", flags), WrapIt);
        MethodInfo drawHeader = typeof(HeaderElement).GetMethod("DrawSelf", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        headerDrawHook = drawHeader == null ? null : new ILHook(drawHeader, ShiftHeaderLabel);
    }

    public override void Unload()
    {
        handleHeaderHook?.Dispose();
        wrapItHook?.Dispose();
        headerDrawHook?.Dispose();
        handleHeaderHook = wrapItHook = null;
        headerDrawHook = null;
        pendingHeaderIcon = null;
        Warnings.Clear();
    }

    private static Hook Hook(MethodInfo method, Delegate detour) => method == null ? null : new Hook(method, detour);

    private static void HandleHeader(HandleHeaderOrig orig, UIElement parent, ref int top, ref int order, PropertyFieldWrapper variable)
    {
        pendingHeaderIcon = GetHeaderIcon(variable?.MemberInfo);
        try
        {
            int childCount = CountChildren(parent);
            orig(parent, ref top, ref order, variable);
            if (pendingHeaderIcon != null && TryGetNewChild(parent, childCount, out UIElement header))
            {
                AddIcon(header, pendingHeaderIcon, null, ConfigIconPlacement.Inside, null, null);
            }
        }
        finally
        {
            pendingHeaderIcon = null;
        }
    }

    private static Tuple<UIElement, UIElement> WrapIt(WrapItOrig orig, UIElement parent, ref int top, PropertyFieldWrapper memberInfo, object item, int order, object list, Type arrayType, int index)
    {
        var result = orig(parent, ref top, memberInfo, item, order, list, arrayType, index);
        if (result?.Item1 == null || result.Item2 == null)
        {
            return result;
        }

        MemberInfo member = memberInfo?.MemberInfo;
        LockState memberLock = GetMemberLock(member, item);
        LockState groupLock = GetGroupLock(member, item);
        LockState dependencySourceLock = GetDependencySourceLock(member, item);

        ConfigIconAttribute iconAttribute = item is TMLHeaderAttribute ? null : member?.GetCustomAttribute<ConfigIconAttribute>(true);
        ConfigIconPlacement placement = iconAttribute?.Placement ?? ConfigIconPlacement.Inside;
        Asset<Texture2D> icon = item is TMLHeaderAttribute ? pendingHeaderIcon : GetIcon(iconAttribute);
        Asset<Texture2D> offIcon = GetOffIcon(iconAttribute);
        Func<bool> useOnIcon = offIcon == null ? null : GetBoolGetter(member, item);
        Func<bool> disabled = memberLock?.IsLocked ?? groupLock?.IsLocked;
        bool grayWhenOff = iconAttribute?.GrayWhenOff == true;
        LockState grayRowLock = dependencySourceLock ?? (grayWhenOff && useOnIcon != null ? new LockState(() => !IsLocked(useOnIcon), null) : null);

        result = icon == null ? result : Tuple.Create(result.Item1, AddIcon(result.Item2, icon, offIcon, placement, useOnIcon, disabled, grayWhenOff));

        if (memberLock != null)
        {
            return Wrap(result, element => new LockWrapper(element, memberLock, overlay: true), memberInfo, item, list, index);
        }

        if (groupLock != null)
        {
            return Wrap(result, element => new LockWrapper(element, groupLock, overlay: false), memberInfo, item, list, index);
        }

        return grayRowLock == null ? result : Wrap(result, element => new LockWrapper(element, grayRowLock, overlay: false), memberInfo, item, list, index);
    }

    private static Tuple<UIElement, UIElement> Wrap(Tuple<UIElement, UIElement> result, Func<UIElement, UIElement> createWrapper, PropertyFieldWrapper memberInfo, object item, object list, int index)
    {
        UIElement element = result.Item2;
        result.Item1.RemoveChild(result.Item2);
        UIElement wrapper = createWrapper(element);
        if (wrapper is ConfigElement config)
        {
            config.Bind(memberInfo, item, list as IList, index);
            config.OnBind();
        }

        result.Item1.Append(wrapper);
        result.Item1.Height.Set(wrapper.Height.Pixels, 0f);
        result.Item1.Recalculate();
        return Tuple.Create(result.Item1, wrapper);
    }

    private static Asset<Texture2D> GetIcon(ConfigIconAttribute attribute) => attribute == null ? null : GetTexture(attribute.AssFieldName, attribute.ItemId);

    private static Asset<Texture2D> GetOffIcon(ConfigIconAttribute attribute) => attribute?.OffAssFieldName?.Length > 0 ? GetTexture(attribute.OffAssFieldName, -1) : null;

    private static Asset<Texture2D> GetHeaderIcon(MemberInfo member)
    {
        return member?.GetCustomAttribute<HeaderIconAttribute>(true) is { } header
            ? GetTexture(header.AssFieldName, header.ItemId)
            : null;
    }

    private static Asset<Texture2D> GetTexture(string fieldName, int itemId)
    {
        if (itemId >= 0)
        {
            return Main.Assets.Request<Texture2D>($"Images/Item_{itemId}", AssetRequestMode.ImmediateLoad);
        }

        return string.IsNullOrEmpty(fieldName)
            ? null
            : typeof(Ass).GetField(fieldName, BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as Asset<Texture2D>;
    }

    private static int CountChildren(UIElement element) => element.Children.Count();

    private static bool TryGetNewChild(UIElement element, int oldCount, out UIElement child)
    {
        child = element.Children.Skip(oldCount).LastOrDefault();
        return child != null;
    }

    private static LockState GetMemberLock(MemberInfo member, object item)
    {
        var requires = member?.GetCustomAttribute<RequiresFieldAttribute>(true);
        return requires == null ? null : CreateRequiredLock(item, requires.FieldName, member);
    }

    private static LockState GetGroupLock(MemberInfo member, object item)
    {
        object target = GetValue(member, item);
        string fieldName = target == null ? null : FindRequiredField(target.GetType());
        return fieldName == null ? null : CreateRequiredLock(target, fieldName, member);
    }

    private static LockState GetDependencySourceLock(MemberInfo member, object item)
    {
        Func<bool> enabled = GetBoolGetter(member, item);
        if (enabled == null || member?.Name == null || item == null || !IsRequiredByAnotherMember(item.GetType(), member.Name))
        {
            return null;
        }

        return new LockState(() => !enabled(), null);
    }

    private static LockState CreateRequiredLock(object owner, string fieldName, MemberInfo sourceMember)
    {
        Func<bool> locked = ResolveLock(owner, fieldName, sourceMember);
        return locked == null ? null : new LockState(locked, GetRequiredTooltip(owner?.GetType(), fieldName));
    }

    private static string GetRequiredTooltip(Type ownerType, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return null;
        }

        string label = GetRequiredFieldLabel(ownerType, fieldName);
        return $"[c/{LockedTooltipColor}:Locked: Requires {label} to be enabled]";
    }

    private static string GetRequiredFieldLabel(Type ownerType, string fieldName)
    {
        if (ownerType != null)
        {
            string label = GetLocalizedLabel(ownerType.Name, fieldName);
            if (!string.IsNullOrWhiteSpace(label))
            {
                return label;
            }
        }

        string serverLabel = GetLocalizedLabel(nameof(ServerConfig), fieldName);
        return string.IsNullOrWhiteSpace(serverLabel) ? NicifyName(fieldName) : serverLabel;
    }

    private static string GetLocalizedLabel(string configTypeName, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(configTypeName) || string.IsNullOrWhiteSpace(fieldName))
        {
            return null;
        }

        string key = $"{ConfigLocalizationPrefix}.{configTypeName}.{fieldName}.Label";
        string value = Language.GetTextValue(key);
        return string.IsNullOrWhiteSpace(value) || value == key ? null : value;
    }

    private static string NicifyName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "";
        }

        List<char> result = [];
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(name[i - 1]))
            {
                result.Add(' ');
            }

            result.Add(c);
        }

        return new string([.. result]);
    }

    private static string FindRequiredField(Type type)
    {
        return PublicMembers(type)
            .Select(member => member.GetCustomAttribute<RequiresFieldAttribute>(true)?.FieldName)
            .FirstOrDefault(fieldName => HasReadableBool(type, fieldName));
    }

    private static bool IsRequiredByAnotherMember(Type type, string fieldName) => PublicMembers(type).Any(member => member.GetCustomAttribute<RequiresFieldAttribute>(true)?.FieldName == fieldName);

    private static IEnumerable<MemberInfo> PublicMembers(Type type) => type?.GetMembers(BindingFlags.Instance | BindingFlags.Public) ?? [];

    private static Func<bool> ResolveLock(object item, string fieldName, MemberInfo member)
    {
        if (item == null || string.IsNullOrWhiteSpace(fieldName))
        {
            Warn(item, member, fieldName, item == null ? "no containing config object was available" : "required field name was empty");
            return null;
        }

        Type type = item.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
        if (type.GetField(fieldName, flags) is { FieldType: { } fieldType } field)
        {
            if (fieldType == typeof(bool))
            {
                return () => !(bool)field.GetValue(item);
            }

            Warn(item, member, fieldName, $"'{fieldName}' is a {fieldType.Name} field, not a bool");
            return null;
        }

        if (type.GetProperty(fieldName, flags) is { PropertyType: { } propertyType } property)
        {
            if (propertyType == typeof(bool) && property.CanRead)
            {
                return () => !(bool)property.GetValue(item);
            }

            Warn(item, member, fieldName, property.CanRead ? $"'{fieldName}' is a {propertyType.Name} property, not a bool" : $"'{fieldName}' cannot be read");
            return null;
        }

        Warn(item, member, fieldName, $"'{fieldName}' was not found on {type.Name}");
        return null;
    }

    private static bool HasReadableBool(Type type, string name)
    {
        if (type == null || string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
        return type.GetField(name, flags)?.FieldType == typeof(bool)
            || type.GetProperty(name, flags) is { PropertyType: { } propertyType, CanRead: true } && propertyType == typeof(bool);
    }

    private static object GetValue(MemberInfo member, object item) => member switch
    {
        FieldInfo field when item != null => field.GetValue(item),
        PropertyInfo { CanRead: true } property when item != null => property.GetValue(item),
        _ => null
    };

    private static Func<bool> GetBoolGetter(MemberInfo member, object item) => member switch
    {
        FieldInfo { FieldType: { } type } field when item != null && type == typeof(bool) => () => (bool)field.GetValue(item),
        PropertyInfo { PropertyType: { } type, CanRead: true } property when item != null && type == typeof(bool) => () => (bool)property.GetValue(item),
        _ => null
    };

    private static void Warn(object item, MemberInfo member, string fieldName, string reason)
    {
        string key = $"{item?.GetType().FullName ?? "<null>"}.{member?.Name ?? "<unknown>"}:{fieldName}:{reason}";
        if (Warnings.Add(key))
        {
            Log.Warn($"Config field '{member?.Name ?? "<unknown>"}' has invalid RequiresField('{fieldName}'): {reason}. The row will remain unlocked.");
        }
    }

    private static UIElement AddIcon(UIElement element, Asset<Texture2D> asset, Asset<Texture2D> offAsset, ConfigIconPlacement placement, Func<bool> useOnAsset, Func<bool> locked, bool grayWhenOff = false)
    {
        if (element == null || asset == null || HasIcon(element))
        {
            return element;
        }

        bool cut = placement == ConfigIconPlacement.Cut;
        ConfigElement config = cut ? null : element as ConfigElement;

        if (cut)
        {
            element.Left.Pixels += TextOffset;
            element.Width.Pixels -= TextOffset;
        }

        element.Append(new ConfigIconImage(asset, offAsset, !cut && config == null, useOnAsset, locked, grayWhenOff)
        {
            Left = { Pixels = cut ? IconLeft - TextOffset : IconLeft },
            VAlign = 0.5f
        });

        if (config != null)
        {
            config.DrawLabel = false;
            element.Append(new ConfigIconLabel(config));
        }

        element.Recalculate();
        element.Parent?.Recalculate();
        return element;
    }

    private static bool HasIcon(UIElement element) => element.Children.Any(child => child is ConfigIconImage);

    private static void ShiftHeaderLabel(ILContext il)
    {
        var cursor = new ILCursor(il);
        if (!TryGotoFontLoad(cursor) || il.Body.Variables.Count <= 2)
        {
            return;
        }

        VariableDefinition textPosition = il.Body.Variables[2];
        cursor.Emit(OpCodes.Ldloc, textPosition);
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<Vector2, UIElement, Vector2>>((position, element) => HasShiftingIcon(element) ? new(position.X + TextOffset, position.Y) : position);
        cursor.Emit(OpCodes.Stloc, textPosition);
    }

    private static bool TryGotoFontLoad(ILCursor cursor)
    {
        try
        {
            cursor.GotoNext(MoveType.Before, i => i.MatchLdarg(1), i => i.OpCode == OpCodes.Ldsfld && i.Operand is FieldReference field && field.DeclaringType.FullName == "Terraria.GameContent.FontAssets" && field.Name == "ItemStack");
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool HasShiftingIcon(UIElement element) => element.Children.Any(child => child is ConfigIconImage { ShiftsText: true });

    private static bool IsLocked(Func<bool> locked)
    {
        try
        {
            return locked?.Invoke() == true;
        }
        catch (Exception e)
        {
            Log.Warn($"Failed to read config lock state: {e.Message}");
            return false;
        }
    }

    private static bool IsHovering(UIElement element)
    {
        return element?.ContainsPoint(Main.MouseScreen) == true;
    }

    private static void SetLockTooltip(UIElement element, LockState lockState)
    {
        if (IsHovering(element) && !string.IsNullOrWhiteSpace(lockState?.Tooltip))
        {
            UIModConfig.Tooltip = lockState.Tooltip;
        }
    }

    private static void DrawTexture(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, float scale, Color color)
    {
        spriteBatch.Draw(texture, position, null, color * 0.85f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    private static string GetLabel(ConfigElement config)
    {
        string label = config?.TextDisplayFunction?.Invoke() ?? config?.MemberInfo?.Name ?? "";
        return config is { ReloadRequired: true, ValueChanged: true }
            ? label + " - [c/FF0000:" + Language.GetTextValue("tModLoader.ModReloadRequired") + "]"
            : label;
    }

    private sealed record LockState(Func<bool> IsLocked, string Tooltip);

    private sealed class ConfigIconImage : UIImage
    {
        private readonly Asset<Texture2D> onTexture;
        private readonly Asset<Texture2D> offTexture;
        private readonly Func<bool> useOnTexture;
        private readonly Func<bool> locked;
        private readonly bool grayWhenOff;

        public bool ShiftsText { get; }

        public ConfigIconImage(Asset<Texture2D> onTexture, Asset<Texture2D> offTexture, bool shiftsText, Func<bool> useOnTexture, Func<bool> locked, bool grayWhenOff) : base(onTexture.Value)
        {
            this.onTexture = onTexture;
            this.offTexture = offTexture;
            this.useOnTexture = useOnTexture;
            this.locked = locked;
            this.grayWhenOff = grayWhenOff;

            ShiftsText = shiftsText;
            IgnoresMouseInteraction = true;
            Width.Set(IconSize, 0f);
            Height.Set(IconSize, 0f);
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            bool isLocked = IsLocked(locked);
            bool useOffTexture = offTexture != null && (isLocked || useOnTexture != null && !IsLocked(useOnTexture));
            Asset<Texture2D> selected = useOffTexture ? offTexture : onTexture;
            Texture2D value = selected?.Value;
            if (value == null)
            {
                return;
            }

            CalculatedStyle dimensions = GetDimensions();
            float scale = Math.Min(IconSize / value.Width, IconSize / value.Height);
            Vector2 position = new(dimensions.X + (dimensions.Width - value.Width * scale) * 0.5f, dimensions.Y + (dimensions.Height - value.Height * scale) * 0.5f);

            if (isLocked || useOffTexture && grayWhenOff)
            {
                DrawTextureGrayscale(sb, value, position, scale);
                return;
            }

            DrawTexture(sb, value, position, scale, Color.White);
        }

        private static void DrawTextureGrayscale(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, float scale)
        {
            GraphicsDevice device = spriteBatch.GraphicsDevice;
            Rectangle scissor = device.ScissorRectangle;

            RasterizerState current = device.RasterizerState;
            RasterizerState clippedRasterizer = new()
            {
                CullMode = current?.CullMode ?? CullMode.CullCounterClockwiseFace,
                FillMode = current?.FillMode ?? FillMode.Solid,
                MultiSampleAntiAlias = current?.MultiSampleAntiAlias ?? false,
                ScissorTestEnable = true,
                DepthBias = current?.DepthBias ?? 0,
                SlopeScaleDepthBias = current?.SlopeScaleDepthBias ?? 0
            };

            if (!EffectLoader.TryGetGrayscaleEffect(out Effect effect))
            {
                DrawTexture(spriteBatch, texture, position, scale, Color.Gray);
                return;
            }

            effect.Parameters["Intensity"]?.SetValue(1f);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, clippedRasterizer, effect, Main.UIScaleMatrix);
            device.ScissorRectangle = scissor;
            DrawTexture(spriteBatch, texture, position, scale, Color.White);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, clippedRasterizer, null, Main.UIScaleMatrix);
            device.ScissorRectangle = scissor;
        }
    }

    private sealed class ConfigIconLabel : UIElement
    {
        private readonly ConfigElement owner;

        public ConfigIconLabel(ConfigElement owner)
        {
            this.owner = owner;
            IgnoresMouseInteraction = true;
            Width.Set(0f, 1f);
            Height.Set(0f, 1f);
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            if (owner == null || owner.DrawLabel)
            {
                return;
            }

            CalculatedStyle dimensions = owner.GetDimensions();
            ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.ItemStack.Value, GetLabel(owner), new(dimensions.X + TextOffset, dimensions.Y + 8f), owner.MemberInfo?.CanWrite == false ? Color.Gray : Color.White, 0f, Vector2.Zero, new(0.8f), Math.Max(20f, dimensions.Width - TextOffset - 8f), 2f);
        }
    }

    private sealed class LockWrapper : UIElement
    {
        private readonly UIElement content;
        private readonly ConfigElement config;
        private readonly LockState lockState;
        private readonly bool overlay;
        private Color originalBackgroundColor;
        private bool hasOriginalBackgroundColor;

        public LockWrapper(UIElement content, LockState lockState, bool overlay)
        {
            this.content = content;
            this.lockState = lockState;
            this.overlay = overlay;
            config = content as ConfigElement;

            CopyStyle(content);
            OverflowHidden = content.OverflowHidden;
            content.Left.Set(0f, 0f);
            content.Top.Set(0f, 0f);
            content.Width.Set(0f, 1f);
            Append(content);
        }

        public override void Update(GameTime gameTime)
        {
            Sync(true);
            base.Update(gameTime);
        }

        public override void Recalculate()
        {
            base.Recalculate();
            Sync(false);
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            bool locked = IsLocked(lockState?.IsLocked);
            SyncBackground(locked);
            if (!locked)
            {
                return;
            }

            SetLockTooltip(this, lockState);
            if (overlay)
            {
                DrawOverlay(sb, GetDimensions());
            }
        }

        protected override void DrawChildren(SpriteBatch sb)
        {
            if (!overlay || !IsLocked(lockState?.IsLocked))
            {
                base.DrawChildren(sb);
            }
        }

        private void Sync(bool reflow)
        {
            bool locked = IsLocked(lockState?.IsLocked);
            content.IgnoresMouseInteraction = overlay && locked;
            SyncBackground(locked);
            if (locked)
            {
                SetLockTooltip(this, lockState);
            }

            if (overlay && locked)
            {
                SetHeight(this, RowHeight, reflow);
            }
            else
            {
                SetSyncedHeight(this, content, reflow);
            }
        }

        private void SyncBackground(bool locked)
        {
            if (config == null || BackgroundColorField == null)
            {
                return;
            }

            if (!hasOriginalBackgroundColor)
            {
                originalBackgroundColor = (Color)BackgroundColorField.GetValue(config);
                hasOriginalBackgroundColor = true;
            }

            BackgroundColorField.SetValue(config, locked ? LockedBackgroundColor : originalBackgroundColor);
        }

        private void DrawOverlay(SpriteBatch sb, CalculatedStyle d)
        {
            ConfigElement.DrawPanel2(sb, new(d.X, d.Y), TextureAssets.SettingsPanel.Value, d.Width + 1f, RowHeight, IsHovering(this) ? new(105, 105, 105, 230) : new(80, 80, 80, 220));
            bool hasIcon = DrawIconChildren(sb, content);
            DrawLockedLabel(sb, d, hasIcon);
            DrawLockIcon(sb, d);
        }

        private static bool DrawIconChildren(SpriteBatch sb, UIElement element)
        {
            bool drewIcon = false;
            foreach (ConfigIconImage icon in element.Children.OfType<ConfigIconImage>())
            {
                icon.Draw(sb);
                drewIcon = true;
            }

            return drewIcon;
        }

        private void DrawLockedLabel(SpriteBatch sb, CalculatedStyle d, bool hasIcon)
        {
            float x = d.X + (hasIcon ? TextOffset : IconLeft);
            float maxWidth = Math.Max(20f, d.Width - (x - d.X) - LockIconSize - 24f);
            ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.ItemStack.Value, GetLabel(config), new(x, d.Y + 8f), Color.Silver, 0f, Vector2.Zero, new(0.8f), maxWidth, 2f);
        }

        private static void DrawLockIcon(SpriteBatch sb, CalculatedStyle d)
        {
            Texture2D texture = Ass.IconLock?.Value;
            if (texture == null)
            {
                return;
            }

            float scale = Math.Min(LockIconSize / texture.Width, LockIconSize / texture.Height);
            Vector2 size = texture.Size() * scale;
            DrawTexture(sb, texture, new(d.X + d.Width - size.X - 10f, d.Y + (d.Height - size.Y) * 0.5f), scale, LockedIconColor);
        }
    }

    private static void SetSyncedHeight(UIElement wrapper, UIElement content, bool reflow)
    {
        float contentHeight = Math.Max(content.GetOuterDimensions().Height, content.Height.Pixels);
        SetHeight(wrapper, Math.Max(RowHeight, contentHeight), reflow);
    }

    private static void SetHeight(UIElement wrapper, float height, bool reflow)
    {
        if (Math.Abs(wrapper.Height.Pixels - height) <= 0.1f)
        {
            return;
        }

        wrapper.Height.Set(height, 0f);
        wrapper.Parent?.Height.Set(height, 0f);
        if (reflow)
        {
            Reflow(wrapper.Parent);
        }
    }

    private static void Reflow(UIElement element)
    {
        for (UIElement ancestor = element; ancestor != null; ancestor = ancestor.Parent)
        {
            if (ancestor is UIList list)
            {
                list.Recalculate();
                return;
            }
        }

        element?.Recalculate();
    }
}
