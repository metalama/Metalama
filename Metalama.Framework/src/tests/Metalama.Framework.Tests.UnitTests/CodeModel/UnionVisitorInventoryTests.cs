// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if ROSLYN_5_11_0_OR_GREATER
using Metalama.Framework.DesignTime.DiagnosticSuppressing;
using Metalama.Framework.Engine.CodeModel.Source;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

/// <summary>
/// The guard that issue #1941 asks for: a syntax visitor that classifies, hashes or rewrites a struct declaration
/// must also handle a union declaration, because Roslyn dispatches a virtual method per node kind and a syntax kind
/// cannot override one.
/// </summary>
/// <remarks>
/// <para>
/// The test is a reflection over the two assemblies that are compiled once per Roslyn variant. It runs in the latest
/// variant only, because the lower one has no <c>VisitUnionDeclaration</c> method to override, and it requires the
/// opt-in of <c>eng/RoslynPreview.props</c>, because the overrides are compiled under that symbol.
/// </para>
/// <para>
/// A visitor that must not handle a union is named in <see cref="_visitorsThatDoNotVisitAUnion"/> with the reason.
/// The test also fails when an entry of that list becomes stale, so that a visitor which gains the override is
/// removed from the list rather than left in it.
/// </para>
/// </remarks>
public sealed class UnionVisitorInventoryTests
{
    /// <summary>
    /// The visitors whose visit methods are produced by <c>eng/src/GenerateMetaSyntaxRewriter</c> from the grammar of
    /// the Roslyn version. The generator writes one method per node of the grammar, so it cannot omit a kind, and the
    /// guard has nothing to add for them.
    /// </summary>
    private static readonly HashSet<string> _generatedVisitors =
        new HashSet<string>( StringComparer.Ordinal )
        {
            "Metalama.Framework.DesignTime.Pipeline.Diff.CompileTimeCodeHasher",
            "Metalama.Framework.DesignTime.Pipeline.Diff.RunTimeCodeHasher",
            "Metalama.Framework.Engine.Templating.MetaSyntaxRewriter",
            "Metalama.Framework.Engine.Templating.RoslynVersionSyntaxVerifier"
        };

    /// <summary>
    /// The visitors that override the struct declaration and deliberately do not override the union declaration. Each
    /// entry names the issue that owns the visitor, because the visitors of the other themes are corrected by the
    /// sub-issues of #1940 and not by #1941.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> _visitorsThatDoNotVisitAUnion =
        new Dictionary<string, string>( StringComparer.Ordinal )
        {
            ["Metalama.Framework.Engine.CompileTime.CompileTimeCompilationBuilder+FindCompileTimeCodeVisitor"] =
                "Issue #1942 gives the compile-time classification its union dispatch.",
            ["Metalama.Framework.Engine.CompileTime.CompileTimeCompilationBuilder+ProduceCompileTimeCodeRewriter"] =
                "Issue #1942 gives the compile-time rewriter its union dispatch.",
            ["Metalama.Framework.Engine.CompileTime.CompileTimeCompilationBuilder+CollectSerializableTypesVisitor"] =
                "Issue #1942 owns the compile-time compilation, of which the serializable type collection is part.",
            ["Metalama.Framework.Engine.CompileTime.CompileTimeCompilationBuilder+CollectSerializableFieldsVisitor"] =
                "Issue #1942 owns the compile-time compilation, of which the serializable field collection is part.",
            ["Metalama.Framework.Engine.Templating.TemplateAnnotator"] =
                "Issue #1942 gives the template annotator its union dispatch.",
            ["Metalama.Framework.Engine.Templating.TemplatingCodeValidator+Visitor"] =
                "Issue #1942 owns the templating theme, of which this validator is part.",
            ["Metalama.Framework.Engine.Formatting.TextSpanClassifier"] =
                "Issue #1942 corrects the classifier, and it cannot be corrected before the template annotator."
        };

    /// <summary>
    /// The assemblies that are compiled once per Roslyn variant and can therefore name the union node.
    /// <c>Metalama.Framework.Sdk</c> and <c>Metalama.Framework.Engine.Analyzers</c> are excluded, because they are
    /// single builds pinned to the minimum Roslyn application programming interface version, in which the union node
    /// does not exist.
    /// </summary>
    private static Assembly[] GetVariantAssemblies()
        => [typeof(SourceNamedTypeImpl).Assembly, typeof(TheDiagnosticSuppressor).Assembly];

    private static bool DeclaresVisitMethod( Type type, string methodName )
        => type.GetMethod(
               methodName,
               BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly )
           != null;

    [Fact]
    public void EveryVisitorOfAStructDeclarationAlsoVisitsAUnionDeclaration()
    {
        var missing = GetVariantAssemblies()
            .SelectMany( a => a.GetTypes() )
            .Where( t => DeclaresVisitMethod( t, "VisitStructDeclaration" ) && !DeclaresVisitMethod( t, "VisitUnionDeclaration" ) )
            .Select( t => t.FullName! )
            .Where( n => !_visitorsThatDoNotVisitAUnion.ContainsKey( n ) && !_generatedVisitors.Contains( n ) )
            .OrderBy( n => n, StringComparer.Ordinal )
            .ToArray();

        Assert.Empty( missing );
    }

    [Fact]
    public void NoEntryOfTheExceptionListIsStale()
    {
        // The two assemblies declare compiler-generated types of the same name, so the types are searched rather than
        // indexed by name.
        var types = GetVariantAssemblies().SelectMany( a => a.GetTypes() ).ToArray();

        foreach ( var entry in _visitorsThatDoNotVisitAUnion )
        {
            var type = types.FirstOrDefault( t => string.Equals( t.FullName, entry.Key, StringComparison.Ordinal ) );

            Assert.True( type != null, $"'{entry.Key}' no longer exists." );

            Assert.True(
                DeclaresVisitMethod( type!, "VisitStructDeclaration" ),
                $"'{entry.Key}' no longer visits a struct declaration, so it does not belong to this list." );

            Assert.False(
                DeclaresVisitMethod( type!, "VisitUnionDeclaration" ),
                $"'{entry.Key}' now visits a union declaration, so it must be removed from this list." );
        }
    }
}
#endif
