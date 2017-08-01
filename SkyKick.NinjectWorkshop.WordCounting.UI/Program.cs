using System.Threading;
using Ninject;

namespace SkyKick.NinjectWorkshop.WordCounting.UI
{
    class Program
    {
        static void Main(string[] args)
        {
            var kernel = new Startup().BuildKernel();

            var repl = kernel.Get<Repl.Repl>();
            
            while (true)
            {
                repl.RunAsync(CancellationToken.None).Wait();
            }
        }
    }
}
