using Microsoft.EntityFrameworkCore;
using PlumbingServiceWebAPI.Models;
using MimeKit;
using MailKit.Net.Smtp;

namespace PlumbingServiceWebAPI.Services
{
    public class NotificationService
    {
        public async Task UserAccountCreated(User user)
        {
            string to = user.Email;
            string subject = "Спасибо за создание аккаунта";
            string message = "";
            message += "Имя пользователя: " + user.Name + "<br/>";
            message += "Email: " + user.Email + "<br/>";
            await SendEmailAsync(to, subject, message);
        }

        public async Task UserAccountDeleted(User user)
        {
            string to = user.Email;
            string subject = "Ваш аккаунт удален";
            string message = "";
            message += "Имя пользователя: " + user.Name + "<br/>";
            message += "Email: " + user.Email + "<br/>";
            await SendEmailAsync(to, subject, message);
        }

        public async Task OrderCreated(Order order, List<OrderContent> content, ApplicationContext db)
        {
            Employee? employee = await db.Employees.FirstOrDefaultAsync(e => e.ID == order.EmployeeID);
            User? manager = await db.Users.FirstOrDefaultAsync(u => u.ID == employee!.ManagerID);
            List<TypeOfWork> types = await db.TypesOfWork.ToListAsync();

            string to = order.UserMail;
            string subject = "Уведомление о создании заявки";
            string message = "";
            message += "Идентификатор заказа: " + order.ID.ToString() + "<br/>";
            message += "Имя заказчика: " + order.UserName + "<br/>";
            message += "Адрес электронной почты заказчика: " + order.UserMail + "<br/>";
            message += "Адрес проведения работ: " + order.UserAddress + "<br/>";
            message += "Время проведения работ: " + order.OrderDateTime.ToString() + "<br/>";
            message += "Предварительная длительность работ (мин.): " + order.LengthMinutes.ToString() + "<br/>";
            message += "Предварительная стоимость работ (руб.): " + order.Cost.ToString() + "<br/>";
            if (order.Commentary != null)
                message += "Комментарий к заказу: " + order.Commentary + "<br/>";
            message += "Менеджер заказа: " + manager!.Name + " (" + manager!.Email + ")" + "<br/>";
            message += "Содержание заказа: " + "<br/>";

            TypeOfWork tmp;
            foreach (OrderContent c in content)
            {
                tmp = types.FirstOrDefault(t => t.ID == c.TypeOfWorkID)!;
                if (tmp.Enumerable)
                    message += "   " + tmp.Name + "(x" + c.Count.ToString() + ")" + "<br/>";
                else
                    message += "   " + tmp.Name + "<br/>";
            }
            await SendEmailAsync(to, subject, message);
        }

        public async Task OrderCancelled(Order order, ApplicationContext db)
        {
            Employee? employee = await db.Employees.FirstOrDefaultAsync(e => e.ID == order.EmployeeID);
            User? manager = await db.Users.FirstOrDefaultAsync(u => u.ID == employee!.ManagerID);

            string to = order.UserMail;
            string subject = "Уведомление об отмене заявки";
            string message = "";
            message += "Заявка " + order.ID.ToString() + " отменена." + "<br/>";

            await SendEmailAsync(to, subject, message);
        }

        public async Task OrderClosed(Order order, ApplicationContext db)
        {
            Employee? employee = await db.Employees.FirstOrDefaultAsync(e => e.ID == order.EmployeeID);
            User? manager = await db.Users.FirstOrDefaultAsync(u => u.ID == employee!.ManagerID);

            string to = order.UserMail;
            string subject = "Уведомление о завершении заказа";
            string message = "";
            message += "Заказ " + order.ID.ToString() + " завершен." + "<br/>";

            await SendEmailAsync(to, subject, message);
        }

        public async Task EmployeeChanged(Employee employee, ApplicationContext db)
        {
            List<Order> orders = await db.Orders.Where(o => o.EmployeeID == employee.ID).ToListAsync();
            User? manager = await db.Users.FirstOrDefaultAsync(u => u.ID == employee.ManagerID);

            string to = "";
            string subject = "Информация о заявке ";
            string message = "Контактные данные менеджера по Вашему заказу были изменены." + Environment.NewLine;
            message += "Новые контактные данные:" + "<br/>";
            message += "Имя: " + manager!.Name + "<br/>";
            message += "Email: " + manager!.Email + "<br/>";

            foreach (Order order in orders)
            {
                subject += order.ID.ToString();
                to = order.UserMail;
                await SendEmailAsync(to, subject, message);
            }
        }

        public async Task ManagerAccountChanged(User manager, ApplicationContext db)
        {
            List<Order> orders = await db.Orders.Include(o => o.Executor).Where(o => o.Executor!.ManagerID == manager.ID).ToListAsync();

            string to = "";
            string subject = "Информация о заявке ";
            string message = "Контактные данные менеджера по Вашему заказу были изменены." + "<br/>";
            message += "Новые контактные данные:" + "<br/>";
            message += "Имя: " + manager.Name + "<br/>";
            message += "Email: " + manager.Email + "<br/>";

            foreach (Order order in orders)
            {
                subject += order.ID.ToString();
                to = order.UserMail;
                await SendEmailAsync(to, subject, message);
            }
        }



        public async Task SendEmailAsync(string to, string subject, string message)
        {
            using var emailMessage = new MimeMessage();

            emailMessage.From.Add(new MailboxAddress("Сантехнический сервис \"Сантехнический сервис\"", "amsukhanov@stud.etu.ru"));
            emailMessage.To.Add(new MailboxAddress("", to));
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = message
            };

            using var client = new SmtpClient();
            await client.ConnectAsync("smtp.yandex.ru", 25, false);
            await client.AuthenticateAsync("amsukhanov@stud.etu.ru", "Jgr9ReVQ");
            await client.SendAsync(emailMessage);

            await client.DisconnectAsync(true);
        }
    }
}