using Microsoft.EntityFrameworkCore;
using PlumbingServiceWebAPI.Models;
using System.Globalization;

namespace PlumbingServiceWebAPI.Services
{
    public class OrderProcessingService
    {
        public async Task<bool> CompletePreset(OrderPreset preset, ApplicationContext db)
        {
            if (!preset.Validate())
                return false;

            List<TypeOfWork> types = await db.TypesOfWork.ToListAsync();
            preset.SetLength(CalculateLength(preset, types));
            if (preset.LengthMinutes == null)
                return false;
            if (preset.LengthMinutes > 480)
                return false;

            DateTime date = (DateTime)preset.OrderTime!;
            if (date.Date >= DateTime.Today.AddDays(7)) 
                return false;

            List<CalendarException> calendarExceptions =
                await db.CalendarExceptions.Where(e => e.ExceptionDate >= date.Date).ToListAsync();
            AddServices(preset, calendarExceptions, types);

            preset.SetCost(CalculateCost(preset, types));
            if (preset.Cost == null)
                return false;

            return true;
        }

        public int? CalculateLength(OrderPreset preset, List<TypeOfWork> types)
        {
            if (!preset.Validate())
                return null;

            TypeOfWork? tmp;
            int lenMinutes = 0;
            foreach (int id in preset.Content.Keys)
            {
                tmp = types.FirstOrDefault(t => t.ID == id);

                if (tmp == null)
                    return null;
                if (!tmp.Choosable)
                    return null;

                if (tmp.Enumerable)
                {
                    if (preset.Content[id] <= 0)
                        return null;
                    lenMinutes += (tmp.LengthMinutes ?? 0) * preset.Content[id];
                }
                else
                {
                    if (preset.Content[id] != 0)
                        return null;
                    lenMinutes += tmp.LengthMinutes ?? 0;
                }
            }
            return lenMinutes;
        }

        private static void AddServices(OrderPreset preset, List<CalendarException> calendarExceptions, List<TypeOfWork> types)
        {
            AddNighttimeWork(preset, types);
            AddHolidayWork(preset, calendarExceptions, types);
        }

        private static void AddNighttimeWork(OrderPreset preset, List<TypeOfWork> types)
        {
            DateTime StartTime = (DateTime)preset.OrderTime!;
            DateTime EndTime = StartTime.AddMinutes(preset.LengthMinutes ?? 0);

            bool nightime = false;
            if (StartTime.Hour >= 22 || StartTime.Hour < 6)
                nightime = true;
            if (EndTime.Hour > 22 || EndTime.Hour <= 6)
                nightime = true;
            if (StartTime.Day != EndTime.Day)
                nightime = true;

            int nighttimeWork = types.FirstOrDefault(t => t.Name == "Работа в ночное время")!.ID;
            if (nightime)
                preset.Content.Add(nighttimeWork, 0);
        }

        private static void AddHolidayWork(OrderPreset preset, List<CalendarException> calendarExceptions, List<TypeOfWork> types)
        {
            DateTime StartTime = (DateTime)preset.OrderTime!;
            DateTime EndTime = StartTime.AddMinutes(preset.LengthMinutes ?? 0);

            bool holidayStart = false;
            bool holidayEnd = false;
            string dayOfWeekStart = CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(StartTime.DayOfWeek);
            string dayOfWeekEnd = CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(StartTime.DayOfWeek);

            if (dayOfWeekStart == "суббота" || dayOfWeekStart == "воскресенье")
                holidayStart = true;
            if (dayOfWeekEnd == "суббота" || dayOfWeekEnd == "воскресенье")
                holidayEnd = true;

            CalendarException? ex = calendarExceptions.FirstOrDefault(e => e.ExceptionDate == StartTime.Date);
            if (ex != null)
                holidayStart = ex.IsHoliday;

            ex = calendarExceptions.FirstOrDefault(e => e.ExceptionDate == EndTime.Date);
            if (ex != null)
                holidayStart = ex.IsHoliday;

            bool holiday = holidayStart || holidayEnd;

            int holidayWork = types.FirstOrDefault(t => t.Name == "Работа в выходные и праздничные дни")!.ID;
            if (holiday)
                preset.Content.Add(holidayWork, 0);
        }

        private static double? CalculateCost(OrderPreset preset, List<TypeOfWork> types)
        {
            TypeOfWork? tmp;
            double cost = 0;
            double coef = 1;
            foreach (int id in preset.Content.Keys)
            {
                tmp = types.FirstOrDefault(t => t.ID == id);

                if (tmp == null)
                    return null;

                if (tmp.Enumerable)
                {
                    if (preset.Content[id] <= 0)
                        return null;
                    cost += (tmp.Cost ?? 0) * preset.Content[id];
                    coef += tmp.Coefficient ?? 0;
                }
                else
                {
                    if (preset.Content[id] != 0)
                        return null;
                    cost += (tmp.Cost ?? 0);
                    coef += tmp.Coefficient ?? 0;
                }
            }
            return Math.Round(cost * coef, 2);
        }
    }
}