using System.Threading;
using System.Threading.Tasks;
using Ninject;
using Ninject.MockingKernel.RhinoMock;
using NUnit.Framework;
using Rhino.Mocks;
using SkyKick.NinjectWorkshop.WordCounting.Email;

namespace SkyKick.NinjectWorkshop.WordCounting.Tests
{
    /// <summary>
    /// Tests <see cref="WordCountingWorkflow"/>
    /// </summary>
    [TestFixture]
    public class WordCountingWorkflowTests
    {
        /// <summary>
        /// Verifies that <see cref="WordCountingWorkflow"/> sends the 
        /// correct email based on the result of <see cref="IWordCountingEngine.CountWordsFromTextSourceAsync"/>.
        /// 
        /// NOTE:  If this test fails with a Null Reference Exception, that likely
        /// means the wrong email was sent, the Mock Behavior didn't match on
        /// <see cref="IEmailClient"/> so <see cref="WordCountingWorkflow"/> ended
        /// up awaiting a null Task
        /// </summary>
        [Test]
        [TestCase(500, "Less than 1000")]
        [TestCase(999, "Less than 1000")]
        [TestCase(1000, "More than 1000")]
        [TestCase(5000, "More than 1000")]
        public async Task SendsCorrectEmailBasedOnWordCount(int wordCount, string expectedEmailBody)
        {
            // ARRANGE
            var fakeTextSource = MockRepository.GenerateMock<ITextSource>();
            var fakeToken = new CancellationTokenSource().Token;

            var mockingKernel = new RhinoMocksMockingKernel();

            mockingKernel
                .Get<IWordCountingEngine>()
                .Expect(x =>
                    x.CountWordsFromTextSourceAsync(
                        Arg.Is(fakeTextSource),
                        Arg.Is(fakeToken)))
                .Return(Task.FromResult(wordCount))
                .Repeat.Once();

            mockingKernel
                .Get<IEmailClient>()
                .Expect(x =>
                    x.SendEmailAsync(
                        to: Arg<string>.Is.Anything,
                        from: Arg<string>.Is.Anything,
                        body: Arg.Is(expectedEmailBody),
                        token: Arg.Is(fakeToken)))
                .Return(Task.FromResult(true))
                .Repeat.Once();

            var wordCountWorkflow = mockingKernel.Get<WordCountingWorkflow>();

            // ACT
            await wordCountWorkflow.RunWordCountWorkflowAsync(fakeTextSource, fakeToken);

            // ASSERT
            mockingKernel
                .Get<IEmailClient>()
                .VerifyAllExpectations();
        }
    }
}
