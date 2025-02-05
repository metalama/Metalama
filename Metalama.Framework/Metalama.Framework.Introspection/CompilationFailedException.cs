// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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