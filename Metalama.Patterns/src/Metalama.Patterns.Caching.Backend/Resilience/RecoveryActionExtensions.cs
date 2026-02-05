// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Caching.Resilience;

/// <summary>
/// Extension methods for the <see cref="RecoveryAction"/> enum.
/// </summary>
[PublicAPI]
public static class RecoveryActionExtensions
{
    /// <summary>
    /// Gets a string for a <see cref="RecoveryAction"/>.
    /// </summary>
    /// <param name="recoveryAction"></param>
    /// <returns></returns>
    public static string ToDisplayString( this RecoveryAction recoveryAction )
        => recoveryAction switch
        {
            // Address ambiguity.
            RecoveryAction.Swallow => nameof(RecoveryAction.Swallow),
            _ => recoveryAction.ToString()
        };
}