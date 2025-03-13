// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;

namespace Metalama.Patterns.Caching.Implementation;

/// <summary>
/// An abstract implementation of <see cref="CachingBackendEnhancerFeatures"/> for use in implementations of <see cref="CachingBackendEnhancer"/>,
/// where the default behavior is to return the features of the underlying <see cref="CachingBackend"/>.
/// </summary>
internal abstract class CachingBackendEnhancerFeatures : CachingBackendFeatures
{
    /// <summary>
    /// Gets the feature of the underlying <see cref="CachingBackend"/>.
    /// </summary>
    protected CachingBackendFeatures UnderlyingBackendFeatures { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingBackendEnhancerFeatures"/> class.
    /// Initializes a new <see cref="CachingBackendEnhancerFeatures"/>.
    /// </summary>
    /// <param name="underlyingBackendFeatures">The feature of the underlying <see cref="CachingBackend"/>.</param>
    protected CachingBackendEnhancerFeatures( CachingBackendFeatures underlyingBackendFeatures )
    {
        this.UnderlyingBackendFeatures = underlyingBackendFeatures;
    }

    /// <inheritdoc />
    public override bool Clear => this.UnderlyingBackendFeatures.Clear;

    /// <inheritdoc />  
    public override bool Events => this.UnderlyingBackendFeatures.Events;

    /// <inheritdoc />
    public override bool Blocking => this.UnderlyingBackendFeatures.Blocking;

    /// <inheritdoc />
    public override bool Dependencies => this.UnderlyingBackendFeatures.Dependencies;

    /// <inheritdoc />
    public override bool ContainsDependency => this.UnderlyingBackendFeatures.ContainsDependency;
}