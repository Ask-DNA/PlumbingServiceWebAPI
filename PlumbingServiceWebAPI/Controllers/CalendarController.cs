using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlumbingServiceWebAPI.Models;
using System.Data;

namespace PlumbingServiceWebAPI.Controllers
{
    public class CalendarController : Controller
    {
        readonly ApplicationContext db;

        public CalendarController(ApplicationContext applicationContext)
        {
            db = applicationContext;
        }

        [HttpPut]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> GetCalendarException([FromBody] CalendarExceptionFilter filter)
        {
            if (filter.ExceptionDate == null || !filter.Validate())
                return new BadRequestObjectResult("Invalid filter options");

            List<CalendarException> exceptions = await db.CalendarExceptions.ToListAsync();
            CalendarException? result = exceptions.FirstOrDefault(filter.Fits);

            if (result == null)
                return new NotFoundResult();

            return new JsonResult(result);
        }

        [HttpPut]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> GetCalendarExceptions([FromBody] CalendarExceptionFilter filter)
        {
            if (!filter.Validate())
                return new BadRequestObjectResult("Invalid filter options");

            List<CalendarException> result = await db.CalendarExceptions.ToListAsync();
            result = result.Where(filter.Fits).ToList();

            if (result.Count == 0)
                return new NotFoundResult();

            return new JsonResult(result);
        }

        [HttpDelete]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteCalendarException([FromBody] CalendarExceptionFilter filter)
        {
            if (filter.ExceptionDate == null || !filter.Validate())
                return new BadRequestObjectResult("Invalid filter options");

            List<CalendarException> exceptions = await db.CalendarExceptions.ToListAsync();
            CalendarException? result = exceptions.FirstOrDefault(filter.Fits);

            if (result == null)
                return new NotFoundResult();

            if (!ValidateChanging(result))
                return new BadRequestObjectResult("Deletion is forbidden");

            db.CalendarExceptions.Remove(result);
            try { await db.SaveChangesAsync(); }
            catch (Exception ex) { return new BadRequestObjectResult(ex.Message); }

            return new OkObjectResult(result);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> CreateCalendarException([FromBody] CalendarExceptionPreset preset)
        {
            CalendarException? calendarException = preset.Create();
            if (calendarException == null)
                return new BadRequestObjectResult("Invalid preset");

            if (!ValidateChanging(calendarException))
                return new BadRequestObjectResult("Creation is forbidden");

            CalendarException? duplicate = await db.CalendarExceptions.FirstOrDefaultAsync(e => e.ExceptionDate == calendarException.ExceptionDate);
            if (duplicate != null)
                return new BadRequestObjectResult("Exception was already created");

            db.CalendarExceptions.Add(calendarException);
            try { await db.SaveChangesAsync(); }
            catch (Exception ex) { return new BadRequestObjectResult(ex.Message); }

            return new OkObjectResult(calendarException);
        }

        private static bool ValidateChanging(CalendarException calendarException) =>
            ((DateTime.Today - calendarException.ExceptionDate).TotalDays > 7);
    }
}
