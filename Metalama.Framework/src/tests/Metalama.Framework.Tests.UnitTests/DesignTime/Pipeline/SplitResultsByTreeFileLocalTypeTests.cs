// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline;

/// <summary>
/// Regression test for https://github.com/metalama/Metalama/issues/2051: <c>SplitResultsByTree</c> files each
/// suppression of a pipeline execution result under the declaration it applies to, and the key is the serializable
/// identifier of that declaration. A declaration of a file-local type has no such identifier, so the conversion threw
/// <see cref="System.ArgumentException"/>, the exception travelled out of the design-time pipeline, and the project
/// lost every diagnostic, every suppression and every generated document until the analysis process was restarted.
/// </summary>
/// <remarks>
/// <para>
/// A declaration identifier is a documentation-comment identifier, which names a type by its namespace and its name
/// only, and two file-local types declared in two files share both. The identifier of such a declaration therefore
/// carries the metadata name of the file-local type as a discriminator, which is the gap recorded in #662 and closed
/// since. These tests require the items to be filed rather than merely require the pass not to abort, because a
/// dropped item is what the project lost in the reported case.
/// </para>
/// <para>
/// The reported case reaches this code without the user writing a file-local type: the ASP.NET Core OpenAPI
/// XML-comment source generator emits one, and an aspect that suppresses a warning on the generated declaration is
/// enough.
/// </para>
/// </remarks>
public sealed class SplitResultsByTreeFileLocalTypeTests : UnitTestClass
{
    public SplitResultsByTreeFileLocalTypeTests( ITestOutputHelper testOutput ) : base( testOutput ) { }

    private const string _aspectCode = """
                                       using Metalama.Framework.Aspects;
                                       using Metalama.Framework.Code;
                                       using Metalama.Framework.Diagnostics;

                                       public class Aspect : TypeAspect
                                       {
                                           private static readonly SuppressionDefinition _suppression = new( "CS0169" );

                                           public override void BuildAspect( IAspectBuilder<INamedType> builder )
                                           {
                                               builder.Diagnostics.Suppress( _suppression, builder.Target );
                                           }
                                       }
                                       """;

    private const string _fileLocalTargetCode = """
                                                [Aspect]
                                                file class FileLocalTarget
                                                {
                                                    private int _unused;
                                                }
                                                """;

    private const string _ordinaryTargetCode = """
                                               [Aspect]
                                               public class OrdinaryTarget
                                               {
                                                   private int _unused;
                                               }
                                               """;

    private const string _overrideAspectCode = """
                                               using Metalama.Framework.Aspects;

                                               public class OverrideAspect : OverrideMethodAspect
                                               {
                                                   public override dynamic? OverrideMethod() => meta.Proceed();
                                               }
                                               """;

    private const string _overrideTargetCode = """
                                               file class FileLocalTarget
                                               {
                                                   [OverrideAspect]
                                                   public void M() { }
                                               }
                                               """;

    private const string _overrideOrdinaryTargetCode = """
                                                       public class OrdinaryTarget
                                                       {
                                                           [OverrideAspect]
                                                           public void M() { }
                                                       }
                                                       """;

    /// <summary>
    /// The aspect suppresses a warning on a file-local type. The suppression must be filed under the identifier of
    /// that type. Before the identifier existed, the computation threw and the result of the whole project was lost.
    /// </summary>
    [Fact]
    public void SuppressionOnFileLocalTypeIsFiled()
    {
        var results = Execute( "fileLocal.cs", _fileLocalTargetCode );

        var suppression = Assert.Single( GetSuppressions( results, "fileLocal.cs" ) );

        Assert.Equal( "CS0169", suppression.Suppression.Definition.SuppressedDiagnosticId );
        Assert.Contains( ";File=<fileLocal>F", suppression.DeclarationId.Id, StringComparison.Ordinal );
    }

