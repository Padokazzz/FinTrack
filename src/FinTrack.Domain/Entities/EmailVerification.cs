namespace FinTrack.Domain.Entities;

public class EmailVerification
{
    public Guid UserId { get; set; }
    public Guid? ChallengeId { get; set; }
    public string? CodeHash { get; set; }
    public DateTime? SentAt { get; set; }
    public int Attempts { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
}
