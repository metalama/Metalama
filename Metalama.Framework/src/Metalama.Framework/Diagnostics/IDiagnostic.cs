// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Utilities;
using System.Collections.Immutable;

namespace Metalama.Framework.Diagnostics;

/// <summary>
/// Represents an instance of a diagnostic, including its parameters and its optional code fixes.
/// </summary>
/// <seealso href="@diagnostics"/>
[CompileTime]
[InternalImplement]
public interface IDiagnostic
{
    /// <summary>
    /// Gets the <see cref="IDiagnosticDefinition"/> from which the current diagnostic has been created.
    /// </summary>
    IDiagnosticDefinition Definition { get; }

    ImmutableArray<IDiagnosticExtension> Extensions { get; }

    /// <summary>
    /// Gets the arguments of the current diagnostic.
    /// </summary>
    object? Arguments { get; }

    IDiagnostic WithExtensions( ImmutableArray<IDiagnosticExtension> extensions );
}