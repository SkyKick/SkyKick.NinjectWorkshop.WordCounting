using System.Threading;
using System.Threading.Tasks;
using Ninject;
using NUnit.Framework;
using Rhino.Mocks;
using SkyKick.Bcl.Extensions.Reflection;
using SkyKick.Bcl.Logging;
using SkyKick.NinjectWorkshop.WordCounting.Email;
using SkyKick.NinjectWorkshop.WordCounting.Http;
using SkyKick.NinjectWorkshop.WordCounting.Threading;
using SkyKick.NinjectWorkshop.WordCounting.UI;
using System;
using System.Net;
using SkyKick.NinjectWorkshop.WordCounting.Tests.Helpers;

namespace SkyKick.NinjectWorkshop.WordCounting.Tests
{
    public class WordCountingWorkflowScenarioTests
    {
        private class TestHarness
        {
            private const string _fakeUrl = "http://test.com";
            private readonly WebTextSource _webTextSource;

            private readonly IWebClient _mockWebClient;
            private readonly IEmailClient _mockEmailClient;
            private readonly ILogger _mockLogger;

            private readonly WordCountingWorkflow _wordCountingWorkflow;

            public TestHarness(WebTextSourceOptions options = null)
            {
                var kernel = new Startup().BuildKernel();

                _mockWebClient = MockRepository.GenerateMock<IWebClient>();
                kernel.Rebind<IWebClient>().ToConstant(_mockWebClient);

                _mockEmailClient = MockRepository.GenerateMock<IEmailClient>();
                _mockEmailClient
                    .Stub(x => x.SendEmailAsync(
                        to: Arg<string>.Is.Anything,
                        from: Arg<string>.Is.Anything,
                        body: Arg<string>.Is.Anything,
                        token: Arg<CancellationToken>.Is.Anything))
                    .Return(Task.FromResult(true));
                kernel.Rebind<IEmailClient>().ToConstant(_mockEmailClient);

                _mockLogger = MockRepository.GenerateMock<ILogger>();
                _mockLogger
                    .Stub(x =>
                        x.Debug(Arg<string>.Is.Anything, Arg<LoggingContext>.Is.Anything))
                        // capture Debug Messages and write to Console so we can see messages
                        // in test window.
                    .Do(new Action<string, LoggingContext>((msg, ctx) => Console.WriteLine(msg)));
                kernel.Rebind<ILogger>().ToConstant(_mockLogger);

                // Disable the Cache Initializer's Thread Sleeper
                kernel.Rebind<IThreadSleeper>().ToConstant(MockRepository.GenerateMock<IThreadSleeper>());
                
                _wordCountingWorkflow = kernel.Get<WordCountingWorkflow>();

                _webTextSource = 
                    new WebTextSource(
                        _mockWebClient,
                        options ?? kernel.Get<WebTextSourceOptions>(),
                        _fakeUrl);
            }

            #region GIVEN Helpers

            public TestHarness WebSiteHasHtml(string html)
            {
                _mockWebClient
                    .Stub(x =>
                        x.GetHtmlAsync(
                            Arg.Is(_fakeUrl),
                            Arg<CancellationToken>.Is.Anything))
                    .Return(Task.FromResult(html));

                return this;
            }

            public TestHarness WebSiteThrowsWebException(HttpStatusCode statusCode)
            {
                _mockWebClient
                    .Stub(x =>
                        x.GetHtmlAsync(
                            Arg.Is(_fakeUrl),
                            Arg<CancellationToken>.Is.Anything))
                    .Throw(WebExceptionHelper.CreateWebExceptionWithStatusCode(statusCode));

                return this;
            }

            #endregion

            #region WHEN Helpers

            public TestHarness RunWordCountWorkflow()
            {
                _wordCountingWorkflow.RunWordCountWorkflowAsync(_webTextSource, CancellationToken.None).Wait();

                return this;
            }

            #endregion

