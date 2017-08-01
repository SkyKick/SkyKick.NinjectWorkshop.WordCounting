using Ninject.Extensions.Conventions;
using Ninject.Extensions.Factory;
using SkyKick.NinjectWorkshop.WordCounting.Cache;
using SkyKick.NinjectWorkshop.WordCounting.File;
using SkyKick.NinjectWorkshop.WordCounting.Http;

namespace SkyKick.NinjectWorkshop.WordCounting
{
    public class NinjectModule : Ninject.Modules.NinjectModule
    {
        public override void Load()
        {
            Kernel.Bind(x =>
                x.FromThisAssembly()
                    .IncludingNonePublicTypes()
                    .SelectAllClasses()
                    .BindDefaultInterface());

            Kernel.Bind<IWebClient>().To<WebClientWrapper>();

            Kernel.Rebind<IWordCountCache>().To<WordCountCache>().InSingletonScope();

            Kernel.Bind<IFileTextSourceFactory>().ToFactory();
        }
    }
}
