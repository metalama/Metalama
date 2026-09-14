// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.IO;

namespace Metalama.Testing.AspectTesting;

/// <summary>
/// Recognizes a file that the test framework adds to the test compilation.
/// </summary>
internal static class TestFrameworkFile
{
    /// <summary>
    /// Determines whether a file is one that the test framework adds to the test compilation, and which is therefore
    /// not a part of the test. The name of such a file begins with an underscore.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The files are <c>___Polyfill_*.cs</c>, which declare the system types that the reference assemblies of the
    /// target framework do not declare, and <c>___GlobalUsings.cs</c>. They belong to no test, so a diagnostic
    /// reported on them is not a result of the test, and their formatting is not the formatting that the test
    /// verifies.
    /// </para>
    /// </remarks>
    public static bool IsTestFrameworkFile( string? path )
        => path != null && Path.GetFileName( path ).StartsWith( "_", StringComparison.Ordinal );
}
