using System;
using SkyKick.NinjectWorkshop.WordCounting.File;
using SkyKick.NinjectWorkshop.WordCounting.Http;

namespace SkyKick.NinjectWorkshop.WordCounting.UI.Repl
{
    public interface IReplTextSourceBuilder
    {
        ITextSource PromptUserForInputAndBuildTextSource(TextSources textSource);
    }

    internal class ReplTextSourceBuilder : IReplTextSourceBuilder
    {
        private readonly IFileTextSourceFactory _fileTextSourceFactory;
        private readonly IWebTextSourceFactory _webTextSourceFactory;

        public ReplTextSourceBuilder(
            IFileTextSourceFactory fileTextSourceFactory, 
            IWebTextSourceFactory webTextSourceFactory)
        {
            _fileTextSourceFactory = fileTextSourceFactory;
            _webTextSourceFactory = webTextSourceFactory;
        }

        public ITextSource PromptUserForInputAndBuildTextSource(TextSources textSource)
        {
            switch (textSource)
            {
                case TextSources.File:
                    Console.Write("Enter Path: ");
                    var path = Console.ReadLine();
                    return _fileTextSourceFactory.CreateFileTextSource(path);

                case TextSources.Web:
                    Console.Write("Enter Url: ");
                    var url = Console.ReadLine();
                    return _webTextSourceFactory.CreateWebTextSource(url);

                default:
                    throw new NotImplementedException(
                        $"{Enum.GetName(typeof(TextSources), textSource)} is Not Supported");
            }
        }
    }
}