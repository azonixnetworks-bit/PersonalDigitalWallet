using System.Security.Claims;

namespace PersonalDigitalVault.Api.Security;

public class CurrentUserService
{
    // =========================================================
    // HTTP CONTEXT ACCESSOR
    // =========================================================

    private readonly IHttpContextAccessor _accessor;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    // Function:
    // Current HTTP request user claims access panna.
    //
    // Input:
    // IHttpContextAccessor
    //
    // Reason:
    // JWT-la irukkura UserId / Role
    // service layer-la safely read panna.
    //
    // Output:
    // Accessor private variable-la store aagum.
    public CurrentUserService(
        IHttpContextAccessor accessor)
    {
        _accessor =
            accessor;
    }


    // =========================================================
    // CURRENT USER ID
    // =========================================================

    // Function:
    // Current authenticated user's database Id return pannum.
    //
    // Input:
    // JWT NameIdentifier claim.
    //
    // Output:
    // Positive integer UserId.
    //
    // Security:
    // Missing / invalid / unauthenticated claim-na
    // unauthorized exception throw pannum.
    public int UserId
    {
        get
        {
            ClaimsPrincipal user =
                GetAuthenticatedPrincipal();


            string? userIdText =
                user.FindFirstValue(
                    ClaimTypes.NameIdentifier);


            if (!int.TryParse(
                    userIdText,
                    out int userId))
            {
                throw new UnauthorizedAccessException(
                    "User is not authenticated.");
            }


            // Database user Id 0 / negative valid illa.
            if (userId <= 0)
            {
                throw new UnauthorizedAccessException(
                    "User is not authenticated.");
            }


            return userId;
        }
    }


    // =========================================================
    // CURRENT USER ROLE
    // =========================================================

    // Function:
    // Current authenticated user's role return pannum.
    //
    // Input:
    // JWT Role claim.
    //
    // Output:
    // User / Admin.
    //
    // Security:
    // Missing role-a empty string-aa silently return panna maatom.
    public string Role
    {
        get
        {
            ClaimsPrincipal user =
                GetAuthenticatedPrincipal();


            string? role =
                user.FindFirstValue(
                    ClaimTypes.Role);


            if (string.IsNullOrWhiteSpace(
                    role))
            {
                throw new UnauthorizedAccessException(
                    "User is not authenticated.");
            }


            return role;
        }
    }


    // =========================================================
    // AUTHENTICATED PRINCIPAL
    // =========================================================

    // Function:
    // Current request authenticated identity irukkaa
    // central-a check pannum.
    //
    // Input:
    // Current HttpContext.
    //
    // Output:
    // Authenticated ClaimsPrincipal.
    //
    // Security:
    // HttpContext missing / identity unauthenticated-na
    // authorization logic continue panna koodathu.
    private ClaimsPrincipal GetAuthenticatedPrincipal()
    {
        var httpContext =
            _accessor.HttpContext;


        if (httpContext == null)
        {
            throw new UnauthorizedAccessException(
                "User is not authenticated.");
        }


        ClaimsPrincipal user =
            httpContext.User;


        if (user.Identity == null ||
            !user.Identity.IsAuthenticated)
        {
            throw new UnauthorizedAccessException(
                "User is not authenticated.");
        }


        return user;
    }
}