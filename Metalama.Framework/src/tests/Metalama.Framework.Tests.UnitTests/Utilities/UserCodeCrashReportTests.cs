// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage;
using Metalama.Compiler;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Diagnostics;
using Metalama.Testing.UnitTesting;
using SharpCrafters.Backstage.Application;
using SharpCrafters.Backstage.Extensibility;
using SharpCrafters.Backstage.ProcessClassification;
using SharpCrafters.Backstage.Telemetry;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Utilities;

/// <summary>
/// Tests of the crash report that is written when compile-time code throws an exception (LAMA0041). See issue #2427.
/// </summary>
public sealed class UserCodeCrashReportTests : UnitTestClass
{
    /// <summary>
    /// A value that the exception message contains and that the removal of sensitive data must remove from the report.
    /// </summary>
    private const string _secret = "SuperSecret123";

    /// <summary>
    /// The code of the test project. The aspect throws an exception from <c>BuildAspect</c>.
    /// </summary>
    private const string _code = $$"""
                                   using System;
                                   using Metalama.Framework.Aspects;
                                   using Metalama.Framework.Code;

                                   class ThrowingAspect : TypeAspect
                                   {
                                       public override void BuildAspect( IAspectBuilder<INamedType> builder )
                                       {
                                           throw new InvalidOperationException( "Login failed for password={{_secret}}" );
                                       }
                                   }

                                   [ThrowingAspect]
                                   class Target { }
                                   """;

    /// <summary>
    /// Extracts the path of the report file from the message of the LAMA0041 diagnostic.
    /// </summary>
    private static readonly Regex _reportPathRegex = new( "Exception details are in '(?<path>[^']+)'" );

    /// <summary>
    /// Initializes a new instance of the <see cref="UserCodeCrashReportTests"/> class.
    /// </summary>
    public UserCodeCrashReportTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// Replaces the application information of the test context by one that creates local crash reports, and registers
    /// the local exception reporter of the Backstage service provider of the test process.
    /// </summary>
    /// <remarks>
    /// The application information of the test context does not create local crash reports, and the test context does
    /// not register the local exception reporter. The report file would not be written without these two services.
    /// </remarks>
    protected override void ConfigureServices( IAdditionalServiceCollection services )
    {
        base.ConfigureServices( services );

        var backstageServices = ((AdditionalServiceCollection) services).BackstageServices;
        backstageServices.Add<IApplicationInfoProvider>( _ => new CrashReportingApplicationInfoProvider(), true );

        backstageServices.Add(
            _ => BackstageServiceFactoryInitializer.ServiceProvider.GetRequiredBackstageService<ILocalExceptionReporter>(),
            true );
    }

    /// <summary>
    /// Verifies that the report of an exception in user code has the content of a report of the Backstage local exception
    /// reporter: the call stack of the reporting code, and the removal of sensitive data.
    /// </summary>
    [Fact]
    public async Task ReportHasTheContentOfTheLocalExceptionReporter()
    {
        using var testContext = this.CreateTestContext();

        // Verifies that the replacement of the application information is effective.
        Assert.True( testContext.ServiceProvider.Global.GetRequiredBackstageService<IApplicationInfoProvider>().Application.ShouldCreateLocalCrashReports );

        var compilation = testContext.CreateCSharpCompilation( _code );
        var pipeline = new CompileTimeAspectPipeline( testContext.ServiceProvider );
        var diagnostics = new DiagnosticBag();

        await pipeline.ExecuteAsync( diagnostics.Report, null, compilation, ImmutableArray<ManagedResource>.Empty );

        var diagnostic = Assert.Single( diagnostics.ToImmutableArray(), d => d.Id == GeneralDiagnosticDescriptors.ExceptionInUserCode.Id );
        var message = diagnostic.GetMessage();
        this.TestOutput.WriteLine( message );

        var match = _reportPathRegex.Match( message );
        Assert.True( match.Success, "The LAMA0041 message does not contain the path of the report." );

        var reportPath = match.Groups["path"].Value;
        Assert.NotEqual( "(none)", reportPath );
        Assert.True( File.Exists( reportPath ), $"The report file '{reportPath}' does not exist." );

        try
        {
            var content = File.ReadAllText( reportPath );
            this.TestOutput.WriteLine( content );

            // The report contains the exception.
            Assert.Contains( "Login failed for password=", content, StringComparison.Ordinal );

            // The report contains the call stack of the reporting code, which has the frames above the catch point.
            Assert.Contains( "===== Reporting Call Stack =====", content, StringComparison.Ordinal );
            Assert.Contains( nameof(this.ReportHasTheContentOfTheLocalExceptionReporter), content, StringComparison.Ordinal );

            // The sensitive data is removed.
            Assert.DoesNotContain( _secret, content, StringComparison.Ordinal );
        }
        finally
        {
            File.Delete( reportPath );
        }
    }

    /// <summary>
    /// An application information that creates local crash reports.
    /// </summary>
    private sealed class CrashReportingApplicationInfo : ApplicationInfoBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CrashReportingApplicationInfo"/> class.
        /// </summary>
        public CrashReportingApplicationInfo() : base( typeof(CrashReportingApplicationInfo).Assembly, MetalamaProduct.Profile ) { }

        /// <inheritdoc />
        public override string Name => "Metalama.Framework.Tests.UnitTests";

        /// <inheritdoc />
        public override bool ShouldCreateLocalCrashReports => true;
    }

    /// <summary>
    /// An <see cref="IApplicationInfoProvider"/> that returns a <see cref="CrashReportingApplicationInfo"/>.
    /// </summary>
    private sealed class CrashReportingApplicationInfoProvider : IApplicationInfoProvider
    {
        /// <inheritdoc />
        public IApplicationInfo Application { get; } = new CrashReportingApplicationInfo();

        /// <inheritdoc />
        public ProcessKind ProcessKind => ProcessKindDetector.GetCurrentProcessKind();
    }
}
