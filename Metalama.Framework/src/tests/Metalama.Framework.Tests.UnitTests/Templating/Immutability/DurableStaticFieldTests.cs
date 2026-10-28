// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Templating.Immutability;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Templating.Immutability;

/// <summary>
/// Tests the rule of <see cref="ImmutabilityValidator"/> that reports a static field or a static automatic property of
/// compile-time code whose type is immutable but not durable (LAMA0892). A static field outlives every compilation, so
/// at design time it must not retain one.
/// </summary>
public sealed class DurableStaticFieldTests : ImmutabilityTestBase
{
    /// <summary>
    /// Gets a value indicating that the fragments declare the scope of each type themselves.
    /// </summary>
    private protected override bool IsCompileTimeAssembly => false;

    private const string _scopePrologue = _prologue + """
                                                      using Metalama.Framework.Aspects;
                                                      using Metalama.Framework.Code;
                                                      using Metalama.Framework.Diagnostics;
                                                      using Metalama.Framework.Fabrics;
                                                      using Microsoft.CodeAnalysis;

                                                      """;

    private static string CompileTime( string body ) => _scopePrologue + "[CompileTime] class C { " + body + " }";

    private static string RunTimeOrCompileTime( string body ) => _scopePrologue + "[RunTimeOrCompileTime] class C { " + body + " }";

    // ------------------------------------------------------------------------------------------------------------
    // True positives.
    // ------------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task CompileTime_StaticFieldOfCompilationBoundType_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync( CompileTime( "private static readonly SyntaxTree? _tree;" ), "LAMA0892" );

        Assert.Contains( "_tree", message, StringComparison.Ordinal );
        Assert.Contains( "SyntaxTree", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task CompileTime_StaticAutomaticProperty_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync( CompileTime( "public static ISymbol? Symbol { get; }" ), "LAMA0892" );

        Assert.Contains( "Symbol", message, StringComparison.Ordinal );
    }

    /// <summary>
    /// A code model interface is not immutable, so the member is reported by LAMA0890, which takes precedence. This is
    /// the case reported in https://github.com/orgs/metalama/discussions/2305.
    /// </summary>
    [Fact]
    public async Task CompileTime_StaticFieldOfCodeModelType_IsReportedAsNotImmutable()
        => await AssertSingleDiagnosticAsync( CompileTime( "private static readonly IDeclaration? _declaration;" ), "LAMA0890" );

    /// <summary>
    /// A delegate cannot be retargeted, so it is immutable, but it retains its target and its closure.
    /// </summary>
    [Fact]
    public async Task CompileTime_StaticFieldOfDelegateType_IsReported()
        => await AssertSingleDiagnosticAsync( CompileTime( "private static readonly Func<int, int> _f = x => x;" ), "LAMA0892" );

    [Fact]
    public async Task TypeOfCompileTimeAssembly_IsCompileTime()
        => await AssertSingleDiagnosticAsync(
            _scopePrologue + "[assembly: CompileTime] class C { private static readonly ISymbol? _symbol; }",
            "LAMA0892" );

    [Fact]
    public async Task NestedTypeOfCompileTimeType_IsCompileTime()
        => await AssertSingleDiagnosticAsync(
            _scopePrologue + "[CompileTime] class C { class N { private static readonly ISymbol? _symbol; } }",
            "LAMA0892" );

    [Fact]
    public async Task Fabric_IsReported()
        => await AssertSingleDiagnosticAsync(
            _scopePrologue
            + "class F : ProjectFabric { private static readonly ISymbol? _symbol; public override void AmendProject( IProjectAmender amender ) { } }",
            "LAMA0892" );

    [Fact]
    public async Task Aspect_IsReported()
        => await AssertSingleDiagnosticAsync( _scopePrologue + "class A : TypeAspect { private static readonly ISymbol? _symbol; }", "LAMA0892" );

    [Fact]
    public async Task RunTimeOrCompileTime_StaticField_IsReported()
        => await AssertSingleDiagnosticAsync( RunTimeOrCompileTime( "private static readonly ISymbol? _symbol;" ), "LAMA0892" );

    [Fact]
    public async Task ImmutableTypeThatIsNotDurable_IsReported()
        => await AssertSingleDiagnosticAsync(
            CompileTime( "private static readonly B? _b; } [CompileTime, ImmutableType] sealed class B {" ),
            "LAMA0892" );

    // ------------------------------------------------------------------------------------------------------------
    // True negatives.
    // ------------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task CompileTime_StaticFieldsOfDurableTypes_AreNotReported()
        => await AssertNoDiagnosticAsync(
            CompileTime(
                "private static readonly int _a = 1; private static readonly string _b = \"\"; "
                + "private static readonly ImmutableArray<string> _c = ImmutableArray<string>.Empty; public static string P { get; } = \"\"; "
                + "private const int Count = 1;" ) );

    [Fact]
    public async Task CompileTime_DiagnosticAndSuppressionDefinitions_AreNotReported()
        => await AssertNoDiagnosticAsync(
            CompileTime(
                "private static readonly DiagnosticDefinition<string> _error = new( \"MY001\", Severity.Error, \"{0}\" ); "
                + "private static readonly DiagnosticDefinition _warning = new( \"MY002\", Severity.Warning, \"Warning\" ); "
                + "private static readonly SuppressionDefinition _suppression = new( \"CS0169\" );" ) );

    /// <summary>
    /// The type argument of a diagnostic definition is the type of the message arguments, which the definition does not
    /// store. It is often a code model declaration.
    /// </summary>
    [Fact]
    public async Task CompileTime_DiagnosticDefinitionOfCodeModelArguments_IsNotReported()
        => await AssertNoDiagnosticAsync(
            CompileTime(
                "private static readonly DiagnosticDefinition<(IType Type, IDeclaration Declaration)> _error = new( \"MY001\", Severity.Error, \"{0} {1}\" );" ) );

    /// <summary>
    /// A member of a mutable type is reported by LAMA0890 only: it must be made immutable before the question of its
    /// durability is worth asking.
    /// </summary>
    [Fact]
    public async Task CompileTime_StaticFieldOfMutableType_IsReportedOnceAsNotImmutable()
        => await AssertSingleDiagnosticAsync(
            CompileTime( "private static readonly Dictionary<string, ISymbol> _symbols = new();" ),
            "LAMA0890" );

    [Fact]
    public async Task CompileTime_DurableMember_IsNotReported()
        => await AssertNoDiagnosticAsync( CompileTime( "[Durable] private static readonly ISymbol? _symbol;" ) );

    [Fact]
    public async Task CompileTime_InstanceField_IsNotReportedByThisRule()
        => await AssertNoDiagnosticAsync( CompileTime( "private readonly ISymbol? _symbol;" ) );

    [Fact]
    public async Task RunTimeOrCompileTime_RunTimeStaticField_IsNotReported()
        => await AssertNoDiagnosticAsync( RunTimeOrCompileTime( "[RunTime] private static readonly ISymbol? _symbol;" ) );

    [Fact]
    public async Task RunTime_StaticField_IsNotReported()
        => await AssertNoDiagnosticAsync(
            _scopePrologue + "class C { private static readonly ISymbol? _symbol; private static readonly IDeclaration? _declaration; }" );

    [Fact]
    public async Task TypeDeclaredDurableByProject_IsNotReported()
    {
        var diagnostics = await GetDiagnosticsAsync(
            CompileTime( "private static readonly B? _b; } [CompileTime, ImmutableType] sealed class B {" ),
            new Dictionary<string, string> { ["MetalamaDurableTypes"] = "B" } );

        Assert.Empty( diagnostics );
    }
}
