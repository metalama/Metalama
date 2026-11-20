// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Extensibility;

/// <summary>
/// Tests the warning that is reported when an extension assembly has no reference that satisfies the Roslyn version of the current process (#2399).
/// </summary>
public sealed class ExtensionAssemblyRoslynVersionTests : UnitTestClass
{
    private const string _extensionName = "Issue2399.Extension";

    public ExtensionAssemblyRoslynVersionTests( ITestOutputHelper testOutput ) : base( testOutput ) { }

    /// <summary>
    /// Verifies that the pipeline reports a warning when every reference of an extension assembly targets another Roslyn version.
    /// </summary>
    [Fact]
    public async Task ExtensionDeclaredForAnotherRoslynVersion_ReportsWarning()
    {
        using var testContext = this.CreateTestContext();

        // The paths do not exist. The pipeline must not try to load them, because no reference satisfies the current process.
        var directory = Path.Combine( testContext.BaseDirectory, "extensions" );

        var extensionAssemblies = ImmutableArray.Create(
            new TargetedAssemblyReference( _extensionName, Path.Combine( directory, "1.0", _extensionName + ".dll" ), null, new Version( 1, 0, 0 ) ),
            new TargetedAssemblyReference( _extensionName, Path.Combine( directory, "2.0", _extensionName + ".dll" ), null, new Version( 2, 0, 0 ) ) );

        var serviceProvider = testContext.ServiceProvider.WithService(
            new ExtensionProjectOptions( testContext.ProjectOptions, extensionAssemblies ),
            true );

        var compilation = testContext.CreateCSharpCompilation( "class C { }" );
        var diagnostics = new DiagnosticBag();

        await new CompileTimeAspectPipeline( serviceProvider ).ExecuteAsync( diagnostics.Report, null, compilation, ImmutableArray<ManagedResource>.Empty );

        var warning = Assert.Single( diagnostics, d => d.GetMessage().Contains( _extensionName, StringComparison.Ordinal ) );

        Assert.Equal( DiagnosticSeverity.Warning, warning.Severity );

        var message = warning.GetMessage();

        // The message names the Roslyn versions that the extension declares.
        Assert.Contains( "1.0.0", message, StringComparison.Ordinal );
        Assert.Contains( "2.0.0", message, StringComparison.Ordinal );

        // No error is reported.
        Assert.DoesNotContain( diagnostics, d => d.Severity == DiagnosticSeverity.Error );
    }

    private sealed class ExtensionProjectOptions : ProjectOptionsWrapper
    {
        public ExtensionProjectOptions( IProjectOptions wrapped, ImmutableArray<TargetedAssemblyReference> extensionAssemblies ) : base( wrapped )
        {
            this.ExtensionAssemblies = extensionAssemblies;
        }

        public override ImmutableArray<TargetedAssemblyReference> ExtensionAssemblies { get; }
    }
}
