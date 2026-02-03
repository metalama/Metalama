// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Caching.Resilience;

/// <summary>
/// A <see cref="RetryPolicy"/> specialized in handling Redis transaction conflicts. The <see cref="RetryPolicy.NoDelayAttempts"/>
/// property defaults to <c>2</c>.
/// </summary>
[PublicAPI]
public class TransactionRetryPolicy : RetryPolicy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionRetryPolicy"/> class.
    /// </summary>
    public TransactionRetryPolicy()
    {
        this.NoDelayAttempts = 2;
        this.MaxAttempts = 8;
    }
}