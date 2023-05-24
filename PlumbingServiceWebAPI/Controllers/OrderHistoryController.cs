using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlumbingServiceWebAPI.Models;
using PlumbingServiceWebAPI.Services;

namespace PlumbingServiceWebAPI.Controllers
{
    public class OrderHistoryController : Controller
    {
        readonly ApplicationContext db;
        readonly SessionService session;

        public OrderHistoryController(ApplicationContext applicationContext, SessionService sessionService)
        {
            db = applicationContext;
            session = sessionService;
        }

        [HttpPut]
        [Authorize(Roles = "User, Manager, Support")]
        public async Task<IActionResult> GetOrder([FromBody] OrderFilter filter)
        {
            User? client = await session.GetClientAsync(HttpContext, db);
            if (client == null)
                return new BadRequestObjectResult("Invalid session");

            if (filter.ID == null || !filter.Validate())
                return new BadRequestObjectResult("Invalid filter options");

            List<OrderHistory> orders;
            orders = client!.RoleName switch
            {
                "Support" => await db.OrdersHistory.Include(o => o.Executor).ToListAsync(),
                "Manager" => await db.OrdersHistory
                    .Include(o => o.Executor)
                    .Where(o => o.Executor != null && o.Executor.ManagerID == client.ID)
                    .ToListAsync(),
                "User" => await db.OrdersHistory
                    .Include(o => o.Executor)
                    .Where(o => o.UserID == client.ID)
                    .ToListAsync(),
                _ => new(),
            };
            OrderHistory? result = orders.FirstOrDefault(filter.Fits);
            if (result == null)
                return new NotFoundResult();

            return new JsonResult(result);
        }

        [HttpPut]
        [Authorize(Roles = "User, Manager, Support")]
        public async Task<IActionResult> GetOrders([FromBody] OrderFilter filter)
        {
            User? client = await session.GetClientAsync(HttpContext, db);
            if (client == null)
                return new BadRequestObjectResult("Invalid session");

            if (!filter.Validate())
                return new BadRequestObjectResult("Invalid filter options");

            List<OrderHistory> result;
            result = client!.RoleName switch
            {
                "Support" => await db.OrdersHistory.Include(o => o.Executor).ToListAsync(),
                "Manager" => await db.OrdersHistory
                    .Include(o => o.Executor)
                    .Where(o => o.Executor != null && o.Executor.ManagerID == client.ID).ToListAsync(),
                "User" => await db.OrdersHistory
                    .Include(o => o.Executor)
                    .Where(o => o.UserID == client.ID).ToListAsync(),
                _ => new(),
            };
            result = result.Where(filter.Fits).ToList();
            if (result.Count == 0)
                return new NotFoundResult();

            return new JsonResult(result);
        }

        [HttpDelete]
        [Authorize(Roles = "Administrator, Manager")]
        public async Task<IActionResult> DeleteOrder([FromBody] OrderFilter filter)
        {
            User? client = await session.GetClientAsync(HttpContext, db);
            if (client == null)
                return new BadRequestObjectResult("Invalid session");

            if (filter.ID == null || !filter.Validate())
                return new BadRequestObjectResult("Invalid filter options");

            List<OrderHistory> orders;
            orders = client!.RoleName switch
            {
                "Administrator" => await db.OrdersHistory.Include(o => o.Executor).ToListAsync(),
                "Manager" => await db.OrdersHistory
                    .Include(o => o.Executor)
                    .Where(o => o.Executor != null && o.Executor.ManagerID == client.ID).ToListAsync(),
                _ => new(),
            };
            OrderHistory? result = orders.FirstOrDefault(filter.Fits);
            if (result == null)
                return new NotFoundResult();

            db.OrdersHistory.Remove(result);
            try { await db.SaveChangesAsync(); }
            catch (Exception ex) { return new BadRequestObjectResult(ex.Message); }

            return new OkObjectResult(result);
        }

        [HttpDelete]
        [Authorize(Roles = "Administrator, Manager")]
        public async Task<IActionResult> DeleteOrders([FromBody] OrderFilter filter)
        {
            User? client = await session.GetClientAsync(HttpContext, db);
            if (client == null)
                return new BadRequestObjectResult("Invalid session");

            if (!filter.Validate())
                return new BadRequestObjectResult("Invalid filter options");

            List<OrderHistory> result;
            result = client!.RoleName switch
            {
                "Administrator" => await db.OrdersHistory.Include(o => o.Executor).ToListAsync(),
                "Manager" => await db.OrdersHistory
                    .Include(o => o.Executor)
                    .Where(o => o.Executor != null && o.Executor.ManagerID == client.ID).ToListAsync(),
                _ => new(),
            };
            result = result.Where(filter.Fits).ToList();
            if (result.Count == 0)
                return new NotFoundResult();

            db.OrdersHistory.RemoveRange(result);
            try { await db.SaveChangesAsync(); }
            catch (Exception ex) { return new BadRequestObjectResult(ex.Message); }

            return new OkObjectResult(result);
        }

        [HttpPut]
        [Authorize(Roles = "Administrator, Manager")]
        public async Task<IActionResult> EditOrder([FromBody] OrderHistoryPreset preset)
        {
            User? client = await session.GetClientAsync(HttpContext, db);
            if (client == null)
                return new BadRequestObjectResult("Invalid session");

            if (preset.ID == null)
                return new BadRequestObjectResult("Invalid preset");

            OrderHistory? baseOrder = null;
            baseOrder = client!.RoleName switch
            {
                "Administrator" => await db.OrdersHistory.Include(o => o.Executor).FirstOrDefaultAsync(o => o.ID == preset.ID),
                "Manager" => await db.OrdersHistory
                    .Include(o => o.Executor)
                    .FirstOrDefaultAsync(o => o.ID == preset.ID && o.Executor != null && o.Executor.ManagerID == client.ID),
                _ => null,
            };
            if (baseOrder == null)
                return new NotFoundResult();

            OrderHistory? result = preset.Edit(baseOrder);
            if (result == null)
                return new BadRequestObjectResult("Invalid preset");

            db.OrdersHistory.Update(result);
            try { await db.SaveChangesAsync(); }
            catch (Exception ex) { return new BadRequestObjectResult(ex.Message); }

            return new OkObjectResult(result);
        }
    }
}
