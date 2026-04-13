// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Pipeline.DesignTime;
using Metalama.Framework.Engine.Services;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.Linker;

/// <summary>
/// Property-style regression test for the linker's directive-trivia preservation invariant.
/// <para>
/// For each (member kind × rewrite path) seed snippet, this test injects a uniquely-tagged
/// <c>#warning probe_*</c> at every "must-survive" trivia position in the target source,
/// re-runs the pipeline, and asserts that each probe appears <em>exactly once</em> in the
/// linker's output.
/// </para>
/// <para>
/// Equality (count == 1), not just presence, is the assertion — that catches both drop and
/// duplication bugs (e.g., a misguided fix to <c>GetIndentationTrivia</c> that emits
/// preserved directives at multiple brace positions where its output is shared).
/// </para>
/// <para>
/// The pipeline configuration (compile-time aspect compilation) is built once per corpus
/// entry and reused across all probe variants, since only the target source is mutated —
/// the aspect source is invariant per entry.
/// </para>
/// </summary>
public sealed class LinkerTriviaPreservationTests : UnitTestClass
{
    public LinkerTriviaPreservationTests( ITestOutputHelper logger ) : base( logger ) { }

    public static IEnumerable<object[]> Corpus()
    {
        yield return new object[] { "Method_BlockBody", _overrideMethodAspect, _methodBlockBodyTarget };
        yield return new object[] { "Method_ExpressionBody", _overrideMethodAspect, _methodExpressionBodyTarget };
        yield return new object[] { "Method_Async", _overrideMethodAspect, _methodAsyncTarget };
        yield return new object[] { "Property_AutoProperty", _overridePropertyAspect, _propertyAutoTarget };
        yield return new object[] { "Property_BlockAccessors", _overridePropertyAspect, _propertyBlockAccessorsTarget };
        yield return new object[] { "Property_ExpressionBodyAccessors", _overridePropertyAspect, _propertyExpressionBodyAccessorsTarget };
        yield return new object[] { "Property_WholeExpressionBody", _overridePropertyAspect, _propertyWholeExpressionBodyTarget };
        yield return new object[] { "Event_FieldLike", _overrideEventAspect, _eventFieldLikeTarget };
        yield return new object[] { "Event_BlockAccessors", _overrideEventAspect, _eventBlockAccessorsTarget };
        yield return new object[] { "Constructor_BlockBody", _initializerAspect, _constructorTarget };
        yield return new object[] { "Constructor_ExpressionBody", _initializerAspect, _constructorExpressionBodyTarget };
    }

