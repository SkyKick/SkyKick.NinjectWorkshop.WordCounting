using System;
using System.Threading;

namespace SkyKick.NinjectWorkshop.WordCounting.Threading
{
    public interface IThreadSleeper
    {
        void Sleep(TimeSpan timeToSleep);
    }

    internal class ThreadSleeper : IThreadSleeper
    {
        public void Sleep(TimeSpan timeToSleep)
        {
            Thread.Sleep(timeToSleep);
        }
    }
}
