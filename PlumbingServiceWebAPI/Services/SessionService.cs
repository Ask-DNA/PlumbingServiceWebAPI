using Microsoft.EntityFrameworkCore;
using PlumbingServiceWebAPI.Models;
using System.Security.Claims;

namespace PlumbingServiceWebAPI.Services
{
    public class SessionService
    {
        public async Task<User?> GetClientAsync(HttpContext context, ApplicationContext db)
        {
            string id = "";
            var ID = context.User.FindFirst(ClaimsIdentity.DefaultNameClaimType);
            //var RoleName = HttpContext.User.FindFirst(ClaimsIdentity.DefaultRoleClaimType);
            if (ID != null)
                id = ID.Value;
            return await db.Users.FirstOrDefaultAsync(u => u.ID == id);
        }
    }
}
