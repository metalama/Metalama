// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.RunTime.Initialization;

/// <summary>
/// Extension methods for <see cref="IInitializable"/> types, providing a post-initialization
/// wrapper that returns the original object for use in expression contexts.
/// </summary>
public static class InitializableExtensions
{
    /// <summary>
    /// Calls <see cref="IInitializable.Initialize"/> on the object and returns it.
    /// This method is emitted by the Metalama Linker at call sites of types implementing
    /// <see cref="IInitializable"/>.
    /// </summary>
    /// <typeparam name="T">The type implementing <see cref="IInitializable"/>.</typeparam>
    /// <param name="obj">The object to initialize.</param>
    /// <param name="metadata">Optional metadata describing the initialization context.
    /// Pass <see cref="InitializationMetadata.Modify"/> for <c>with</c> expressions.</param>
    /// <returns>The same object, after <see cref="IInitializable.Initialize"/> has been called.</returns>
    public static T WithInitialize<T>( this T obj, InitializationMetadata? metadata = null )
        where T : IInitializable
    {
        obj.Initialize( InitializationContext.Create( metadata ?? InitializationMetadata.Default ) );

        return obj;
    }
}