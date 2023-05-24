using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlumbingServiceWebAPI.Models;
using PlumbingServiceWebAPI.Services;

namespace PlumbingServiceWebAPI.Controllers
{
    public class EmployeeController : Controller
    {
        readonly ApplicationContext db;
        readonly SessionService session;

        public EmployeeController(ApplicationContext applicationContext, SessionService sessionService)
        {
            db = applicationContext;
            session = sessionService;
        }

        [HttpPut]
        [Authorize(Roles = "Administrator, Manager")]
        public async Task<IActionResult> GetEmloyee([FromBody] EmployeeFilter filter)
        {
            User? client = await session.GetClientAsync(HttpContext, db);
            if (client == null)
                return new BadRequestObjectResult("Invalid session");

            if (filter.ID == null)
                return new BadRequestObjectResult("Invalid filter options");

            List<Employee> employees;
            employees = client!.RoleName switch
            {
                "Administrator" => await db.Employees.ToListAsync(),
                "Manager" => await db.Employees.Where(e => e.ManagerID == client.ID).ToListAsync(),
                _ => new(),
            };
            Employee? result = employees.FirstOrDefault(filter.Fits);
            if (result == null)
                return new NotFoundResult();

            return new JsonResult(result);
        }

        [HttpPut]
        [Authorize(Roles = "Administrator, Manager")]
        public async Task<IActionResult> GetEmloyees([FromBody] EmployeeFilter filter)
        {
            User? client = await session.GetClientAsync(HttpContext, db);
            if (client == null)
                return new BadRequestObjectResult("Invalid session");

            List<Employee> result;
            result = client!.RoleName switch
            {
                "Administrator" => await db.Employees.ToListAsync(),
                "Manager" => await db.Employees.Where(e => e.ManagerID == client.ID).ToListAsync(),
                _ => new(),
            };
            result = result.Where(filter.Fits).ToList();
            if (result.Count == 0)
                return new NotFoundResult();

            return new JsonResult(result);
        }

        [HttpDelete]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteEmloyee([FromBody] EmployeeFilter filter)
        {
            User? client = await session.GetClientAsync(HttpContext, db);
            if (client == null)
                return new BadRequestObjectResult("Invalid session");

            if (filter.ID == null)
                return new BadRequestObjectResult("Invalid filter options");

            List<Employee> employees;
            employees = await db.Employees.ToListAsync();
            Employee? result = employees.FirstOrDefault(filter.Fits);
            if (result == null)
                return new NotFoundResult();

            if (!await ValidateDeletion(result))
                return new BadRequestObjectResult("Deletion is forbidden");

            db.Employees.Remove(result);
            try { await db.SaveChangesAsync(); }
            catch (Exception ex) { return new BadRequestObjectResult(ex.Message); }

            return new OkObjectResult(result);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> CreateEmloyee([FromBody] EmployeePreset preset)
        {
            if (!preset.ValidateForCreation())
                return new BadRequestObjectResult("Invalid preset");

            Employee employee = preset.Create()!;

            Employee? duplicate = await db.Employees.FirstOrDefaultAsync(e => e.ID == employee.ID);

            if (duplicate != null)
                return new BadRequestObjectResult("Employee was already created");

            db.Employees.Add(employee);
            try { await db.SaveChangesAsync(); }
            catch (Exception ex) { return new BadRequestObjectResult(ex.Message); }

            return new OkObjectResult(employee);
        }

        [HttpPut]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> EditEmloyee([FromBody] EmployeePreset preset, [FromServices] NotificationService emailSender)
        {
            if (!preset.ValidateForEdition())
                return new BadRequestObjectResult("Invalid preset");

            Employee employee = preset.Create()!;

            Employee? check = await db.Employees.FirstOrDefaultAsync(e => e.ID == employee.ID);

            if (check == null)
                return new NotFoundResult();

            db.Employees.Update(employee);
            try { await db.SaveChangesAsync(); }
            catch (Exception ex) { return new BadRequestObjectResult(ex.Message); }

            await emailSender.EmployeeChanged(employee, db);

            return new OkObjectResult(employee);
        }

        [HttpPut]
        [Authorize(Roles = "Administrator, Manager")]
        public async Task<IActionResult> GetSchedule([FromBody] EmployeeFilter filter)
        {
            User? client = await session.GetClientAsync(HttpContext, db);
            if (client == null)
                return new BadRequestObjectResult("Invalid session");

            if (filter.ID == null)
                return new BadRequestObjectResult("Invalid filter options");

            List<Employee> employees;
            employees = client!.RoleName switch
            {
                "Administrator" => await db.Employees.Include(e => e.CurrentOrders).ToListAsync(),
                "Manager" => await db.Employees.Include(e => e.CurrentOrders).Where(e => e.ManagerID == client.ID).ToListAsync(),
                _ => new(),
            };
            Employee? employee = employees.FirstOrDefault(filter.Fits);
            if (employee == null)
                return new NotFoundResult();

            return new JsonResult(employee.CurrentOrders);
        }

        [NonAction]
        private async Task<bool> ValidateDeletion(Employee del)
        {
            var check = await db.Orders.FirstOrDefaultAsync(o => o.EmployeeID == del.ID);
            return check == null;
        }
    }
}
