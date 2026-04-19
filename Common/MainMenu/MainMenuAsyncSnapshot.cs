using System;

namespace PvPAdventure.Common.MainMenu;

internal sealed class MainMenuAsyncSnapshot<TSnapshot>
{
    private readonly object gate = new();
    private TSnapshot snapshot;
    private int requestVersion;

    public MainMenuAsyncSnapshot(TSnapshot initialSnapshot)
    {
        snapshot = initialSnapshot;
    }

    public TSnapshot Current
    {
        get
        {
            lock (gate)
                return snapshot;
        }
    }

    public bool IsLoading { get; private set; }

    public int BeginRefresh()
    {
        lock (gate)
        {
            IsLoading = true;
            return ++requestVersion;
        }
    }

    public bool TrySetSnapshot(int version, TSnapshot nextSnapshot)
    {
        lock (gate)
        {
            if (version != requestVersion)
                return false;

            snapshot = nextSnapshot;
            IsLoading = false;
            return true;
        }
    }

    public bool TryFinishWithoutReplacing(int version)
    {
        lock (gate)
        {
            if (version != requestVersion)
                return false;

            IsLoading = false;
            return true;
        }
    }
}
