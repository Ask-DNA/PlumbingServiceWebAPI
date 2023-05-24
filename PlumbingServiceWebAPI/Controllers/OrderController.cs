using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlumbingServiceWebAPI.Models;
using PlumbingServiceWebAPI.Services;

namespace PlumbingServiceWebAPI.Controllers
{
    public class OrderController : Controller
    {
        readonly ApplicationContext db;
        readonly SessionService session;

        public OrderController(ApplicationContext applicationContext, SessionService sessionService)
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

            List<Order> orders;
            orders = client!.RoleName switch
            {
                "Support" => await db.Orders.Include(o => o.Executor).ToListAsync(),
                "Manager" => await db.Orders
                    .Include(o => o.Executor)
                    .Where(o => o.Executor != null && o.Executor.ManagerID == client.ID)
                    .ToListAsync(),
                "User" => await db.Orders
                    .Include(o => o.Executor)
                    .Where(o => o.UserID == client.ID)
                    .ToListAsync(),
                _ => new(),
            };
            Order? result = orders.FirstOrDefault(filter.Fits);
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

            List<Order> result;
            result = client!.RoleName switch
            {
                "Support" => await db.Orders.Include(o => o.Executor).ToListAsync(),
                "Manager" => await db.Orders
                    .Include(o => o.Executor)
                    .Where(o => o.Executor != null && o.Executor.ManagerID == client.ID).ToListAsync(),
                "User" => await db.Orders
                    .Include(o => o.Executor)
                    .Where(o => o.UserID == client.ID).ToListAsync(),
                _ => new(),
            };
            result = result.Where(filter.Fits).ToList();
            if (result.Count == 0)
                return new NotFoundResult();

            return new JsonResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(
            [FromBody] OrderPreset preset,
            [FromServices] OrderProcessingService orderProcessing,
            [FromServices] EmployeeManagementService employeeManagementService,
            [FromServices] NotificationService emailSender)
        {
            User? client = await session.GetClientAsync(HttpContext, db);
            string clientRole = client?.RoleName ?? "Guest";
            if (clientRole == "Manager" || clientRole == "Administrator")
                return new RedirectResult("/AccessDenied");

            if (clientRole == "User" && preset.UserID != client!.ID)
                return new BadRequestObjectResult("Invalid preset");
            if (preset.UserID != null && (clientRole == "Guest" || clientRole == "Support"))
                return new BadRequestObjectResult("Invalid preset");

            if (!preset.Validate())
                return new BadRequestObjectResult("Invalid preset");
            if (!await orderProcessing.CompletePreset(preset, db))
                return new BadRequestObjectResult("Invalid preset");

            string? employeeID = await employeeManagementService.ChooseEmployee(preset, db);
            if (employeeID == null)
                return new BadRequestObjectResult("Invalid preset");
            preset.SetEmployee(employeeID!);

            Order? order = preset.CreateOrder();
            if (order == null)
                return new BadRequestObjectResult("Invalid preset");

            List<OrderContent>? content = preset.CreateContent();
            if (content == null)
                return new BadRequestObjectResult("Invalid preset");

            Order? check = await db.Orders.FirstOrDefaultAsync(o => o.ID == order.ID);
            if (check != null)
                return new BadRequestObjectResult("Order was already created");

            db.Orders.Add(order);
            try { await db.SaveChangesAsync(); }
            catch (Exception ex) { return new BadRequestObjectResult(ex.Message); }

            db.OrdersContent.AddRange(content);
            try { await db.SaveChangesAsync(); }
            catch (Exception ex) { return new BadRequestObjectResult(ex.Message); }

            await emailSender.OrderCreated(order, content, db);

            return new OkResult();
        }

        [HttpDelete]
        [Authorize(Roles = "User, Manager")]
        public async Task<IActionResult> CancelOrder([FromBody] OrderFilter filter, [FromServices] NotificationService emailSender)
        {
            User? client = await session.GetClientAsync(HttpContext, db);
            if (client == null)
                return new BadRequestObjectResult("Invalid session");

            if (filter.ID == null || !filter.Validate())
                return new BadRequestObjectResult("Invalid filter options");

            List<Order> orders;
            orders = client!.RoleName switch
            {
                "Manager" => await db.Orders
                    .Include(o => o.Executor)
                    .Where(o => o.Executor != null && o.Executor.ManagerID == client.ID)
                    .ToListAsync(),
                "User" => await db.Orders
                    .Include(o => o.Executor)
                    .Where(o => o.UserID == client.ID)
                    .ToListAsync(),
                _ => new(),
            };
            Order? result = orders.FirstOrDefault(filter.Fits);
            if (result == null)
                return new NotFoundResult();

            if (!CancelValidation(result, client!.RoleName))
                return new BadRequestObjectResult("Cancellation is forbidden");

            db.Orders.Remove(result);
            try { await db.SaveChangesAsync(); }
            catch (Exception ex) { return new BadRequestObjectResult(ex.Message); }

            await emailSender.OrderCancelled(result, db);

            return new OkResult();
        }

