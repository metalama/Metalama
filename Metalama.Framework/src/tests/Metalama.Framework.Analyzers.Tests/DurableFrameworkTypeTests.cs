// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Xunit;

namespace Metalama.Framework.Analyzers.Tests;

/// <summary>
/// Tests that the types of the public API of <c>Metalama.Framework</c> that durable objects store are themselves durable.
/// </summary>
/// <remarks>
/// A registration of an extension stores these types beyond the compilation in which it was created, so the analyzer must accept them in a
/// <c>[Durable]</c> type.
/// </remarks>
public sealed class DurableFrameworkTypeTests : DurableAnalyzerTestBase
{
    private const string _preamble = """
                                     using Metalama.Framework.Advising;
                                     using Metalama.Framework.Utilities;
                                     using System;

                                     """;

    private static string Code( string body ) => _preamble + body;

    [Fact]
    public async Task MethodTemplateSelectorField_IsNotReported()
        => await AssertNoDiagnosticAsync( Code( "[Durable] class Registration { private MethodTemplateSelector _templates; }" ) );
}
