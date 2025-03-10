// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System.Collections.Immutable;

namespace Metalama.Framework.Introspection;

/// <summary>
/// Exception thrown when the compilation failed.
/// </summary>
[PublicAPI]
public sealed class CompilationFailedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompilationFailedException"/> class.
    /// </summary>
    public CompilationFailedException( string message, ImmutableArray<IIntrospectionDiagnostic> diagnostics ) : base( message )
    {
        this.Diagnostics = diagnostics;
    }

    /// <summary>
    /// Gets the list of compilation errors or warnings.
    /// </summary>
    public ImmutableArray<IIntrospectionDiagnostic> Diagnostics { get; }
}