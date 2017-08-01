using System.Threading;
using System.Threading.Tasks;
using Ninject;
using NUnit.Framework;
using Rhino.Mocks;
using Should;
using SkyKick.Bcl.Extensions.Reflection;
using SkyKick.Bcl.Logging.ConsoleTestLogger;
using SkyKick.Bcl.Logging.Infrastructure;
using SkyKick.NinjectWorkshop.WordCounting.Http;
using SkyKick.NinjectWorkshop.WordCounting.UI;

namespace SkyKick.NinjectWorkshop.WordCounting.Tests
{
    /// <summary>
    /// Tests for <see cref="WordCountingEngineTests"/>
    /// </summary>
    [TestFixture]
    public class WordCountingEngineTests
    {
        /// <summary>
        /// Cross Component test that tests the happy path of 
        /// <see cref="WordCountingEngine"/> counting the correct
        /// number of words on a web page using mocked
        /// Web Content
        /// </summary>
        [Test]
        [TestCase(
            "SkyKick.NinjectWorkshop.WordCounting.Tests.SampleFiles.TwoWordsHtml.txt",
            2)]
        [TestCase(
            "SkyKick.NinjectWorkshop.WordCounting.Tests.SampleFiles.WordsWithEntersAndNoSpaces.txt",
            3)]
        public async Task CountsWordsInSampleFilesCorrectly(string embeddedHtmlResourceName, int expectedCount)
        {
            // ARRANGE
            var fakeUrl = "http://testing.com/";
            var fakeToken = new CancellationTokenSource().Token;

            var fakeWebContent = GetType().Assembly.GetEmbeddedResourceAsString(embeddedHtmlResourceName);

            var kernel = new Startup().BuildKernel();

            var mockWebClient = MockRepository.GenerateMock<IWebClient>();
            mockWebClient
                .Stub(x => x.GetHtmlAsync(
                    Arg.Is(fakeUrl),
                    Arg.Is(fakeToken)))
                .Return(Task.FromResult(fakeWebContent));

            kernel.Rebind<IWebClient>().ToConstant(mockWebClient);

            var wordCountingEngine = kernel.Get<WordCountingEngine>();

            // ACT
            var count = await wordCountingEngine.CountWordsOnUrlAsync(fakeUrl, fakeToken);

            // ASSERT
            count.ShouldEqual(expectedCount);
        }
    }
}
