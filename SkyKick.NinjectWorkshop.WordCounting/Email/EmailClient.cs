using System.Threading;
using System.Threading.Tasks;
using SkyKick.Bcl.Logging;

namespace SkyKick.NinjectWorkshop.WordCounting.Email
{
    public interface IEmailClient
    {
        Task SendEmailAsync(
            string to, 
            string from, 
            string body, 
            CancellationToken token);
    }

    internal class EmailClient : IEmailClient
    {
        private readonly ILogger _logger;

        public EmailClient(ILogger logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string to, string from, string body, CancellationToken token)
        {
            _logger.Info(
                $"Sending Email To [{to}] From [{from}]: \r\n" +
                body);

            return Task.FromResult(true);
        }
    }
}
