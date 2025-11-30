// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Fabrics
{
    /// <summary>
    /// A compile-time entry point implemented as a nested type that executes within the compiler and IDE to add aspects
    /// and implement transformations for its containing type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Type fabrics are unique nested classes that execute at compile time and design time. Unlike aspects, fabrics do not need to be applied
    /// to any declaration or called from anywhere: their primary method (<see cref="AmendType"/>) is invoked automatically simply because
    /// the nested class exists in the parent type.
    /// </para>
    /// <para>
    /// A <see cref="TypeFabric"/> allows you to advise the current type programmatically, functioning as a type-level aspect added to the
    /// target type. This is useful when you want to apply transformations to different members of a type without creating a reusable aspect.
    /// </para>
    /// <para>
    /// For optimal design-time performance and usability, it is recommended to implement type fabrics in a separate file and mark the
    /// containing type as <c>partial</c>.
    /// </para>
    /// <para>
    /// Type fabrics are always executed first, before any aspect. As a result, they can only add advice to members defined in the source code.
    /// If you need to add advice to members introduced by an aspect, use a helper aspect and order it after the aspects that provide
    /// the members you wish to advise.
    /// </para>
    /// </remarks>
    /// <seealso cref="Fabric"/>
    /// <seealso cref="ITypeAmender"/>
    /// <seealso cref="ProjectFabric"/>
    /// <seealso cref="NamespaceFabric"/>
    /// <seealso href="@fabrics"/>
    /// <seealso href="@fabrics-advising"/>
    /// <seealso href="@aspect-configuration"/>
    /// <seealso href="@fabrics-adding-aspects"/>
    [PublicAPI]
    public abstract class TypeFabric : Fabric
    {
        /// <summary>
        /// Implement this method to programmatically advise the containing type, add aspects to its members, configure options, validate architecture, and report or suppress diagnostics.
        /// This method is invoked automatically at compile time and design time. You can also add declarative advice such as member introductions to the type fabric class itself.
        /// </summary>
        /// <param name="amender">An object that provides access to the containing type's members through <see cref="IAspectBuilder.Advice"/>, allowing you to add advice, introduce members, configure options, and report diagnostics.</param>
        /// <seealso href="@fabrics-advising"/>
        /// <seealso href="@advising-code"/>
        /// <seealso href="@introducing-members"/>
        public virtual void AmendType( ITypeAmender amender ) { }
    }
}