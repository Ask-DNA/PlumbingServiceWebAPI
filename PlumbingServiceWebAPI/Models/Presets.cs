namespace PlumbingServiceWebAPI.Models
{
    public class CalendarExceptionPreset
    {
        public DateTimeJsonable? ExceptionDate { get; set; } = null;
        public bool? IsHoliday { get; set; } = null;

        public CalendarException? Create()
        {
            if (IsHoliday == null)
                return null;
            if (ExceptionDate == null)
                return null;
            if (ExceptionDate.ToDateTime() == null)
                return null;

            DateTime? date = ExceptionDate.ToDateTime();
            return new CalendarException { ExceptionDate = (DateTime)date!, IsHoliday = (bool)IsHoliday! };
        }
    }

    public class ShiftPreset
    {
        public string? EmployeeID { get; set; } = null;
        public DateTimeJsonable? Start { get; set; } = null;
        public DateTimeJsonable? End { get; set; } = null;

        public Shift? Create()
        {
            if (EmployeeID == null)
                return null;
            if (Start == null)
                return null;
            if (End == null)
                return null;

            DateTime? startTime = Start.ToDateTime();
            DateTime? endTime = End.ToDateTime();

            if (startTime == null || endTime == null)
                return null;
            if (startTime >= endTime)
                return null;

            return new Shift { ID = Guid.NewGuid().ToString(), EmployeeID = EmployeeID, Start = (DateTime)startTime!, End = (DateTime)endTime! };
        }
    }

    public class EmployeePreset
    {
        public string? ID { get; set; } = null;
        public string? ManagerID { get; set; } = null;

        public Employee? Create()
        {
            if (ManagerID == null)
                return null;
            string id;
            if (ID == null)
                id = Guid.NewGuid().ToString();
            else
                id = ID;
            return new Employee { ID = id, ManagerID = ManagerID };
        }

        public bool ValidateForCreation() => ID == null && ManagerID != null;

        public bool ValidateForEdition() => ID != null && ManagerID != null;
    }
    
    public class UserPreset
    {
        public string? ID { get; set; } = null;
        public string? Name { get; set; } = null;
        public string? Email { get; set; } = null;
        public string? Password { get; set; } = null;
        public string? RoleName { get; set; } = null;

        public User? Create()
        {
            if (ID != null)
                return null;
            if (Name == null || Email == null || Password == null || RoleName == null)
                return null;
            return new User { ID = Guid.NewGuid().ToString(), Name = Name, Email = Email, Password = Password, RoleName = RoleName };
        }

        public User? Edit(User baseUser)
        {
            if (RoleName != null)
                return null;
            string name = Name ?? baseUser.Name;
            string email = Email ?? baseUser.Email;
            string password = Password ?? baseUser.Password;
            return new User { ID = baseUser.ID, Name = name, Email = email, Password = password, RoleName = baseUser.RoleName };
        }
    }

    public class OrderPreset
    {
        public string? UserID { get; set; } = null;
        public string? UserName { get; set; } = null;
        public string? UserMail { get; set; } = null;
        public string? UserAddress { get; set; } = null;
        public string? Commentary { get; set; } = null;
        public DateTimeJsonable? OrderDateTime { get; set; } = null;
        public Dictionary<int, int> Content { get; set; } = new Dictionary<int, int>();


        public string? ID { get; private set; } = null;
        public string? EmployeeID { get; private set; } = null;
        public double? Cost { get; private set; } = null;
        public int? LengthMinutes { get; private set; } = null;
        public DateTime? OrderTime { get; private set; } = null;

        public void SetLength(int? len) => LengthMinutes = len;
        public void SetCost(double? cost) => Cost = cost;
        public void SetEmployee(string? id) => EmployeeID = id;

        public bool Validate()
        {
            if (UserName == null || UserMail == null || UserAddress == null || OrderDateTime == null)
                return false;
            if (Content.Count == 0)
                return false;
            OrderTime = OrderDateTime.ToDateTime();
            if (OrderTime == null)
                return false;

            return true;
        }

        public Order? CreateOrder()
        {
            ID = Guid.NewGuid().ToString();
            if (!Validate())
                return null;
            if (EmployeeID == null)
                return null;
            if (LengthMinutes == null)
                return null;

            Order result = new Order()
            {
                ID = ID, UserID = UserID, EmployeeID = EmployeeID, 
                UserName = UserName!, UserMail = UserMail!, UserAddress = UserAddress!,
                Commentary = Commentary, OrderDateTime = (DateTime)OrderTime!, 
                Cost = Cost, LengthMinutes = (int)LengthMinutes!
            };

            return result;
        }

        public List<OrderContent>? CreateContent()
        {
            if (ID == null)
                return null;
            List<OrderContent> result = new();
            foreach(int id in Content.Keys)
                result.Add(new OrderContent { OrderID = ID, TypeOfWorkID = id, Count = Content[id] });
            return result;
        }
    }

    public class OrderHistoryPreset
    {
        public string? ID { get; set; } = null;
        public DateTimeJsonable? OrderDateTime { get; set; } = null;
        public string? Info { get; set; } = null;

        public OrderHistory? Create(Order baseOrder)
        {
            DateTime? dateTime = null;
            if (OrderDateTime != null)
            {
                dateTime = OrderDateTime.ToDateTime();
                if (dateTime == null)
                    return null;
            }
            if (dateTime == null)
                dateTime = baseOrder.OrderDateTime;

            if (Info == null)
                return null;


            return new OrderHistory
            {
                ID = ID!,
                UserID = baseOrder.UserID,
                EmployeeID = baseOrder.EmployeeID,
                OrderDateTime = (DateTime)dateTime!,
                Info = Info
            };
        }

        public OrderHistory? Edit(OrderHistory baseOrder)
        {
            DateTime? dateTime = null;
            if (OrderDateTime != null)
            {
                dateTime = OrderDateTime.ToDateTime();
                if (dateTime == null)
                    return null;
            }
            if (dateTime == null)
                dateTime = baseOrder.OrderDateTime;

            string info = Info ?? baseOrder.Info;

            return new OrderHistory
            {
                ID = ID!,
                UserID = baseOrder.UserID,
                EmployeeID = baseOrder.EmployeeID,
                OrderDateTime = (DateTime)dateTime!,
                Info = info
            };
        }
    }
}