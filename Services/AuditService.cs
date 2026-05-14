using System.Security.Claims;
using EmployeeTrainer.Models;

namespace EmployeeTrainer.Services;

public class AuditService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public AuditLog CreateLogEntry(string action, string entityType, int? entityId = null, string? details = null)
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return new AuditLog
        {
            UserId = userIdClaim != null ? int.Parse(userIdClaim) : null,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            Timestamp = DateTime.UtcNow,
            IpAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString()
        };
    }
}