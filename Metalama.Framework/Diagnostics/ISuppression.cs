// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Utilities;
using System;

namespace Metalama.Framework.Diagnostics;

/// <summary>
/// Represents an instance of a diagnostic suppression, including an optional filter delegate.
/// </summary>
/// <seealso href="@diagnostics"/>
[CompileTime]
[InternalImplement]
public interface ISuppression
{
    /// <summary>
    /// Gets the definition of the suppression, containing the ID of the diagnostic to be suppressed.
    /// </summary>
    SuppressionDefinition Definition { get; }

    /// <summary>
    /// Gets the optional filter delegate that will be applied to the diagnostics.
    /// </summary>
    Func<ISuppressibleDiagnostic, bool>? Filter { get; }
}