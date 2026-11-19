// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Framework.Tests.UnitTests.CompileTime;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Aspects;

/// <summary>
/// Runs aspects on a compilation that references an older <c>Metalama.Framework</c> than the engine, as the MCP server, the
/// LINQPad driver and <c>Metalama.Framework.Workspaces</c> do when the analyzed project references an older package.
/// </summary>
public sealed class OlderFrameworkReferenceTests : AspectTestBase
{
    private const string _aspectCode = """
                                       using Metalama.Framework.Aspects;
                                       using Metalama.Framework.Code;

                                       public class OverrideAttribute : OverrideFieldOrPropertyAspect
                                       {
                                           // Only OverrideProperty is overridden. OverrideEnumerableProperty and the other optional templates are
                                           // inherited from the framework, and the aspect requests them by name.
                                           public override dynamic? OverrideProperty
                                           {
                                               get => 42;
                                               set => meta.Proceed();
                                           }
                                       }

                                       public class C
                                       {
                                           [Override]
                                           public int P { get; set; }
                                       }
                                       """;

    private const string _methodAspectCode = """
                                             using Metalama.Framework.Aspects;
                                             using System.Collections.Generic;

                                             public class OverrideAttribute : OverrideMethodAspect
                                             {
                                                 // Only OverrideMethod is overridden. The async and enumerable templates are inherited from the framework.
                                                 public override dynamic? OverrideMethod() => 42;
                                             }

                                             public class C
                                             {
                                                 [Override]
                                                 public int M() => 0;

                                                 [Override]
                                                 public IEnumerable<int> E() { yield return 0; }
                                             }
                                             """;

    [Fact]
    public Task OverrideFieldOrPropertyAspectIsApplied() => this.AssertAppliedAsync( _aspectCode );

    [Fact]
    public Task OverrideMethodAspectIsApplied() => this.AssertAppliedAsync( _methodAspectCode );

    private async Task AssertAppliedAsync( string code )
    {
        using var testContext = this.CreateTestContext();

        var compilation = OlderFrameworkAssembly.ReferenceFrom( testContext.CreateCSharpCompilation( code ), testContext.BaseDirectory );

        var diagnostics = new DiagnosticBag();
        var result = await new CompileTimeAspectPipeline( testContext.ServiceProvider ).ExecuteAsync(
            diagnostics.Report,
            null,
            compilation,
            ImmutableArray<ManagedResource>.Empty );

        // Before the fix, the aspect was skipped with LAMA0033: "defines a member named 'get_OverrideEnumerableProperty', but the
        // member is not annotated with the [Template] custom attribute".
        Assert.DoesNotContain( diagnostics, d => d.Id == "LAMA0033" );
        Assert.True( result.IsSuccessful, string.Join( "\n", diagnostics.SelectAsArray( d => d.ToString() ) ) );

        // The getter of the target property is the template.
        var targetClass = result.Value.ResultingCompilation.Compilation.SyntaxTrees
            .SelectMany( t => t.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>() )
            .Single( c => c.Identifier.Text == "C" );

        Assert.Contains( "42", targetClass.ToString(), System.StringComparison.Ordinal );
    }
}
