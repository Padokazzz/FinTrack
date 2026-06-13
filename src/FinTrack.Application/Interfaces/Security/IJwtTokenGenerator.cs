using FinTrack.Domain.Entities;

namespace FinTrack.Application.Interfaces.Security;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}