using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SkyKick.Bcl.Logging;
using SkyKick.NinjectWorkshop.WordCounting.Email;

namespace SkyKick.NinjectWorkshop.WordCounting
{
    public interface IWordCountingWorkflow
    {
        /// <summary>
        /// Counts Words in <paramref name="source"/>, and sends specific 
        /// emails based on the results.
        /// 
        /// Still returns the total word count.
        /// </summary>
        Task<int> RunWordCountWorkflowAsync(ITextSource source, CancellationToken token);
    }

    internal class WordCountingWorkflow : IWordCountingWorkflow
    {
        private readonly IWordCountingEngine _wordCountingEngine;
        private readonly IEmailClient _emailClient;
        private readonly ILogger _logger;

        public WordCountingWorkflow(IWordCountingEngine wordCountingEngine, IEmailClient emailClient, ILogger logger)
        {
            _wordCountingEngine = wordCountingEngine;
            _emailClient = emailClient;
            _logger = logger;
        }

        public async Task<int> RunWordCountWorkflowAsync(ITextSource source, CancellationToken token)
        {
            var stopWatch = Stopwatch.StartNew();

            int count = 0;
            try
            {
                count = await _wordCountingEngine.CountWordsFromTextSourceAsync(source, token);

                if (count < 1000)
                    await
                        _emailClient
                            .SendEmailAsync(
                                "to@skykick.com",
                                "no-reply@skykick.com",
                                "Less than 1000",
                                token);
                else
                    await
                        _emailClient
                            .SendEmailAsync(
                                "to@skykick.com",
                                "no-reply@skykick.com",
                                "More than 1000",
                                token);
            }
            catch (Exception e)
            {
                _logger.Error($"Exception in Workflow: {e.Message}", e);
            }

            _logger.Debug($"Completed Count Workflow for [{source.TextSourceId}] in [{stopWatch.Elapsed}]");

            return count;
        }
    }
}
