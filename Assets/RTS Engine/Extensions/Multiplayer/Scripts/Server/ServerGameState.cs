namespace RTSEngine.Multiplayer.Server
{
    public enum ServerGameState
    {
        initial = 0,
        awaitingValidation,

        simRunning,
        simPaused
    }
}