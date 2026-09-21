// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Tests.UnitTestHelpers.Mocks;
using Metalama.Testing.UnitTesting;
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
/// only. Two file-local types declared in two files share both, so the identifier provider refuses a declaration of a
/// file-local type rather than return an identifier that resolves to the wrong one. This is the gap recorded in #662,
/// which this test does not close: the suppression of such a declaration is dropped. What the test requires is that
/// the rest of the result survives.
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

    /// <summary>
    /// The aspect suppresses a warning on a file-local type. The pipeline must produce a result. Before the fix it
    /// threw, and the result of the whole project was lost.
    /// </summary>
    [Fact]
    public void SuppressionOnFileLocalTypeDoesNotAbortThePipeline()
    {
        var results = Execute( "fileLocal.cs", _fileLocalTargetCode );

        // The suppression of the file-local declaration has no key, so it is dropped rather than filed.
        Assert.Empty( GetSuppressions( results, "fileLocal.cs" ) );
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
    /// The mixed case: a file-local type and an ordinary type in the same tree. The suppression that has a key must
    /// survive the one that has none.
    /// </summary>
    [Fact]
    public void SuppressionOnOrdinaryTypeSurvivesAFileLocalTypeInTheSameTree()
    {
        var results = Execute( "mixed.cs", _fileLocalTargetCode + "\n\n" + _ordinaryTargetCode );

        var suppression = Assert.Single( GetSuppressions( results, "mixed.cs" ) );

        Assert.Equal( "CS0169", suppression.Suppression.Definition.SuppressedDiagnosticId );
    }

    private DesignTimeAspectPipelineResult Execute( string targetPath, string targetCode )
    {
        using var testContext = this.CreateTestContext();
        using var factory = new TestDesignTimeAspectPipelineFactory( testContext );

        var compilation = testContext.CreateCSharpCompilation(
            new Dictionary<string, string> { ["aspect.cs"] = _aspectCode, [targetPath] = targetCode },
            ignoreErrors: true );

        Assert.True( factory.TryExecute( testContext.ProjectOptions, compilation, default, out var executed ) );

        return executed.Result;
    }

    private static IEnumerable<CacheableScopedSuppression> GetSuppressions( DesignTimeAspectPipelineResult results, string path )
        => results.SyntaxTreeResults.TryGetValue( DocumentKey.FromPath( path ), out var treeResult )
            ? treeResult.Suppressions
            : Enumerable.Empty<CacheableScopedSuppression>();
}
