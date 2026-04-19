namespace PvPAdventure.Common.MainMenu.ServerList;

internal static class ServerListExampleContent
{
    public static ServerListUIContent Create()
    {
        return new ServerListUIContent(
        [
            new ServerEntryContent("127.0.0.1", 5555, 0, 16, true),
            new ServerEntryContent("127.0.0.1", 7777, 5, 16, true),
            new ServerEntryContent("127.0.0.2", 5555, 3, 16, false),
            new ServerEntryContent("127.0.0.3", 5555, 11, 16, true),
            new ServerEntryContent("eu.tpvpa.net", 7777, 16, 16, true),
            new ServerEntryContent("dev.tpvpa.net", 5555, 1, 8, false)
        ]);
    }
}
