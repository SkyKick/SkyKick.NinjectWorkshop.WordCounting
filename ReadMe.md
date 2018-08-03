## Workshop on Dependency Injection, Mocking, and Testing

_NOTE:_ If you are reading this file inside Visual Studio, it's recommended to install the [Markdown Editor](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.MarkdownEditor).

_NOTE:_ If you do not have access to the SkyKick nuget feed, a copy of the assemblies used in this workshops is located in the `libs` folder.

This workshop does a deep dive on how to leverage Single Responsibility, 
Dependency Injection, Mocking and other Testing technologies to 
create or modify an application to make it highly testable and 
author highly valuable Unit, Cross Component and Scenario Tests.

The Workshop starts with a very simple application and goes step-by-step on how to refactor
and redesign the following code so that we end up with a cleanly designed application with a regression test library and excellent code coverage: 

```csharp
static int CountWordsOnUrl(string url)
{
    string html = string.Empty;
    using (var webClient = new WebClient())
        html = webClient.DownloadString(url);
            
    var text = new CsQuery.CQ(html).Text();

    return text.Split(' ').Length;
}
```

### Table of Contents
1. [Chapter 0 Create Initial PoC](#chapter-0-create-initial-poc)
1. [Chapter 1 Single Responsibility Refactor](#chapter-1-single-responsibility-refactor)
1. [Chapter 2 Initial Tests](#chapter-2-initial-tests)
1. [Chapter 3 Dependency Injection with Ninject](#chapter-3-dependency-injection-with-ninject)
1. [Chapter 4 TDD and the Regresstion Test Suite](#chapter-4-tdd-and-the-regresstion-test-suite)
1. [Chapter 5 Testing Error Handling Policy](#chapter-5-testing-error-handling-policy)
1. [Chapter 6 Replacing Singletons with DI](#chapter-6-replacing-singletons-with-di)
1. [Chapter 7 Factories and File Input](#chapter-7-factories-and-file-input)
1. [Chapter 8 BDD and Scenario Tests](#chapter-8-bdd-and-scenario-tests)


#### Chapter 0 Create Initial PoC

 1. Create an empty Solution called `SkyKick.NinjectWorkshop.WordCounting`

 1. Create a new Solution Folder called `V1`
 1. Inside `V1` Folder, create a new Console Application called `SkyKick.NinjectWorkshop.WordCounting.Prototype`
 1. Add a NuGet reference to `CsQuery 1.3.4`
 1. Add a Reference to `System.Net.Http`
 1. Update the Program.cs with the following code:
    ```csharp
    using System;
    using System.Net;

    namespace SkyKick.NinjectWorkshop.WordCounting.Prototype
    {
        class Program
        {
            static void Main(string[] args)
            {
                while (true)
                {
                    Console.Write("Enter Url: ");

                    var url = Console.ReadLine();

                    Console.WriteLine($"Number of words on [{url}]: {CountWordsOnUrl(url)}");
                    Console.WriteLine();
                }
            }

            static int CountWordsOnUrl(string url)
            {
                string html = string.Empty;
                using (var webClient = new WebClient())
                    html = webClient.DownloadString(url);
            
                var text = new CsQuery.CQ(html).Text();

                return text.Split(' ').Length;
            }
        }
    }
    ```   
  1. Run the Program.  
     1. Enter `https://www.skykick.com`
     
     2. Make sure that a word count is written to the screen

#### Chapter 1 Single Responsibility Refactor

The Prototype application gets the initial job done, but it's not testable.  The `CountWordsOnUrl` method has too many responsibilities, it must know how to:
- Make a Http `Get` Request to a WebSite and receive its Response
- Parsing Text from Html
- Counting the number of words in a String

To make this application more testable, we'll start by following the [Single Responsibility Principle](https://en.wikipedia.org/wiki/Single_responsibility_principle) and break each Responsibility above into its own class.


Each class will be exposed to the broader system as an interface.  This will allow us to easily mock behavior.  Additionally, consumers will not need to be concerned with knowing about individual implementations, they will only declare the interface or contracts that they need in order for they themselves to to do their work.  This principle is called [Inversion of Control](https://en.wikipedia.org/wiki/Inversion_of_control).

8. Create a new Solution Folder called `V2`

1. Create a new Class Library project in the `V2` folder called `SkyKick.NinjectWorkshop.WordCounting`.  This project will store all of the logic of the Word Counting application.
    1. Add a NuGet reference to `SkyKick.Bcl.Logging` from the SkyKick nuget feed.  This package provides the `ILogger` interface and has nice support for DI and Testing.
1. Create a new Console Application project in the `V2` folder called `SkyKick.NinjectWorkshop.WordCounting.UI`.  This project will contain the Console UI used to interact with the Word Counting application.
    1. Add a reference to `SkyKick.NinjectWorkshop.WordCounting`

    1. Add a NuGet reference to `SkyKick.Bcl.Logging` from the SkyKick nuget feed
1. Create a new Class Library project in the `V2` folder called `SkyKick.NinjectWorkshop.WordCounting.Tests`.  This project will contain Tests for both `SkyKick.NinjectWorkshop.WordCounting` and `SkyKcik.NinjectWorkshop.WordCounting.UI`.
    1. Add a reference to `SkyKick.NinjectWorkshop.WordCounting`

    1. Add a reference to `SkyKick.NinjectWorkshop.WordCounting.UI`
1. Move the Word Counting Algorithm to its own class.
   1. Create a new file in `SkyKick.NinjectWorkshop.WordCounting` called `WordCountingAlgorithm`.

    1. This class will contain just the logic for counting the number of words in a string:
        ```csharp
        ﻿namespace SkyKick.NinjectWorkshop.WordCounting
        {
            public interface IWordCountingAlgorithm
            {
                int CountWordsInString(string content);
            }

            internal class WordCountingAlgorithm : IWordCountingAlgorithm
            {
                public int CountWordsInString(string content)
                {
                    return content.Split(' ').Length;
                }
            }
        }
        ```
1. Move the code that reads from the Web to its own file. 
   1.  **`NOTE:`** This is a very important concept - we will wrap code that performs IO, especially static framework code and remove it from Logic code.  This will allow us to write tests that mock out the IO call and fully test our Logic code.  Additionally, from an academic sense, this encapsulation frees our Logic code from knowing the specific semantics of interacting with IO; though in practice the Logic will still need to be responsible for correctly interfacing with IO subsystems (via the wrappers) to handle things like retries and disposing.

   1. Create a new Folder in `SkyKick.NinjectWorkshop.WordCounting` called `Http`.
    1. Create a new Class file called `WebClientWrapper` in `Http`:
        ```csharp
        ﻿using System.Net;
        using System.Threading;
        using System.Threading.Tasks;
        using SkyKick.Bcl.Logging;

        namespace SkyKick.NinjectWorkshop.WordCounting.Http
        {
            public interface IWebClient
            {
                Task<string> GetHtmlAsync(string url, CancellationToken token);
            }

            internal class WebClientWrapper : IWebClient
            {
                private readonly ILogger _logger;

                public WebClientWrapper(ILogger logger)
                {
                    _logger = logger;
                }

                public async Task<string> GetHtmlAsync(string url, CancellationToken token)
                {
                    _logger.Debug($"Downloading [{url}]");

                    using (var client = new WebClient())
                        return await client.DownloadStringTaskAsync(url);
                }
            }
        }
        ```
 1. Move the code that gets Text from a Website into its own file.
    1. Add a NuGet reference to `CsQuery 1.3.4` to `SkyKick.NinjectWorkshop.WordCounting`

    1. Create a new Class file called `WebTextSource` to the Http folder:
        ```csharp
        ﻿using System.Threading;
        using System.Threading.Tasks;

        namespace SkyKick.NinjectWorkshop.WordCounting.Http
        {
            public interface IWebTextSource
            {
                Task<string> GetTextFromUrlAsync(string url, CancellationToken token);
            }

            internal class WebTextSource : IWebTextSource
            {
                private readonly IWebClient _webClient;

                public WebTextSource(IWebClient webClient)
                {
                    _webClient = webClient;
                }

                public async Task<string> GetTextFromUrlAsync(string url, CancellationToken token)
                {
                    var html = await _webClient.GetHtmlAsync(url, token);

                    return new CsQuery.CQ(html).Text();
                }
            }
        }
        ```
    1. **`NOTE:`** This class is using the `IWebClient` that we created in the previous step so it doesn't directly interact with `System.Net.Http.WebClient`.  Also, we use `IWebClient` in the Constructor Parameter instead of explictly refrencing `WebClientWrapper`. Both of these design chocies will allow us to very easily mock out reading from a website when we start writing unit tests.
1. Combine the pieces into `WordCountingEngine`
   1. Create a new Class at the root of `SkyKick.NinjectWorkshop.WordCounting` called `WordCountingEngine`:
        ```csharp
        ﻿using System.Threading;
        using System.Threading.Tasks;
        using SkyKick.Bcl.Logging;
        using SkyKick.NinjectWorkshop.WordCounting.Http;

        namespace SkyKick.NinjectWorkshop.WordCounting
        {
            public interface IWordCountingEngine
            {
                Task<int> CountWordsOnUrlAsync(string url, CancellationToken token);
            }

            internal class WordCountingEngine : IWordCountingEngine
            {
                private readonly IWebTextSource _webTextSource;
                private readonly IWordCountingAlgorithm _wordCountingAlgorithm;

                private readonly ILogger _logger;

                public WordCountingEngine(
                    IWebTextSource webTextSource, 
                    IWordCountingAlgorithm wordCountingAlgorithm, 
                    ILogger logger)
                {
                    _webTextSource = webTextSource;
                    _wordCountingAlgorithm = wordCountingAlgorithm;
                    _logger = logger;
                }

                public async Task<int> CountWordsOnUrlAsync(string url, CancellationToken token)
                {
                    _logger.Debug($"Counting Words on [{url}]");

                    var text = await _webTextSource.GetTextFromUrlAsync(url, token);

                    return _wordCountingAlgorithm.CountWordsInString(text);
                }
            }
        }

        ```

    1. This class neatly ties together the `WordCountingAlgorithm` `IWebTextSource`.  It's Single Responsibility is to call `IWebTextSource` and pass its output to `WordCountingAlgoirthm` thus allowing both pieces to operate as independent units.
1. Create a Repl (Read Evaluate Print Loop) to parse UI input and invoke the `IWordCountingEngine`
    1. This externalizes the Responsibility of parsing user input out of `Program`, which will become responsible only for initializing the system.  

    1. Create a new Class called `Repl` in 
       `SkyKick.NinjectWorkshop.WordCounting.UI`:
        ```csharp
        ﻿using System;
        using System.Threading;
        using System.Threading.Tasks;

        namespace SkyKick.NinjectWorkshop.WordCounting.UI
        {
            internal class Repl
            {
                private readonly IWordCountingEngine _wordCountingEngine;

                public Repl(IWordCountingEngine wordCountingEngine)
                {
                    _wordCountingEngine = wordCountingEngine;
                }

                public async Task RunAsync(CancellationToken token)
                {
                    Console.Write("Enter Url: ");

                    var url = Console.ReadLine();

                    var count = await _wordCountingEngine.CountWordsOnUrlAsync(url, token);

                    Console.WriteLine($"Number of words on [{url}]: {count}");
                    Console.WriteLine();
                }
            }
        }
        ```
1. Update Program to use `Repl`
   1. Replace the default code in `Program` with:
        ```csharp
        ﻿using System.Threading;
        using SkyKick.Bcl.Logging.ConsoleTestLogger;
        using SkyKick.Bcl.Logging.Infrastructure;
        using SkyKick.Bcl.Logging.Log4Net;
        using SkyKick.NinjectWorkshop.WordCounting.Http;

        namespace SkyKick.NinjectWorkshop.WordCounting.UI
        {
            class Program
            {
                static void Main(string[] args)
                {
                    var repl = 
                        new Repl(
                            new WordCountingEngine(
                                new WebTextSource(
                                    new WebClientWrapper(
                                        new ConsoleTestLogger(
                                            typeof(WebClientWrapper), 
                                            new LoggerImplementationHelper()))),
                                new WordCountingAlgorithm(),
                                new ConsoleTestLogger(
                                    typeof(WordCountingEngine), 
                                    new LoggerImplementationHelper())));

                    while (true)
                    {
                        repl.RunAsync(CancellationToken.None).Wait();
                    }
                }
            }
        }
        ```


    1. Take a careful look at `new Repl(...)`.  This is `Program` Single Responsiblity - initializing the object graph for `Repl`.  Because we have designed the class library based on Inversion of Control, we create the entire object graph for `Repl`.  We haven't yet introduced a Dependency Injection framework, but once we do, one of the primary benefits will be that we give DI a series of Bindings and it will take over creating this object graph.
        1. This manual creation of the object graph is sometime refered to as *"Poor Man's DI"*
    1. If you try to compile right now you'll get a compiler error because `WordCountingEngine` and the other concrete classes in `SkyKick.NinjectWorkshop.WordCounting` are inaccessible because of their protection level.
        1. Temporarily, update `SkyKick.NinjectWorkshop.WordCounting` `AssemblyInfo.cs` to allow `SkyKick.NinjectWorkshop.WordCounting.UI` to access `internal` classes:
            ```csharp
           [assembly: InternalsVisibleTo("SkyKick.NinjectWorkshop.WordCounting.UI")]
           ```
        1. We'll fix this later once we introduce Ninject; we'll be able to safely hide implementation classes with Ninject so we can enforce consumers of `SkyKick.NinjectWorkshop.WordCounting` are only allowed to reference interfaces.
 1. Run the Program.  
     1. Enter `https://www.skykick.com`
     
     2. Make sure that a word count is written to the screen

#### Chapter 2 Initial Tests

Now that we have applied Single Responsibility and broken apart the prototype into its constituent parts, lets take advantage of the design and create some Tests

In this section we'll create what we'ver termed a *Cross Component Test*.  This is a Test built using a Unit Test Framework but rather than testing a single class or unit, it tests multiple classes working together.  Writing a Unit Test for `WordCountingEngine` that just verifies that it takes the output from `WebTextSource` and passes it to `WordCountingAlgorithm` would not be very valuable.  Instead if we create a Cross Component Test that uses all of these classes together, but with a mocked `IWebClient` to simulate a web response, we get a test that actually verifies behavior and is valuable.

19. Add NuGet Packages to `SkyKick.NinjectWorkshop.WordCounting.Tests`

    1. Add a NuGet reference to `NUnit 2.6.4` .
    
    1. Add a NuGet reference to `RhinoMocks 3.6.1`.
    1. Add a NuGet reference to `Should 1.1.20`.  This Library adds fluent extensions compliement `Assert` like `ShouldEqual()` which we'll make use of in our tests.
    1. Add a NuGet reference to `SkyKick.Bcl.Logging` from the SkyKick nuget feed
    1. Add a NuGet reference to `SkyKick.Bcl.Extensions` from the SkyKick nuget feed
1. Allow access to Internals for Tests
   1.  Often Tests will need to access `internal` concrete implementations in order to test them.  This is perfectly ok.

    1. Update `SkyKick.NinjectWorkshop.WordCounting` `AssemblyInfo.cs` to allow `SkyKick.NinjectWorkshop.WordCounting.Tests` to access `internal` classes:
        ```csharp
        [assembly: InternalsVisibleTo("SkyKick.NinjectWorkshop.WordCounting.Tests")]
        ```

1.  Add a Sample Html File
    1.  The Cross Component Test we will write will simulate making a call to a web server using a mock of `IWebClient` and will expect html to comeback.  So we'll add a file that contians that markup.

    1. Create a new Folder in `SkyKick.NinjectWorkshop.WordCounting.Tests` called `SampleFiles`
    1. Create a new Text File in `SampleFiles` called `TwoWordsHtml.txt`:
         ```html
        <html><body>Hello World</body></html>
        ```
    1. In the Solution Explorer, right click on `TwoWordsHtml.txt` and select Properties from the Context Menu.  In the Properties Window, change the Build Action to `Embedded Resource`
        1. This will add `TwoWordsHtml.txt` to the compiled Tests dll.  Using `SkyKick.Bcl.Extensions` it will be very easy to read this file from a Test without having to worry about paths.
1. Write `WordCountingEngineTests`

   1. Create a new Class at the root in `SkyKick.NinjectWorkshop.WordCounting.Tests` called `WordCountingEngineTests`: 

        ```csharp
        ﻿using System.Threading;
        using System.Threading.Tasks;
        using NUnit.Framework;
        using Rhino.Mocks;
        using Should;
        using SkyKick.Bcl.Extensions.Reflection;
        using SkyKick.Bcl.Logging.ConsoleTestLogger;
        using SkyKick.Bcl.Logging.Infrastructure;
        using SkyKick.NinjectWorkshop.WordCounting.Http;

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
                /// number of words on a web page using mocked Web Content
                /// </summary>
                [Test]
                [TestCase(
                    "SkyKick.NinjectWorkshop.WordCounting.Tests.SampleFiles.TwoWordsHtml.txt",
                    2)]
                public async Task CountsWordsInSampleFilesCorrectly(
                    string embeddedHtmlResourceName, 
                    int expectedCount)
                {
                    // ARRANGE
                    var fakeUrl = "http://testing.com/";
                    var fakeToken = new CancellationTokenSource().Token;

                    var fakeWebContent = GetType().Assembly.GetEmbeddedResourceAsString(embeddedHtmlResourceName);

                    var mockWebClient = MockRepository.GenerateMock<IWebClient>();
                    mockWebClient
                        .Stub(x => x.GetHtmlAsync(
                            Arg.Is(fakeUrl),
                            Arg.Is(fakeToken)))
                        .Return(Task.FromResult(fakeWebContent));

                    var wordCountingEngine =
                        new WordCountingEngine(
                            new WebTextSource(
                                mockWebClient),
                            new WordCountingAlgorithm(),
                            new ConsoleTestLogger(
                                typeof(WordCountingEngine), 
                                new LoggerImplementationHelper()));

                    // ACT
                    var count = await wordCountingEngine.CountWordsOnUrlAsync(fakeUrl, fakeToken);

                    // ASSERT
                    count.ShouldEqual(expectedCount);
                }
            }
        }
        ```
    1. Run the `CountWordsInSampleFilesCorreclty` Test and verify it passes

1. Explore `WordCountingEngineTests`
    1. There's a lot of important concepts here so lets explore them:
       1. `/// Tests for <see cref="WordCountingEngineTests"/>`{.language-csharp} - I like to add this to Test Fixtures to clearly indicate the primary class that will be tested.  Additionally, using the `<see cref=""/>`{.language-xml} makes it easy to navigate back to the main class.
        
        1. `/// Cross Component test that tests ...`{.language-csharp} I like to add comments at the top of most tests to quickly describe what the test it meant to do.  This makes it easier to maintain the test.

        1. `[TestCase("TwoWordsHtml.txt", 2)]`{.language-csharp} This Attribute instructs NUnit to pass these input parameters to  `CountWordsInSampleFilesCorrectly`.  This is a very important concept because it allows us to write a single Test body and have multiple `[TestCase]` inputs. 
            1. This is the start of building a Regression Test Library.  Later on we'll see how once we find a bug, we can add a new Sample File and then add a new [TestCase] to capture the bug and prove we've resolved it.

        1. `GetType().Assembly.GetEmbeddedResourceAsString(embeddedHtmlResourceName)`{.language-csharp} This is provided by `SkyKick.Bcl.Extensions.Reflection`.  It's a helper for loading `TwoWordsHtml.txt`.  Having test input in a seperate file makes it easier to maintain and work with.  When it comes to string test data, and especially large string test data, having a seperate file is very handy as it means you don't have to deal with odd whitespace or escaping quotes (`"`)

        1. `var mockWebClient = MockRepository.GenerateMock<IWebClient>();`{.language-csharp} Welcome to Rhino Mocks! The method create a dynamic proxy object implementation of `IWebClient` that allows us a number of powerful operations.  We can stub out fake behaviors, inspect method arguments and a lot more. 
            1. `MockRepository.GenerateMock<>();`{.language-csharp}` is your entry point for creating this mocked objects.  
            1. It's technically possible to create a mock of a concrete objects that exposes virtual methods, but its a hell of a lot easier to use interfaces.  This is one of the reasons why it's good practice to create an interface,  even if you will only have one implementation.

        1. `.Stub(x => x.GetHtmlAsync(`{.language-csharp} This instructs Rhino Mocks on how to add a Behavior when ever anyone calls `GetHtmlAsync`
            1. `Arg.Is(fakeUrl)`{.language-csharp} In order to compile, a value must be passed in for ever method parameter needed by `GetHtmlAsync`.  Rhino Mocks offers the `Arg` class to help with this.  Most commonly you can pass `Arg<string>.Is.Anything`{.language-csharp}.  This indicates to Rhino Mocks that this Behavior should trigger regardless of what the input is.  However, for our case we add some extra verification in our test and say we want to ensure that the `url` passed to `IWebClient.GetHtmlAsync` matches the one passed to `WordCountingEngine.CountWordsOnUrlAsync`.  If `WordCountingEngine` passes something other than `_fakeUrl`, our test would fail.

            1. `Return(Task.FromResult(fakeWebContent))`{.language-csharp} **This is the key to our test.**  When `WordCountingEngine.CountWordsOnUrlAsync()`{.language-csharp} calls our mocked `IWebClient.GetHtmlAsync()`{.language-csharp} we return `fakeWebContent`! 

        1. `new WordCountingEngine(new WebTextSource(mockWebClient) ...`{.language-csharp} We build up a full object graph for `WordCountingEngine` only replacing the `IWebClient` with our `mockWebClient`.  This way we can test multiple classes.

        1. `count.ShouldEqual(expectedCount)`{.language-csharp} This is functuatlly equivelant to `Assert.AredEqual(count, expectedCount)`{.language-csharp}, but I find the extension methods provided by the `Should` library to be easier to read and better express intent.

       1. `// ARRANGE`{.language-csharp} Arrange-Act-Assert, or AAA for short, is a common convention for organizing a Unit Test and is good practice.  Using it improves the readability and maintainability of your tests.  Part of the convnetion includes labeling the different sections with a comment.
            1. Arrange - The series of steps necessary to initialize the Class Under Test.  This includes defining Fakes, creating Mocks and creating an instance of the Class Under Test. 
            1. Act - Perform the action that is to be tested.  Often this is invoking a method on the Class Under Test.  Be wary if you find that you are writing a substantial amount of code in this section.  This could mean that you're test is trying to perform too many actions and should be broken into smaller tests or should be a Scenario Test (we'll cover that later) or that you've violated Single Responsibility and you have a class that is doing too many things.
            1. Assert - Validate the result (ie return value) of the Act section and any expected or not-expected side effects (ie calling to a database or throwing an exception).

        1. Fakes vs Mocks vs Stubs - These are terms used to describe different types of variables in a Test and are often prepending to the variable name.  There is disagrement by different experts and frameworks on how the terms should be used: https://stackoverflow.com/questions/346372/whats-the-difference-between-faking-mocking-and-stubbing.  Here's how I use the terms:
            1. Fakes - Dummy data that will be fed to the Class Under Test that either contains no behavior (in the case of data) or, in the case of a class dependency, contains unverifiable behavior, because verifying the behavior would not be valuable.  For example, I might implement a `FakeRepository` that impmements an `IRepository` interface, but is just a wrapper around a `List`.
            1. Mocks - A proxy class that implements an interface and is generated by Rhino Mocks.   Mocks have Behavior defined using methods like `.Stub()` and `.Expecte()` and you can verify the Class Under Test has interacted with the Mock (ie `wordCountingEngine` called `_mockWebClient.GetHtmlAsync`)
            1. Stubs - I don't use this term.  Often the difference between Mocks and Stubs offered by industry experts or mocking frameworks is the difference is whether or not Behavior or meant to be verified.  In practice I have not found it valuable to differentiate.

**Summary**
Our hard work has paid off!  We've taken an untestable application and used SOLID principles to write highly testable code.  And we've proven it by writing an extensible Cross Component test that can be used to start a Regression Test Suite!

#### Chapter 3 Dependency Injection with Ninject

We've refactored our code and it's highly testable.  But, using *"Poor Man's DI"* we're left to build the Object Graph ourselves:

```csharp
var repl = 
    new Repl(
        new WordCountingEngine(
            new WebTextSource(
                new WebClientWrapper(
                    new ConsoleTestLogger(
                        typeof(WebClientWrapper), 
                        new LoggerImplementationHelper()))),
            new WordCountingAlgorithm(),
            new ConsoleTestLogger(
                typeof(WordCountingEngine), 
                new LoggerImplementationHelper())));
```

 Even with only a few classes this already unwieldly.  Imagine having 100s or 1000s of classes; this would not be sustainable.

The primary benefit of using a Dependency Injection framework like Ninject, is it provides tooling so that we don't have to build up this Object Graph.

24. Building a Kernel

    1. Add a NuGet reference to `Ninject 3.2.2.0` to `SkyKick.NinjectWorkshop.WordCounting.UI` if it hasn't already been added.

    1. Create a new Class at the root of  `SkyKick.NinjectWorkshop.WordCounting.UI` called `Startup`:
        ```csharp
        ﻿using Ninject;

        namespace SkyKick.NinjectWorkshop.WordCounting.UI
        {
            public class Startup
            {
                public IKernel BuildKernel()
                {
                    return new StandardKernel();
                }
            }
        }
        ```
        1. Note that this is *not* `static`.  There is no reason for this method to be `static` and in fact, marking it static could be a deteriment to testability, as we'll see later on.

        1. The name `Startup` is not strictly necessary.  It's a convention that I was first expsoed to in ASP.NET Mvc and have since adopted.  I like puting the `BuildKernel` method in a class called `Startup` because it clearly indicates that this it should only be invoked at Startup and should not be called by any application code, other than the code related to starting up the application.
    1.  Update `Program.cs` to use `Startup.BuildKernel`
    
        ```csharp
        using System.Threading;       
        using Ninject;

        namespace SkyKick.NinjectWorkshop.WordCounting.UI
        {
            class Program
            {
                static void Main(string[] args)
                {
                    var kernel = new Startup().BuildKernel();

                    var repl = kernel.Get<Repl>();
            
                    while (true)
                    {
                        repl.RunAsync(CancellationToken.None).Wait();
                    }
                }
            }
        }
        ```
        1. We've now delegated building `Repl` to Ninject! 
        2. **Important:**  Deciding where to build and access a Kernel is a very important design decision.  It should *ONLY* be done at the Entry Point of an application.  For a Cloud Service, that's in `RoleEntryPoint`.  For a Console Application, that's in `Program.Main`  For Web Applications (asp.net mvc, or api), there's a [specialized plugin](https://github.com/ninject/ninject.web.mvc) that automatically plugs in to the ASP.NET Framework's Controller Factory so that you should never access the Kernel at all.

            1. This can be difficult in code bases that were not designed with Inversion of Control and it may be necessary to build and use the Kernel deeper in the stack.  However, once a Kernel is built and used it should not be referenced lower in the stack.
            2. Designing classes that take a dependency of the Kernel is a (anti-)pattern known as Service Locator.  In this design each class is passed the Kernel and they use the Kernel to resolve their dependencies themselves.  Service Locator is bad.  This is discussed at greater detail below in an Appendix.

1. Run `SkyKick.NinjectWorkshop.WordCounting.UI`
    1. You should immediately get an error like:
    
        ```
        Ninject.ActivationException: 'Error activating IWordCountingEngine

        No matching bindings are available, and the type is not self-bindable.

        Activation path:

          2) Injection of dependency IWordCountingEngine into parameter wordCountingEngine of constructor of type Repl

          1) Request for Repl



        Suggestions:

          1) Ensure that you have defined a binding for IWordCountingEngine.

          2) If the binding was defined in a module, ensure that the module has been loaded into the kernel.

          3) Ensure you have not accidentally created more than one kernel.

          4) If you are using constructor arguments, ensure that the parameter name matches the constructors parameter name.

          5) If you are using automatic module loading, ensure the search path and filters are correct.
        ```

     1. There is a problem and Ninject is trying to be helpful.  It was asked to build `Repl`, but `Repl` takes a dependency on `IWordCountingEngine`.  Ninject doesn't know how to build a `IWordCountingEngine`.  We need to tell Ninject which concrete type to build when someone asks for a `IWordCountingEngine`.

1.  Add a simple binding:

    1. Update `Startup`:
        ```csharp
        ﻿using Ninject;
        using SkyKick.NinjectWorkshop.WordCounting;

        namespace SkyKick.NinjectWorkshop.WordCounting.UI
        {
            public class Startup
            {
                public IKernel BuildKernel()
                {
                    var kernel = new StandardKernel();

                    kernel.Bind<IWordCountingEngine>().To<WordCountingEngine>();

                    return kernel;
                }
            }
        }
        ```
        1. This tells Ninject that whenever anyone needs a `IWordCountingEngine`, build a `WordCountingEngine` and give them that instance.

    1. Run `SkyKick.NinjectWorkshop.WordCounting.UI`
        i. The Exception message has now changed, and Ninject has run into the next type it doesn't know how to build.

1. Ninject Modules

   1. Adding all of the necessary bindings by hand will be labor intensive and it's easy to forget to add a binding if you add a new class.  Fortunatly, if we use the convention `Foo` implements `IFoo` we can leverage that convention to automatically add all the bindings!

    1. Add a NuGet reference to `Ninject.Extensions.Conventions 3.2.0.0` to `SkyKick.NinjectWorkshop.WordCounting`

    1. Add a new Class to the root of `SkyKick.NinjectWorkshop.WordCounting` called `NinjectModule`:
        ```csharp
        ﻿using Ninject.Extensions.Conventions;
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
                }
            }
        }
        ```
        1. One or more `NinjectModule` can be passed to the `StandardKernel` constructor and adds bindings.
        1. Using `Ninject.Extensions.Conventions` we can add our default bindings - all classes that follow the naming convention `Foo` implements `IFoo` will automatically bind.
        1. By using `IncludingNonePublicTypes()` `internal` classes will be bound as well.  This means we no longer need to leak internal types to `SkyKick.NinjectWorkshop.WordCounting.UI`

    1.  Update the `AssemblyInfo` class in `SkyKick.NinjectWorkshop.WordCounting.Properties` and remove the line:
        ```csharp  
        [assembly: InternalsVisibleTo("SkyKick.NinjectWorkshop.WordCounting.UI")]
        ```
1. Update `Startup.BuildKernel`
    1. Add the new NinjectModule to `Startup.BuildKernel`:
        ```csharp
        ﻿using Ninject;

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
        ```

        1. Note the use of the full namespace for referencing the `NinjectModule`. I find this to be quite helpful as you'll often be pulling in multiple Modules, and they are all called `NinjectModule`.

1. Run `SkyKick.NinjectWorkshop.WordCounting.UI`
    1. We still get a Ninject Exception, but we've gotten a lot further.  If we look at the exception message `IWebClient` was not bound.  The implementation class is called `WebClientWrapper`.  It doesn't follow the convention, so we'll need to manually add a binding.

1. Before we go any futher, lets TDD this problem by creating a Test to verify Bindings

   
    1.  Update the `AssemblyInfo` class in `SkyKick.NinjectWorkshop.WordCounting.UI.Properties` and add the line:
        ```csharp
        [assembly: InternalsVisibleTo("SkyKick.NinjectWorkshop.WordCounting.Tests")]
        ```
    1. Create a new Class in the root of `SkyKick.NinjectWorkshop.WordCounting.Tests` called `NinjectBindingTests`:
        ```csharp
        ﻿using Ninject;
        using NUnit.Framework;
        using Should;
        using SkyKick.NinjectWorkshop.WordCounting.UI;

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
        ```
        1. This is a very simple but powerful test that will confirm our bindings are not working.
    1. Run the `CanLoadRepl` and confirm that it fails.    

1. Update `NinjectModule` with a binding for `IWebClient`
   ```csharp
    ﻿using Ninject.Extensions.Conventions;
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
            }
        }
    }
   ```
1. Run the `CanLoadRepl` Test
   i. It still fails, but we're almost there!  This time we get an Exception trying to find a Binding for `SkyKick.Bcl.Logging.ILogger`

1. The `SkyKick.Bcl.Logging` includes a `NinjectModule` that we can use. Add it to `Startup.BuildKernel()`:
    ```csharp
    ﻿using Ninject;

    namespace SkyKick.NinjectWorkshop.WordCounting.UI
    {
        public class Startup
        {
            public IKernel BuildKernel()
            {
                return new StandardKernel(
                    new SkyKick.Bcl.Logging.ConsoleTestLogger.NinjectModule(),
                    new SkyKick.NinjectWorkshop.WordCounting.NinjectModule());
            }
        }
    }
    ```

1. Run the `CanLoadRepl` Test
   1. Test should now pass!!

1. Run the Program to confirm  
     1. Enter `https://www.skykick.com`
     
     2. Make sure that a word count is written to the screen
     
1. Finally, let's update `WordCountingEngineTests` so it too can use Ninject instead of building an Object Graph for `WordCountingEngine`:
    ```csharp
    ﻿using System.Threading;
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
            /// number of words on a web page using mocked Web Content
            /// </summary>
            [Test]
            [TestCase(
                "SkyKick.NinjectWorkshop.WordCounting.Tests.SampleFiles.TwoWordsHtml.txt",
                2)]
            public async Task CountsWordsInSampleFilesCorrectly(
                string embeddedHtmlResourceName, 
                int expectedCount)
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
    ```
    1. We need to use `Rebind<IWebClient>()` to override the existing binding for `IWebClient` that's already in the Kernel.
    1. We can use `.ToConstant` to tell Ninject that we want to use a pre-exisiting instance (our mock) instead of having Ninject build anything for us.
    1. Because we ave manipulating the bindings on the Kernel it's very important that the BuildKernel() is not static and returns a new Kernel on every call.  Otherwise, if we had multiple tests that were manipulating bindings our tests could interfere with each other.
1. Run `CountsWordsInSampleFilesCorrectly` and confirm the test passes.
 
#### Chapter 4 TDD and the Regresstion Test Suite

We have a pretty good application at this point; we're using SOLID design principles and have **86%** Test Coverage of `SkyKick.NinjectWorkshop.WordCounting`!

But our QA team found a bug!  A web site with a certain type of html is tripping up `WordCountingAlgorithm`.  So let's TDD the problem and expand our Regression Test Suite

38. Create a new Text File in `SampleFiles` called `WordsWithEntersAndNoSpaces.txt`:

       ```html
       <html>
       <body>
       One
       Two
       Thre
       </body>
       </html>
       ```

    1. Double check there aren't any spaces at the end of the words in `WordsWithEntersAndNoSpaces.txt`
    1. In the Solution Explorer, right click on `WordsWithEntersAndNoSpaces.txt` and select Properties from the Context Menu.  In the Properties Window, change the Build Action to `Embedded Resource`

1. Add the new `TestCase` to `WordCountingEngineTests.CountsWordsInSampleFilesCorrectly`:

    ```csharp
    [Test]
    [TestCase(
        "SkyKick.NinjectWorkshop.WordCounting.Tests.SampleFiles.TwoWordsHtml.txt",
        2)]
    [TestCase(
        "SkyKick.NinjectWorkshop.WordCounting.Tests.SampleFiles.WordsWithEntersAndNoSpaces.txt",
        3)]
    public async Task CountsWordsInSampleFilesCorrectly(
        string embeddedHtmlResourceName, 
        int expectedCount)
    ```

1. Run the new Test Case and verify it fails

1. Now that we have a failing test and have proved the bug, lets fix `WordCountingAlgorithm`:

    ```csharp
    using System;

    namespace SkyKick.NinjectWorkshop.WordCounting
    {
        public interface IWordCountingAlgorithm
        {
            int CountWordsInString(string content);
        }

        internal class WordCountingAlgorithm : IWordCountingAlgorithm
        {
            public int CountWordsInString(string content)
            {
                return 
                content
                    .Replace("\n", " ")
                    .Split(new []{' '}, StringSplitOptions.RemoveEmptyEntries)
                    .Length;
            }
        }
    }
    ```
1.  Run all Test Cases for CountsWordsInSampleFilesCorrectly and verify the Test passes proving the bug is fixed, and we didn't introduce a regression!

You just TDD'd a bug and expanded the Regression Test Library!

#### Chapter 5 Testing Error Handling Policy

Currently our application doesn't have any error handling policy. Lets add one in and see how it 
can be tested.

Lets add the requirement that 
 - If the `IWebClient` throws a general exception or 
gets a 500, we should retry 3 times with a back off period of 0.5s, 1s and 10s.
- If the `IWebClient` gets any http error code *other* than a 500, we should fail immediately and not perform a retry.

43. Add a NuGet reference to `Polly 5.3.0` to `SkyKick.NinjectWorkshop.WordCounting`

1. Create a new Class called `WebTextSourceOptions` in `SkyKick.NinjectWorkshop.WordCounting.Http`:
    ```csharp
    using System;

    namespace SkyKick.NinjectWorkshop.WordCounting.Http
    {
        public class WebTextSourceOptions
        {
            public TimeSpan[] RetryTimes { get; set; } = new[]
            {
                TimeSpan.FromSeconds(0.5),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(10)
            };
        }
    }
    ```
    1. It's ok for `WebTextSourceOptions` to include default values in a real system.

1. Update `WebTextSource` to add retry logic:
    ```csharp
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Polly;

    namespace SkyKick.NinjectWorkshop.WordCounting.Http
    {
        public interface IWebTextSource
        {
            Task<string> GetTextFromUrlAsync(string url, CancellationToken token);
        }

        internal class WebTextSource : IWebTextSource
        {
            private readonly IWebClient _webClient;
            private readonly WebTextSourceOptions _options;

            public WebTextSource(IWebClient webClient, WebTextSourceOptions options)
            {
                _webClient = webClient;
                _options = options;
            }

            public async Task<string> GetTextFromUrlAsync(string url, CancellationToken token)
            {
                var policy =
                    Polly.Policy
                        .Handle<WebException>(webException => 
                            (webException.Response as HttpWebResponse)?.StatusCode == 
                                HttpStatusCode.InternalServerError)
                        .Or<Exception>()
                        .WaitAndRetryAsync(_options.RetryTimes);

                var html = await policy.ExecuteAsync( _ => _webClient.GetHtmlAsync(url, token), token);

                return new CsQuery.CQ(html).Text();
            }
        }
    }
    ```
    1. We could add retry to` WebClientWrapper`, but we want wrappers to be very light weight, they really shouldn't include any additional logic ontop of the api code they wrap.

    1. Note how` WebTextSourceOptions` is injected.  This means `WebTextSource` is not responsible for knowing how to get its own settings, it must be injected.  This also gives us greater flexiblity for testing.
        1. This pattern aligns very nicely with `SkyKick.Bcl.Configuration` and the new Configuration system in .net Core which provides a DI supported subsytem for configuration

    1. Note: on the `ExecuteAsync` lambda, the _ for the lambda parameter.  This is short hand indicating that the variable (`CancellationToken`) wont be used.

1. Create a new Folder called Http in `SkyKick.NinjectWorkshop.WordCounting.Tests`

1. Create a new Class called `WebTextSourceTests` in `SkyKick.NinjectWorkshop.WordCounting.Tests.Http`:

   ```csharp
    using System;
    using System.Collections;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Rhino.Mocks;
    using Should;
    using SkyKick.NinjectWorkshop.WordCounting.Http;
    using SkyKick.Bcl.Extensions.Reflection;

    namespace SkyKick.NinjectWorkshop.WordCounting.Tests.Http
    {
        /// <summary>
        /// Tests for <see cref="WebTextSource"/>
        /// </summary>
        [TestFixture]
        public class WebTextSourceTests
        {
            public IEnumerable InvokesRetryPolicyExceptions()
            {
                yield return new object[]
                {
                    new Exception("General Exception should be retried"),
                    true
                };

                yield return new object[]
                {
                    CreateWebExceptionWithStatusCode(HttpStatusCode.InternalServerError), 
                    // retry on a 500
                    true
                };

                yield return new object[]
                {
                    CreateWebExceptionWithStatusCode(HttpStatusCode.NotFound), 
                    // do not retry on 404
                    false
                };
            }

            /// <summary>
            /// <see cref="WebTextSource"/> will retry on certain 
            /// exceptions but not others.  Verifies when <see cref="IWebClient"/>
            /// throws <paramref name="webClientException"/> that the 
            /// retry policy is invoked if <paramref name="expectRetry"/>.  This
            /// is verified by counting the number of times 
            /// <see cref="IWebClient.GetHtmlAsync"/> is called.
            /// </summary>
            [Test]
            [TestCaseSource(nameof(InvokesRetryPolicyExceptions))]
            public async Task InvokesRetryPolicyOnErrors(Exception webClientException, bool expectRetry)
            {
                // ARRANGE
                var fakeWebTextSourceOptions = new WebTextSourceOptions
                {
                    RetryTimes = new[]
                    {
                        TimeSpan.FromSeconds(0),
                        TimeSpan.FromSeconds(0),
                        TimeSpan.FromSeconds(0)
                    }
                };

                var fakeUrl = "http://testing.com";
                var fakeToken = new CancellationTokenSource().Token;

                var mockWebClient = MockRepository.GenerateMock<IWebClient>();
                mockWebClient
                    .Expect(x => x.GetHtmlAsync(Arg.Is(fakeUrl), Arg.Is(fakeToken)))
                    .Throw(webClientException)
                    .Repeat.Times(
                        // 1 for initial call and then any retries
                        1 +
                        (expectRetry
                            ? fakeWebTextSourceOptions.RetryTimes.Length
                            : 0));

                var webTextSource = new WebTextSource(mockWebClient, fakeWebTextSourceOptions);

                // ACT
                try
                {
                    await webTextSource.GetTextFromUrlAsync(fakeUrl, fakeToken);

                    Assert.Fail("Expected an exception to be thrown but was not.");
                }
                catch (Exception e)
                {
                    // ASSERT
                    e.ShouldEqual(webClientException);

                    mockWebClient.VerifyAllExpectations();
                }
            }

            /// <summary>
            /// Have to use reflection to build <see cref="WebException"/>
            /// because Microsoft doesn't provide public constructors / setters
            /// <para />
            /// This leverages tools from <see cref="SkyKick.Bcl.Extensions.Reflection"/>
            /// to make it a bit easier.
            /// </summary>
            private WebException CreateWebExceptionWithStatusCode(HttpStatusCode status)
            {
                var httpWebResponse = 
                    (HttpWebResponse)
                    Activator.CreateInstance(
                        typeof(HttpWebResponse), 
                        false);

                typeof(HttpWebResponse)
                    .CreateFieldAccessor<HttpStatusCode>("m_StatusCode")
                    .Set(httpWebResponse, status);

                var webException = new WebException("");

                typeof(WebException)
                    .CreateFieldAccessor<WebResponse>("m_Response")
                    .Set(webException, httpWebResponse);

                return webException;
            }
        }
    }

    ```

   1.  Normally it would be very hard to test a retry policy based on an exception thrown by a 3rd party/framework utility, but because we have a wrapper and `WebTextSourceOptions`, it's quite easy.

    1. Use `[TestCaseSouce]` to point to a method that generates test input. This allows us to run code to generate Test Cases that wouldn't be possible with just [TestCase].  This allows our test code to test a single hypothesis (specific exception triggers retry) while still maximizing code reuse.

    1. Use .Expect() to have the ability to Verify that method was called with given method parameters a set number of times.

   1. Use .Throw() to easily have a mock throw an exception
   1. We create a `WebTextSourceOptions` with an array of 0 second retry times to `Verify()` that the retry policy is retrying Web Requests
    1. Use `VerifyAllExpectations()` to verify `GetHtmlAsync` was called the correct number of times
    
1. Run `InvokesRetryPolicyOnErrors` Tests

   1. One of the Test Cases fails! We just found a bug in the retry logic - it retries on a non-transient exception.  That would have been very very hard to identify in a running system!

   1. The Exception that is logged is quiet daunting.  We caught an exception, but it's not the exception we thought it would be, so the `ShouldEqual(webClientException)` threw a new exception. The `Actual` exception is what was thrown by the `WebTextSource`: A `NullReferenceException`.
        1. This is a very important exception to understand when working with Mocks, especially when dealing with Async code.

        1. Key to understanding is knowing how a Mock behaves by default, which is it will return `default()` for any method that has not been stubbed with either `Stub()` or `Expect()`.  When we an Async method is called on a Mock with no Stub, Rhino will return null, and the code will end up trying to `await null` which leades to the `NullReferenceException.`

1. Update InvokesRetryPolicyOnErrors to use a Strict Mock:

   ```csharp
   var mockWebClient = MockRepository.GenerateStrictMock<IWebClient>();
   ```

1. Re-Run `InvokesRetryPolicyOnErrors` Tests
    1. We now get a better `Exception` in the Actual output a `ExpectationViolationException`.  Using Strict mocks will have Rhino throw a very specific `Exception` if the code under test tries to invoke a method that hasn't been stubbed. This is quite useful for helping to diagnose failing tests that use mocks.

1. Update `WebTextSource`:


    ```csharp
    var policy =
        Polly.Policy
            .Handle<WebException>(webException => 
                (webException.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.InternalServerError)
            .Or<Exception>(ex => !(ex is WebException))
            .WaitAndRetryAsync(_options.RetryTimes);
    ```
    
1. Re-Run `InvokesRetryPolicyOnErrors` Tests
   1. Everything should pass.  You just diagnosed and fixed a retry policy bug completly in unit tests, before your code ever made it to prod!

#### Chapter 6 Replacing Singletons with DI

Performance optimization time.  We expect our Word Counter will be asked to count the same url over and over again.  So to speed performance, we'll add a cache. But there's a catch, the cache we will use has a start up penalty.  Before DI we'd use the Singleton pattern to make sure we only instantiated one instance of the cache so we'd only get hit with the penalty once.  But we can use Ninject to replace the Singleton pattern ensuring we only get one instance of the cache.  This eliminates the need for making the class static and results in  highly testable code!

53. Create a new Folder in `SkyKick.NinjectWorkshop.WordCounting` called `Threading`

1.  Create a new Class in `SkyKick.NinjectWorkshop.WordCounting.Threading` called `ThreadSleeper`:

       ```csharp
       using System;
       using System.Threading;

       namespace SkyKick.NinjectWorkshop.WordCounting.Threading
       {
           public interface IThreadSleeper
           {
               void Sleep(TimeSpan timeToSleep);
           }

           internal class ThreadSleeper : IThreadSleeper
           {
               public void Sleep(TimeSpan timeToSleep)
               {
                   Thread.Sleep(timeToSleep);
               }
           }
       }
       ```

1.  Create a new Folder in `SkyKick.NinjectWorkshop.WordCounting` called `Cache`

1.  Create a new Class in `SkyKick.NinjectWorkshop.WordCounting.Cache` called `WordCountCache`:
    ```csharp
    using System;
    using System.Collections.Generic;
    using SkyKick.Bcl.Logging;
    using SkyKick.NinjectWorkshop.WordCounting.Threading;

    namespace SkyKick.NinjectWorkshop.WordCounting.Cache
    {
        public interface IWordCountCache
        {
            bool TryGet(string key, out int value);
            void Add(string key, int value);
        }

        internal class WordCountCache : IWordCountCache
        {
            private readonly Dictionary<string, int> _cache = new Dictionary<string, int>();

            private readonly ILogger _logger;
            private readonly IThreadSleeper _threadSleeper;

            public WordCountCache(ILogger logger, IThreadSleeper threadSleeper)
            {
                _logger = logger;
                _threadSleeper = threadSleeper;
            }

            public bool TryGet(string key, out int value)
            {
                EnsureInitialized();

                var cacheHit =  _cache.TryGetValue(key, out value);

                _logger.Info( (cacheHit ? "Cache Hit" : "Cache Miss") + $": {key}");

                return cacheHit;
            }

            public void Add(string key, int value)
            {
                EnsureInitialized();

                _cache[key] = value;
            }

            private bool _isInitialized;

            private void EnsureInitialized()
            {
                if (_isInitialized)
                    return;

                _logger.Warn("Initializing Cache");

                _threadSleeper.Sleep(TimeSpan.FromSeconds(3));

                _isInitialized = true;
            }
        }
    }
    ```
    1.  Note how we use `IThreadSleeper` to wrap the call to `Thread.Sleep`.  While this might seem a bit extereme, it's very helpful in enabling us to write a unit test that doesn't rely on a call try `TryGet()` taking a long time.

1. Update `WordCountingEngine` to use `IWordCountCache`:

     ```csharp
    using System.Threading;
    
    using System.Threading.Tasks;
    using SkyKick.Bcl.Logging;
    using SkyKick.NinjectWorkshop.WordCounting.Cache;
    using SkyKick.NinjectWorkshop.WordCounting.Http;

    namespace SkyKick.NinjectWorkshop.WordCounting
    {
        public interface IWordCountingEngine
        {
            Task<int> CountWordsOnUrlAsync(string url, CancellationToken token);
        }

        internal class WordCountingEngine : IWordCountingEngine
        {
            private readonly IWebTextSource _webTextSource;
            private readonly IWordCountingAlgorithm _wordCountingAlgorithm;
            private readonly IWordCountCache _wordCountCache;

            private readonly ILogger _logger;

            public WordCountingEngine(
                IWebTextSource webTextSource, 
                IWordCountingAlgorithm wordCountingAlgorithm, 
                ILogger logger, 
                IWordCountCache wordCountCache)
            {
                _webTextSource = webTextSource;
                _wordCountingAlgorithm = wordCountingAlgorithm;
                _logger = logger;
                _wordCountCache = wordCountCache;
            }

            public async Task<int> CountWordsOnUrlAsync(string url, CancellationToken token)
            {
                _logger.Debug($"Counting Words on [{url}]");

                int wordCount;
                if (_wordCountCache.TryGet(url, out wordCount))
                    return wordCount;

                var text = await _webTextSource.GetTextFromUrlAsync(url, token);

                wordCount = _wordCountingAlgorithm.CountWordsInString(text);

                _wordCountCache.Add(url, wordCount);

                return wordCount;
            }
        }
    }
    ```

1. Run the `SkyKick.Ninject.Workshop.WordCounting.UI`
     1. Enter `https://www.skykick.com`.  Note the log message that the Cache is initializing and the program waits for 3 seconds.

     1. Enter https://www.skykick.com again.  Note how there is no log message about initialization and instead we get a log message about a cache hit.

     1. The .UI program is not running multithreaded and the way it's designed, the `Repl` class keeps the full object graph between user input so it's ok that `WordCountCache` is not actually a singleton.

1. Create a Guard Test for `WordCountCache`

   1. Event though `SkyKick.NinjectWorkshop.WordCounting.UI` isn't using the cache from multiple requests, `SkyKick.NinjectWorkshop.WordCounting` might need to support more advanced scenarios in the future, so we want to document that it should be created as a Singleton.  We'll create a Guard Test - a quick test that protects a small but very important implementation detail against modification.
    
    1. Create a new Folder in `SkyKick.NinjectWorkshop.WordCounting.Tests` called `Cache`
    
    1. Create a new Class in `SkyKick.NinjectWorkshop.WordCounting.Tests.Cache` called `WordCountCacheTests`:
        ```csharp
        using System;
        using Ninject;
        using NUnit.Framework;
        using Rhino.Mocks;
        using Should;
        using SkyKick.Bcl.Logging;
        using SkyKick.NinjectWorkshop.WordCounting.Cache;
        using SkyKick.NinjectWorkshop.WordCounting.Threading;

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
                    kernel.Get<IWordCountCache>().Add(fakeKey, fakeValue);

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
        ```
        1.  Because we are testing a component of `SkyKick.NinjectWorkshop.WordCounting`, it's not really appropriate or necessary to use `Startup().BuildKernel()`, so we'll create a new Kernel, using only the modules necessary to build `WordCountCache`.

        1. Note that when stubbing a method that has optional parameters, like for `ILogger.Warn` it's always necessary to pass Arg values for the optional parameters, otherwise RhinoMocks will throw an exception.
        1. We can Bind mocks to a StandardKernel for our test and Ninject is perfectly happy.
           1. However, for `IThreadSleeper` we must use `Rebind()`.  the `SkyKick.NinjectWorkshop.WordCounting.NinjectModule` already has a binding for `IThreadSleeepr`.  If we use `Bind<IThreadSleeper>.ToConstant(mockThreadSleeper)` the call will succeed, however when we do a `kernel.Get()` Ninject will throw an exception because it will not know which of the two bindings to use.  

           1. There is no problem if you use `Rebind `if there is not an existing binding.
        1. Note how it's very useful to have a wrapper around `Thread.Sleep`, it allows the test to run in a fraction of a second, instead of waiting three seconds for the Initialize methods to complete.
        1. Because we're using quantum logging that supports DI, we can also verify that logging occurs :)

1. Run the `WordCountCacheShouldBeBoundAsASingleton` Test and confirm that it fails.  Ninject is exhibiting default behavior, each call to `kernel.Get<IWordCountCache>()` will return a new instance.

1. Update `SkyKick.NinjectWorkshop.WordCounting.NinjectModule`:
    ```csharp
    using Ninject;
    using Ninject.Extensions.Conventions;
    using SkyKick.NinjectWorkshop.WordCounting.Cache;
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
            }
        }
    }
    ```

    1. The InSingletonScope instructs Ninject to only create one instance of a class on the first request and then reuse it for all subsequent requests. 

    1. Notice how we have to use `Rebind` in this case, because the `SelectAllClasses().BindDefaultInterface()` will include a default binding for `IWordCountCache` that we'll need to replace.

1. Re-Run the `WordCountCacheShouldBeBoundAsASingleton` Test
    1. Confirm the test now passes!
    
    2. An interesting thing to note, is we only set `InSingletonScope()` when `IWordCountCache` is requested.  If you were to change the test to request `WordCountCache` it would again fail because Ninject would create two different instances for the request to `Get<WordCountCache>()`.
  
        1. This can be fixed by adding `Bind<WordCountCache>().ToSelf().InSingletonScope()` in the Ninjet Module.
           1. Note the use of `.ToSelf()`, this is done instead of `Bind<WordCountCache>().To<WordCountCache>()`
    
           1. That fixes the case if both requests are for `Get<WordCountCache>()`.  But what if one request was `Get<IWordCountCache>()` and the other was `Get<WordCountCache>()`?  Then it would fail, because Ninject sees each request as different, with different `InSingletonScopes()` bindings.  To solve this is certainly possible, but requires more advanced bindings:
                ```csharp
                 Kernel.Bind<WordCountCache>().To<WordCountCache>().InSingletonScope();
                 Kernel.Rebind<IWordCountCache>().ToMethod(ctx => ctx.Kernel.Get<WordCountCache>());
                ```

            1. When would you use this?  It's valuable when use Interface Segregation but have one object implement two interfaces.  For example, if you had seperate interfaces for a repository, one read only and one write only: `IUserReadRepository` and `IUserWriteRepository`.  And both interfaces are implemented by `UserRepository`.  If `UserRepository` needed to be a Singleton because it did some long running initialization, then it would be necessary to use this technique to make sure a request to either interface returned the same instance:
                ```csharp
                 Kernel.Bind<UserRepository>().ToSelf().InSingletonScope();
                 Kernel.Bind<IUserReadRepository>().ToMethod(ctx => ctx.Kernel.Get<UserRepository>());
                 Kernel.Bind<IUserWriteRepository>().ToMethod(ctx => ctx.Kernel.Get<UserRepository>());
                ````

1. Improve `WordCountingEngineTests` Performance

   1. You might have noticed that our cross component tests are now running a lot longer - `WordCountingEngine` is having to initialize its cache on every Test execution.

    1. We'll add a mock IThreadSleeper that doesn't actually sleep so our tests run quickly again.
    
    1. Update `SkyKick.NinjectWorkshop.WordCoutning.Tests.WordCountingEngineTests`:
        ```csharp
        using System;
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
        using SkyKick.NinjectWorkshop.WordCounting.Threading;
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
                public async Task CountsWordsInSampleFilesCorrectly(
                    string embeddedHtmlResourceName,
                    int expectedCount)
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

                    var mockThreadSleeper = MockRepository.GenerateMock<IThreadSleeper>();
                    mockThreadSleeper
                        .Stub(x => x.Sleep(Arg<TimeSpan>.Is.Anything));

                    kernel.Rebind<IWebClient>().ToConstant(mockWebClient);
                    kernel.Rebind<IThreadSleeper>().ToConstant(mockThreadSleeper);

                    var wordCountingEngine = kernel.Get<WordCountingEngine>();

                    // ACT
                    var count = await wordCountingEngine.CountWordsOnUrlAsync(fakeUrl, fakeToken);

                    // ASSERT
                    count.ShouldEqual(expectedCount);
                }
            }
        }
        ```

        1. This is another example where we can take advantage of the benefit of having the `IThreadSleeper` wrapper.

1. Re-Run `WordCountingEngineTests` and confirm it passes and runs in less than 1 second.

#### Chapter 7 Factories and File Input

We have just recieved a new requirement: Our application must be able to read and count words from File in addition to a reading and counting from a Web Server.

This will require a bit of a redesign as the initial design was tightly coupled with the idea of reading from Web pages.

65. Create a new Class at the root of `SkyKick.NinjectWorkshop.WordCounting` called `ITextSource`:
    
    ```csharp
    using System.Threading;
    using System.Threading.Tasks;

    namespace SkyKick.NinjectWorkshop.WordCounting
    {
        /// <summary>
        /// Interface for any component that can provide
        /// Text for <see cref="WordCountingEngine"/> to count.
        /// </summary>
        public interface ITextSource
        {
            /// <summary>
            /// Identifies a specific instance of a 
            /// <see cref="ITextSource"/>.  Used
            /// for Caching and Logging
            /// </summary>
            string TextSourceId {get; }

            Task<string> GetTextAsync(CancellationToken token);
        }
    }
    ```
    1. To make 'text source' generic, we can't have a named method (like `GetTextFromUrl`) that takes initialization data.  We'll need to do all of our initialization in the constructor.

    1. We'll expose a `TextSourceId` for logging / cache key

1. Update `WebTextSource` to implement `ITextSource`:

    ```csharp
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Polly;

    namespace SkyKick.NinjectWorkshop.WordCounting.Http
    {
        /// <summary>
        /// Don't build / bind directly, use <see cref="IWebTextSourceFactory"/>
        /// </summary>
        internal class WebTextSource : ITextSource
        {
            private readonly IWebClient _webClient;
            private readonly WebTextSourceOptions _options;
            private readonly string _url;

            public WebTextSource(IWebClient webClient, WebTextSourceOptions options, string url)
            {
                _webClient = webClient;
                _options = options;
                _url = url;
            }

            public string TextSourceId => _url;

            public async Task<string> GetTextAsync(CancellationToken token)
            {
                var policy =
                    Polly.Policy
                        .Handle<WebException>(webException => 
                            (webException.Response as HttpWebResponse)?.StatusCode == 
                                HttpStatusCode.InternalServerError)
                        .Or<Exception>(ex => !(ex is WebException))
                        .WaitAndRetryAsync(_options.RetryTimes);

                var html = await policy.ExecuteAsync( _ => _webClient.GetHtmlAsync(_url, token), token);

                return new CsQuery.CQ(html).Text();
            }
        }
    }
    ```
    1. Note how we now need to take `url` in the constructor
        1. Because of this we now have a parameter that we need to pass in to the constructor that does not support DI.  Time to use a Factory.

1. Create a new Class in `SkyKick.NinjectWorkshop.WordCounting.Http` called `WebTextSourceFactory`:

    ```csharp
    namespace SkyKick.NinjectWorkshop.WordCounting.Http
    {
        public interface IWebTextSourceFactory
        {
            ITextSource CreateWebTextSource(string url);
        }

        internal class WebTextSourceFactory : IWebTextSourceFactory
        {
            private readonly IWebClient _webClient;
            private readonly WebTextSourceOptions _options;
        
            public WebTextSourceFactory(IWebClient webClient, WebTextSourceOptions options)
            {
                _webClient = webClient;
                _options = options;
            }

            public ITextSource CreateWebTextSource(string url)
            {
                return new WebTextSource(_webClient, _options, url);
            }
        }
    }
    ```
    1. We'll need to create an interface to define the factory signature so the factory can be consumed by other classes.

    1. Implementation will use constructor injection to pull in all of the dependencies that `WebTextSource` needs, and then will complement that with the non-injectable parameters it needs: `url`.
    1. This allows us to still use DI everywhere, but still support initialization input that will be provided by run time data; in this case user input
    1. It might feel wierd to see the `new` keyword again, but this is perfectly ok.
    
1. Update `WordCountingEngine` to use `ITextSource`:

    ```csharp
    using System.Threading;
    using System.Threading.Tasks;
    using SkyKick.Bcl.Logging;
    using SkyKick.NinjectWorkshop.WordCounting.Cache;
    using SkyKick.NinjectWorkshop.WordCounting.Http;

    namespace SkyKick.NinjectWorkshop.WordCounting
    {
        public interface IWordCountingEngine
        {
            Task<int> CountWordsFromTextSourceAsync(ITextSource source, CancellationToken token);
        }

        internal class WordCountingEngine : IWordCountingEngine
        {
            private readonly IWordCountingAlgorithm _wordCountingAlgorithm;
            private readonly IWordCountCache _wordCountCache;

            private readonly ILogger _logger;

            public WordCountingEngine(
                IWordCountingAlgorithm wordCountingAlgorithm, 
                ILogger logger, 
                IWordCountCache wordCountCache)
            {
                _wordCountingAlgorithm = wordCountingAlgorithm;
                _logger = logger;
                _wordCountCache = wordCountCache;
            }

            public async Task<int> CountWordsFromTextSourceAsync(
                ITextSource source, 
                CancellationToken token)
            {
                _logger.Debug($"Counting Words on [{source.TextSourceId}]");

                int wordCount;
                if (_wordCountCache.TryGet(source.TextSourceId, out wordCount))
                    return wordCount;

                var text = await source.GetTextAsync(token);

                wordCount = _wordCountingAlgorithm.CountWordsInString(text);

                _wordCountCache.Add(source.TextSourceId, wordCount);

                return wordCount;
            }
        }
    }
    ```
    1. We've replaced the url parameter to now use a `ITextSource`
   
1. Create a new Folder in `SkyKick.NinjectWorkshop.WordCounting` called `File`

1. Create a new Class in `SkyKick.NinjectWorkshop.WordCounting.File` called `FileTextSource`:
   ```csharp
    using System.Threading;
    using System.Threading.Tasks;
    using SkyKick.Bcl.Extensions.File;

    namespace SkyKick.NinjectWorkshop.WordCounting.File
    {
        public interface IFileTextSource : ITextSource{}

        /// <summary>
        /// Don't build / bind directly, use <see cref="IFileTextSourceFactory"/>
        /// </summary>
        internal class FileTextSource : IFileTextSource
        {
            private readonly IFile _file;
            private readonly string _path;

            public FileTextSource(IFile file, string path)
            {
                _file = file;
                _path = path;
            }
        
            public string TextSourceId => _path;

            public Task<string> GetTextAsync(CancellationToken token)
            {
                return Task.FromResult(_file.RealAllText(_path));
            }
        }
    }
    ```
    1. We'll use `SkyKick.Bcl.Extensions.File.IFile` to pull in an existing abstraction around the File System.

1. Create a new Class in `SkyKick.NinjectWorkshop.WordCounting.File` called `IFileTextSourceFactory`:

    ```csharp
    ﻿namespace SkyKick.NinjectWorkshop.WordCounting.File
    {
        public interface IFileTextSourceFactory
        {
            IFileTextSource CreateFileTextSource(string path);
        }
    }
    ```
    1. For `IFileTextSourceFactory` we'll use a plugin to avoid having to write the boiler plate factory code we wrote in `WebTextSourceFactory` that pulled in the dependencies and passed them to the `WebTextSouce` constructor.
        1. This plugin will use a number of conventions. Method must start with `Create` and we must create a `IFileTextSource` to help the Factory

1. Add a Nuget reference to `Ninject.Extensions.Factory 3.2.1.0` in `SkyKick.NinjectWorkshop.WordCounting`

1. Update `SkyKick.NinjectWorkshop.WordCounting.NinjectModule` with the specail binding for `IFileTextSourceFactory`:

    ```csharp
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
    ```

1. We're going to need to modify `SkyKick.NinjectWorkshop.WordCounting.UI.Repl` and create helper classes for it to use, but before we do let's create a new namespace for repl.

    1. Create a Folder in `SkyKick.NinjectWorkshop.WordCounting.UI` called `Repl`
    
    2. Move the `Repl` class file into the `Repl` folder.
    
    3. Update the namespace in the `Repl` class to `SkyKick.NinjectWorkshop.WordCounting.UI.Repl`

1. Add a new Class to `SkyKick.NinjectWorkshop.WordCounting.UI.Repl` called `TextSources`: 

    ```csharp
    namespace SkyKick.NinjectWorkshop.WordCounting.UI.Repl
    {
        public enum TextSources
        {
            File = 1,
            Web = 2
        }
    }
    ```
    
1. Add a new Class to `SkyKick.NinjectWorkshop.WordCounting.UI.Repl` called `ReplTextSourceBuilder`: 

    ```csharp
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
    ```

    1. This will drive accepting user input and using the correct Text Source Factory to create a `ITextSource`.
    1. We inject both factories and then decide, based on user input, which one to use to build the ITextSource we want to build.

1. Update `SkyKick.NinjectWorkshop.WordCounting.UI.Repl.Repl` to use `ReplTextSourceBuilder`:
    ```csharp
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    namespace SkyKick.NinjectWorkshop.WordCounting.UI.Repl
    {
        internal class Repl
        {
            private readonly IReplTextSourceBuilder _replTextSourceBuilder;
            private readonly IWordCountingEngine _wordCountingEngine;

            public Repl(IReplTextSourceBuilder replTextSourceBuilder, IWordCountingEngine wordCountingEngine)
            {
                _replTextSourceBuilder = replTextSourceBuilder;
                _wordCountingEngine = wordCountingEngine;
            }

            public async Task RunAsync(CancellationToken token)
            {
                Console.WriteLine("Available Text Sources: ");

                Console.WriteLine(
                    string.Join(
                        "\r\n",
                        Enum.GetValues(typeof(TextSources))
                            .Cast<object>()
                            .Select(v =>
                                $"Enter [{(int)v}] for {Enum.GetName(typeof(TextSources), v)}")
                            .ToArray()));

                var textSourceSelection = (TextSources)Enum.Parse(typeof(TextSources), Console.ReadLine());

                var textSource = _replTextSourceBuilder.PromptUserForInputAndBuildTextSource(textSourceSelection);

                var count = await _wordCountingEngine.CountWordsFromTextSourceAsync(textSource, token);

                Console.WriteLine($"Number of words on [{textSource.TextSourceId}]: {count}");
                Console.WriteLine();
            }
        }
    }
    ```

1. We're now injecting a `IReplTextSourceBuilder` into `Repl`.  We don't have a Ninject Module for `SkyKick.NinjectWorkshop.WordCounting.UI` so `Repl` will no longer resolve correclty.

    1. Add a new Class to `SkyKick.NinjectWorkshop.WordCounting.UI` called `NinjectModule`: 

        ```csharp 
        ﻿using Ninject.Extensions.Conventions;

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
        ```

1. Update `Startup` to use the new `NinjectModule`: 
   ```csharp
   using Ninject;

    namespace SkyKick.NinjectWorkshop.WordCounting.UI
    {
        public class Startup
        {
            public IKernel BuildKernel()
            {
                return new StandardKernel(
                    new SkyKick.Bcl.Logging.ConsoleTestLogger.NinjectModule(),
                    new SkyKick.NinjectWorkshop.WordCounting.NinjectModule(),
                    new SkyKick.NinjectWorkshop.WordCounting.UI.NinjectModule());
            }
        }
    }
   ```

1. We've refactored a few classes that have impacted our Tests.  We'll need to update them.

    1. This shows that having Tests does incur costs - it requires effort to keep them up to date.  Therefor it's important that the Tests deliver value.  Blindly adding a Unit Test becase you can isn't necessarily the best approach.  This is one of the reasons the `WordCountingEngine` Cross Component test is valuable - it tests a number of classes together so we get more test coverage for less maintenance cost.

    1. Update `WordCountingEngineTests`:
        ```csharp
        using System;
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
        using SkyKick.NinjectWorkshop.WordCounting.Threading;
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
                public async Task CountsWordsInSampleFilesCorrectly(
                    string embeddedHtmlResourceName,
                    int expectedCount)
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

                    var mockThreadSleeper = MockRepository.GenerateMock<IThreadSleeper>();
                    mockThreadSleeper
                        .Stub(x => x.Sleep(Arg<TimeSpan>.Is.Anything));

                    kernel.Rebind<IWebClient>().ToConstant(mockWebClient);
                    kernel.Rebind<IThreadSleeper>().ToConstant(mockThreadSleeper);

                    var webTextSource = kernel.Get<IWebTextSourceFactory>().CreateWebTextSource(fakeUrl);

                    var wordCountingEngine = kernel.Get<WordCountingEngine>();

                    // ACT
                    var count = await wordCountingEngine.CountWordsFromTextSourceAsync(webTextSource, fakeToken);

                    // ASSERT
                    count.ShouldEqual(expectedCount);
                }
            }
        }
        ```
    1. Update `WebTextSourceTests`:
        ```csharp
        public async Task InvokesRetryPolicyOnErrors(Exception webClientException, bool expectRetry)
        {
            // ARRANGE
            var fakeWebTextSourceOptions = new WebTextSourceOptions
            {
                RetryTimes = new[]
                {
                    TimeSpan.FromSeconds(0),
                    TimeSpan.FromSeconds(0),
                    TimeSpan.FromSeconds(0)
                }
            };

            var fakeUrl = "http://testing.com";
            var fakeToken = new CancellationTokenSource().Token;

            var mockWebClient = MockRepository.GenerateStrictMock<IWebClient>();
            mockWebClient
                .Expect(x => x.GetHtmlAsync(Arg.Is(fakeUrl), Arg.Is(fakeToken)))
                .Throw(webClientException)
                .Repeat.Times(
                    // 1 for initial call and then any retries
                    1 +
                    (expectRetry
                        ? fakeWebTextSourceOptions.RetryTimes.Length
                        : 0));

            var webTextSource = new WebTextSource(mockWebClient, fakeWebTextSourceOptions, fakeUrl);

            // ACT
            try
            {
                await webTextSource.GetTextAsync(fakeToken);

                Assert.Fail("Expected an exception to be thrown but was not.");
            }
            catch (Exception e)
            {
                // ASSERT
                e.ShouldEqual(webClientException);

                mockWebClient.VerifyAllExpectations();
            }
        }
        ```

1. Run all Tests and verify they pass

#### Chapter 8 BDD and Scenario Tests

Lets add some arbitrary complexity to our application to simulate a real word business demand.  Then we'll see how to leverage Behavior Driven Development (BDD)'s style of testing to easily write some powerful and wide reaching tests.

For this example let's say we've gotten the following requirements:

- If the Word Count is greater than 1000 words then we'll send an email saying "More than 1000 words"
- If the Word Count is less than 1000 words then we'll send an email saying "Less than 1000 words"
- If there is an error counting words, then no email is sent.

82. Create a new Folder in `SkyKick.NinjectWorkshop.WordCounting` called `Email`

1. Create a new Class in `SkyKick.NinjectWorkshop.WordCounting.Email` called `EmailClient`: 

    ```csharp
    using System.Threading;
    using System.Threading.Tasks;
    using SkyKick.Bcl.Logging;

    namespace SkyKick.NinjectWorkshop.WordCounting.Email
    {
        public interface IEmailClient
        {
            Task SendEmailAsync(
                string to, 
                string from, 
                string body, 
                CancellationToken token);
        }

        internal class EmailClient : IEmailClient
        {
            private readonly ILogger _logger;

            public EmailClient(ILogger logger)
            {
                _logger = logger;
            }

            public Task SendEmailAsync(string to, string from, string body, CancellationToken token)
            {
                _logger.Info(
                    $"Sending Email To [{to}] From [{from}]: \r\n" +
                    body);

                return Task.FromResult(true);
            }
        }
    }
    ```

1. Create a new Class in `SkyKick.NinjectWorkshop.WordCounting` called `WordCountingWorkflow`:
    ```csharp
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using SkyKick.Bcl.Logging;
    using SkyKick.NinjectWorkshop.WordCounting.Email;

    namespace SkyKick.NinjectWorkshop.WordCounting
    {
        public interface IWordCountingWorkflow
        {
            /// <summary>
            /// Counts Words in <paramref name="source"/>, and sends specific 
            /// emails based on the results.
            /// 
            /// Still returns the total word count.
            /// </summary>
            Task<int> RunWordCountWorkflowAsync(ITextSource source, CancellationToken token);
        }

        internal class WordCountingWorkflow : IWordCountingWorkflow
        {
            private readonly IWordCountingEngine _wordCountingEngine;
            private readonly IEmailClient _emailClient;
            private readonly ILogger _logger;

            public WordCountingWorkflow(
                IWordCountingEngine wordCountingEngine, 
                IEmailClient emailClient,
                ILogger logger)
            {
                _wordCountingEngine = wordCountingEngine;
                _emailClient = emailClient;
                _logger = logger;
            }

            public async Task<int> RunWordCountWorkflowAsync(ITextSource source, CancellationToken token)
            {
                var stopWatch = Stopwatch.StartNew();

                int count = 0;
                try
                {
                    count = await _wordCountingEngine.CountWordsFromTextSourceAsync(source, token);

                    if (count < 1000)
                        await
                            _emailClient
                                .SendEmailAsync(
                                    "to@skykick.com",
                                    "no-reply@skykick.com",
                                    "Less than 1000",
                                    token);
                    else
                        await
                            _emailClient
                                .SendEmailAsync(
                                    "to@skykick.com",
                                    "no-reply@skykick.com",
                                    "More than 1000",
                                    token);
                }
                catch (Exception e)
                {
                    _logger.Error($"Exception in Workflow: {e.Message}", e);
                }

                _logger.Debug($"Completed Count Workflow for [{source.TextSourceId}] in [{stopWatch.Elapsed}]");

                return count;
            }
        }
    }
    ```

1. Update `Repl` to use `WordCountingWorkflow`:

    ```csharp
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    namespace SkyKick.NinjectWorkshop.WordCounting.UI.Repl
    {
        internal class Repl
        {
            private readonly IReplTextSourceBuilder _replTextSourceBuilder;
            private readonly IWordCountingWorkflow _wordCountingWorkflow;

            public Repl(IReplTextSourceBuilder replTextSourceBuilder, IWordCountingWorkflow wordCountingWorkflow)
            {
                _replTextSourceBuilder = replTextSourceBuilder;
                _wordCountingWorkflow = wordCountingWorkflow;
            }

            public async Task RunAsync(CancellationToken token)
            {
                Console.WriteLine("Available Text Sources: ");

                Console.WriteLine(
                    string.Join(
                        "\r\n",
                        Enum.GetValues(typeof(TextSources))
                            .Cast<object>()
                            .Select(v =>
                                $"Enter [{(int)v}] for {Enum.GetName(typeof(TextSources), v)}")
                            .ToArray()));

                var textSourceSelection = (TextSources)Enum.Parse(typeof(TextSources), Console.ReadLine());

                var textSource = _replTextSourceBuilder.PromptUserForInputAndBuildTextSource(textSourceSelection);

                var count = await _wordCountingWorkflow.RunWordCountWorkflowAsync(textSource, token);

                Console.WriteLine($"Number of words on [{textSource.TextSourceId}]: {count}");
                Console.WriteLine();
            }
        }
    }
    ```

1. Verify all Tests in the Solution pass.

1. Create a new Folder in `SkyKick.NinjectWorkshop.WordCounting.Tests` called `Helpers`

1. Create a new Class in `SkyKick.NinjectWorkshop.WordCounting.Tests.Helpers` called `WebExceptionHelper`:
    ```csharp
    using System;
    using System.Net;
    using SkyKick.Bcl.Extensions.Reflection;

    namespace SkyKick.NinjectWorkshop.WordCounting.Tests.Helpers
    {
        public static class WebExceptionHelper
        {
            /// <summary>
            /// Have to use reflection to build <see cref="WebException"/>
            /// because Microsoft doesn't provide public constructors / setters
            /// <para />
            /// This leverages tools from <see cref="SkyKick.Bcl.Extensions.Reflection"/>
            /// to make it a bit easier.
            /// </summary>
            public static WebException CreateWebExceptionWithStatusCode(HttpStatusCode status)
            {
                var httpWebResponse = 
                    (HttpWebResponse)
                    Activator.CreateInstance(
                        typeof(HttpWebResponse), 
                        false);

                typeof(HttpWebResponse)
                    .CreateFieldAccessor<HttpStatusCode>("m_StatusCode")
                    .Set(httpWebResponse, status);

                var webException = new WebException("");

                typeof(WebException)
                    .CreateFieldAccessor<WebResponse>("m_Response")
                    .Set(webException, httpWebResponse);

                return webException;
            }
        }
    }
    ```

    1. Optional:  `CreateWebExceptionWithStatusCode` is a copy of the private method that was in `WebTextSourceTests`.  Remove the private method from `WebTextSourceTests` and update the Tests in that file to use `WebExceptionHelper`.

1. Add a NuGet reference to `Ninject.MockingKernel.RhinoMocks 3.2.2.0` to `SkyKick.NinjectWorkshop.WordCounting.Tests`

1. Update the NuGet reference to `Ninject.MockingKernel` to `3.2.2.0` in `SkyKick.NinjectWorkshop.WordCounting.Tests`
   1. `Ninject.MockingKernel.RhinoMocks` automatically installs `Ninject.MockingKernel`, however it installs an incompatible version.  If you don't upgrade you'll get Binding Exceptions when trying to use the Mocking Kernel.

1. Add a new Class in `SkyKick.NinjectWorkshop.WordCounting.Tests` called `WordCountingWorkflowTests`:
    ```csharp
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
            /// correct email based on the result of 
            /// <see cref="IWordCountingEngine.CountWordsFromTextSourceAsync"/>.
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
    ```

    1. `WordCountingWorkflow` has a lot of dependencies, so this Test uses a Mocking Kernel to make it easier to deal with them. Mocking Kernel will automatically mock any dependency that is requested via a `Get()` call.  We can also add real bindings to it if we wanted, but that's not necessary here.

        1. Use the `mockingKernel.Get<>().Stub()`{.language-csharp} syntax to directly add a Stub to a mock.
        
        1. Notice how we haven't added any Behavior for an `ILogger` even though `WordCountWorkflow` takes one as a dependency.  The Mocking Kernel will automatically generate a mock for us and give it `wordCountWorkflow`.  Any because all of the logging calls return void, the default mock provided workds just fine here.

    1. Pro Tip - I like to add hints in test descriptions on how to interpret and fix a test if it shows failure conditions.  In this case, if the wrong email is sent, a null refernece exception will be thrown, so I document this in the test comments.

##### Introducing Behavior Driven Development

The `SendsCorrectEmailBasedOnWordCount` we just wrote is a good *unit* test, it tests the primary function of `WordCountingWorkflow` as an isolated unit.  However, since we have followed the Single Responsibility principle, `WordCountingWorkflow` primary work is done in ` if (count < 1000)`{.language-csharp} and our test is of limited *value*.  It would be more valuable if, instead, we could test the larger business value that is being provided.

[Behavior Driven Development (BDD)](https://www.agilealliance.org/glossary/bdd) provides a framework for doing this.  It estabilishes a set of keywords that can be used to describe an entire business scenario and has the added bonus of doing so in such a way that we create a very human readable set of documentation on what our system does that can be easily understood by multiple stake holders including developers, QA, Product Managers, and other Business Users.  

The BDD keywords are *Given, When, Then*:

 - Given - Describe the setup for a Scerario
 - When - Describe the execution of a Scenario
 - Then - Describe the expecetatios following execution of a Scenario

This common sytnax and collaboriation between stake holders is especially powerful when combined with the Agile process in a technique known as [Acceptance Test Drvien Development](https://www.agilealliance.org/glossary/atdd/).  ATDD codifies a story using BDD's Given/When/Then keywords before development begins.  By generating compilable and verifiable tests we can both prove a Story has been completed by pointing towards a series of passing Tests as well as create a record of all completed Stories as development progresses.  Additionally, the practice of generating Scenarios during the planning process can aid in estimation - the more numerous and complex the Scenarios are necessary to describe a Story, the larger its likely to be.

Lets take a look at an example of some BDD tests with a few Scenarios similar to:

```
GIVEN a Url that points to a web site with 3000 words
WHEN the word counting workflow is run
THEN the more than the "more than 1000 words" email is sent
and THEN the website is queried only once
and THEN no exception is logged
and THEN no exception is thrown
```

92. Add the Sample Files we'll need for the Scenario
    1. Create a new File in `SkyKick.NinjectWorkshop.WordCounting.Tests\SampleFiles` called `3000Words.txt` and copy the contents from: [3000Words.txt](https://raw.githubusercontent.com/SkyKick/SkyKick.NinjectWorkshop.WordCounting/master/SkyKick.NinjectWorkshop.WordCounting.Tests/SampleFiles/3000Words.txt)
    1. Create a new File in `SkyKick.NinjectWorkshop.WordCounting.Tests\SampleFiles` called `500Words.txt` and copy the contents from: [500Words.txt](https://raw.githubusercontent.com/SkyKick/SkyKick.NinjectWorkshop.WordCounting/master/SkyKick.NinjectWorkshop.WordCounting.Tests/SampleFiles/500Words.txt)

1. Create a new Class in `SkyKick.NinjectWorkshop.WordCounting.Tests` called `WordCountingWorkflowScenarioTests`: 

   ```csharp
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
                    _wordCountingWorkflow
                        .RunWordCountWorkflowAsync(_webTextSource, CancellationToken.None)
                        .Wait();

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
        }
    }
   ```
    1. The `TestHarness` will be used by all of the Scenarios we'll use in the next steps.  The idea is it will allow our Scenarios to be very clean and concise.
    
    1. Test Harness sets up Mocks and then exposes helper methods for our BDD tests to perform setup and validation.  This is very similar to a Cross Componenet test, the idea here is to test as much of the stack as possible, so we'll only mock out the `WebClient` and `EmailClient`, so we're fully running `WordCountingWorkflow`, `WordCountingEngine`, `WordCountingAlgorithm`, `WordCountCache`, and `WebTextSource`

    1. Note how `VerifyTheOnlyEmailSentHad` does a double verification, first verifying that the correct email was sent the correct number of times and then verifying that no other email was sent.  This technique is important for making sure that Tests are robust enough to catch a case when the wrong input is pased to a method.

    1. Note how all of the Given/When/Then helpers return `TestHarness`.  This is called Fluent Syntax and is not strictly necessary.  It will allows us to do method chaining and make the consuming code a bit more readable.

1. Add the Scenarios to `WordCountingWorkflowScenarioTests`:
    ```csharp
        public class WordCountingWorkflowScenarioTests
        {
            //private class TestHarness { .. }

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
        }
    ```

    1. Notice how concise and readable the code is and yet how much code coverage we get!  The tough work of setting up the test is done in the `TestHarness` so that the Scenario can be quite clean.

    1. Because the Scenario is a class marked with its own `[TestFixture]`{.language-csharp} we can have multiple `[Test]` methods.  This makes it very easy to adhear to the BDD *Then* syntax and makes it so that each `[Test]` method is focused on proving a single post condition.

    1. I execute the *Given* step in the classes constructor to setup the `TestHarness` and initialize it with the `WebSiteHasHtml`.

    1. The *When* step is executed in the `WhenTheWordCountingWorkflowIsRun` method, which clearly indicates the action that is being performed.  Using the `[TestFixtureSetUp]` attribute ensures that the method is only executed once, even if we're executing multiple `[Test]` methods.

1. Let's add another Scenario to `WordCountingWorkflowScenarioTests` to cover the event that the web site has only 500 words:
    ```csharp
        public class WordCountingWorkflowScenarioTests
        {
            //private class TestHarness { .. }

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
    ```
    1. This should highlight that, once the `TestHarness` is in place, its incredibly easy to add new Scenarios!

1. We've shown "happy path" Scenarios.  But we can also capture failure Scenarios.  Add a new child class to `WordCountingWorkflowScenarioTests`: 
    ```csharp
        public class WordCountingWorkflowScenarioTests
        {
            //private class TestHarness { .. }

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
        }
    ```

1. And finally we'll add one more Scenario that captures our retry logic - when the web server returns a 500 error.  Add another child class to `WordCountingWorkflowScenarioTests`: 
    ```csharp
    public class WordCountingWorkflowScenarioTests
    {
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
    ```

    1. These failure Scenarios show how easy it is to add not just happy path Scenarios, but also Scenarios that cover complex retry logic!