            #region THEN Helpers

            public TestHarness VerifyWebClientWasCalled(int numberOfTimes)
            {
                _mockWebClient
                    .AssertWasCalled(x => 
                        x.GetHtmlAsync(
                            Arg.Is(_fakeUrl),
                            Arg<CancellationToken>.Is.Anything),

                        options => options.Repeat.Times(numberOfTimes));

                return this;
            }

            public TestHarness VerifyTheOnlyEmailSentHad(string body, int numberOfTimes)
            {
                // test the expected email was sent the correct number of times
                _mockEmailClient
                    .AssertWasCalled(x => 
                        x.SendEmailAsync(
                            to: Arg<string>.Is.Anything,
                            from: Arg<string>.Is.Anything,
                            body: Arg.Is(body),
                            token: Arg<CancellationToken>.Is.Anything),
                        
                        options => options.Repeat.Times(numberOfTimes));

                // test no other emails were sent
                _mockEmailClient
                    .AssertWasNotCalled(x => 
                        x.SendEmailAsync(
                            to: Arg<string>.Is.Anything,
                            from: Arg<string>.Is.Anything,
                            body: Arg<string>.Matches(b => !string.Equals(b, body)),
                            token: Arg<CancellationToken>.Is.Anything));

                return this;
            }

            public TestHarness VerifyThatNoEmailWasSent()
            {
                // can just reuse VerifyTheOnlyEmailSentHad, but pass it 0
                return VerifyTheOnlyEmailSentHad(body: "no body", numberOfTimes: 0);
            }


            public TestHarness VerifyExceptionLoggedAsExpected(bool shouldBeLogged)
            {
                _mockLogger
                    .AssertWasCalled(x => 
                        x.Error(
                            Arg<string>.Matches(msg => msg.Contains("Exception")),
                            Arg<Exception>.Is.Anything,
                            Arg<LoggingContext>.Is.Anything),
                        
                        options => options.Repeat.Times(shouldBeLogged ? 1 : 0));

                return this;
            }

            #endregion
        }

        [TestFixture]
        [Category("WordCountingWorkflowScenarios")]
        public class GivenAUrlThatPointsToAWebSiteWith3000Words
        {
            private readonly TestHarness _testHarness;

            public GivenAUrlThatPointsToAWebSiteWith3000Words()
            {
                _testHarness = new TestHarness();

                _testHarness
                    .WebSiteHasHtml(
                        GetType().Assembly.GetEmbeddedResourceAsString(
                            "SkyKick.NinjectWorkshop.WordCounting.Tests.SampleFiles.3000Words.txt"));
            }

            [TestFixtureSetUp]
            public void WhenTheWordCountingWorkflowIsRun()
            {
                _testHarness.RunWordCountWorkflow();
            }

            [Test]
            public void ThenTheWebSiteIsQueriedOnlyOnce()
            {
                _testHarness.VerifyWebClientWasCalled(numberOfTimes: 1);
            }

            [Test]
            public void ThenTheMoreThan1000WordsEmailIsSent()
            {
                _testHarness.VerifyTheOnlyEmailSentHad(body: "More than 1000", numberOfTimes: 1);
            }

            [Test]
            public void ThenNoExceptionIsLogged()
            {
                _testHarness.VerifyExceptionLoggedAsExpected(shouldBeLogged: false);
            }

            [Test]
            public void ThenNoExceptionIsThrown()
            {
                // if an exception was thrown, we wouldn't get here so nothing to test
            }
        }

        [TestFixture]
        [Category("WordCountingWorkflowScenarios")]
        public class GivenAUrlThatPointsToAWebSiteWith500Words
        {
            private readonly TestHarness _testHarness;

            public GivenAUrlThatPointsToAWebSiteWith500Words()
            {
                _testHarness = new TestHarness();

                _testHarness
                    .WebSiteHasHtml(
                        GetType().Assembly.GetEmbeddedResourceAsString(
                            "SkyKick.NinjectWorkshop.WordCounting.Tests.SampleFiles.500Words.txt"));
            }

