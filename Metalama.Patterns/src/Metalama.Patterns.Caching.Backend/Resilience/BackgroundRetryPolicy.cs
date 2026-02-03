// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Caching.Resilience;

/// <summary>
/// A <see cref="RetryPolicy"/> specialized in handling background operations. The <see cref="RetryPolicy.BaseDelay"/> property is set to 0.25 s.
/// </summary>
[PublicAPI]
public class BackgroundRetryPolicy : RetryPolicy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundRetryPolicy"/> class.
    /// </summary>
    public BackgroundRetryPolicy()
    {
        this.BaseDelay = TimeSpan.FromMilliseconds( 250 );
    }
}