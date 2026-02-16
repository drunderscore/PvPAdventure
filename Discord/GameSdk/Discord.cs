using System;
using System.Runtime.InteropServices;

namespace PvPAdventure.Discord.GameSdk;

public partial class Discord : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct FFIEvents
    {

    }

    [StructLayout(LayoutKind.Sequential)]
    internal partial struct FFIMethods
    {
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate void DestroyHandler(IntPtr MethodsPtr);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate Result RunCallbacksMethod(IntPtr methodsPtr);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate void SetLogHookCallback(IntPtr ptr, LogLevel level, [MarshalAs(UnmanagedType.LPStr)] string message);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate void SetLogHookMethod(IntPtr methodsPtr, LogLevel minLevel, IntPtr callbackData, SetLogHookCallback callback);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate IntPtr GetApplicationManagerMethod(IntPtr discordPtr);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate IntPtr GetUserManagerMethod(IntPtr discordPtr);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate IntPtr GetImageManagerMethod(IntPtr discordPtr);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate IntPtr GetActivityManagerMethod(IntPtr discordPtr);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate IntPtr GetRelationshipManagerMethod(IntPtr discordPtr);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate IntPtr GetLobbyManagerMethod(IntPtr discordPtr);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate IntPtr GetNetworkManagerMethod(IntPtr discordPtr);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate IntPtr GetOverlayManagerMethod(IntPtr discordPtr);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate IntPtr GetStorageManagerMethod(IntPtr discordPtr);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate IntPtr GetStoreManagerMethod(IntPtr discordPtr);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate IntPtr GetVoiceManagerMethod(IntPtr discordPtr);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate IntPtr GetAchievementManagerMethod(IntPtr discordPtr);

        internal DestroyHandler Destroy;

        internal RunCallbacksMethod RunCallbacks;

        internal SetLogHookMethod SetLogHook;

        internal GetApplicationManagerMethod GetApplicationManager;

        internal GetUserManagerMethod GetUserManager;

        internal GetImageManagerMethod GetImageManager;

        internal GetActivityManagerMethod GetActivityManager;

        internal GetRelationshipManagerMethod GetRelationshipManager;

        internal GetLobbyManagerMethod GetLobbyManager;

        internal GetNetworkManagerMethod GetNetworkManager;

        internal GetOverlayManagerMethod GetOverlayManager;

        internal GetStorageManagerMethod GetStorageManager;

        internal GetStoreManagerMethod GetStoreManager;

        internal GetVoiceManagerMethod GetVoiceManager;

        internal GetAchievementManagerMethod GetAchievementManager;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal partial struct FFICreateParams
    {
        internal Int64 ClientId;

        internal UInt64 Flags;

        internal IntPtr Events;

        internal IntPtr EventData;

        internal IntPtr ApplicationEvents;

        internal UInt32 ApplicationVersion;

        internal IntPtr UserEvents;

        internal UInt32 UserVersion;

        internal IntPtr ImageEvents;

        internal UInt32 ImageVersion;

        internal IntPtr ActivityEvents;

        internal UInt32 ActivityVersion;

        internal IntPtr RelationshipEvents;

        internal UInt32 RelationshipVersion;

        internal IntPtr LobbyEvents;

        internal UInt32 LobbyVersion;

        internal IntPtr NetworkEvents;

        internal UInt32 NetworkVersion;

        internal IntPtr OverlayEvents;

        internal UInt32 OverlayVersion;

        internal IntPtr StorageEvents;

        internal UInt32 StorageVersion;

        internal IntPtr StoreEvents;

        internal UInt32 StoreVersion;

        internal IntPtr VoiceEvents;

        internal UInt32 VoiceVersion;

        internal IntPtr AchievementEvents;

        internal UInt32 AchievementVersion;
    }

    [DllImport(Constants.LinuxLibraryName, ExactSpelling = true, CallingConvention = CallingConvention.Winapi, EntryPoint = "DiscordCreate")]
    private static extern Result DiscordCreate_Linux(UInt32 version, ref FFICreateParams createParams, out IntPtr manager);

    [DllImport(Constants.WindowsLibraryName, ExactSpelling = true, CallingConvention = CallingConvention.Winapi, EntryPoint = "DiscordCreate")]
    private static extern Result DiscordCreate_Windows(UInt32 version, ref FFICreateParams createParams, out IntPtr manager);

    private static Result DiscordCreate(UInt32 version, ref FFICreateParams createParams, out IntPtr manager)
    {
        return Environment.OSVersion.Platform switch
        {
            PlatformID.Win32NT => DiscordCreate_Windows(version, ref createParams, out manager),
            PlatformID.Unix => DiscordCreate_Linux(version, ref createParams, out manager),
            _ => throw new Exception("Your platform does not support the Discord SDK.")
        };
    }

    public delegate void SetLogHookHandler(LogLevel level, string message);

    private GCHandle SelfHandle;

    private IntPtr EventsPtr;

    private FFIEvents Events;

    private IntPtr ApplicationEventsPtr;

    private ApplicationManager.FFIEvents ApplicationEvents;

    internal ApplicationManager ApplicationManagerInstance;

    private IntPtr UserEventsPtr;

    private UserManager.FFIEvents UserEvents;

    internal UserManager UserManagerInstance;

    private IntPtr ImageEventsPtr;

    private ImageManager.FFIEvents ImageEvents;

    internal ImageManager ImageManagerInstance;

    private IntPtr ActivityEventsPtr;

    private ActivityManager.FFIEvents ActivityEvents;

    internal ActivityManager ActivityManagerInstance;

    private IntPtr RelationshipEventsPtr;

    private RelationshipManager.FFIEvents RelationshipEvents;

    internal RelationshipManager RelationshipManagerInstance;

    private IntPtr LobbyEventsPtr;

    private LobbyManager.FFIEvents LobbyEvents;

    internal LobbyManager LobbyManagerInstance;

    private IntPtr NetworkEventsPtr;

    private NetworkManager.FFIEvents NetworkEvents;

    internal NetworkManager NetworkManagerInstance;

    private IntPtr OverlayEventsPtr;

    private OverlayManager.FFIEvents OverlayEvents;

    internal OverlayManager OverlayManagerInstance;

    private IntPtr StorageEventsPtr;

    private StorageManager.FFIEvents StorageEvents;

    internal StorageManager StorageManagerInstance;

    private IntPtr StoreEventsPtr;

    private StoreManager.FFIEvents StoreEvents;

    internal StoreManager StoreManagerInstance;

    private IntPtr VoiceEventsPtr;

    private VoiceManager.FFIEvents VoiceEvents;

    internal VoiceManager VoiceManagerInstance;

    private IntPtr AchievementEventsPtr;

    private AchievementManager.FFIEvents AchievementEvents;

    internal AchievementManager AchievementManagerInstance;

    private IntPtr MethodsPtr;

    private Object MethodsStructure;

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

    private GCHandle? setLogHook;

    public Discord(Int64 clientId, UInt64 flags)
    {
        FFICreateParams createParams;
        createParams.ClientId = clientId;
        createParams.Flags = flags;
        Events = new FFIEvents();
        EventsPtr = Marshal.AllocHGlobal(Marshal.SizeOf(Events));
        createParams.Events = EventsPtr;
        SelfHandle = GCHandle.Alloc(this);
        createParams.EventData = GCHandle.ToIntPtr(SelfHandle);
        ApplicationEvents = new ApplicationManager.FFIEvents();
        ApplicationEventsPtr = Marshal.AllocHGlobal(Marshal.SizeOf(ApplicationEvents));
        createParams.ApplicationEvents = ApplicationEventsPtr;
        createParams.ApplicationVersion = 1;
        UserEvents = new UserManager.FFIEvents();
        UserEventsPtr = Marshal.AllocHGlobal(Marshal.SizeOf(UserEvents));
        createParams.UserEvents = UserEventsPtr;
        createParams.UserVersion = 1;
        ImageEvents = new ImageManager.FFIEvents();
        ImageEventsPtr = Marshal.AllocHGlobal(Marshal.SizeOf(ImageEvents));
        createParams.ImageEvents = ImageEventsPtr;
        createParams.ImageVersion = 1;
        ActivityEvents = new ActivityManager.FFIEvents();
        ActivityEventsPtr = Marshal.AllocHGlobal(Marshal.SizeOf(ActivityEvents));
        createParams.ActivityEvents = ActivityEventsPtr;
        createParams.ActivityVersion = 1;
        RelationshipEvents = new RelationshipManager.FFIEvents();
        RelationshipEventsPtr = Marshal.AllocHGlobal(Marshal.SizeOf(RelationshipEvents));
        createParams.RelationshipEvents = RelationshipEventsPtr;
        createParams.RelationshipVersion = 1;
        LobbyEvents = new LobbyManager.FFIEvents();
        LobbyEventsPtr = Marshal.AllocHGlobal(Marshal.SizeOf(LobbyEvents));
        createParams.LobbyEvents = LobbyEventsPtr;
        createParams.LobbyVersion = 1;
        NetworkEvents = new NetworkManager.FFIEvents();
        NetworkEventsPtr = Marshal.AllocHGlobal(Marshal.SizeOf(NetworkEvents));
        createParams.NetworkEvents = NetworkEventsPtr;
        createParams.NetworkVersion = 1;
        OverlayEvents = new OverlayManager.FFIEvents();
        OverlayEventsPtr = Marshal.AllocHGlobal(Marshal.SizeOf(OverlayEvents));
        createParams.OverlayEvents = OverlayEventsPtr;
        createParams.OverlayVersion = 2;
        StorageEvents = new StorageManager.FFIEvents();
        StorageEventsPtr = Marshal.AllocHGlobal(Marshal.SizeOf(StorageEvents));
        createParams.StorageEvents = StorageEventsPtr;
        createParams.StorageVersion = 1;
        StoreEvents = new StoreManager.FFIEvents();
        StoreEventsPtr = Marshal.AllocHGlobal(Marshal.SizeOf(StoreEvents));
        createParams.StoreEvents = StoreEventsPtr;
        createParams.StoreVersion = 1;
        VoiceEvents = new VoiceManager.FFIEvents();
        VoiceEventsPtr = Marshal.AllocHGlobal(Marshal.SizeOf(VoiceEvents));
        createParams.VoiceEvents = VoiceEventsPtr;
        createParams.VoiceVersion = 1;
        AchievementEvents = new AchievementManager.FFIEvents();
        AchievementEventsPtr = Marshal.AllocHGlobal(Marshal.SizeOf(AchievementEvents));
        createParams.AchievementEvents = AchievementEventsPtr;
        createParams.AchievementVersion = 1;
        InitEvents(EventsPtr, ref Events);
        var result = DiscordCreate(3, ref createParams, out MethodsPtr);
        if (result != Result.Ok)
        {
            Dispose();
            throw new ResultException(result);
        }
    }

    private void InitEvents(IntPtr eventsPtr, ref FFIEvents events)
    {
        Marshal.StructureToPtr(events, eventsPtr, false);
    }

    public void Dispose()
    {
        if (MethodsPtr != IntPtr.Zero)
        {
            Methods.Destroy(MethodsPtr);
        }
        SelfHandle.Free();
        Marshal.FreeHGlobal(EventsPtr);
        Marshal.FreeHGlobal(ApplicationEventsPtr);
        Marshal.FreeHGlobal(UserEventsPtr);
        Marshal.FreeHGlobal(ImageEventsPtr);
        Marshal.FreeHGlobal(ActivityEventsPtr);
        Marshal.FreeHGlobal(RelationshipEventsPtr);
        Marshal.FreeHGlobal(LobbyEventsPtr);
        Marshal.FreeHGlobal(NetworkEventsPtr);
        Marshal.FreeHGlobal(OverlayEventsPtr);
        Marshal.FreeHGlobal(StorageEventsPtr);
        Marshal.FreeHGlobal(StoreEventsPtr);
        Marshal.FreeHGlobal(VoiceEventsPtr);
        Marshal.FreeHGlobal(AchievementEventsPtr);
        if (setLogHook.HasValue)
        {
            setLogHook.Value.Free();
        }
    }

    public void RunCallbacks()
    {
        var res = Methods.RunCallbacks(MethodsPtr);
        if (res != Result.Ok)
        {
            throw new ResultException(res);
        }
    }

    [MonoPInvokeCallback]
    private static void SetLogHookCallbackImpl(IntPtr ptr, LogLevel level, string message)
    {
        GCHandle h = GCHandle.FromIntPtr(ptr);
        SetLogHookHandler callback = (SetLogHookHandler)h.Target;
        callback(level, message);
    }

    public void SetLogHook(LogLevel minLevel, SetLogHookHandler callback)
    {
        if (setLogHook.HasValue)
        {
            setLogHook.Value.Free();
        }
        setLogHook = GCHandle.Alloc(callback);
        Methods.SetLogHook(MethodsPtr, minLevel, GCHandle.ToIntPtr(setLogHook.Value), SetLogHookCallbackImpl);
    }

    public ApplicationManager GetApplicationManager()
    {
        if (ApplicationManagerInstance == null)
        {
            ApplicationManagerInstance = new ApplicationManager(
              Methods.GetApplicationManager(MethodsPtr),
              ApplicationEventsPtr,
              ref ApplicationEvents
            );
        }
        return ApplicationManagerInstance;
    }

    public UserManager GetUserManager()
    {
        if (UserManagerInstance == null)
        {
            UserManagerInstance = new UserManager(
              Methods.GetUserManager(MethodsPtr),
              UserEventsPtr,
              ref UserEvents
            );
        }
        return UserManagerInstance;
    }

    public ImageManager GetImageManager()
    {
        if (ImageManagerInstance == null)
        {
            ImageManagerInstance = new ImageManager(
              Methods.GetImageManager(MethodsPtr),
              ImageEventsPtr,
              ref ImageEvents
            );
        }
        return ImageManagerInstance;
    }

    public ActivityManager GetActivityManager()
    {
        if (ActivityManagerInstance == null)
        {
            ActivityManagerInstance = new ActivityManager(
              Methods.GetActivityManager(MethodsPtr),
              ActivityEventsPtr,
              ref ActivityEvents
            );
        }
        return ActivityManagerInstance;
    }

    public RelationshipManager GetRelationshipManager()
    {
        if (RelationshipManagerInstance == null)
        {
            RelationshipManagerInstance = new RelationshipManager(
              Methods.GetRelationshipManager(MethodsPtr),
              RelationshipEventsPtr,
              ref RelationshipEvents
            );
        }
        return RelationshipManagerInstance;
    }

    public LobbyManager GetLobbyManager()
    {
        if (LobbyManagerInstance == null)
        {
            LobbyManagerInstance = new LobbyManager(
              Methods.GetLobbyManager(MethodsPtr),
              LobbyEventsPtr,
              ref LobbyEvents
            );
        }
        return LobbyManagerInstance;
    }

    public NetworkManager GetNetworkManager()
    {
        if (NetworkManagerInstance == null)
        {
            NetworkManagerInstance = new NetworkManager(
              Methods.GetNetworkManager(MethodsPtr),
              NetworkEventsPtr,
              ref NetworkEvents
            );
        }
        return NetworkManagerInstance;
    }

    public OverlayManager GetOverlayManager()
    {
        if (OverlayManagerInstance == null)
        {
            OverlayManagerInstance = new OverlayManager(
              Methods.GetOverlayManager(MethodsPtr),
              OverlayEventsPtr,
              ref OverlayEvents
            );
        }
        return OverlayManagerInstance;
    }

    public StorageManager GetStorageManager()
    {
        if (StorageManagerInstance == null)
        {
            StorageManagerInstance = new StorageManager(
              Methods.GetStorageManager(MethodsPtr),
              StorageEventsPtr,
              ref StorageEvents
            );
        }
        return StorageManagerInstance;
    }

    public StoreManager GetStoreManager()
    {
        if (StoreManagerInstance == null)
        {
            StoreManagerInstance = new StoreManager(
              Methods.GetStoreManager(MethodsPtr),
              StoreEventsPtr,
              ref StoreEvents
            );
        }
        return StoreManagerInstance;
    }

    public VoiceManager GetVoiceManager()
    {
        if (VoiceManagerInstance == null)
        {
            VoiceManagerInstance = new VoiceManager(
              Methods.GetVoiceManager(MethodsPtr),
              VoiceEventsPtr,
              ref VoiceEvents
            );
        }
        return VoiceManagerInstance;
    }

    public AchievementManager GetAchievementManager()
    {
        if (AchievementManagerInstance == null)
        {
            AchievementManagerInstance = new AchievementManager(
              Methods.GetAchievementManager(MethodsPtr),
              AchievementEventsPtr,
              ref AchievementEvents
            );
        }
        return AchievementManagerInstance;
    }
}

