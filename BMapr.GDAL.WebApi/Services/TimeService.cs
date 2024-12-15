namespace BMapr.GDAL.WebApi.Services
{
    public class TimeService
    {
        public static DateTime UnixSecondsToDateTime(long timestamp, bool local = false)
        {
            var offset = DateTimeOffset.FromUnixTimeSeconds(timestamp);
            return local ? offset.LocalDateTime : offset.UtcDateTime;
        }
        public static DateTime UnixMillisecondsToDateTime(long timestamp, bool local = false)
        {
            var offset = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
            return local ? offset.LocalDateTime : offset.UtcDateTime;
        }
    }
}
