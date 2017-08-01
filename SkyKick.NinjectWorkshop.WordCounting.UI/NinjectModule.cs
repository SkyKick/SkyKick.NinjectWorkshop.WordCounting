using Ninject.Extensions.Conventions;

namespace SkyKick.NinjectWorkshop.WordCounting.UI
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
        }
    }
}
