using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

using PlumbingServiceWebAPI.Services;
using PlumbingServiceWebAPI.Models;

namespace PlumbingServiceWebAPI.Controllers
{
    public class UserController : Controller
    {
        readonly ApplicationContext db;
        readonly SessionService session;

        public UserController(ApplicationContext applicationContext, SessionService sessionService)
        {
            db = applicationContext;
            session = sessionService;
        }

        [HttpPost]
        public async Task<IActionResult> Login(string? returnUrl, [FromBody] UserFilter loginData)
        {
            if (loginData.Email == null || loginData.Password == null)
                return new BadRequestObjectResult("Login or password is not defined");

            User? user = await db.Users.FirstOrDefaultAsync(u => u.Email == loginData.Email && u.Password == loginData.Password);
            if (user is null)
                return new BadRequestObjectResult("Invalid login or password");

            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, user.ID),
                new Claim(ClaimsIdentity.DefaultRoleClaimType, user.RoleName)
            };
            var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            await HttpContext.SignInAsync(claimsPrincipal);

            return new RedirectResult(returnUrl ?? "/");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return new OkResult();
        }

        [HttpPut]
        [Authorize]
        public async Task<IActionResult> GetUser([FromBody] UserFilter filter)
        {
            User? client = await session.GetClientAsync(HttpContext, db);
            if (client == null)
                return new BadRequestObjectResult("Invalid session");

            if (filter.ID == null && filter.Email == null)
                return new BadRequestObjectResult("Invalid filter options");

            List<User> users;
            users = client!.RoleName switch
            {
                "Administrator" => await db.Users.Where(
                    u => u.RoleName == "Administrator" || u.RoleName == "Support" || u.RoleName == "Manager").ToListAsync(),
                "Support" => await db.Users.Where(
                    u => u.RoleName == "User" || u.ID == client.ID).ToListAsync(),
                _ => await db.Users.Where(u => u.ID == client.ID).ToListAsync(),
            };
            User? result = users.FirstOrDefault(filter.Fits);
            if (result == null)
                return new NotFoundResult();

            return new JsonResult(result);
        }

        [HttpPut]
        [Authorize(Roles = "Administrator, Support")]
        public async Task<IActionResult> GetUsers([FromBody] UserFilter filter)
        {
            User? client = await session.GetClientAsync(HttpContext, db);
            if (client == null)
                return new BadRequestObjectResult("Invalid session");

            List<User> result;
            result = client!.RoleName switch
            {
                "Administrator" => await db.Users.Where(
                    u => u.RoleName == "Administrator" || u.RoleName == "Support" || u.RoleName == "Manager").ToListAsync(),
                "Support" => await db.Users.Where(
                    u => u.RoleName == "User" || u.ID == client.ID).ToListAsync(),
                _ => new(),
            };
            
            result = result.Where(filter.Fits).ToList();
            if (result.Count == 0)
                return new NotFoundResult();

            return new JsonResult(result);
        }

        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> DeleteUser([FromBody] UserFilter filter, [FromServices] NotificationService emailSender)
        {
            User? client = await session.GetClientAsync(HttpContext, db);
            if (client == null)
                return new BadRequestObjectResult("Invalid session");

            if (filter.ID == null && filter.Email == null)
                return new BadRequestObjectResult("Invalid filter options");

            List<User> users;
            users = client!.RoleName switch
            {
                "Administrator" => await db.Users.Where(
                    u => u.RoleName == "Administrator" || u.RoleName == "Support" || u.RoleName == "Manager")
                    .ToListAsync(),
                "Support" => await db.Users.Where(
                    u => u.RoleName == "User" || u.ID == client.ID)
                    .ToListAsync(),
                _ => await db.Users.Where(u => u.ID == client.ID).ToListAsync(),
            };
            User? result = users.FirstOrDefault(filter.Fits);
            if (result == null)
                return new NotFoundResult();

            if (!await ValidateDeletion(result))
                return new BadRequestObjectResult("Deletion is forbidden");

            db.Users.Remove(result);
            try { await db.SaveChangesAsync(); }
            catch(Exception ex) { return new BadRequestObjectResult(ex.Message); }

            if (result.RoleName == "User")
                await emailSender.UserAccountDeleted(result);

            return new OkObjectResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] UserPreset preset, [FromServices] NotificationService emailSender)
        {
            User? client = await session.GetClientAsync(HttpContext, db);
            string clientRole = client?.RoleName ?? "Guest";
            if (clientRole == "Manager" || clientRole == "User")
                return new RedirectResult("/AccessDenied");

            User? create = preset.Create();
            if (create == null)
                return new BadRequestObjectResult("Invalid preset");

            if ((clientRole == "Guest" || clientRole == "Support") && create.RoleName != "User")
                return new BadRequestObjectResult("Invalid role for this action");
            if (clientRole == "Administrator" && create.RoleName == "User")
                return new BadRequestObjectResult("Invalid role for this action");

            User? checkID = await db.Users.FirstOrDefaultAsync(u => u.ID == create.ID);
            if (checkID != null)
                return new BadRequestObjectResult("User with such ID was already created");
            User? checkEmail = await db.Users.FirstOrDefaultAsync(u => u.Email == create.Email);
            if (checkEmail != null)
                return new BadRequestObjectResult("User with such email was already created");

            db.Users.Add(create);
            try { await db.SaveChangesAsync(); }
            catch (Exception ex) { return new BadRequestObjectResult(ex.Message); }

            if (create.RoleName == "User")
                await emailSender.UserAccountCreated(create);

            return new OkObjectResult(create);
        }

        [HttpPut]
        [Authorize]
        public async Task<IActionResult> EditUser([FromBody] UserPreset preset, [FromServices] NotificationService emailSender)
        {
            User? client = await session.GetClientAsync(HttpContext, db);
            if (client == null)
                return new BadRequestObjectResult("Invalid session");

            if (preset.ID == null)
                return new BadRequestObjectResult("Invalid preset");

            User? baseUser = await db.Users.FirstOrDefaultAsync(u => u.ID == preset.ID);
            if (baseUser == null)
                return new NotFoundResult();

            switch (client!.RoleName)
            {
                case "Administrator":
                    if (baseUser!.RoleName == "User")
                        return new BadRequestObjectResult("Invalid role for this action");
                    break;
                case "Support":
                    if (baseUser!.RoleName != "User" && baseUser!.ID != client.ID)
                        return new BadRequestObjectResult("Invalid role for this action");
                    break;
                default:
                    if (baseUser!.ID != client.ID)
                        return new BadRequestObjectResult("Invalid role for this action");
                    break;
            }

            User? newUser = preset.Edit(baseUser);
            if (newUser == null)
                return new BadRequestObjectResult("Invalid preset");

            User? checkEmail = await db.Users.FirstOrDefaultAsync(u => u.Email == newUser.Email && u.ID != newUser.ID);
            if (checkEmail != null)
                return new BadRequestObjectResult("User with such email was already created");

            db.Users.Update(newUser);
            try { await db.SaveChangesAsync(); }
            catch (Exception ex) { return new BadRequestObjectResult(ex.Message); }

            if (newUser.RoleName == "Manager")
                await emailSender.ManagerAccountChanged(newUser, db);

            return new OkObjectResult(newUser);
        }

        [NonAction]
        private async Task<bool> ValidateDeletion(User del)
        {
            if (del.RoleName == "User")
            {
                var check = await db.Orders.FirstOrDefaultAsync(o => o.UserID == del.ID);
                return check == null;
            }
            if (del.RoleName == "Manager")
            {
                var check = await db.Employees.FirstOrDefaultAsync(o => o.ManagerID == del.ID);
                return check == null;
            }
            return true;
        }
    }
}
