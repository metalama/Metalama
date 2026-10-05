// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Extensibility.CallSites;
using Metalama.Framework.Engine.Observers;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Extensibility;

/// <summary>
/// Tests of the redirections that <see cref="ExtensionTransformationFactory"/> creates, as the linker applies them.
/// </summary>
/// <remarks>
/// The extension of these tests redirects every invocation of <c>Source.Compute</c> in the source compilation to <c>Interceptors.Compute</c>.
/// The rewritten code is tested in detail by the aspect tests of the proof of concept in <c>Metalama.Framework.Tests.AspectTests.ExtensionPoints</c>.
/// </remarks>
public sealed class ExtensionTransformationLinkerTests : UnitTestClass
{
    private static readonly Dictionary<string, string> _code = new()
    {
        ["A.cs"] = """
                   using Metalama.Framework.Aspects;

                   internal class TheAspect : TypeAspect { }

                   internal static class Source
                   {
                       public static int Compute( int x ) => x;
                   }

                   internal static class Interceptors
                   {
                       public static int Compute( int x ) => x;
                   }

                   [TheAspect]
                   internal class C
                   {
                       int M() => Source.Compute( 1 );
                   }
                   """,
        ["B.cs"] = """
                   internal class D
                   {
                       int M() => Source.Compute( 2 ) + Source.Compute( 3 );
                   }
                   """
    };

    /// <summary>
    /// Verifies that the intermediate compilation, which the linker produces before it inlines and simplifies the code, binds the redirected
    /// calls to the target of the redirection.
    /// </summary>
    [Fact]
    public async Task IntermediateCompilation_ContainsRewrittenCall()
    {
        var observer = new IntermediateCompilationObserver();

        await this.ExecuteAsync( _code.ToList(), observer );

        var compilation = Assert.Single( observer.Compilations );
        var treeOfD = compilation.SyntaxTreeCollection.Single( t => t.FilePath == "B.cs" );
        var semanticModel = compilation.Compilation.GetSemanticModel( treeOfD );

        var invokedMethods = (await treeOfD.GetRootAsync( TestContext.Current.CancellationToken ))
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select( i => semanticModel.GetSymbolInfo( i ).Symbol )
            .ToList();

        Assert.Equal( 2, invokedMethods.Count );
        Assert.All( invokedMethods, m => Assert.Equal( "Interceptors.Compute(int)", m?.ToDisplayString() ) );
    }

    /// <summary>
    /// Verifies that the rewritten code does not depend on the order of the syntax trees in the compilation.
    /// </summary>
    [Fact]
    public async Task Determinism_ShuffledTreeOrder()
    {
        var orderedCode = _code.ToOrderedList( p => p.Key, StringComparer.Ordinal );
        var result1 = await this.ExecuteAsync( orderedCode );
        var result2 = await this.ExecuteAsync( Enumerable.Reverse( orderedCode ).ToList() );

        var text1 = GetTextByPath( result1 );
        var text2 = GetTextByPath( result2 );

        Assert.Contains( "global::Interceptors.Compute(2)", text1["B.cs"].Replace( " ", "" ), StringComparison.Ordinal );
        Assert.Equal( text1, text2 );
    }

    private static IReadOnlyDictionary<string, string> GetTextByPath( CompileTimeAspectPipelineResult result )
        => result.ResultingCompilation.SyntaxTreeCollection
            .Where( t => t.FilePath is "A.cs" or "B.cs" )
            .ToDictionary( t => t.FilePath, t => t.ToString() );

