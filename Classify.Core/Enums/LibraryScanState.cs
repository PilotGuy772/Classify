namespace Classify.Core.Enums;

public enum LibraryScanState
{
    Idle,
    Scanning,
    AwaitingUserInput,
    Completed,
    Canceled,
    Failed
}