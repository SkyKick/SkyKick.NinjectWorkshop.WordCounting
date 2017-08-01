using System.Threading;
using System.Threading.Tasks;
using SkyKick.Bcl.Extensions.File;

namespace SkyKick.NinjectWorkshop.WordCounting.File
{
    public interface IFileTextSource : ITextSource{}

    /// <summary>
    /// Don't build / bind directly, use <see cref="IFileTextSourceFactory"/>
    /// </summary>
    internal class FileTextSource : IFileTextSource
    {
        private readonly IFile _file;
        private readonly string _path;

        public FileTextSource(IFile file, string path)
        {
            _file = file;
            _path = path;
        }
        
        public string TextSourceId => _path;

        public Task<string> GetTextAsync(CancellationToken token)
        {
            return Task.FromResult(_file.RealAllText(_path));
        }
    }
}
