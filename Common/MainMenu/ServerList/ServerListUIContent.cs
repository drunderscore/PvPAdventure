namespace PvPAdventure.Common.MainMenu.ServerList;

internal readonly record struct ServerListUIContent(ServerEntryContent[] Entries);

internal readonly record struct ServerEntryContent(
    string IP,
    int Port,
    int Players,
    int MaxPlayers,
    bool Status);
