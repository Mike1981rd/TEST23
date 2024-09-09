using System.Security.Claims;
using System.Security.Principal;

namespace AuroraPOS.Models
{
    public static class IdentityExtensions
    {

		public static string GetUserName(this IIdentity identity)
		{
			ClaimsIdentity claimsIdentity = identity as ClaimsIdentity;
			Claim claim = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier);

			return claim?.Value ?? string.Empty;
		}
		public static string GetName(this IIdentity identity)
        {
            ClaimsIdentity claimsIdentity = identity as ClaimsIdentity;
            Claim claim = claimsIdentity?.FindFirst(ClaimTypes.GivenName);

            return claim?.Value ?? string.Empty;
        }
        public static string GetRole(this IIdentity identity)
        {
            ClaimsIdentity claimsIdentity = identity as ClaimsIdentity;
            Claim claim = claimsIdentity?.FindFirst(ClaimTypes.Role);

            return claim?.Value ?? string.Empty;
        }
    }
}
