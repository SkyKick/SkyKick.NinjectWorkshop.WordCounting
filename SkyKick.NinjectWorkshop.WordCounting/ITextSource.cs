using System.Threading;
using System.Threading.Tasks;

namespace SkyKick.NinjectWorkshop.WordCounting
{
    /// <summary>
    /// Interface for any component that can provide
    /// Text for <see cref="WordCountingEngine"/> to count.
    /// </summary>
    public interface ITextSource
    {
        /// <summary>
        /// Identifies a specific instance of a 
        /// <see cref="ITextSource"/>.  Used
        /// for Caching and Logging
        /// </summary>
        string TextSourceId {get; }

        Task<string> GetTextAsync(CancellationToken token);
    }
}
