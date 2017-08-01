using System;
using System.Threading;
using System.Threading.Tasks;

namespace SkyKick.NinjectWorkshop.WordCounting.UI
{
    internal class Repl
    {
        private readonly IWordCountingEngine _wordCountingEngine;

        public Repl(IWordCountingEngine wordCountingEngine)
        {
            _wordCountingEngine = wordCountingEngine;
        }

        public async Task RunAsync(CancellationToken token)
        {
            Console.Write("Enter Url: ");

            var url = Console.ReadLine();

            var count = await _wordCountingEngine.CountWordsOnUrlAsync(url, token);

            Console.WriteLine($"Number of words on [{url}]: {count}");
            Console.WriteLine();
        }
    }
}