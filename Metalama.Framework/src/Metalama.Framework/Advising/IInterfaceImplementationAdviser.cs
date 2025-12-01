// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Advising;

/// <summary>
/// Provides methods to introduce explicit interface member implementations.
/// </summary>
/// <remarks>
/// <para>
/// This interface is obtained from the <see cref="IImplementInterfaceAdviceResult.ExplicitMembers"/> property or
/// <see cref="IInterfaceImplementationResult.ExplicitMembers"/> property after calling
/// <see cref="AdviserExtensions.ImplementInterface(IAdviser{INamedType}, INamedType, OverrideStrategy, object?)"/>.
/// </para>
/// <para>
/// Use the extension methods in <see cref="InterfaceImplementationAdviserExtensions"/> to introduce explicit
/// interface member implementations such as methods, properties, events, and indexers.
/// </para>
/// </remarks>
/// <seealso cref="InterfaceImplementationAdviserExtensions"/>
/// <seealso cref="IImplementInterfaceAdviceResult"/>
/// <seealso cref="IInterfaceImplementationResult"/>
/// <seealso cref="AdviserExtensions.ImplementInterface(IAdviser{INamedType}, INamedType, OverrideStrategy, object?)"/>
/// <seealso href="@implementing-interfaces"/>
[CompileTime]
public interface IInterfaceImplementationAdviser
{
    /// <summary>
    /// Gets the target type that is implementing the interface.
    /// </summary>
    INamedType Target { get; }
}