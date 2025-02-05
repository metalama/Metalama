// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Utilities;
using System.Collections.Immutable;

namespace Metalama.Framework.Diagnostics;

/// <summary>
/// Represents a diagnostic that can be suppressed using <see cref="ISuppression.Filter"/>.
/// </summary>
[InternalImplement]
[CompileTime]
public interface ISuppressibleDiagnostic
{
    /// <summary>
    /// Gets the ID of the diagnostic (e.g. <c>CS0169</c>).
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the message of the diagnostic formatted using the invariant (English language) culture.
    /// </summary>
    string InvariantMessage { get; }

    /// <summary>
    /// Gets the arguments of the diagnostic message. These often include members or types related to the diagnostic.
    /// </summary>
    ImmutableArray<object?> Arguments { get; }

    /// <summary>
    /// Gets the source file, line and column of the diagnostic, when available.
    /// </summary>
    SourceSpan? Span { get; }
}