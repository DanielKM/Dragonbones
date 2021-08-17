namespace RTSEngine.Multiplayer.Utilities
{
    public enum DisconnectionReason
    {
        normal,

        timeout,

        // Lobby related
        lobbyNotFound,
        gameCodeMismatch,
        lobbyHostKick,
        lobbyNotAvailable,
        lobbyAlreadyStarting,

        // Server related,
        nextHostNotFound,
        serverKick,
    }
}
