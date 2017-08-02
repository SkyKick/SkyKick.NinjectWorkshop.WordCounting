using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SkyKick.NinjectWorkshop.WordCounting.UI.Repl
{
    internal class Repl
    {
        private readonly IReplTextSourceBuilder _replTextSourceBuilder;
        private readonly IWordCountingWorkflow _wordCountingWorkflow;

        public Repl(IReplTextSourceBuilder replTextSourceBuilder, IWordCountingWorkflow wordCountingWorkflow)
        {
            _replTextSourceBuilder = replTextSourceBuilder;
            _wordCountingWorkflow = wordCountingWorkflow;
        }

        public async Task RunAsync(CancellationToken token)
        {
            Console.WriteLine("Available Text Sources: ");

            Console.WriteLine(
                string.Join(
                    "\r\n",
                    Enum.GetValues(typeof(TextSources))
                        .Cast<object>()
                        .Select(v =>
                            $"Enter [{(int)v}] for {Enum.GetName(typeof(TextSources), v)}")
                        .ToArray()));

            var textSourceSelection = (TextSources)Enum.Parse(typeof(TextSources), Console.ReadLine());

            var textSource = _replTextSourceBuilder.PromptUserForInputAndBuildTextSource(textSourceSelection);

            var count = await _wordCountingWorkflow.RunWordCountWorkflowAsync(textSource, token);

            Console.WriteLine($"Number of words on [{textSource.TextSourceId}]: {count}");
            Console.WriteLine();
        }
    }
}