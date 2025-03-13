// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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