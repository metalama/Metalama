// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Templating.Immutability;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Templating.Immutability;

/// <summary>
/// The harness shared by the tests of <see cref="ImmutabilityValidator"/>, which compiles a fragment of source in memory
/// and runs <see cref="TemplatingCodeValidator"/> over it.
/// </summary>
/// <remarks>
/// The immutability rules examine only compile-time and run-time-or-compile-time types. By default, the fragment is
/// therefore compiled with a second syntax tree that declares <c>[assembly: CompileTime]</c>, so that every top-level
/// type of the fragment is compile-time unless it says otherwise. A test class that needs to control the scope of each
/// type overrides <see cref="IsCompileTimeAssembly"/>.
/// </remarks>
public abstract class ImmutabilityTestBase : UnitTestClass
{
    /// <summary>
    /// The prologue that every fragment needs.
    /// </summary>
    private protected const string _prologue = """
                                               using System;
                                               using System.Collections.Generic;
                                               using System.Collections.Immutable;
                                               using Metalama.Framework.Utilities;

                                               """;

    /// <summary>
    /// Gets a value indicating whether the fragment is compiled in an assembly marked <c>[assembly: CompileTime]</c>.
    /// </summary>
    private protected virtual bool IsCompileTimeAssembly => true;

    /// <summary>
    /// Creates a compilation of a fragment, with the references that the fragments need.
    /// </summary>
    private protected CSharpCompilation CreateCompilation(
        MetalamaTestContext testContext,
        string code,
        bool withMetalamaReference = true )
    {
        var sources = new Dictionary<string, string> { ["Fragment.cs"] = code };

        if ( this.IsCompileTimeAssembly && withMetalamaReference )
        {
            sources["Assembly.cs"] = "[assembly: Metalama.Framework.Aspects.CompileTime]";
        }

        return testContext.CreateCSharpCompilation(
            sources,
            ignoreErrors: true,
            additionalReferences:
            [
                MetadataReference.CreateFromFile( typeof(SyntaxNode).Assembly.Location ),
                MetadataReference.CreateFromFile( typeof(CSharpSyntaxNode).Assembly.Location ),
                MetadataReference.CreateFromFile( typeof(Regex).Assembly.Location ),
                MetadataReference.CreateFromFile( typeof(Uri).Assembly.Location )
            ],
            addMetalamaReferences: withMetalamaReference );
    }

    /// <summary>
    /// Compiles a fragment and returns the immutability diagnostics, in a stable order.
    /// </summary>
    /// <param name="code">The source to compile. It is not required to be free of compilation errors.</param>
    /// <param name="properties">The MSBuild properties of the project, such as <c>MetalamaImmutableTypes</c>.</param>
    /// <param name="withMetalamaReference">Whether the compilation references Metalama.</param>
    private protected Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(
        string code,
        IReadOnlyDictionary<string, string>? properties = null,
        bool withMetalamaReference = true )
    {
        var options = new MetalamaTestContextOptions
        {
            Properties = properties?.ToImmutableDictionary() ?? ImmutableDictionary<string, string>.Empty
        };

        using var testContext = this.CreateTestContext( options );

        var compilation = this.CreateCompilation( testContext, code, withMetalamaReference );
        var diagnostics = new List<Diagnostic>();
        var syntaxTree = compilation.SyntaxTrees.Single( t => t.FilePath.EndsWith( "Fragment.cs", StringComparison.Ordinal ) );

        TemplatingCodeValidator.Validate(
            testContext.ServiceProvider,
            compilation.GetSemanticModel( syntaxTree ),
            diagnostics.Add,
            null,
            false,
            false,
            testContext.CancellationToken );

        // The names of the MSBuild items are verified once per compilation, by the build-time entry point.
        ImmutabilityValidator.ReportUnknownDeclaredTypeNames(
            compilation,
            testContext.ServiceProvider.GetService<Engine.Options.IProjectOptions>(),
            diagnostics.Add );

        return Task.FromResult(
            diagnostics
                .Where( d => d.Id.StartsWith( "LAMA088", StringComparison.Ordinal ) || d.Id.StartsWith( "LAMA089", StringComparison.Ordinal ) )
                .OrderBy( d => d.Location.SourceSpan.Start )
                .ToImmutableArray() );
    }

    /// <summary>
    /// Asserts that the fragment produces exactly one diagnostic, with the expected identifier, and returns its message
    /// so that a test may assert on the path.
    /// </summary>
    private protected async Task<string> AssertSingleDiagnosticAsync( string code, string expectedId )
    {
        var diagnostics = await this.GetDiagnosticsAsync( code );

        Assert.Single( diagnostics );
        Assert.Equal( expectedId, diagnostics[0].Id );

        return diagnostics[0].GetMessage();
    }

    /// <summary>
    /// Asserts that the fragment produces no diagnostic at all.
    /// </summary>
    private protected async Task AssertNoDiagnosticAsync( string code, bool withMetalamaReference = true )
    {
        var diagnostics = await this.GetDiagnosticsAsync( code, withMetalamaReference: withMetalamaReference );

        Assert.Empty( diagnostics.Select( d => d.GetMessage() ) );
    }
}
