using System;

namespace KFrame
{
    public class KFrameTiming
    {
        public static Func<DateTime> IFrameAbsoluteExpiration = () => DateTime.Today.AddDays(1);
        public static Func<TimeSpan> IFrameCacheMaxAge = () => DateTime.Today.AddDays(1) - DateTime.Now;
        public static Func<DateTimeOffset> IFrameCacheExpires = () => DateTime.Today.ToUniversalTime().AddDays(1);

        public static Func<DateTime> PFrameAbsoluteExpiration = () => DateTime.Now.AddMinutes(1);
        public static Func<DateTime> PFramePolling = () => DateTime.Now.AddMinutes(1);
        public static Func<decimal> PFrameSourceExpireInDays = () => 2M;
    }
}