        [HttpPut]
        public async Task<IActionResult> CalculateCost([FromBody] OrderPreset preset, [FromServices] OrderProcessingService orderProcessing)
        {
            User? client = await session.GetClientAsync(HttpContext, db);
            string clientRole = client?.RoleName ?? "Guest";
            if (clientRole == "Manager" || clientRole == "Administrator")
                return new RedirectResult("/AccessDenied");

            preset.UserMail = "";
            preset.UserName = "";
            preset.UserAddress = "";

            if (!preset.Validate())
                return new BadRequestObjectResult("Invalid preset");
            if (!await orderProcessing.CompletePreset(preset, db))
                return new BadRequestObjectResult("Invalid preset");

            double? result = preset.Cost;
            if (result == null)
                return new BadRequestObjectResult("Invalid preset");

            return new JsonResult(result);
        }

        [HttpPut]
        public async Task<IActionResult> CalculateLength([FromBody] OrderPreset preset, [FromServices] OrderProcessingService orderProcessing)
        {
            User? client = await session.GetClientAsync(HttpContext, db);
            string clientRole = client?.RoleName ?? "Guest";
            if (clientRole == "Manager" || clientRole == "Administrator")
                return new RedirectResult("/AccessDenied");

            preset.OrderDateTime = new DateTimeJsonable { Year = 2000, Day = 1, Month = 1 };
            preset.UserMail = "";
            preset.UserName = "";
            preset.UserAddress = "";

            if (!preset.Validate())
                return new BadRequestObjectResult("Invalid preset");

            List<TypeOfWork> types = await db.TypesOfWork.ToListAsync();
            int? result = orderProcessing.CalculateLength(preset, types);
            if (result == null)
                return new BadRequestObjectResult("Invalid preset");

            return new JsonResult(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetTypesOfWork()
        {
            User? client = await session.GetClientAsync(HttpContext, db);
            string clientRole = client?.RoleName ?? "Guest";
            if (clientRole == "Manager" || clientRole == "Administrator")
                return new RedirectResult("/AccessDenied");

            List<TypeOfWork> result = await db.TypesOfWork.ToListAsync();
            return new JsonResult(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetFreeTime(
            int? lengthMinutes, int? dd, int? mm, int? yyyy,
            [FromServices] EmployeeManagementService employeeManagementService)
        {
            User? client = await session.GetClientAsync(HttpContext, db);
            string clientRole = client?.RoleName ?? "Guest";
            if (clientRole == "Manager" || clientRole == "Administrator")
                return new RedirectResult("/AccessDenied");

            if (lengthMinutes == null || dd == null || mm == null || yyyy == null)
                return new BadRequestObjectResult("Invalid parameters");
            DateTime date;
            try
            {
                date = new DateTime(yyyy!.Value, mm!.Value, dd!.Value);
            } catch (Exception) { return new BadRequestObjectResult("Invalid parameters"); }
            
            if (DateTime.Now.AddDays(7) < date)
                return new BadRequestObjectResult("Invalid parameters");
            if (lengthMinutes > 480)
                return new BadRequestObjectResult("Invalid parameters");

            Dictionary<int, bool> result = await employeeManagementService.GetFreeTime(date, (int)lengthMinutes!, db);
            return new JsonResult(result);
        }

        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> CloseOrder([FromBody] OrderHistoryPreset preset, [FromServices] NotificationService emailSender)
        {
            User? client = await session.GetClientAsync(HttpContext, db);
            if (client == null)
                return new BadRequestObjectResult("Invalid session");

            if (preset.ID == null)
                return new BadRequestObjectResult("Invalid preset");

            Order? order = await db.Orders
                .Include(o => o.Executor)
                .FirstOrDefaultAsync(o => o.ID == preset.ID && o.Executor!.ManagerID == client.ID);
            if (order == null)
                return new NotFoundResult();

            if (!ClosingValidation(order))
                return new BadRequestObjectResult("Closing is forbidden");

            OrderHistory? history = preset.Create(order);
            if (history == null)
                return new BadRequestObjectResult("Invalid preset");

            db.Orders.Remove(order);
            db.OrdersHistory.Add(history);
            try { await db.SaveChangesAsync(); }
            catch (Exception ex) { return new BadRequestObjectResult(ex.Message); }

            await emailSender.OrderClosed(order, db);

            return new OkResult();
        }

        [NonAction]
        private static bool ClosingValidation(Order order) => !(order.OrderDateTime.Date >= DateTime.Today);

        [NonAction]
        private static bool CancelValidation(Order cancel, string clientRoleName)
        {
            if (clientRoleName == "Manager")
                return true;
            double hours = (cancel.OrderDateTime - DateTime.Now).TotalHours;
            if (hours < 24)
                return false;
            return true;
        }
    }
}
