namespace WangDesk.App.Services;

public sealed class ReminderTriggeredEventArgs : EventArgs
{
    public ReminderTriggeredEventArgs(PomodoroMode completedMode)
    {
        CompletedMode = completedMode;
    }

    public PomodoroMode CompletedMode { get; }
}
