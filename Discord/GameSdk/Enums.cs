namespace PvPAdventure.Discord.GameSdk;

public enum Result
{
    Ok = 0,
    ServiceUnavailable = 1,
    InvalidVersion = 2,
    LockFailed = 3,
    InternalError = 4,
    InvalidPayload = 5,
    InvalidCommand = 6,
    InvalidPermissions = 7,
    NotFetched = 8,
    NotFound = 9,
    Conflict = 10,
    InvalidSecret = 11,
    InvalidJoinSecret = 12,
    NoEligibleActivity = 13,
    InvalidInvite = 14,
    NotAuthenticated = 15,
    InvalidAccessToken = 16,
    ApplicationMismatch = 17,
    InvalidDataUrl = 18,
    InvalidBase64 = 19,
    NotFiltered = 20,
    LobbyFull = 21,
    InvalidLobbySecret = 22,
    InvalidFilename = 23,
    InvalidFileSize = 24,
    InvalidEntitlement = 25,
    NotInstalled = 26,
    NotRunning = 27,
    InsufficientBuffer = 28,
    PurchaseCanceled = 29,
    InvalidGuild = 30,
    InvalidEvent = 31,
    InvalidChannel = 32,
    InvalidOrigin = 33,
    RateLimited = 34,
    OAuth2Error = 35,
    SelectChannelTimeout = 36,
    GetGuildTimeout = 37,
    SelectVoiceForceRequired = 38,
    CaptureShortcutAlreadyListening = 39,
    UnauthorizedForAchievement = 40,
    InvalidGiftCode = 41,
    PurchaseError = 42,
    TransactionAborted = 43,
    DrawingInitFailed = 44,
}

public enum CreateFlags
{
    Default = 0,
    NoRequireDiscord = 1,
}

public enum LogLevel
{
    Error = 1,
    Warn,
    Info,
    Debug,
}

public enum UserFlag
{
    Partner = 2,
    HypeSquadEvents = 4,
    HypeSquadHouse1 = 64,
    HypeSquadHouse2 = 128,
    HypeSquadHouse3 = 256,
}

public enum PremiumType
{
    None = 0,
    Tier1 = 1,
    Tier2 = 2,
}

public enum ImageType
{
    User,
}

public enum ActivityPartyPrivacy
{
    Private = 0,
    Public = 1,
}

public enum ActivityType
{
    Playing,
    Streaming,
    Listening,
    Watching,
}

public enum ActivityActionType
{
    Join = 1,
    Spectate,
}

public enum ActivitySupportedPlatformFlags
{
    Desktop = 1,
    Android = 2,
    iOS = 4,
}

public enum ActivityJoinRequestReply
{
    No,
    Yes,
    Ignore,
}

public enum Status
{
    Offline = 0,
    Online = 1,
    Idle = 2,
    DoNotDisturb = 3,
}

public enum RelationshipType
{
    None,
    Friend,
    Blocked,
    PendingIncoming,
    PendingOutgoing,
    Implicit,
}

public enum LobbyType
{
    Private = 1,
    Public,
}

public enum LobbySearchComparison
{
    LessThanOrEqual = -2,
    LessThan,
    Equal,
    GreaterThan,
    GreaterThanOrEqual,
    NotEqual,
}

public enum LobbySearchCast
{
    String = 1,
    Number,
}

public enum LobbySearchDistance
{
    Local,
    Default,
    Extended,
    Global,
}

public enum KeyVariant
{
    Normal,
    Right,
    Left,
}

public enum MouseButton
{
    Left,
    Middle,
    Right,
}

public enum EntitlementType
{
    Purchase = 1,
    PremiumSubscription,
    DeveloperGift,
    TestModePurchase,
    FreePurchase,
    UserGift,
    PremiumPurchase,
}

public enum SkuType
{
    Application = 1,
    DLC,
    Consumable,
    Bundle,
}

public enum InputModeType
{
    VoiceActivity = 0,
    PushToTalk,
}