    private async Task<CompileTimeAspectPipelineResult> ExecuteAsync( IReadOnlyList<KeyValuePair<string, string>> code, ILinkerObserver? observer = null )
    {
        var additionalServices = new AdditionalServiceCollection();
        additionalServices.AddProjectService( new RedirectionSwitch() );

        if ( observer != null )
        {
            additionalServices.AddProjectService( observer );
        }

        using var testContext = this.CreateTestContext(
            this.CreateDefaultTestContextOptions() with { ExtensionTypes = ImmutableArray.Create( typeof(RedirectingExtension) ) },
            additionalServices );

        var pipeline = new CompileTimeAspectPipeline( testContext.ServiceProvider );

        // The dictionary that the helper receives keeps the insertion order, which is the order of the syntax trees in the compilation.
        var compilation = testContext.CreateCSharpCompilation( new OrderedCode( code ) );

        Assert.Equal( code.SelectAsArray( p => p.Key ), compilation.SyntaxTrees.Select( t => t.FilePath ).Where( p => p is "A.cs" or "B.cs" ) );

        var diagnostics = new List<Diagnostic>();

        var result = await pipeline.ExecuteAsync( diagnostics.Add, null, compilation, ImmutableArray<ManagedResource>.Empty, testContext.CancellationToken );

        Assert.True( result.IsSuccessful, string.Join( Environment.NewLine, diagnostics ) );

        return result.Value;
    }

    /// <summary>
    /// A read-only dictionary that enumerates its entries in the order given at construction.
    /// </summary>
    private sealed class OrderedCode : IReadOnlyDictionary<string, string>
    {
        private readonly IReadOnlyList<KeyValuePair<string, string>> _entries;

        public OrderedCode( IReadOnlyList<KeyValuePair<string, string>> entries )
        {
            this._entries = entries;
        }

        public int Count => this._entries.Count;

        public string this[ string key ] => this._entries.Single( p => p.Key == key ).Value;

        public IEnumerable<string> Keys => this._entries.SelectAsArray( p => p.Key );

        public IEnumerable<string> Values => this._entries.SelectAsArray( p => p.Value );

        public bool ContainsKey( string key ) => this._entries.Any( p => p.Key == key );

        public bool TryGetValue( string key, out string value )
        {
            foreach ( var entry in this._entries )
            {
                if ( entry.Key == key )
                {
                    value = entry.Value;

                    return true;
                }
            }

            value = null!;

            return false;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => this._entries.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => this.GetEnumerator();
    }

    /// <summary>
    /// The project service whose presence enables <see cref="RedirectingExtension"/>, so that the extension does nothing in other tests.
    /// </summary>
    private sealed class RedirectionSwitch : IProjectService;

    /// <summary>
    /// Records the intermediate compilations of the linker.
    /// </summary>
    private sealed class IntermediateCompilationObserver : ILinkerObserver
    {
        public List<PartialCompilation> Compilations { get; } = new();

        public void OnIntermediateCompilationCreated( PartialCompilation compilation ) => this.Compilations.Add( compilation );
    }

    /// <summary>
    /// An extension that redirects every invocation of <c>Source.Compute</c> to <c>Interceptors.Compute</c>, in the name of the aspect applied to
    /// the type <c>C</c>.
    /// </summary>
    private sealed class RedirectingExtension : PipelineExtension
    {
        public override Task ExecuteTransformingContributorsAsync( ExtensionTransformationContext context, CancellationToken cancellationToken )
        {
            if ( context.ServiceProvider.GetService<RedirectionSwitch>() == null )
            {
                return Task.CompletedTask;
            }

            var compilation = context.SourceCompilationWithFinalAspects;
            var aspectInstance = (IAspectInstanceInternal) compilation.Types.OfName( "C" ).Single().Enhancements().GetAspectInstances().Single();

            var origin = new ExtensionContributionOrigin(
                aspectInstance.Predecessors[0],
                aspectInstance.ToString()!,
                TemplateProvider.FromInstance( aspectInstance.Aspect ),
                null,
                new AspectLayerId( aspectInstance.AspectClass ),
                aspectInstance );

            var target = CallSiteRedirectionTarget.Existing(
                context.FinalCompilation.Types.OfName( "Interceptors" ).Single().Methods.OfName( "Compute" ).Single() );

            var callSites = context.SourceCompilation.PartialCompilation.SyntaxTreeCollection
                .SelectMany( t => t.GetRoot( cancellationToken ).DescendantNodes() )
                .OfType<InvocationExpressionSyntax>()
                .Where( i => i.Expression.ToString() == "Source.Compute" );

            foreach ( var callSite in callSites )
            {
                context.TransformationFactory.RedirectInvocation( origin, new InvocationRedirectionRequest( callSite, target, CallSiteReceiverMode.Drop ) );
            }

            return Task.CompletedTask;
        }
    }
}
