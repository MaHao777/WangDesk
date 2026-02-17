namespace WangDesk.App.Services;

public sealed class PomodoroSessionEndedEventArgs : EventArgs
{
    public PomodoroSessionEndedEventArgs(
        PomodoroMode mode,
        DateTime startedAtLocal,
        DateTime endedAtLocal,
        TimeSpan elapsed)
    {
        Mode = mode;
        StartedAtLocal = startedAtLocal;
        EndedAtLocal = endedAtLocal;
        Elapsed = elapsed;
    }

    public PomodoroMode Mode { get; }

    public DateTime StartedAtLocal { get; }

    public DateTime EndedAtLocal { get; }

    public TimeSpan Elapsed { get; }
}
