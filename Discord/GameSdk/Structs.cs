using System;
using System.Runtime.InteropServices;

namespace PvPAdventure.Discord.GameSdk;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public partial struct User
{
    public Int64 Id;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string Username;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
    public string Discriminator;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string Avatar;

    public bool Bot;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public partial struct OAuth2Token
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string AccessToken;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
    public string Scopes;

    public Int64 Expires;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public partial struct ImageHandle
{
    public ImageType Type;

    public Int64 Id;

    public UInt32 Size;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public partial struct ImageDimensions
{
    public UInt32 Width;

    public UInt32 Height;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public partial struct ActivityTimestamps
{
    public Int64 Start;

    public Int64 End;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public partial struct ActivityAssets
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string LargeImage;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string LargeText;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string SmallImage;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string SmallText;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public partial struct PartySize
{
    public Int32 CurrentSize;

    public Int32 MaxSize;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public partial struct ActivityParty
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string Id;

    public PartySize Size;

    public ActivityPartyPrivacy Privacy;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public partial struct ActivitySecrets
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string Match;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string Join;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string Spectate;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public partial struct Activity
{
    public ActivityType Type;

    public Int64 ApplicationId;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string Name;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string State;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string Details;

    public ActivityTimestamps Timestamps;

    public ActivityAssets Assets;

    public ActivityParty Party;

    public ActivitySecrets Secrets;

    public bool Instance;

    public UInt32 SupportedPlatforms;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public partial struct Presence
{
    public Status Status;

    public Activity Activity;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public partial struct Relationship
{
    public RelationshipType Type;

    public User User;

    public Presence Presence;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public partial struct Lobby
{
    public Int64 Id;

    public LobbyType Type;

    public Int64 OwnerId;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string Secret;

    public UInt32 Capacity;

    public bool Locked;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public partial struct ImeUnderline
{
    public Int32 From;

    public Int32 To;

    public UInt32 Color;

    public UInt32 BackgroundColor;

    public bool Thick;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public partial struct Rect
{
    public Int32 Left;

    public Int32 Top;

    public Int32 Right;

    public Int32 Bottom;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public partial struct FileStat
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
    public string Filename;

    public UInt64 Size;

    public UInt64 LastModified;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public partial struct Entitlement
{
    public Int64 Id;

    public EntitlementType Type;

    public Int64 SkuId;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public partial struct SkuPrice
{
    public UInt32 Amount;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
    public string Currency;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public partial struct Sku
{
    public Int64 Id;

    public SkuType Type;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string Name;

    public SkuPrice Price;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public partial struct InputMode
{
    public InputModeType Type;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string Shortcut;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public partial struct UserAchievement
{
    public Int64 UserId;

    public Int64 AchievementId;

    public byte PercentComplete;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    public string UnlockedAt;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public partial struct LobbyTransaction
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct FFIMethods
    {
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate Result SetTypeMethod(IntPtr methodsPtr, LobbyType type);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate Result SetOwnerMethod(IntPtr methodsPtr, Int64 ownerId);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate Result SetCapacityMethod(IntPtr methodsPtr, UInt32 capacity);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate Result SetMetadataMethod(IntPtr methodsPtr, [MarshalAs(UnmanagedType.LPStr)] string key, [MarshalAs(UnmanagedType.LPStr)] string value);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate Result DeleteMetadataMethod(IntPtr methodsPtr, [MarshalAs(UnmanagedType.LPStr)] string key);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate Result SetLockedMethod(IntPtr methodsPtr, bool locked);

        internal SetTypeMethod SetType;

        internal SetOwnerMethod SetOwner;

        internal SetCapacityMethod SetCapacity;

        internal SetMetadataMethod SetMetadata;

        internal DeleteMetadataMethod DeleteMetadata;

        internal SetLockedMethod SetLocked;
    }

    internal IntPtr MethodsPtr;

    internal Object MethodsStructure;

    private FFIMethods Methods
    {
        get
        {
            if (MethodsStructure == null)
            {
                MethodsStructure = Marshal.PtrToStructure(MethodsPtr, typeof(FFIMethods));
            }
            return (FFIMethods)MethodsStructure;
        }

    }

    public void SetType(LobbyType type)
    {
        if (MethodsPtr != IntPtr.Zero)
        {
            var res = Methods.SetType(MethodsPtr, type);
            if (res != Result.Ok)
            {
                throw new ResultException(res);
            }
        }
    }

    public void SetOwner(Int64 ownerId)
    {
        if (MethodsPtr != IntPtr.Zero)
        {
            var res = Methods.SetOwner(MethodsPtr, ownerId);
            if (res != Result.Ok)
            {
                throw new ResultException(res);
            }
        }
    }

    public void SetCapacity(UInt32 capacity)
    {
        if (MethodsPtr != IntPtr.Zero)
        {
            var res = Methods.SetCapacity(MethodsPtr, capacity);
            if (res != Result.Ok)
            {
                throw new ResultException(res);
            }
        }
    }

    public void SetMetadata(string key, string value)
    {
        if (MethodsPtr != IntPtr.Zero)
        {
            var res = Methods.SetMetadata(MethodsPtr, key, value);
            if (res != Result.Ok)
            {
                throw new ResultException(res);
            }
        }
    }

    public void DeleteMetadata(string key)
    {
        if (MethodsPtr != IntPtr.Zero)
        {
            var res = Methods.DeleteMetadata(MethodsPtr, key);
            if (res != Result.Ok)
            {
                throw new ResultException(res);
            }
        }
    }

    public void SetLocked(bool locked)
    {
        if (MethodsPtr != IntPtr.Zero)
        {
            var res = Methods.SetLocked(MethodsPtr, locked);
            if (res != Result.Ok)
            {
                throw new ResultException(res);
            }
        }
    }
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public partial struct LobbyMemberTransaction
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct FFIMethods
    {
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate Result SetMetadataMethod(IntPtr methodsPtr, [MarshalAs(UnmanagedType.LPStr)] string key, [MarshalAs(UnmanagedType.LPStr)] string value);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate Result DeleteMetadataMethod(IntPtr methodsPtr, [MarshalAs(UnmanagedType.LPStr)] string key);

        internal SetMetadataMethod SetMetadata;

        internal DeleteMetadataMethod DeleteMetadata;
    }

    internal IntPtr MethodsPtr;

    internal Object MethodsStructure;

    private FFIMethods Methods
    {
        get
        {
            if (MethodsStructure == null)
            {
                MethodsStructure = Marshal.PtrToStructure(MethodsPtr, typeof(FFIMethods));
            }
            return (FFIMethods)MethodsStructure;
        }

    }

    public void SetMetadata(string key, string value)
    {
        if (MethodsPtr != IntPtr.Zero)
        {
            var res = Methods.SetMetadata(MethodsPtr, key, value);
            if (res != Result.Ok)
            {
                throw new ResultException(res);
            }
        }
    }

    public void DeleteMetadata(string key)
    {
        if (MethodsPtr != IntPtr.Zero)
        {
            var res = Methods.DeleteMetadata(MethodsPtr, key);
            if (res != Result.Ok)
            {
                throw new ResultException(res);
            }
        }
    }
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public partial struct LobbySearchQuery
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct FFIMethods
    {
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate Result FilterMethod(IntPtr methodsPtr, [MarshalAs(UnmanagedType.LPStr)] string key, LobbySearchComparison comparison, LobbySearchCast cast, [MarshalAs(UnmanagedType.LPStr)] string value);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate Result SortMethod(IntPtr methodsPtr, [MarshalAs(UnmanagedType.LPStr)] string key, LobbySearchCast cast, [MarshalAs(UnmanagedType.LPStr)] string value);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate Result LimitMethod(IntPtr methodsPtr, UInt32 limit);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate Result DistanceMethod(IntPtr methodsPtr, LobbySearchDistance distance);

        internal FilterMethod Filter;

        internal SortMethod Sort;

        internal LimitMethod Limit;

        internal DistanceMethod Distance;
    }

    internal IntPtr MethodsPtr;

    internal Object MethodsStructure;

    private FFIMethods Methods
    {
        get
        {
            if (MethodsStructure == null)
            {
                MethodsStructure = Marshal.PtrToStructure(MethodsPtr, typeof(FFIMethods));
            }
            return (FFIMethods)MethodsStructure;
        }

    }

    public void Filter(string key, LobbySearchComparison comparison, LobbySearchCast cast, string value)
    {
        if (MethodsPtr != IntPtr.Zero)
        {
            var res = Methods.Filter(MethodsPtr, key, comparison, cast, value);
            if (res != Result.Ok)
            {
                throw new ResultException(res);
            }
        }
    }

    public void Sort(string key, LobbySearchCast cast, string value)
    {
        if (MethodsPtr != IntPtr.Zero)
        {
            var res = Methods.Sort(MethodsPtr, key, cast, value);
            if (res != Result.Ok)
            {
                throw new ResultException(res);
            }
        }
    }

    public void Limit(UInt32 limit)
    {
        if (MethodsPtr != IntPtr.Zero)
        {
            var res = Methods.Limit(MethodsPtr, limit);
            if (res != Result.Ok)
            {
                throw new ResultException(res);
            }
        }
    }

    public void Distance(LobbySearchDistance distance)
    {
        if (MethodsPtr != IntPtr.Zero)
        {
            var res = Methods.Distance(MethodsPtr, distance);
            if (res != Result.Ok)
            {
                throw new ResultException(res);
            }
        }
    }
}
