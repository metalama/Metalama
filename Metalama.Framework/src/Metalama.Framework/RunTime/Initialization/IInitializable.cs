// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.RunTime.Initialization;

/// <summary>
/// Marks a type as having a post-initialization hook. The <see cref="Initialize"/> method
/// is called after all constructors and object/collection initializers have completed,
/// ensuring that all properties (including <c>init</c>-only and <c>required</c> members)
/// are set before validation or derived value computation runs.
/// </summary>
/// <remarks>
/// <para>
/// The Metalama Linker automatically rewrites call sites (<c>new T()</c>, <c>new T { ... }</c>,
/// <c>with { ... }</c>) to invoke <see cref="InitializableExtensions.WithInitialize{T}"/> after construction.
/// </para>
/// <para>
/// Implementing classes should declare <see cref="Initialize"/> as <c>public virtual</c>
/// (or <c>override</c>) on non-sealed classes to allow derived types to extend initialization behavior.
/// </para>
/// </remarks>
public interface IInitializable
{
    /// <summary>
    /// Called after all constructors and object/collection initializers have completed.
    /// </summary>
    /// <param name="context">
    /// The initialization context carrying caller intent, aspect behavior slots, and optional metadata.
    /// </param>
    void Initialize( InitializationContext context = default );
}