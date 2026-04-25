// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Testing.UnitTesting;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime;

/// <summary>
/// Tests for the WPF precompile pipeline mode that emits aspect-introduced member signatures only,
/// without running the linker. Used by WPF's MarkupCompilePass1 temporary assembly.
/// </summary>
public sealed class WpfPrecompileTests : UnitTestClass
{
    private const string _aspectThatIntroducesAMemberAndOverridesAMethod = """
                                                                          using Metalama.Framework.Aspects;
                                                                          using Metalama.Framework.Code;

                                                                          class TrackChanges : TypeAspect
                                                                          {
                                                                              [Introduce]
                                                                              public bool IsChanged { get; private set; }

                                                                              [Introduce]
                                                                              public void AcceptChanges() => this.IsChanged = false;
                                                                          }

                                                                          class LoggingAspect : OverrideMethodAspect
                                                                          {
                                                                              public override dynamic? OverrideMethod()
                                                                              {
                                                                                  System.Console.WriteLine("entering " + meta.Target.Method.Name);
                                                                                  return meta.Proceed();
                                                                              }
                                                                          }

                                                                          [TrackChanges]
                                                                          partial class Document
                                                                          {
                                                                              public string? Name { get; set; }

                                                                              [LoggingAspect]
                                                                              public void Save() { }
                                                                          }
                                                                          """;

    [Fact]
    public async Task WpfPrecompile_EmitsIntroducedMemberSignatures()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCSharpCompilation( _aspectThatIntroducesAMemberAndOverridesAMethod );
        var pipeline = new WpfPrecompileAspectPipeline( testContext.ServiceProvider );

        var result = await pipeline.ExecuteAsync( null, null, compilation, default, testContext.CancellationToken );

        Assert.True( result.IsSuccessful );

        var addedTrees = result.Value.SyntaxTreeTransformations
            .Where( t => t.OldTree == null )
            .ToArray();

        Assert.NotEmpty( addedTrees );

        var introducedCode = string.Join( "\n", addedTrees.SelectAsArray( t => t.NewTree!.ToString() ) );

        // Introduced members appear as signatures.
        Assert.Contains( "IsChanged", introducedCode );
        Assert.Contains( "AcceptChanges", introducedCode );

        // The override aspect's body rewriting (the Console.WriteLine wrapping) does not.
        Assert.DoesNotContain( "entering", introducedCode );
    }

    [Fact]
    public async Task DefaultScenario_EmitsLinkedBodies()
    {
        // Negative control: same input under the default scenario produces transformed bodies (linker output),
        // proving the WpfPrecompile assertions above are not vacuous.
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCSharpCompilation( _aspectThatIntroducesAMemberAndOverridesAMethod );
        var pipeline = new CompileTimeAspectPipeline( testContext.ServiceProvider );

        var result = await pipeline.ExecuteAsync( null, null, compilation, default, testContext.CancellationToken );

        Assert.True( result.IsSuccessful );

        var allOutputCode = string.Join(
            "\n",
            result.Value.SyntaxTreeTransformations
                .Select( t => (t.NewTree ?? t.OldTree)!.ToString() ) );

        Assert.Contains( "entering", allOutputCode );
    }
}
