// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.RunTime.Initialization;

/// <summary>
/// Base class for metadata attached to an <see cref="InitializationContext"/>.
/// Subclass to carry extension-specific context (e.g., deserialization framework info).
/// Instances should be singletons where possible to avoid allocation.
/// </summary>
public class InitializationMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InitializationMetadata"/> class.
    /// </summary>
    protected InitializationMetadata() { }

    /// <summary>
    /// Normal construction. This is the implicit metadata when
    /// <see cref="InitializationContext.Metadata"/> is <c>null</c>.
    /// </summary>
    public static InitializationMetadata Default { get; } = new();

    /// <summary>
    /// The object was created via a <c>with</c> expression or clone operation.
    /// <see cref="IInitializable.Initialize"/> should revalidate invariants and reinitialize derived state.
    /// </summary>
    public static InitializationMetadata Modify { get; } = new();

}
