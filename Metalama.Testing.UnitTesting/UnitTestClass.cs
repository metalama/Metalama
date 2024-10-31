// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Maintenance;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Services;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Xunit.Abstractions;

namespace Metalama.Testing.UnitTesting
{
    /// <summary>
    /// A base class for all Metalama unit tests that require Metalama services. Exposes a <see cref="CreateTestContext(IAdditionalServiceCollection,string?,string?)"/>
    /// that creates a context with all services. The next step is typically to call one of the methods or properties of the returned <see cref="TestContext"/>.
    /// </summary>
    public abstract class UnitTestClass
    {
        // PERF: Intentionally as a global instance so that it can be shared among all tests.
        private static readonly ReferenceAssemblyLocatorProvider _referenceAssemblyLocatorProvider;
        
        static UnitTestClass()
        {
            TestingServices.Initialize();

            _referenceAssemblyLocatorProvider =
                new ReferenceAssemblyLocatorProvider( BackstageServiceFactory.ServiceProvider.GetRequiredBackstageService<ITempFileManager>() );
        }

        private readonly ITestOutputHelper? _testOutputHelper;
        private readonly bool _injectLoggingService;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitTestClass"/> class.
        /// </summary>
        /// <param name="testOutputHelper"></param>
        protected UnitTestClass( ITestOutputHelper? testOutputHelper = null, bool injectLoggingService = true )
        {
            this._testOutputHelper = testOutputHelper;
            this._injectLoggingService = injectLoggingService;
        }

        /// <summary>
        /// Gets an object allowing to write to the test output. 
        /// </summary>
        protected ITestOutputHelper TestOutput => this._testOutputHelper.AssertNotNull();

        private void AddXunitLogging( IAdditionalServiceCollection testServices )
        {
            // If we have an Xunit test output, override the logger.
            if ( this._testOutputHelper != null && this._injectLoggingService )
            {
                var loggerFactory = new XunitLoggerFactory( this._testOutputHelper );
                ((AdditionalServiceCollection) testServices).BackstageServices.Add( _ => loggerFactory );
            }
        }

        /// <summary>
        /// Adds services or mocks that are common to all tests in the current class. This method is called
        /// by <see cref="CreateTestContext(string?,string?)"/> and the
        /// <paramref name="services"/> parameter is the one passed to the <see cref="CreateTestContext(IAdditionalServiceCollection,string?,string?)"/>, if any,
        /// or an empty collection otherwise.
        /// </summary>
        protected virtual void ConfigureServices( IAdditionalServiceCollection services )
        {
            this.AddSyntaxGenerationOptions( services );
            this.AddXunitLogging( services );
            
            services.AddGlobalService( _ => _referenceAssemblyLocatorProvider );
        }

        protected virtual void AddSyntaxGenerationOptions( IAdditionalServiceCollection services )
        {
            services.AddProjectService( SyntaxGenerationOptions.Formatted );
        }

        /// <summary>
        /// Creates a collection of additional services that can then be passed to <see cref="CreateTestContext(IAdditionalServiceCollection,string?,string?)"/>.
        /// </summary>
        [PublicAPI]
        protected static IAdditionalServiceCollection CreateAdditionalServiceCollection( params IService[] services )
        {
            return new AdditionalServiceCollection( services );
        }

        [MustDisposeResource]
        protected TestContext CreateTestContext( [CallerFilePath] string? callerFile = null, [CallerMemberName] string? callerMemberName = null )
            => this.CreateTestContextImpl( null, null, callerFile, callerMemberName );

        /// <summary>
        /// Creates a test context with a collection of additional services or mocks.
        /// </summary>
        [MustDisposeResource]
        protected TestContext CreateTestContext(
            IAdditionalServiceCollection service,
            [CallerFilePath] string? callerFile = null,
            [CallerMemberName] string? callerMemberName = null )
            => this.CreateTestContextImpl(
                null,
                service,
                callerFile,
                callerMemberName );

        /// <summary>
        /// Creates a test context, optionally with a non-default <see cref="TestContextOptions"/> or a collection of additional services or mocks.
        /// </summary>
        [MustDisposeResource]
        protected TestContext CreateTestContext(
            TestContextOptions? contextOptions,
            IAdditionalServiceCollection? services = null,
            [CallerFilePath] string? callerFile = null,
            [CallerMemberName] string? callerMemberName = null )
            => this.CreateTestContextImpl( contextOptions, services, callerFile, callerMemberName );

        [MustDisposeResource]
        private TestContext CreateTestContextImpl(
            TestContextOptions? contextOptions,
            IAdditionalServiceCollection? services = null,
            string? callerFile = null,
            string? callerMemberName = null )
        {
            var context = this.CreateTestContextCore(
                contextOptions ?? this.GetDefaultTestContextOptions(),
                this.GetMockServices( services ) );

            context.TestName = $"{callerFile}:{callerMemberName}";
            context.TestOutputWriter = this._testOutputHelper;

            return context;
        }

        [MustDisposeResource]
        protected virtual TestContext CreateTestContextCore( TestContextOptions contextOptions, IAdditionalServiceCollection services )
            => new( contextOptions, services );

        protected virtual TestContextOptions GetDefaultTestContextOptions()
            => new() { AdditionalAssemblies = ImmutableArray.Create( this.GetType().Assembly ) };

        private IAdditionalServiceCollection GetMockServices( IAdditionalServiceCollection? arg )
        {
            var services = arg ?? new AdditionalServiceCollection();
            this.ConfigureServices( services );

            return services;
        }
    }
}