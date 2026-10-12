// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Metalama.Framework.Analyzers.Tests;

/// <summary>
/// Tests of the durability rules for the unions of C# 15. The compiler lowers a union to a type whose only storage is
/// an <see cref="object"/> property, so the analyzer must judge a union from its case types and not from its storage.
/// See issue #2115.
/// </summary>
public sealed class DurableUnionTests : DurableAnalyzerTestBase
{
    /// <summary>
    /// The language version that declares the union.
    /// </summary>
    private const LanguageVersion _languageVersion = LanguageVersion.CSharp15;

    /// <summary>
    /// The usings of every fragment, and the declarations that the compiler requires of a union. The reference
    /// assemblies of .NET 10 do not declare them, and the compiler reports CS0656 when it cannot find them.
    /// </summary>
    private const string _preamble = """
                                     using Metalama.Framework.Utilities;
                                     using Microsoft.CodeAnalysis;
                                     using System;
                                     using System.Runtime.CompilerServices;

                                     namespace System.Runtime.CompilerServices
                                     {
                                         [AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct )]
                                         public sealed class UnionAttribute : Attribute;

                                         public interface IUnion;
                                     }

                                     """;

    private static string Code( string body )
    {
        var code = _preamble + body;

        // A fragment that does not compile would make a test that expects no diagnostic pass for the wrong reason.
        var errors = CreateCompilation( code, _languageVersion )
            .GetDiagnostics()
            .Where( d => d.Severity == DiagnosticSeverity.Error )
            .Select( d => d.ToString() );

        Assert.Empty( errors );

        return code;
    }

    private static Task AssertNoUnionDiagnosticAsync( string body )
        => AssertNoDiagnosticAsync( Code( body ), languageVersion: _languageVersion );

    private static Task<string> AssertSingleUnionDiagnosticAsync( string body, string expectedId )
        => AssertSingleDiagnosticAsync( Code( body ), expectedId, _languageVersion );

    #region Type rule

    [Fact]
    public async Task FieldOfUnionWithDurableCases_IsNotReported()
        => await AssertNoUnionDiagnosticAsync( "[Durable] class A { private U _u; } union U( string, int );" );

    [Fact]
    public async Task FieldOfUnionWithACompilationBoundCase_IsReportedOnTheCase()
    {
        var message = await AssertSingleUnionDiagnosticAsync(
            "[Durable] class A { private U _u; } union U( string, SyntaxTree );",
            "LAMA0870" );

        Assert.Contains( "SyntaxTree", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task FieldOfUnionWithADelegateCase_IsReportedOnTheCase()
    {
        var message = await AssertSingleUnionDiagnosticAsync(
            "[Durable] class A { private U _u; } union U( string, Func<int> );",
            "LAMA0870" );

        Assert.Contains( "Func<int>", message, StringComparison.Ordinal );
    }

    /// <remarks>
    /// The storage of the union is not analyzed, so a union marked durable is not reported for its
    /// <see cref="object"/> property.
    /// </remarks>
    [Fact]
    public async Task DurableUnionWithDurableCases_IsNotReported()
        => await AssertNoUnionDiagnosticAsync( "[Durable] union U( string, int );" );

    /// <remarks>
    /// The attribute form of a union. Its case types are the parameter types of its public constructors that have a
    /// single parameter.
    /// </remarks>
    [Fact]
    public async Task FieldOfAttributeUnionWithDurableCases_IsNotReported()
        => await AssertNoUnionDiagnosticAsync(
            """
            [Durable] class A { private U _u; }

            [Union]
            class U : IUnion
            {
                public U( string s ) { this.Value = s; }

                public U( int i ) { this.Value = i; }

                public object Value { get; }
            }
            """ );

    [Fact]
    public async Task FieldOfAttributeUnionWithACompilationBoundCase_IsReportedOnTheCase()
    {
        var message = await AssertSingleUnionDiagnosticAsync(
            """
            [Durable] class A { private U _u; }

            [Union]
            class U : IUnion
            {
                public U( string s ) { this.Value = s; }

                public U( SyntaxTree tree ) { this.Value = tree; }

                public object Value { get; }
            }
            """,
            "LAMA0870" );

        Assert.Contains( "SyntaxTree", message, StringComparison.Ordinal );
    }

    /// <remarks>
    /// A union with a member provider interface. Its case types are the parameter types of its static
    /// <c>Create</c> methods.
    /// </remarks>
    [Fact]
    public async Task FieldOfUnionWithMemberProviderAndDurableCases_IsNotReported()
        => await AssertNoUnionDiagnosticAsync(
            """
            [Durable] class A { private U _u; }

            [Union]
            class U : IUnion, U.IUnionMembers
            {
                private U( object value ) { this.Value = value; }

                public object Value { get; }

                public static U Create( string s ) => new U( s );

                public static U Create( int i ) => new U( i );

                public interface IUnionMembers
                {
                    object Value { get; }

                    static abstract U Create( string s );

                    static abstract U Create( int i );
                }
            }
            """ );

    #endregion

    #region Use-site rule

    [Fact]
    public async Task PassingADurableCaseToADurableUnionParameter_IsNotReported()
        => await AssertNoUnionDiagnosticAsync(
            "union U( string, int ); class A { static void Keep([Durable] U u) { } static void M() { Keep(\"x\"); } }" );

    [Fact]
    public async Task PassingACompilationBoundCaseToADurableUnionParameter_IsReported()
    {
        var message = await AssertSingleUnionDiagnosticAsync(
            "union U( SyntaxTree, int ); class A { static void Keep([Durable] U u) { } static void M(SyntaxTree t) { Keep(t); } }",
            "LAMA0872" );

        Assert.Contains( "SyntaxTree", message, StringComparison.Ordinal );
    }

    /// <remarks>
    /// The compiler does not convert a lambda to a union directly, so the lambda is cast to the case type first. The
    /// rule must find the lambda behind that cast.
    /// </remarks>
    [Fact]
    public async Task PassingANonCapturingLambdaToADurableUnionParameter_IsNotReported()
        => await AssertNoUnionDiagnosticAsync(
            "union U( string, Func<int> ); class A { static void Keep([Durable] U u) { } static void M() { Keep((Func<int>) (() => 42)); } }" );

    [Fact]
    public async Task PassingACapturingLambdaToADurableUnionParameter_IsReported()
    {
        var message = await AssertSingleUnionDiagnosticAsync(
            "union U( string, Func<int> ); "
            + "class A { static void Keep([Durable] U u) { } static void M(Compilation c) { Keep((Func<int>) (() => c.GetHashCode())); } }",
            "LAMA0878" );

        Assert.Contains( "'c'", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task DurableUnionFieldInitializedWithANonCapturingLambda_IsNotReported()
        => await AssertNoUnionDiagnosticAsync(
            "union U( string, Func<int> ); class A { [Durable] private U _u = (Func<int>) (() => 42); }" );

    [Fact]
    public async Task AssigningACapturingLambdaToADurableUnionField_IsReported()
        => await AssertSingleUnionDiagnosticAsync(
            "union U( string, Func<int> ); "
            + "class A { [Durable] private U _u; public void M(Compilation c) { this._u = (Func<int>) (() => c.GetHashCode()); } }",
            "LAMA0878" );

    #endregion
}
