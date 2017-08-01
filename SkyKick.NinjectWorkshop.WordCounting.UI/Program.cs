using System.Threading;
using SkyKick.Bcl.Logging.ConsoleTestLogger;
using SkyKick.Bcl.Logging.Infrastructure;
using SkyKick.Bcl.Logging.Log4Net;
using SkyKick.NinjectWorkshop.WordCounting.Http;

namespace SkyKick.NinjectWorkshop.WordCounting.UI
{
    class Program
    {
        static void Main(string[] args)
        {
            var repl = 
                new Repl(
                    new WordCountingEngine(
                        new WebTextSource(
                            new WebClientWrapper(
                                new ConsoleTestLogger(
                                    typeof(WebClientWrapper), 
                                    new LoggerImplementationHelper()))),
                        new WordCountingAlgorithm(),
                        new ConsoleTestLogger(
                            typeof(WordCountingEngine), 
                            new LoggerImplementationHelper())));

            while (true)
            {
                repl.RunAsync(CancellationToken.None).Wait();
            }
        }
    }
}
