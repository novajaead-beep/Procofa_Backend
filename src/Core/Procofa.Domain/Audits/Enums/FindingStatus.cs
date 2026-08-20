namespace Procofa.Domain.Audits.Enums;

/// <summary>
/// Máquina de estados de un hallazgo (HU-16 a HU-19):
/// Open → InProgress → InReview → Closed
///                          └──────→ Rejected → InProgress (Reopen)
/// </summary>
public enum FindingStatus
{
    Open = 1,
    InProgress = 2,
    InReview = 3,
    Closed = 4,
    Rejected = 5
}