    [Theory]
    [MemberData( nameof(Corpus) )]
    public async Task DirectivesArePreservedAsync( string label, string aspectSource, string targetSource )
    {
        using var testContext = this.CreateTestContext();

        var sources = new Dictionary<string, string> { ["Aspect.cs"] = aspectSource, ["Target.cs"] = targetSource };
        var seedCompilation = testContext.CreateCSharpCompilation( sources );

        // Sanity check: the unmodified seed must compile cleanly through the pipeline before
        // we can blame the linker for any per-probe failure.
        var pipeline = new TestablePreviewAspectPipeline( testContext.ServiceProvider );
        var initDiagnostics = new DiagnosticBag();

        Assert.True(
            pipeline.InvokeTryInitialize( initDiagnostics, seedCompilation, default, out var configuration ),
            $"{label}: pipeline initialization failed.\n{FormatDiagnostics( initDiagnostics )}" );

        var seedDiagnostics = new DiagnosticBag();
        var seedResult = await pipeline.ExecutePreviewAsync( seedDiagnostics, PartialCompilation.CreateComplete( seedCompilation ), configuration!, default );

        Assert.True(
            seedResult.IsSuccessful,
            $"{label}: seed (no probes) pipeline run failed.\n{FormatDiagnostics( seedDiagnostics )}" );

        // Enumerate the must-survive insertion sites on aspect-target members.
        var insertionSites = EnumerateInsertionSites( targetSource ).ToList();
        Assert.NotEmpty( insertionSites );

        var failures = new List<string>();

        for ( var i = 0; i < insertionSites.Count; i++ )
        {
            var probeId = $"probe_{label}_{i}";
            var modifiedTarget = targetSource.Insert( insertionSites[i], $"\n#warning {probeId}\n" );

            var modifiedSources = new Dictionary<string, string> { ["Aspect.cs"] = aspectSource, ["Target.cs"] = modifiedTarget };
            var modifiedCompilation = testContext.CreateCSharpCompilation( modifiedSources );

            var iterDiagnostics = new DiagnosticBag();

            var result = await pipeline.ExecutePreviewAsync(
                iterDiagnostics,
                PartialCompilation.CreateComplete( modifiedCompilation ),
                configuration!,
                default );

            if ( !result.IsSuccessful )
            {
                failures.Add( $"  site #{i} (offset {insertionSites[i]}): pipeline failed — {FormatDiagnostics( iterDiagnostics )}" );

                continue;
            }

            var outputText = string.Concat( result.Value.SyntaxTrees.Values.Select( t => t.GetRoot().ToFullString() ) );
            var occurrences = CountOccurrences( outputText, probeId );

            if ( occurrences != 1 )
            {
                failures.Add( $"  site #{i} (offset {insertionSites[i]}): expected 1 occurrence of '{probeId}', got {occurrences}" );
            }
        }

        Assert.True(
            failures.Count == 0,
            $"{label}: {failures.Count}/{insertionSites.Count} insertion sites failed:\n{string.Join( "\n", failures )}" );
    }

    /// <summary>
    /// Enumerates character offsets in <paramref name="source"/> where injecting a <c>#warning</c>
    /// directive must round-trip through the linker. Only positions on members carrying an
    /// attribute list (i.e., aspect targets) are considered, since untargeted members go
    /// through the linker's pass-through path which trivially preserves trivia.
    /// </summary>
    private static IEnumerable<int> EnumerateInsertionSites( string source )
    {
        var root = CSharpSyntaxTree.ParseText( source ).GetRoot();

        foreach ( var member in root.DescendantNodes().OfType<MemberDeclarationSyntax>() )
        {
            if ( !member.AttributeLists.Any() )
            {
                continue;
            }

            // (a) Just after the closing ']' of the last attribute list — between attributes
            //     and the modifier/return-type. The linker rewrites the modifier list, so this
            //     trivia slot is exposed to its rewriter.
            yield return member.AttributeLists.Last().CloseBracketToken.Span.End;

            // (b) Inside method/constructor/destructor/operator block bodies,
            //     OR around the arrow and semicolon of expression-bodied ones.
            switch ( member )
            {
                case BaseMethodDeclarationSyntax { Body: { } body }:
                    foreach ( var site in EnumerateBraceBodySites( body ) )
                    {
                        yield return site;
                    }

                    break;

                case BaseMethodDeclarationSyntax { ExpressionBody: { } bmdExpr } bmd:
                    foreach ( var site in EnumerateArrowSemicolonSites( bmdExpr, bmd.SemicolonToken ) )
                    {
                        yield return site;
                    }

                    break;
            }

            // (c) Around the arrow/semicolon of a property/indexer whose whole declaration is
            //     expression-bodied (e.g. `int Value => 42;`). Accessor-level expression bodies
            //     are handled below via the accessor list.
            switch ( member )
            {
                case PropertyDeclarationSyntax { ExpressionBody: { } pExpr } pd:
                    foreach ( var site in EnumerateArrowSemicolonSites( pExpr, pd.SemicolonToken ) )
                    {
                        yield return site;
                    }

                    break;

                case IndexerDeclarationSyntax { ExpressionBody: { } iExpr } id:
                    foreach ( var site in EnumerateArrowSemicolonSites( iExpr, id.SemicolonToken ) )
                    {
                        yield return site;
                    }

                    break;
            }

            // (d) Inside accessor bodies of property/indexer/event declarations.
            var accessors = member switch
            {
                PropertyDeclarationSyntax p => p.AccessorList,
                IndexerDeclarationSyntax i => i.AccessorList,
                EventDeclarationSyntax e => e.AccessorList,
                _ => null
            };

            if ( accessors != null )
            {
                // Accessor-list opening brace: leading (before {) and trailing (after {) slots.
                yield return accessors.OpenBraceToken.SpanStart;
                yield return accessors.OpenBraceToken.Span.End;

                foreach ( var accessor in accessors.Accessors )
                {
                    if ( accessor.Body is { } accBody )
                    {
                        foreach ( var site in EnumerateBraceBodySites( accBody ) )
                        {
                            yield return site;
                        }
                    }
                    else if ( accessor.ExpressionBody is { } accExpr )
                    {
                        // Expression-bodied accessor: `get => expr;` / `set => expr;`.
                        foreach ( var site in EnumerateArrowSemicolonSites( accExpr, accessor.SemicolonToken ) )
                        {
                            yield return site;
                        }
                    }
                    else if ( !accessor.SemicolonToken.IsKind( SyntaxKind.None ) )
                    {
                        // Auto-accessor: `get;` / `set;` — only the semicolon slot is user-visible.
                        yield return accessor.SemicolonToken.SpanStart;
                        yield return accessor.SemicolonToken.Span.End;
                    }
                }

                // Accessor-list closing brace: leading (before }) and trailing (after }) slots.
                yield return accessors.CloseBraceToken.SpanStart;
                yield return accessors.CloseBraceToken.Span.End;
            }
        }
    }

