using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlumbingServiceWebAPI.Models;
using PlumbingServiceWebAPI.Services;

namespace PlumbingServiceWebAPI.Controllers
{
    public class ShiftController : Controller
    {
        readonly ApplicationContext db;
        readonly SessionService session;

        public ShiftController(ApplicationContext applicationContext, SessionService sessionService)
        {
            db = applicationContext;
            session = sessionService;
        }

        [HttpPut]
        [Authorize(Roles = "Administrator, Manager")]
        public async Task<IActionResult> GetShift([FromBody] ShiftFilter filter)
        {
            User? client = await session.GetClientAsync(HttpContext, db);
            if (client == null)
                return new BadRequestObjectResult("Invalid session");

            if (filter.ID == null || !filter.Validate())
                return new BadRequestObjectResult("Invalid filter options");

            List<Shift> shifts;
            shifts = client!.RoleName switch
            {
                "Administrator" => await db.Shifts.Include(s => s.Employee).ToListAsync(),
                "Manager" => await db.Shifts
                    .Include(s => s.Employee)
                    .Where(s => s.Employee!.ManagerID == client.ID)
                    .ToListAsync(),
                _ => new(),
            };
            Shift? result = shifts.FirstOrDefault(filter.Fits);
            if (result == null)
                return new NotFoundResult();

            return new JsonResult(result);
        }

        [HttpPut]
        [Authorize(Roles = "Administrator, Manager")]
        public async Task<IActionResult> GetShifts([FromBody] ShiftFilter filter)
        {
            User? client = await session.GetClientAsync(HttpContext, db);
            if (client == null)
                return new BadRequestObjectResult("Invalid session");

            if (!filter.Validate())
                return new BadRequestObjectResult("Invalid filter options");

            List<Shift> result;
            result = client!.RoleName switch
            {
                "Administrator" => await db.Shifts.Include(s => s.Employee).ToListAsync(),
                "Manager" => await db.Shifts
                    .Include(s => s.Employee)
                    .Where(s => s.Employee!.ManagerID == client.ID)
                    .ToListAsync(),
                _ => new(),
            };
            result = result.Where(filter.Fits).ToList();
            if (result == null)
                return new NotFoundResult();

            return new JsonResult(result);
        }

        [HttpDelete]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteShift([FromBody] ShiftFilter filter)
        {
            if (filter.ID == null || !filter.Validate())
                return new BadRequestObjectResult("Invalid filter options");

            List<Shift> shifts = await db.Shifts.Include(s => s.Employee).ToListAsync();
            Shift? result = shifts.FirstOrDefault(filter.Fits);
            
            if (result == null)
                return new NotFoundResult();

            if (ValidateDeletion(result))
                return new BadRequestObjectResult("Deletion is forbidden");

            db.Shifts.Remove(result);
            try { await db.SaveChangesAsync(); }
            catch (Exception ex) { return new BadRequestObjectResult(ex.Message); }

            return new OkObjectResult(result);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> CreateShift([FromBody] ShiftPreset preset)
        {
            Shift? shift = preset.Create();
            if (shift == null)
                return new BadRequestObjectResult("Invalid preset");

            if (!await ValidateCreation(shift))
                return new BadRequestObjectResult("Creation is forbidden");

            Employee? employee = await db.Employees.FirstOrDefaultAsync(e => e.ID == shift.EmployeeID);
            if (employee == null)
                return new NotFoundResult();

            db.Shifts.Add(shift);
            try { await db.SaveChangesAsync(); }
            catch (Exception ex) { return new BadRequestObjectResult(ex.Message); }

            return new OkObjectResult(shift);
        }

        [NonAction]
        private static bool ValidateDeletion(Shift del)
        {
            DateTime startDay = new(del.Start.Year, del.Start.Month, del.Start.Day, 0, 0, 0);
            DateTime today = DateTime.Today;
            return today.AddDays(7) < startDay;
        }

        [NonAction]
        private async Task<bool> ValidateCreation(Shift create)
        {
            double hours = (create.End - create.Start).TotalHours;
            if (hours < 1 || hours > 8)
                return false;

            List<Shift> shifts = await db.Shifts.Where(s => s.EmployeeID == create.EmployeeID).ToListAsync();
            if (shifts.Count == 0)
                return true;
            
            foreach (Shift shift in shifts)
            {
                if (shift.Start >= create.Start && shift.Start < create.End)
                    return false;
                if (shift.End > create.Start && shift.End <= create.End)
                    return false;
            }
            return true;
        }
    }
}
