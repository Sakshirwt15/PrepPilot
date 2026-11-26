using System.Security.Claims;

namespace PrepPilot.Api;

public static class ClaimsExtensions
{
    /// <summary>Id of the signed-in user. Only call from [Authorize] endpoints.</summary>
    public static int GetUserId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? throw new InvalidOperationException("Missing user id claim."));

    /// <summary>Id of the signed-in user, or null for anonymous callers.</summary>
    public static int? TryGetUserId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;
}