    /// <summary>
    /// Yields offsets for the four trivia slots of an expression-bodied member: leading and
    /// trailing of the <c>=&gt;</c> arrow, and leading and trailing of the terminating <c>;</c>.
    /// The semicolon slots are skipped when the member has no explicit semicolon (shouldn't
    /// happen for the expression-bodied shapes we test, but guarded for safety).
    /// </summary>
    private static IEnumerable<int> EnumerateArrowSemicolonSites(
        ArrowExpressionClauseSyntax expressionBody,
        SyntaxToken semicolonToken )
    {
        yield return expressionBody.ArrowToken.SpanStart;
        yield return expressionBody.ArrowToken.Span.End;

        if ( !semicolonToken.IsKind( SyntaxKind.None ) )
        {
            yield return semicolonToken.SpanStart;
            yield return semicolonToken.Span.End;
        }
    }

    /// <summary>
    /// Yields offsets for all four brace-trivia slots: leading and trailing of both open and close
    /// brace, plus the positions between statements inside the body. Leading-of-open goes BEFORE
    /// <c>{</c>, trailing-of-open goes AFTER <c>{</c> (before the first statement), leading-of-close
    /// goes BEFORE <c>}</c> (after the last statement), trailing-of-close goes AFTER <c>}</c>.
    /// </summary>
    private static IEnumerable<int> EnumerateBraceBodySites( BlockSyntax body )
    {
        yield return body.OpenBraceToken.SpanStart;
        yield return body.OpenBraceToken.Span.End;

        foreach ( var stmt in body.Statements )
        {
            yield return stmt.SpanStart;
        }

        yield return body.CloseBraceToken.SpanStart;
        yield return body.CloseBraceToken.Span.End;
    }

    private static int CountOccurrences( string haystack, string needle )
    {
        var count = 0;
        var idx = 0;

        while ( ( idx = haystack.IndexOf( needle, idx, StringComparison.Ordinal ) ) >= 0 )
        {
            count++;
            idx += needle.Length;
        }

        return count;
    }

    private static string FormatDiagnostics( DiagnosticBag bag )
        => string.Join( "\n  ", bag.SelectAsArray( d => d.GetMessage( CultureInfo.InvariantCulture ) ) );

