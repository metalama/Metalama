// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.DesignTime.Utilities;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Services;
using System;
using System.Runtime.CompilerServices;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Testing.UnitTesting
{
    /// <summary>
    /// A base class for all Metalama unit tests that require Metalama services. Exposes a <see cref="CreateTestContext(IAdditionalServiceCollection,string?,string?)"/>
    /// that creates a context with all services. The next step is typically to call one of the methods or properties of the returned <see cref="TestContext"/>.
    /// </summary>
    public abstract class UnitTestClass : IDisposable
    {
        static UnitTestClass()
        {
            TestingServices.Initialize();
        }

        private readonly ITestOutputHelper? _logger;
        private readonly bool _injectLoggingService;
        private readonly TestExceptionReporter _exceptionReporter = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitTestClass"/> class.
        /// </summary>
        /// <param name="logger"></param>
        protected UnitTestClass( ITestOutputHelper? logger = null, bool injectLoggingService = true )
        {
            this._logger = logger != null ? new TestOutputHelperWrapper( logger ) : null;
            this._injectLoggingService = injectLoggingService;
        }

        /// <summary>
        /// Gets an object allowing to write to the test output. 
        /// </summary>
        protected ITestOutputHelper TestOutput => this._logger.AssertNotNull();

        private void AddXunitLogging( IAdditionalServiceCollection testServices )
        {
            // If we have an Xunit test output, override the logger.
            if ( this._logger != null && this._injectLoggingService )
            {
                var loggerFactory = new XunitLoggerFactory( this._logger );
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
            this.AddExceptionHandler( services );

            services.AddGlobalService( _ => TestingServices.CompileTimeAssemblyLocatorProvider );
        }

#pragma warning disable LAMA0821
        protected virtual void ConfigureExtensions( ITestExtensionCollector collector ) { }
#pragma warning restore LAMA0821

        protected virtual void AddSyntaxGenerationOptions( IAdditionalServiceCollection services )
        {
            services.AddProjectService( SyntaxGenerationOptions.Formatted );
        }

        // Resharper disable once VirtualMemberNeverOverridden.Global
        protected virtual void AddExceptionHandler( IAdditionalServiceCollection services )
        {
            ((AdditionalServiceCollection) services).BackstageServices.Add( this._exceptionReporter );
            services.AddGlobalService( provider => new DesignTimeExceptionHandler( provider ) );
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
                this.ConfigureTestContextOptions( contextOptions ?? this.CreateDefaultTestContextOptions() ),
                this.GetMockServices( services ) );

            context.TestName = $"{callerFile}:{callerMemberName}";
            context.TestOutputWriter = this._logger;

            return context;
        }

        [MustDisposeResource]
        protected virtual TestContext CreateTestContextCore( TestContextOptions contextOptions, IAdditionalServiceCollection services )
            => new( contextOptions, services );

        protected virtual TestContextOptions CreateDefaultTestContextOptions() => new();

        private TestContextOptions ConfigureTestContextOptions( TestContextOptions contextOptions )
        {
            var collector = new TestExtensionCollector();
            this.ConfigureExtensions( collector );

            return contextOptions with
            {
                AdditionalAssemblies = contextOptions.AdditionalAssemblies.Add( this.GetType().Assembly ),
                ExtensionTypes = contextOptions.ExtensionTypes.AddRange( collector.ExtensionTypes ),
                DesignTimeExtensionTypes = contextOptions.DesignTimeExtensionTypes.AddRange( collector.DesignTimeExtensionTypes ),
                CompileTimeAssemblies = contextOptions.CompileTimeAssemblies.AddRange( collector.CompileTimeAssemblies )
            };
        }

        private IAdditionalServiceCollection GetMockServices( IAdditionalServiceCollection? arg )
        {
            var services = arg ?? new AdditionalServiceCollection();
            this.ConfigureServices( services );

            return services;
        }

        public void Dispose()
        {
            // We generally don't want to see any exceptions reported during the test.
            // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
            Assert.DoesNotContain( this._exceptionReporter.ReportedExceptions, e => e.GetType().Name is not "ConnectionLostException" );
        }
    }
}