// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.RunTime.Initialization;

/// <summary>
/// Marks a method as a post-initialization hook. The method is called after all constructors
/// and object/collection initializers have completed, ensuring that all properties
/// (including <c>init</c>-only and <c>required</c> members) are set before validation
/// or derived value computation runs.
/// </summary>
/// <remarks>
/// <para>
/// The marked method must return the declaring type (or a base type) and accept either
/// no parameters or a single <see cref="InitializationContext"/> parameter with a default value.
/// </para>
/// <para>
/// Multiple <c>[OnInitialized]</c> methods are allowed per type (e.g., from partial classes
/// or source generators). Use <see cref="Order"/> to control execution order.
/// </para>
/// </remarks>
[AttributeUsage( AttributeTargets.Method, Inherited = false, AllowMultiple = false )]
public sealed class OnInitializedAttribute : Attribute
{
    /// <summary>
    /// Controls the execution order when multiple <c>[OnInitialized]</c> methods exist on a type
    /// or across an inheritance hierarchy. Lower values run first. Default is 0.
    /// When two methods have the same <see cref="Order"/>, methods declared on base types run
    /// before methods on derived types. If both are on the same type, they are ordered alphabetically
    /// by method name.
    /// </summary>
    public int Order { get; set; }
}