            [TestFixtureSetUp]
            public void WhenTheWordCountingWorkflowIsRun()
            {
                _testHarness.RunWordCountWorkflow();
            }

            [Test]
            public void ThenTheWebSiteIsQueriedOnlyOnce()
            {
                _testHarness.VerifyWebClientWasCalled(numberOfTimes: 1);
            }

            [Test]
            public void ThenTheMoreThan1000WordsEmailIsSent()
            {
                _testHarness.VerifyTheOnlyEmailSentHad(body: "Less than 1000", numberOfTimes: 1);
            }

            [Test]
            public void ThenNoExceptionIsLogged()
            {
                _testHarness.VerifyExceptionLoggedAsExpected(shouldBeLogged: false);
            }

            [Test]
            public void ThenNoExceptionIsThrown()
            {
                // if an exception was thrown, we wouldn't get here so nothing to test
            }
        }

        [TestFixture]
        [Category("WordCountingWorkflowScenarios")]
        public class GivenAUrlThatPointsToAWebSiteThatDoesNotExist
        {
            private readonly TestHarness _testHarness;

            public GivenAUrlThatPointsToAWebSiteThatDoesNotExist()
            {
                _testHarness = new TestHarness();

                _testHarness.WebSiteThrowsWebException(HttpStatusCode.NotFound);
            }

            [TestFixtureSetUp]
            public void WhenTheWordCountingWorkflowIsRun()
            {
                _testHarness.RunWordCountWorkflow();
            }

            [Test]
            public void ThenTheWebSiteIsQueriedOnlyOnce()
            {
                _testHarness.VerifyWebClientWasCalled(numberOfTimes: 1);
            }

            [Test]
            public void ThenNoEmailIsSent()
            {
                _testHarness.VerifyThatNoEmailWasSent();
            }

            [Test]
            public void ThenAnExceptionIsLogged()
            {
                _testHarness.VerifyExceptionLoggedAsExpected(shouldBeLogged: true);
            }

            [Test]
            public void ThenNoExceptionIsThrown()
            {
                // if an exception was thrown, we wouldn't get here so nothing to test
            }
        }

        [TestFixture]
        [Category("WordCountingWorkflowScenarios")]
        public class GivenAUrlThatPointsToAWebSiteThatThrowsAnInternalServerError
        {
            private readonly TestHarness _testHarness;
            private readonly WebTextSourceOptions _webTextSourceOptions;

            public GivenAUrlThatPointsToAWebSiteThatThrowsAnInternalServerError()
            {
                _webTextSourceOptions = new WebTextSourceOptions
                {
                    RetryTimes = new[]
                    {
                        TimeSpan.FromSeconds(0),
                        TimeSpan.FromSeconds(0)
                    }
                };

                _testHarness = new TestHarness(_webTextSourceOptions);

                _testHarness.WebSiteThrowsWebException(HttpStatusCode.InternalServerError);
            }

            [TestFixtureSetUp]
            public void WhenTheWordCountingWorkflowIsRun()
            {
                _testHarness.RunWordCountWorkflow();
            }

            [Test]
            public void ThenTheWebSiteIsQueriedMultipleTimes()
            {
                _testHarness.VerifyWebClientWasCalled(
                    numberOfTimes: _webTextSourceOptions.RetryTimes.Length + 1);
            }

            [Test]
            public void ThenNoEmailIsSent()
            {
                _testHarness.VerifyThatNoEmailWasSent();
            }

            [Test]
            public void ThenAnExceptionIsLogged()
            {
                _testHarness.VerifyExceptionLoggedAsExpected(shouldBeLogged: true);
            }

            [Test]
            public void ThenNoExceptionIsThrown()
            {
                // if an exception was thrown, we wouldn't get here so nothing to test
            }
        }
    }
}