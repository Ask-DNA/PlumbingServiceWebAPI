namespace PlumbingServiceWebAPI.Models
{
    public class CalendarExceptionFilter
    {
        public bool? IsHoliday { get; set; } = null;

        public DateTimeJsonable? ExceptionDate { get; set; } = null;
        public DateTimeJsonable? DateFrom { get; set; } = null;
        public DateTimeJsonable? DateTo { get; set; } = null;

        private DateTime? CalendarExceptionDate { get; set; } = null;
        private DateTime? From { get; set; } = null;
        private DateTime? To { get; set; } = null;

        public bool Fits(CalendarException calendarException)
        {
            bool result = true;
            if (CalendarExceptionDate != null)
                result = result && CalendarExceptionDate.Equals(calendarException.ExceptionDate);
            if (IsHoliday != null)
                result = result && IsHoliday.Equals(calendarException.IsHoliday);
            if (From != null)
                result = result && From <= calendarException.ExceptionDate;
            if (To != null)
                result = result && To >= calendarException.ExceptionDate;
            return result;
        }

        public bool Validate()
        {
            if (ExceptionDate != null)
            {
                CalendarExceptionDate = ExceptionDate.ToDateTime();
                if (CalendarExceptionDate == null)
                    return false;
            }

            if (DateTo != null)
            {
                To = DateTo.ToDateTime();
                if (To == null)
                    return false;
            }

            if (DateFrom != null)
            {
                From = DateFrom.ToDateTime();
                if (From == null)
                    return false;
            }

            if (From != null && To != null)
                return From > To;

            return true;
        }
    }

    public class ShiftFilter
    {
        public string? ID { get; set; } = null;
        public string? EmployeeID { get; set; } = null;
        public string? ManagerID { get; set; } = null;

        public DateTimeJsonable? DateFrom { get; set; } = null;
        public DateTimeJsonable? DateTo { get; set; } = null;

        private DateTime? From { get; set; } = null;
        private DateTime? To { get; set; } = null;

        public bool Fits(Shift shift)
        {
            bool result = true;
            if (From != null)
                result = result && From <= shift.Start;
            if (To != null)
                result = result && To >= shift.End;
            if (ID != null)
                result = result && ID.Equals(shift.ID);
            if (EmployeeID != null)
                result = result && EmployeeID.Equals(shift.EmployeeID);
            if (ManagerID != null)
                result = result && ManagerID.Equals(shift.Employee?.ManagerID);
            return result;
        }

        public bool Validate()
        {
            if (DateTo != null)
            {
                To = DateTo.ToDateTime();
                if (To == null)
                    return false;
            }

            if (DateFrom != null)
            {
                From = DateFrom.ToDateTime();
                if (From == null)
                    return false;
            }

            if (From != null && To != null)
                return From > To;

            return true;
        }
    }

    public class EmployeeFilter
    {
        public string? ID { get; set; } = null;
        public string? ManagerID { get; set; } = null;

        public bool Fits(Employee employee)
        {
            bool result = true;
            if (ID != null)
                result = result && ID.Equals(employee.ID);
            if (ManagerID != null)
                result = result && ManagerID.Equals(employee.ManagerID);
            return result;
        }
    }

    public class UserFilter
    {
        public string? ID { get; set; } = null;
        public string? Name { get; set; } = null;
        public string? Email { get; set; } = null;
        public string? Password { get; set; } = null;
        public List<string> RoleNames { get; set; } = new List<string>();

        public bool Fits(User user)
        {
            bool result = true;
            if (ID != null)
                result = result && ID.Equals(user.ID);
            if (Name != null)
                result = result && Name.Equals(user.Name);
            if (Email != null)
                result = result && Email.Equals(user.Email);
            if (Password != null)
                result = result && Password.Equals(user.Password);
            if (RoleNames.Count > 0)
                result = result && RoleNames.Contains(user.RoleName);
            return result;
        }
    }

    public class OrderFilter
    {
        public string? ID { get; set; } = null;
        public string? UserID { get; set; } = null;
        public string? EmployeeID { get; set; } = null;
        public string? ManagerID { get; set; } = null;

        public DateTimeJsonable? DateFrom { get; set; } = null;
        public DateTimeJsonable? DateTo { get; set; } = null;

        private DateTime? From { get; set; } = null;
        private DateTime? To { get; set; } = null;

        public bool Fits(OrderHistory orderHistory)
        {
            bool result = true;
            if (From != null)
                result = result && From <= orderHistory.OrderDateTime;
            if (To != null)
                result = result && To >= orderHistory.OrderDateTime;
            if (ID != null)
                result = result && ID.Equals(orderHistory.ID);
            if (EmployeeID != null)
                result = result && EmployeeID.Equals(orderHistory.EmployeeID);
            if (UserID != null)
                result = result && UserID.Equals(orderHistory.UserID);
            if (ManagerID != null)
                result = result && ManagerID.Equals(orderHistory.Executor?.ManagerID);

            return result;
        }

        public bool Fits(Order order)
        {
            bool result = true;
            if (From != null)
                result = result && From <= order.OrderDateTime;
            if (To != null)
                result = result && To >= order.OrderDateTime;
            if (ID != null)
                result = result && ID.Equals(order.ID);
            if (EmployeeID != null)
                result = result && EmployeeID.Equals(order.EmployeeID);
            if (UserID != null)
                result = result && UserID.Equals(order.UserID);
            if (ManagerID != null && order.Executor != null)
                result = result && ManagerID.Equals(order.Executor.ManagerID);

            return result;
        }

        public bool Validate()
        {
            if (DateTo != null)
            {
                To = DateTo.ToDateTime();
                if (To == null)
                    return false;
            }
                
            if (DateFrom != null)
            {
                From = DateFrom.ToDateTime();
                if (From == null)
                    return false;
            }

            if (From != null && To != null)
                return From > To;

            return true;
        }
    }
}

