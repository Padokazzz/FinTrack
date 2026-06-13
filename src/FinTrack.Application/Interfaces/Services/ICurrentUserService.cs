namespace FinTrack.Application.Interfaces.Services;

public interface ICurrentUserService
{
    Guid UserId { get; }
}