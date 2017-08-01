using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ninject;
using NUnit.Framework;
using Rhino.Mocks;
using Should;
using SkyKick.Bcl.Logging;
using SkyKick.NinjectWorkshop.WordCounting.Cache;
using SkyKick.NinjectWorkshop.WordCounting.Threading;
using SkyKick.NinjectWorkshop.WordCounting.UI;

namespace SkyKick.NinjectWorkshop.WordCounting.Tests.Cache
{
    /// <summary>
    /// Tests for <see cref="WordCountCache"/>
    /// </summary>
    [TestFixture]
    public class WordCountCacheTests
    {
        /// <summary>
        /// Makes sure that requesting multiple instances of
        /// <see cref="WordCountCache"/> does not require multiple
        /// calls to <see cref="WordCountCache.EnsureInitialized"/>.  We also
        /// validate that the cache shares values between multiple instances.
        /// 
        /// We can leverage the fact that <see cref="WordCountCache.EnsureInitialized"/>
        /// class <see cref="IThreadSleeper"/> as a proxy to count the number of 
        /// <see cref="WordCountCache.EnsureInitialized"/>.
        /// 
        /// As an added bonus, we can also make sure we log every time the cache
        /// is Initialized. 
        /// </summary>
        [Test]
        public void WordCountCacheShouldBeBoundAsASingleton()
        {
            // ARRANGE
            var fakeKey = "fake";
            var fakeValue = 5;

            var kernel = 
                new StandardKernel(
                    new SkyKick.NinjectWorkshop.WordCounting.NinjectModule());

            var mockLogger = MockRepository.GenerateMock<ILogger>();
            mockLogger
                .Expect(x => x.Warn(
                    // test will fail if logging code in WordCountCache changes
                    Arg.Is("Initializing Cache"),
                    // optional parameter, but have to pass a value
                    // or RhinoMocks will throw exception
                    Arg<LoggingContext>.Is.Null))
                .Repeat.Once();

            var mockThreadSleeper = MockRepository.GenerateMock<IThreadSleeper>();
            mockThreadSleeper
                .Expect(x => x.Sleep(Arg<TimeSpan>.Is.Anything))
                .Repeat.Once();

            kernel.Bind<ILogger>().ToConstant(mockLogger);
            kernel.Rebind<IThreadSleeper>().ToConstant(mockThreadSleeper);

            // ACT
            kernel.Get<IWordCountCache>()
                .Add(fakeKey, fakeValue);

            int outValue;
            var containsKey =
                kernel.Get<IWordCountCache>()
                    .TryGet(fakeKey, out outValue);

            // ASSERT
            containsKey.ShouldBeTrue();
            outValue.ShouldEqual(fakeValue);

            mockLogger.VerifyAllExpectations();
            mockThreadSleeper.VerifyAllExpectations();
        }
    }
}
