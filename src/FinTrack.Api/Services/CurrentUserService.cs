using System.Security.Claims;
using FinTrack.Application.Interfaces.Services;

namespace FinTrack.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            return Guid.Parse(userId);
        }
    }
}