    /// <summary>
    /// The control case: the same aspect on an ordinary type. Its suppression must still be filed, so that the fix
    /// does not drop more than the declarations that have no identifier.
    /// </summary>
    [Fact]
    public void SuppressionOnOrdinaryTypeIsPreserved()
    {
        var results = Execute( "ordinary.cs", _ordinaryTargetCode );

        var suppression = Assert.Single( GetSuppressions( results, "ordinary.cs" ) );

        Assert.Equal( "CS0169", suppression.Suppression.Definition.SuppressedDiagnosticId );
    }

    /// <summary>
    /// The mixed case: a file-local type and an ordinary type in the same tree. Both suppressions must be filed, and
    /// under different identifiers.
    /// </summary>
    [Fact]
    public void SuppressionsOnBothAFileLocalAndAnOrdinaryTypeAreFiled()
    {
        var results = Execute( "mixed.cs", _fileLocalTargetCode + "\n\n" + _ordinaryTargetCode );

        var suppressions = GetSuppressions( results, "mixed.cs" ).ToList();

        Assert.Equal( 2, suppressions.Count );
        Assert.All( suppressions, s => Assert.Equal( "CS0169", s.Suppression.Definition.SuppressedDiagnosticId ) );

        Assert.Single( suppressions, s => s.DeclarationId.Id.Contains( ";File=", StringComparison.Ordinal ) );
        Assert.Single( suppressions, s => !s.DeclarationId.Id.Contains( ";File=", StringComparison.Ordinal ) );
    }

    /// <summary>
    /// The same method files an aspect instance under the identifier of the declaration it targets, and that
    /// computation aborted the pass for a file-local type as well. The aspect instance must now be filed, under an
    /// identifier that carries the discriminator.
    /// </summary>
    /// <remarks>
    /// The transformations of this execution are not asserted, because this pipeline pass produces none even for an
    /// ordinary type. <see cref="AspectInstanceOnOrdinaryTypeIsFiled"/> is the control that establishes it.
    /// </remarks>
    [Fact]
    public void AspectInstanceOnFileLocalTypeIsFiled()
    {
        var results = Execute( "override.cs", _overrideTargetCode, _overrideAspectCode );

        var treeResult = results.SyntaxTreeResults[DocumentKey.FromPath( "override.cs" )];

        var aspectInstance = Assert.Single( treeResult.AspectInstances );

        Assert.Contains( ";File=<override>F", aspectInstance.TargetDeclarationId.Id, StringComparison.Ordinal );
    }

    /// <summary>
    /// The control case for <see cref="AspectInstanceOnFileLocalTypeIsFiled"/>: the same aspect on an ordinary type
    /// produces one aspect instance and no transformation, which is the shape the file-local case must match.
    /// </summary>
    [Fact]
    public void AspectInstanceOnOrdinaryTypeIsFiled()
    {
        var results = Execute( "overrideOrdinary.cs", _overrideOrdinaryTargetCode, _overrideAspectCode );

        var treeResult = results.SyntaxTreeResults[DocumentKey.FromPath( "overrideOrdinary.cs" )];

        var aspectInstance = Assert.Single( treeResult.AspectInstances );

        Assert.DoesNotContain( ";File=", aspectInstance.TargetDeclarationId.Id, StringComparison.Ordinal );
        Assert.Empty( treeResult.Transformations );
    }

    private DesignTimeAspectPipelineResult Execute( string targetPath, string targetCode, string aspectCode = _aspectCode )
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var compilation = testContext.CreateCSharpCompilation(
            new Dictionary<string, string> { ["aspect.cs"] = aspectCode, [targetPath] = targetCode },
            ignoreErrors: true );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, default, out var executed ) );

        return executed.Result;
    }

    private static IEnumerable<CacheableScopedSuppression> GetSuppressions( DesignTimeAspectPipelineResult results, string path )
        => results.SyntaxTreeResults.TryGetValue( DocumentKey.FromPath( path ), out var treeResult )
            ? treeResult.Suppressions
            : Enumerable.Empty<CacheableScopedSuppression>();
}
