// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Analyzers.Durability;
using Metalama.Framework.Analyzers.Immutability;
using Xunit;

namespace Metalama.Framework.Analyzers.Tests;

/// <summary>
/// Tests of LAMA0875, which enforces the convention that a member holding a value weakly is named <c>Dangerous</c>.
/// </summary>
/// <remarks>
/// The convention exists so that a reader knows the value may be absent and that the caller is responsible for
/// establishing that it is not. Nothing else in the analyzer says anything about a weak reference, because one is
/// durable whatever it refers to.
/// </remarks>
public sealed class DurableWeakReferenceNamingTests : DurableAnalyzerTestBase
{
    private const string _preamble = """
                                     using Metalama.Framework.Utilities;
                                     using Microsoft.CodeAnalysis;
                                     using System;

                                     """;

    private static string Code( string body ) => _preamble + body;

    [Fact]
    public async Task WeakReferenceNotNamedDangerous_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            Code( "[Durable] class A { private WeakReference<Compilation>? _last; }" ),
            "LAMA0875" );

        Assert.Contains( "_last", message, StringComparison.Ordinal );
    }

    [Fact]
    public async Task NonGenericWeakReference_IsReported()
        => await AssertSingleDiagnosticAsync(
            Code( "[Durable] class A { private WeakReference? _last; }" ),
            "LAMA0875" );

    [Fact]
    public async Task WeakReferenceNamedDangerous_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "[Durable] class A { private WeakReference<Compilation>? _lastDangerous; }" ) );

    [Fact]
    public async Task WeakReferenceInAnUnmarkedType_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "class A { private WeakReference<Compilation>? _last; }" ) );
}
