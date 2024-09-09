using AuroraPOS.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace AuroraPOS.Permissions
{
    internal class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly AppDbContext _dbcontext;
        public PermissionAuthorizationHandler(AppDbContext context)
        {
            _dbcontext = context;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            if (context.User == null)
            {
                return;
            }

            //if (context.User.IsInRole("Admin") || context.User.IsInRole("ADMIN"))
            //    context.Succeed(requirement);

            var claims = context.User.Claims;
            var permissionss = claims.Where(x => x.Type == "Permission" && x.Value == requirement.Permission &&
                                                            x.Issuer == "LOCAL AUTHORITY");
            if (permissionss.Any())
            {
                context.Succeed(requirement);
                return;
            }
        }
    }
}
