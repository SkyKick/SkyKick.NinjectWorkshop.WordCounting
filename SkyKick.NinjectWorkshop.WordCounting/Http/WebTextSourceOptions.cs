using System;

namespace SkyKick.NinjectWorkshop.WordCounting.Http
{
    public class WebTextSourceOptions
    {
        public TimeSpan[] RetryTimes { get; set; } = new[]
        {
            TimeSpan.FromSeconds(0.5),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(10)
        };
    }
}