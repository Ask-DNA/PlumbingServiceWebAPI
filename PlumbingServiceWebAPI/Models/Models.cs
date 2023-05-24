namespace PlumbingServiceWebAPI.Models
{
    public class CalendarException
    {
        public DateTime ExceptionDate { get; set; }
        public bool IsHoliday { get; set; }
    }

    public class Role
    {
        public string Name { get; set; } = null!;
        public List<User>? Users { get; set; } = null;
    }

    public class User
    {
        public string ID { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string RoleName { get; set; } = null!;

        public Role? Role { get; set; } = null;
        public List<Employee>? Employees { get; set; } = null;
        public List<Order>? CurrentOrders { get; set; } = null;
        public List<OrderHistory>? ClosedOrders { get; set; } = null;
    }

    public class Employee
    {
        public string ID { get; set; } = null!;
        public string ManagerID { get; set; } = null!;

        public User? Manager { get; set; } = null;
        public List<Shift>? Shifts { get; set; } = null;
        public List<Order>? CurrentOrders { get; set; } = null;
        public List<OrderHistory>? ClosedOrders { get; set; } = null;
    }

    public class Shift
    {
        public string ID { get; set; } = null!;
        public string EmployeeID { get; set; } = null!;
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public Employee? Employee { get; set; } = null;
    }

    public class TypeOfWork
    {
        public int ID { get; set; }
        public string Category { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int? LengthMinutes { get; set; } = null;
        public double? Cost { get; set; } = null;
        public double? Coefficient { get; set; } = null;
        public bool Choosable { get; set; }
        public bool Enumerable { get; set; }

        public List<OrderContent>? Content { get; set; } = null;
    }

    public class Order
    {
        public string ID { get; set; } = null!;
        public string? UserID { get; set; } = null;
        public string EmployeeID { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string UserMail { get; set; } = null!;
        public string UserAddress { get; set; } = null!;
        public string? Commentary { get; set; } = null;
        public DateTime OrderDateTime { get; set; }
        public double? Cost { get; set; } = null;
        public int LengthMinutes { get; set; }

        public User? Customer { get; set; } = null;
        public Employee? Executor { get; set; } = null;
        public List<OrderContent>? Content { get; set; } = null;
    }

    public class OrderContent
    {
        public string OrderID { get; set; } = null!;
        public int TypeOfWorkID { get; set; }
        public int? Count { get; set; } = null;

        public Order? Order { get; set; } = null;
        public TypeOfWork? TypeOfWork { get; set; } = null;
    }

    public class OrderHistory
    {
        public string ID { get; set; } = null!;
        public string? UserID { get; set; } = null;
        public string? EmployeeID { get; set; } = null;
        public DateTime OrderDateTime { get; set; }
        public string Info { get; set; } = null!;

        public User? Customer { get; set; } = null;
        public Employee? Executor { get; set; } = null;
    }
}