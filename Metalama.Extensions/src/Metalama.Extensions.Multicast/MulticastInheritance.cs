// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using System;

namespace Metalama.Extensions.Multicast;

/// <summary>
/// Specifies how a multicast attribute is inherited along inheritance hierarchies.
/// </summary>
/// <remarks>
/// <para>
/// In Metalama, aspect inheritance is ruled at the class level by the <see cref="InheritableAttribute"/> custom attribute. In Metalama itself, only
/// <c>Strict</c> inheritance is implemented. <see cref="MulticastAspect"/> and <see cref="MulticastImplementation"/> implement an emulation of the <c>Multicast</c>
/// inheritance mode by passing the proper parameter to the constructor.
/// </para>
/// <para>
/// This enum is provided for PostSharp compatibility and is obsolete. Use <see cref="InheritableAttribute"/> on aspect classes instead.
/// </para>
/// </remarks>
/// <seealso cref="InheritableAttribute"/>
/// <seealso cref="IMulticastAttribute"/>
/// <seealso href="@multicast"/>
[Obsolete(
    "Inheritance is Metalama is implemented at the aspect class level with the [Inheritable] attribute, and the difference between Strict and " +
    "Multicast is made by an argument passed to the MulticastImplementation constructor." )]
[RunTimeOrCompileTime]
[PublicAPI]
public enum MulticastInheritance
{
    /// <summary>
    /// No inheritance.
    /// </summary>
    None,

    /// <summary>
    /// This is the default inheritance mode when <see cref="InheritableAttribute"/> is added to an aspect class.
    /// It means that multicasting is performed before inheritance, but not after. There is no need of <see cref="IMulticastAttribute"/> to enable
    /// this kind of inheritance with Metalama.
    /// </summary>
    Strict,

    /// <summary>
    /// This inheritance mode means that multicasting is also performed after inheritance. To enable this inheritance mode with Metalama, the aspect
    /// must implement multicasting using <see cref="MulticastAspect"/> or <see cref="IMulticastAttribute"/> and <see cref="MulticastImplementation"/>,
    /// and must pass the adequate value to the constructor of these classes. 
    /// </summary>
    Multicast
}