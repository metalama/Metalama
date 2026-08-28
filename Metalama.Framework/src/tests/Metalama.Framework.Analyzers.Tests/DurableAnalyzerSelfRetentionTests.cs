// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Xunit;

namespace Metalama.Framework.Analyzers.Tests;

/// <summary>
/// Asserts that the analyzer does not itself retain the compilations it analyses.
/// </summary>
/// <remarks>
/// <para>
/// Roslyn keeps one instance of a <see cref="DiagnosticAnalyzer"/> alive for the lifetime of the process, so a cache
/// of symbols held in a field of the analyzer would retain the compilation those symbols came from. That is exactly
/// the defect this analyzer exists to report, which is why the property is asserted rather than assumed.
/// </para>
/// <para>
/// This is also the positive control of the suite, in the spirit of <c>MemoryLeakAssertSelfTests</c>: a set of
/// durability tests that all pass is indistinguishable from one whose assertions never fire.
/// </para>
/// </remarks>
public sealed class DurableAnalyzerSelfRetentionTests
{
    private static readonly MetadataReference[] _references =
    [
        MetadataReference.CreateFromFile( typeof(object).Assembly.Location ),
        MetadataReference.CreateFromFile(
            Path.Combine( Path.GetDirectoryName( typeof(object).Assembly.Location )!, "netstandard.dll" ) ),
        MetadataReference.CreateFromFile(
            Path.Combine( Path.GetDirectoryName( typeof(object).Assembly.Location )!, "System.Runtime.dll" ) ),
        MetadataReference.CreateFromFile( typeof(SyntaxNode).Assembly.Location ),
        MetadataReference.CreateFromFile( typeof(DurableAttribute).Assembly.Location )
    ];

    private const string _code = """
                                 using Metalama.Framework.Utilities;
                                 using Microsoft.CodeAnalysis;

                                 [Durable]
                                 class Annotated
                                 {
                                     private SyntaxTree? _tree;
                                     private string? _name;
                                 }
                                 """;

    /// <remarks>
    /// Every strong reference is confined to this method, which is never inlined, because a debug build keeps every
    /// local alive until the end of the method that declares it. The method therefore hands back only a weak
    /// reference, exactly as <c>DesignTimeEditingSimulator</c> does in the design-time memory-leak suite.
    /// </remarks>
    [MethodImpl( MethodImplOptions.NoInlining )]
    private static async Task<WeakReference> AnalyzeAndGetWeakReferenceAsync( DiagnosticAnalyzer analyzer, string assemblyName )
    {
        var compilation = CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText( _code, new CSharpParseOptions( LanguageVersion.CSharp12 ) )],
            _references,
            new CSharpCompilationOptions( OutputKind.DynamicallyLinkedLibrary ) );

        var diagnostics = await compilation
            .WithAnalyzers( ImmutableArray.Create( analyzer ) )
            .GetAnalyzerDiagnosticsAsync();

        // Prove that the run did what it was supposed to do. A retention assertion over an analysis that produced
        // nothing would pass for the wrong reason.
        Assert.Contains( diagnostics, d => d.Id == "LAMA0870" );

        return new WeakReference( compilation );
    }

    [Fact]
    public async Task AnalyzingASecondCompilation_DoesNotRetainTheFirst()
    {
        var analyzer = new DurableContractAnalyzer();

        var first = await AnalyzeAndGetWeakReferenceAsync( analyzer, "First" );

        // The second run is what would surface a cache held on the analyzer instance rather than on the compilation.
        await AnalyzeAndGetWeakReferenceAsync( analyzer, "Second" );

        Collect();

        Assert.False( first.IsAlive, "The analyzer retained the first compilation." );

        GC.KeepAlive( analyzer );
    }

    /// <remarks>
    /// A single collection is not enough, because an object with a finalizer is reclaimed only by a later one.
    /// </remarks>
    private static void Collect()
    {
        for ( var i = 0; i < 3; i++ )
        {
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true );
            GC.WaitForPendingFinalizers();
        }
    }
}
