using Microsoft.EntityFrameworkCore;
using PlumbingServiceWebAPI.Models;

namespace PlumbingServiceWebAPI.Services
{
    public class EmployeeManagementService
    {
        public async Task<Dictionary<int, bool>> GetFreeTime(DateTime date, int lengthMinutes, ApplicationContext db)
        {
            Dictionary<int, bool> result = new()
            {
                { 0, false },
                { 1, false },
                { 2, false },
                { 3, false },
                { 4, false },
                { 5, false },
                { 6, false },
                { 7, false },
                { 8, false },
                { 9, false },
                { 10, false },
                { 11, false },
                { 12, false },
                { 13, false },
                { 14, false },
                { 15, false },
                { 16, false },
                { 17, false },
                { 18, false },
                { 19, false },
                { 20, false },
                { 21, false },
                { 22, false },
                { 23, false }
            };
            DateTime startTime, endTime;
            List<Employee> employees = await db.Employees.Include(e => e.Shifts).Include(e => e.CurrentOrders).ToListAsync();
            foreach (int hour in result.Keys)
            {
                startTime = date.Date.AddHours(hour);
                endTime = startTime.AddMinutes(lengthMinutes);
                result[hour] = IsPeriodFree(startTime, endTime, employees);
            }
            
            return result;
        }

        private static bool IsPeriodFree(DateTime startTime, DateTime endTime, List<Employee> employees)
        {
            foreach (Employee employee in employees)
            {
                if (IsEmployeeFree(startTime, endTime, employee))
                    return true;
            }
            return false;
        }

        private static bool IsEmployeeFree(DateTime startTime, DateTime endTime, Employee employee)
        {
            if (employee.Shifts == null)
                return false;
            List<Shift> shifts = employee.Shifts;

            bool shiftFound = false;
            foreach (Shift shift in shifts)
            {
                if (shift.Start <= startTime && shift.End >= endTime)
                {
                    shiftFound = true;
                    break;
                }
            }
            if (!shiftFound)
                return false;

            if (employee.CurrentOrders == null)
                return true;
            List<Order> orders = employee.CurrentOrders;

            bool conflictFound = false;
            endTime = endTime.AddHours(1);
            DateTime orderStart, orderEnd;
            foreach (Order order in orders)
            {
                orderStart = order.OrderDateTime;
                orderEnd = order.OrderDateTime.AddMinutes(order.LengthMinutes).AddHours(1);
                if (orderEnd <= startTime)
                    conflictFound = false;
                else if (orderStart >= endTime)
                    conflictFound = false;
                else
                {
                    conflictFound = true;
                    break;
                }
            }
            return !conflictFound;
        }

        public async Task<string?> ChooseEmployee(OrderPreset preset, ApplicationContext db)
        {
            if (preset.OrderTime == null)
                return null;
            if (preset.LengthMinutes == null)
                return null;
            DateTime startTime = preset.OrderTime.Value;
            DateTime endTime = startTime.AddMinutes(preset.LengthMinutes.Value);

            List<Employee> employees = await db.Employees
                .Include(e => e.Shifts)
                .Include(e => e.CurrentOrders)
                .ToListAsync();

            employees = employees.Where(e => IsEmployeeFree(startTime, endTime, e)).ToList();
            if (employees.Count == 0)
                return null;

            int minCount = int.MaxValue;
            Employee result = employees[0];
            foreach (Employee employee in employees)
            {
                if (employee.CurrentOrders == null)
                    return employee.ID;
                if (minCount > employee.CurrentOrders.Count)
                {
                    minCount = employee.CurrentOrders.Count;
                    result = employee;
                }
            }

            return result.ID;
        }
    }
}