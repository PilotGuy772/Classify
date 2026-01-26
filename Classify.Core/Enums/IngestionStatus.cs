namespace Classify.Core.Enums;

public enum IngestionStatus
{
    /// <summary>
    /// File has been found by the walker and entered into the database.
    /// </summary>
    Seen,
    /// <summary>
    /// A music metadata match has been proposed for consideration. The proposed match may be empty if no suitable match was found, with confidence zero.
    /// </summary>
    MatchProposed,
    /// <summary>
    /// If the proposed match is of low confidence, the matched data is waiting for user confirmation or manual entry.
    /// </summary>
    AwaitingUserInput,
    /// <summary>
    /// The user has confirmed the input. If this is the current status, a MatchProposal should be present with a 100% confidence.
    /// </summary>
    UserConfirmed,
    /// <summary>
    /// The matched data has been confirmed by the user, or confidence in suggestions is high. All necessary supporting types have been filled out and added to the DB.
    /// </summary>
    Complete,
    /// <summary>
    /// The process failed. There should be an error message associated with this. Some day.
    /// </summary>
    Failed
}