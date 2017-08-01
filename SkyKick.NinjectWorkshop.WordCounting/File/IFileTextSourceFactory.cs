namespace SkyKick.NinjectWorkshop.WordCounting.File
{
    public interface IFileTextSourceFactory
    {
        IFileTextSource CreateFileTextSource(string path);
    }
}