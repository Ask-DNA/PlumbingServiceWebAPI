namespace PlumbingServiceWebAPI.Models
{
    public class DateTimeJsonable
    {
        public int Year { get; set; } = 0;
        public int Month { get; set; } = 0;
        public int Day { get; set; } = 0;
        public int Hour { get; set; } = 0;
        public int Minute { get; set; } = 0;
        public int Second { get; set; } = 0;

        public DateTime? ToDateTime()
        {
            DateTime? result;
            try { result = new(Year, Month, Day, Hour, Minute, Second); }
            catch (Exception) { result = null; }
            return result;
        }
    }
}
