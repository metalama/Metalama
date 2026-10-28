// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Xunit;

namespace Metalama.Framework.Analyzers.Tests;

/// <summary>
/// Tests the rule that reports a static field or a static automatically implemented property of compile-time code
/// whose declared type is not durable (LAMA0892). A static field outlives every compilation, so at design time it must
/// not retain one.
/// </summary>
public sealed class DurableStaticFieldTests : DurableAnalyzerTestBase
{
    private const string _preamble = """
                                     using Metalama.Framework.Aspects;
                                     using Metalama.Framework.Code;
                                     using Metalama.Framework.Code.DeclarationBuilders;
                                     using Metalama.Framework.Diagnostics;
                                     using Metalama.Framework.Fabrics;
                                     using Metalama.Framework.Utilities;
                                     using Microsoft.CodeAnalysis;
                                     using System;
                                     using System.Collections.Generic;
                                     using System.Collections.Immutable;

                                     """;

    private static string CompileTime( string body ) => _preamble + "[CompileTime] class C { " + body + " }";

    private static string RunTimeOrCompileTime( string body ) => _preamble + "[RunTimeOrCompileTime] class C { " + body + " }";

    #region True positives

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
    /// The case reported in https://github.com/orgs/metalama/discussions/2305.
    /// </summary>
    [Fact]
    public async Task CompileTime_StaticFieldOfCodeModelType_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            CompileTime( "private static readonly Dictionary<string, AttributeConstruction> _annotations = new();" ),
            "LAMA0892" );

        Assert.Contains( "AttributeConstruction", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task CompileTime_StaticFieldOfUnmarkedType_IsReported()
        => await AssertSingleDiagnosticAsync( CompileTime( "private static readonly B? _b; } class B {" ), "LAMA0892" );

    [Fact]
    public async Task TypeOfCompileTimeAssembly_IsCompileTime()
        => await AssertSingleDiagnosticAsync( _preamble + "[assembly: CompileTime] class C { private static readonly ISymbol? _symbol; }", "LAMA0892" );

    [Fact]
    public async Task NestedTypeOfCompileTimeType_IsCompileTime()
        => await AssertSingleDiagnosticAsync(
            _preamble + "[CompileTime] class C { class N { private static readonly ISymbol? _symbol; } }",
            "LAMA0892" );

    [Fact]
    public async Task Fabric_IsCompileTime()
        => await AssertSingleDiagnosticAsync(
            _preamble
            + "class F : ProjectFabric { private static readonly IDeclaration? _declaration; public override void AmendProject( IProjectAmender amender ) { } }",
            "LAMA0892" );

    [Fact]
    public async Task Aspect_IsReported()
        => await AssertSingleDiagnosticAsync( _preamble + "class A : TypeAspect { private static readonly IDeclaration? _declaration; }", "LAMA0892" );

    [Fact]
    public async Task RunTimeOrCompileTime_StaticField_IsReported()
        => await AssertSingleDiagnosticAsync( RunTimeOrCompileTime( "private static readonly ISymbol? _symbol;" ), "LAMA0892" );

    #endregion

    #region True negatives

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

    [Fact]
    public async Task CompileTime_InstanceField_IsNotReportedByThisRule()
        => await AssertNoDiagnosticAsync( CompileTime( "private readonly ISymbol? _symbol;" ) );

    [Fact]
    public async Task RunTimeOrCompileTime_RunTimeStaticField_IsNotReported()
        => await AssertNoDiagnosticAsync( RunTimeOrCompileTime( "[RunTime] private static readonly ISymbol? _symbol;" ) );

    [Fact]
    public async Task RunTime_StaticField_IsNotReported()
        => await AssertNoDiagnosticAsync( _preamble + "class C { private static readonly ISymbol? _symbol; private static readonly IDeclaration? _declaration; }" );

    #endregion
}
