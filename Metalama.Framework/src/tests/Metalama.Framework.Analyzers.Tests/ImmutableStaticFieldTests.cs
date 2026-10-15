// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Analyzers.Immutability;
using Xunit;

namespace Metalama.Framework.Analyzers.Tests;

/// <summary>
/// Tests the rule of <see cref="ImmutableContractAnalyzer"/> that reports the static fields and static automatic
/// properties of compile-time and run-time-or-compile-time types (LAMA0889, LAMA0890 and LAMA0891).
/// </summary>
public sealed class ImmutableStaticFieldTests : ImmutableAnalyzerTestBase
{
    private const string _scopePrologue = _prologue + """
                                                      using Metalama.Framework.Aspects;
                                                      using Metalama.Framework.Diagnostics;
                                                      using Metalama.Framework.Fabrics;

                                                      """;

    private static string CompileTime( string body ) => _scopePrologue + "[CompileTime] class C { " + body + " }";

    private static string RunTimeOrCompileTime( string body ) => _scopePrologue + "[RunTimeOrCompileTime] class C { " + body + " }";

    // ------------------------------------------------------------------------------------------------------------
    // Compile-time types.
    // ------------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task CompileTime_NonReadOnlyField_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync( CompileTime( "private static int _count;" ), "LAMA0889" );

        Assert.Contains( "_count", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task CompileTime_PropertyWithSetter_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync( CompileTime( "public static int Count { get; set; }" ), "LAMA0889" );

        Assert.Contains( "Count", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task CompileTime_FieldOfMutableType_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync( CompileTime( "private static readonly List<int> _items = new();" ), "LAMA0890" );

        Assert.Contains( "List<int>", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task CompileTime_ArrayField_IsReported()
        => await AssertSingleDiagnosticAsync( CompileTime( "private static readonly int[] _items = new int[0];" ), "LAMA0890" );

    [Fact]
    public async Task CompileTime_NonReadOnlyFieldOfMutableType_IsReportedOnce()
        => await AssertSingleDiagnosticAsync( CompileTime( "private static List<int> _items = new();" ), "LAMA0889" );

    [Fact]
    public async Task CompileTime_ReadOnlyFieldsOfImmutableTypes_AreNotReported()
        => await AssertNoDiagnosticAsync(
            CompileTime(
                "private static readonly int _a = 1; private static readonly string _b = \"\"; "
                + "private static readonly ImmutableArray<int> _c = ImmutableArray<int>.Empty; public static string P { get; } = \"\";" ) );

    [Fact]
    public async Task CompileTime_ConstField_IsNotReported()
        => await AssertNoDiagnosticAsync( CompileTime( "private const int Count = 1;" ) );

    [Fact]
    public async Task CompileTime_DiagnosticAndSuppressionDefinitions_AreNotReported()
        => await AssertNoDiagnosticAsync(
            CompileTime(
                "private static readonly DiagnosticDefinition<string> _error = new( \"MY001\", Severity.Error, \"{0}\" ); "
                + "private static readonly DiagnosticDefinition _warning = new( \"MY002\", Severity.Warning, \"Warning\" ); "
                + "private static readonly SuppressionDefinition _suppression = new( \"CS0169\" );" ) );

    [Fact]
    public async Task Fabric_IsCompileTime()
        => await AssertSingleDiagnosticAsync(
            _scopePrologue
            + "class F : ProjectFabric { private static int _count; public override void AmendProject( IProjectAmender amender ) { } }",
            "LAMA0889" );

    [Fact]
    public async Task NestedTypeOfCompileTimeType_IsCompileTime()
        => await AssertSingleDiagnosticAsync(
            _scopePrologue + "[CompileTime] class C { class N { private static int _count; } }",
            "LAMA0889" );

    [Fact]
    public async Task TypeOfCompileTimeAssembly_IsCompileTime()
        => await AssertSingleDiagnosticAsync(
            _scopePrologue + "[assembly: CompileTime] class C { private static int _count; }",
            "LAMA0889" );

    [Fact]
    public async Task CompileTime_RunTimeAttribute_DoesNotWaive()
        => await AssertSingleDiagnosticAsync( CompileTime( "[RunTime] private static int _count;" ), "LAMA0889" );

    // ------------------------------------------------------------------------------------------------------------
    // Run-time-or-compile-time types.
    // ------------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task RunTimeOrCompileTime_NonReadOnlyField_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync( RunTimeOrCompileTime( "private static int _count;" ), "LAMA0891" );

        Assert.Contains( "is not read-only", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task RunTimeOrCompileTime_FieldOfMutableType_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            RunTimeOrCompileTime( "private static readonly Dictionary<string, int> _cache = new();" ),
            "LAMA0891" );

        Assert.Contains( "which is not immutable", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task RunTimeOrCompileTime_RunTimeField_IsNotReported()
        => await AssertNoDiagnosticAsync(
            RunTimeOrCompileTime( "[RunTime] private static int _count; [RunTime] private static readonly List<int> _items = new();" ) );

    [Fact]
    public async Task RunTimeOrCompileTime_RunTimeProperty_IsNotReported()
        => await AssertNoDiagnosticAsync( RunTimeOrCompileTime( "[RunTime] public static int Count { get; set; }" ) );

    [Fact]
    public async Task RunTimeOrCompileTime_ReadOnlyFieldOfImmutableType_IsNotReported()
        => await AssertNoDiagnosticAsync( RunTimeOrCompileTime( "private static readonly string _name = \"\";" ) );

    [Fact]
    public async Task Aspect_IsRunTimeOrCompileTime()
        => await AssertSingleDiagnosticAsync( _scopePrologue + "class A : TypeAspect { private static int _count; }", "LAMA0891" );

    [Fact]
    public async Task Aspect_TemplateField_IsNotReported()
        => await AssertNoDiagnosticAsync( _scopePrologue + "class A : TypeAspect { [Template] private static readonly List<int> _items = new(); }" );

    [Fact]
    public async Task NestedFabricOfRunTimeOrCompileTimeType_IsCompileTime()
        => await AssertSingleDiagnosticAsync(
            _scopePrologue
            + "[RunTimeOrCompileTime] class C { class F : TypeFabric { private static int _count; public override void AmendType( ITypeAmender amender ) { } } }",
            "LAMA0889" );

    // ------------------------------------------------------------------------------------------------------------
    // Run-time types.
    // ------------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task RunTime_StaticFields_AreNotReported()
        => await AssertNoDiagnosticAsync(
            _scopePrologue + "class C { private static int _count; private static readonly List<int> _items = new(); }" );

    [Fact]
    public async Task RunTimeTypeImplementingRunTimeOrCompileTimeInterfaceOfSystem_IsNotReported()
        => await AssertNoDiagnosticAsync( _scopePrologue + "class C : IEquatable<C> { private static int _count; public bool Equals( C? other ) => false; }" );
}
