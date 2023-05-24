using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PlumbingServiceWebAPI.Models
{
    public class ApplicationContext : DbContext
    {
        public DbSet<CalendarException> CalendarExceptions { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Employee> Employees { get; set; } = null!;
        public DbSet<Shift> Shifts { get; set; } = null!;
        public DbSet<TypeOfWork> TypesOfWork { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderContent> OrdersContent { get; set; } = null!;
        public DbSet<OrderHistory> OrdersHistory { get; set; } = null!;

        public ApplicationContext(DbContextOptions<ApplicationContext> options)
            : base(options)
        {
            Database.EnsureDeleted();
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CalendarException>(CalendarExceptionConfigure);
            modelBuilder.Entity<Role>(RoleConfigure);
            modelBuilder.Entity<User>(UserConfigure);
            modelBuilder.Entity<Employee>(EmployeeConfigure);
            modelBuilder.Entity<Shift>(ShiftConfigure);
            modelBuilder.Entity<TypeOfWork>(TypeOFWorkConfigure);
            modelBuilder.Entity<Order>(OrderConfigure);
            modelBuilder.Entity<OrderContent>(OrderContentConfigure);
            modelBuilder.Entity<OrderHistory>(OrderHistoryConfigure);

            

            string managerid1 = Guid.NewGuid().ToString();
            string managerid2 = Guid.NewGuid().ToString();

            modelBuilder.Entity<User>().HasData(
                new User { ID = Guid.NewGuid().ToString(), Name = "Иван Иванов", Email = "ivanivanov@mail.ru", Password = "1234", RoleName = "User"},
                new User { ID = managerid1, Name = "Иван Иванов", Email = "manager_ivan@mail.ru", Password = "4321", RoleName = "Manager" },
                new User { ID = managerid2, Name = "Алексей Алексеев", Email = "manager_alexey@mail.ru", Password = "0000", RoleName = "Manager" }
                );

            string employeeid1 = Guid.NewGuid().ToString();

            modelBuilder.Entity<Employee>().HasData(
                new Employee { ID = employeeid1, ManagerID = managerid1 },
                new Employee { ID = Guid.NewGuid().ToString(), ManagerID = managerid1 },
                new Employee { ID = Guid.NewGuid().ToString(), ManagerID = managerid1 },
                new Employee { ID = Guid.NewGuid().ToString(), ManagerID = managerid2 },
                new Employee { ID = Guid.NewGuid().ToString(), ManagerID = managerid2 }
                );

            modelBuilder.Entity<Shift>().HasData(
                new Shift { 
                    ID = Guid.NewGuid().ToString(), 
                    EmployeeID = employeeid1, 
                    Start = new DateTime(2023, 5, 27, 6, 0 ,0),
                    End = new DateTime(2023, 5, 27, 12, 0, 0)
                }
                );
        }

        public void CalendarExceptionConfigure(EntityTypeBuilder<CalendarException> builder)
        {
            builder.HasKey(obj => obj.ExceptionDate);

            builder.Property(obj => obj.ExceptionDate).HasColumnType("datetime2").IsRequired();
            builder.Property(obj => obj.IsHoliday).IsRequired();
        }

        public void RoleConfigure(EntityTypeBuilder<Role> builder)
        {
            builder.HasKey(obj => obj.Name);

            builder.Property(obj => obj.Name).IsRequired();

            builder.HasData(
                new Role { Name = "Guest" },
                new Role { Name = "User" },
                new Role { Name = "Manager" },
                new Role { Name = "Support" },
                new Role { Name = "Administrator" }
                );
        }

        public void UserConfigure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(obj => obj.ID);

            builder.HasAlternateKey(obj => obj.Email);

            builder
                .HasOne(obj => obj.Role)
                .WithMany(role => role.Users)
                .HasForeignKey(obj => obj.RoleName)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(obj => obj.ID).IsRequired();
            builder.Property(obj => obj.Name).IsRequired();
            builder.Property(obj => obj.Email).IsRequired();
            builder.Property(obj => obj.Password).IsRequired();
        }

        public void EmployeeConfigure(EntityTypeBuilder<Employee> builder)
        {
            builder.HasKey(obj => obj.ID);

            builder
                .HasOne(obj => obj.Manager)
                .WithMany(manager => manager.Employees)
                .HasForeignKey(obj => obj.ManagerID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(obj => obj.ID).IsRequired();
        }

        public void ShiftConfigure(EntityTypeBuilder<Shift> builder)
        {
            builder.HasKey(obj => obj.ID);

            builder.HasAlternateKey(obj => new { obj.Start, obj.End });

            builder
                .HasOne(obj => obj.Employee)
                .WithMany(employee => employee.Shifts)
                .HasForeignKey(obj => obj.EmployeeID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(obj => obj.ID).IsRequired();
            builder.Property(obj => obj.EmployeeID).IsRequired();
            builder.Property(obj => obj.Start).HasColumnType("datetime2").IsRequired();
            builder.Property(obj => obj.End).HasColumnType("datetime2").IsRequired();
        }

        public void TypeOFWorkConfigure(EntityTypeBuilder<TypeOfWork> builder)
        {
            builder.HasKey(obj => obj.ID);

            builder.HasAlternateKey(obj => new { obj.Category, obj.Name });

            builder.Property(obj => obj.ID).IsRequired();
            builder.Property(obj => obj.Category).IsRequired();
            builder.Property(obj => obj.Name).IsRequired();
            builder.Property(obj => obj.Choosable).IsRequired();
            builder.Property(obj => obj.Enumerable).IsRequired();

            builder.HasData(
                new TypeOfWork
                {
                    ID = 1,
                    Category = "Общие работы",
                    Name = "Поиск места протечки",
                    LengthMinutes = 30,
                    Cost = 300,
                    Coefficient = null,
                    Choosable = true,
                    Enumerable = true
                },
                new TypeOfWork
                {
                    ID = 2,
                    Category = "Общие работы",
                    Name = "Устранение протечки",
                    LengthMinutes = 45,
                    Cost = 650,
                    Coefficient = null,
                    Choosable = true,
                    Enumerable = true
                },
                new TypeOfWork
                {
                    ID = 3,
                    Category = "Устранение засоров",
                    Name = "Устранение простого засора",
                    LengthMinutes = 30,
                    Cost = 500,
                    Coefficient = null,
                    Choosable = true,
                    Enumerable = true
                },
                new TypeOfWork
                {
                    ID = 4,
                    Category = "Устранение засоров",
                    Name = "Устранение сложного засора",
                    LengthMinutes = 60,
                    Cost = 1000,
                    Coefficient = null,
                    Choosable = true,
                    Enumerable = true
                },
                new TypeOfWork
                {
                    ID = 5,
                    Category = "Монтаж и демонтаж бытовой техники",
                    Name = "Монтаж посудомоечной машины",
                    LengthMinutes = 60,
                    Cost = 3000,
                    Coefficient = null,
                    Choosable = true,
                    Enumerable = true
                },
                new TypeOfWork
                {
                    ID = 6,
                    Category = "Монтаж и демонтаж бытовой техники",
                    Name = "Демонтаж посудомоечной машины",
                    LengthMinutes = 60,
                    Cost = 2000,
                    Coefficient = null,
                    Choosable = true,
                    Enumerable = true
                },
                new TypeOfWork
                {
                    ID = 7,
                    Category = "Дополнительные услуги",
                    Name = "Работа в ночное время",
                    LengthMinutes = 0,
                    Cost = null,
                    Coefficient = 0.1,
                    Choosable = false,
                    Enumerable = false
                },
                new TypeOfWork
                {
                    ID = 8,
                    Category = "Дополнительные услуги",
                    Name = "Работа в выходные и праздничные дни",
                    LengthMinutes = 0,
                    Cost = null,
                    Coefficient = 0.15,
                    Choosable = false,
                    Enumerable = false
                }
                );
        }

        public void OrderConfigure(EntityTypeBuilder<Order> builder)
        {
            builder.HasKey(obj => obj.ID);

            builder
                .HasOne(obj => obj.Customer)
                .WithMany(customer => customer.CurrentOrders)
                .HasForeignKey(obj => obj.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            builder
                .HasOne(obj => obj.Executor)
                .WithMany(executor => executor.CurrentOrders)
                .HasForeignKey(obj => obj.EmployeeID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(obj => obj.ID).IsRequired();
            builder.Property(obj => obj.EmployeeID).IsRequired();
            builder.Property(obj => obj.UserName).IsRequired();
            builder.Property(obj => obj.UserMail).IsRequired();
            builder.Property(obj => obj.UserAddress).IsRequired();
            builder.Property(obj => obj.LengthMinutes).IsRequired();
            builder.Property(obj => obj.OrderDateTime).HasColumnType("datetime2").IsRequired();
        }

        public void OrderContentConfigure(EntityTypeBuilder<OrderContent> builder)
        {
            builder.HasKey(obj => new { obj.OrderID, obj.TypeOfWorkID });

            builder
                .HasOne(obj => obj.Order)
                .WithMany(order => order.Content)
                .HasForeignKey(obj => obj.OrderID)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasOne(obj => obj.TypeOfWork)
                .WithMany(typeOfWork => typeOfWork.Content)
                .HasForeignKey(obj => obj.TypeOfWorkID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(obj => obj.OrderID).IsRequired();
            builder.Property(obj => obj.TypeOfWorkID).IsRequired();
        }

        public void OrderHistoryConfigure(EntityTypeBuilder<OrderHistory> builder)
        {
            builder.HasKey(obj => obj.ID);

            builder
                .HasOne(obj => obj.Customer)
                .WithMany(customer => customer.ClosedOrders)
                .HasForeignKey(obj => obj.UserID)
                .OnDelete(DeleteBehavior.SetNull);

            builder
                .HasOne(obj => obj.Executor)
                .WithMany(executor => executor.ClosedOrders)
                .HasForeignKey(obj => obj.EmployeeID)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Property(obj => obj.ID).IsRequired();
            builder.Property(obj => obj.OrderDateTime).HasColumnType("datetime2").IsRequired();
            builder.Property(obj => obj.Info).IsRequired();
        }
    }
}
