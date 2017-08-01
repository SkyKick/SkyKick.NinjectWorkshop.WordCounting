using Ninject;

namespace SkyKick.NinjectWorkshop.WordCounting.UI
{
    public class Startup
    {
        public IKernel BuildKernel()
        {
            return new StandardKernel(
                new SkyKick.NinjectWorkshop.WordCounting.NinjectModule());
        }
    }
}