    /// <summary>
    /// Test-only subclass that exposes the <c>protected</c> <see cref="AspectPipeline.TryInitialize"/>
    /// so the test can perform compile-time aspect compilation once and reuse the result.
    /// </summary>
    private sealed class TestablePreviewAspectPipeline : PreviewAspectPipeline
    {
        public TestablePreviewAspectPipeline( ProjectServiceProvider serviceProvider )
            : base( serviceProvider, ExecutionScenario.Preview ) { }

        public bool InvokeTryInitialize(
            IDiagnosticAdder diagnosticAdder,
            Compilation compilation,
            CancellationToken cancellationToken,
            out AspectPipelineConfiguration? configuration )
            => this.TryInitialize( diagnosticAdder, compilation, null, cancellationToken, out configuration );
    }

    // ---------------- Aspects ----------------

    private const string _overrideMethodAspect = @"
using Metalama.Framework.Aspects;

public class OverrideAttribute : OverrideMethodAspect
{
    public override dynamic? OverrideMethod() => meta.Proceed();
}
";

    private const string _overridePropertyAspect = @"
using Metalama.Framework.Aspects;

public class OverrideAttribute : OverrideFieldOrPropertyAspect
{
    public override dynamic? OverrideProperty
    {
        get => meta.Proceed();
        set => meta.Proceed();
    }
}
";

    private const string _overrideEventAspect = @"
using Metalama.Framework.Aspects;

public class OverrideAttribute : OverrideEventAspect
{
    public override void OverrideAdd( dynamic value ) => meta.Proceed();
    public override void OverrideRemove( dynamic value ) => meta.Proceed();
}
";

    private const string _initializerAspect = @"
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

public class OverrideAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach ( var ctor in builder.Target.Constructors )
        {
            builder.With( ctor ).AddInitializer( nameof(Initialize) );
        }
    }

    [Template]
    private void Initialize() { }
}
";

    // ---------------- Targets ----------------

    private const string _methodBlockBodyTarget = @"
public class Target
{
    [Override]
    public int Compute( int x )
    {
        return x + 1;
    }
}
";

    private const string _methodExpressionBodyTarget = @"
public class Target
{
    [Override]
    public int Compute( int x ) => x + 1;
}
";

    private const string _methodAsyncTarget = @"
using System.Threading.Tasks;

public class Target
{
    [Override]
    public async Task<int> ComputeAsync( int x )
    {
        await Task.Yield();
        return x + 1;
    }
}
";

    private const string _propertyAutoTarget = @"
public class Target
{
    [Override]
    public int Value { get; set; }
}
";

    private const string _propertyBlockAccessorsTarget = @"
public class Target
{
    private int _value;

    [Override]
    public int Value
    {
        get
        {
            return this._value;
        }
        set
        {
            this._value = value;
        }
    }
}
";

    private const string _propertyExpressionBodyAccessorsTarget = @"
public class Target
{
    private int _value;

    [Override]
    public int Value
    {
        get => this._value;
        set => this._value = value;
    }
}
";

    private const string _eventFieldLikeTarget = @"
using System;

public class Target
{
    [Override]
    public event EventHandler? Changed;
}
";

    private const string _eventBlockAccessorsTarget = @"
using System;

public class Target
{
    private EventHandler? _changed;

    [Override]
    public event EventHandler? Changed
    {
        add
        {
            this._changed += value;
        }
        remove
        {
            this._changed -= value;
        }
    }
}
";

    private const string _constructorTarget = @"
[Override]
public class Target
{
    public Target( int x )
    {
        this.X = x;
    }

    public int X { get; }
}
";

    private const string _propertyWholeExpressionBodyTarget = @"
public class Target
{
    [Override]
    public int Value => 42;
}
";

    private const string _constructorExpressionBodyTarget = @"
[Override]
public class Target
{
    public int X;

    public Target( int x ) => this.X = x;
}
";
}
