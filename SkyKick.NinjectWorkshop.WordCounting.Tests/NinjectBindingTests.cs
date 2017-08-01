using Ninject;
using NUnit.Framework;
using Should;
using SkyKick.NinjectWorkshop.WordCounting.UI;
using SkyKick.NinjectWorkshop.WordCounting.UI.Repl;

namespace SkyKick.NinjectWorkshop.WordCounting.Tests
{
    /// <summary>
    /// Validates the Bindings in 
    /// <see cref="Startup.BuildKernel"/>
    /// </summary>
    [TestFixture]
    public class NinjectBindingTests
    {
        /// <summary>
        /// <see cref="Repl"/> is the DI entry
        /// point used by <see cref="Program.Main"/>, so 
        /// verify all dependencies are correctly bound.
        /// </summary>
        [Test]
        public void CanLoadRepl()
        {
            // ARRANGE
            var kernel = new Startup().BuildKernel();

            // ACT
            var repl = kernel.Get<Repl>();

            // ASSERT
            repl.ShouldNotBeNull();
        }
    }
}
