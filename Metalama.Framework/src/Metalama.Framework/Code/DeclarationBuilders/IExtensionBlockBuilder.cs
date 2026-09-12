// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Utilities;

namespace Metalama.Framework.Code.DeclarationBuilders;

/// <summary>
/// Builder for introducing a new extension block. Inherits from <see cref="INamedTypeBuilder"/>
/// to allow introducing members, but restricts operations that are not valid for extension blocks.
/// </summary>
/// <remarks>
/// <para>Extension blocks have the following restrictions compared to regular named types:</para>
/// <list type="bullet">
///   <item>Cannot have a base type (always throws on <see cref="INamedTypeBuilder.BaseType"/> setter).</item>
///   <item>The <see cref="IMemberOrNamedTypeBuilder.Name"/> property is used internally to generate deterministic file names for the design-time syntax tree. It is automatically assigned if not set.</item>
///   <item>Cannot set accessibility (extension blocks don't have access modifiers).</item>
///   <item>Cannot be abstract, sealed, partial, or closed. The setter of <see cref="INamedTypeBuilder.IsClosed"/> throws a <see cref="System.NotSupportedException"/> for both values, because an extension block is not a class.</item>
///   <item>Cannot contain fields (will fail at advice execution time).</item>
///   <item>Cannot contain auto-properties (will fail at advice execution time).</item>
///   <item>Cannot contain nested types (will fail at advice execution time).</item>
///   <item>Cannot contain constructors (will fail at advice execution time).</item>
/// </list>
/// </remarks>
/// <seealso cref="IExtensionBlock"/>
/// <seealso cref="INamedTypeBuilder"/>
/// <seealso cref="Metalama.Framework.Advising.IAdviceFactory.IntroduceExtensionBlock(Metalama.Framework.Code.INamedType,Metalama.Framework.Code.IType,string,System.Action{Metalama.Framework.Code.DeclarationBuilders.IExtensionBlockBuilder})"/>
/// <seealso cref="Metalama.Framework.Aspects.AdviserExtensions.IntroduceExtensionBlock(Metalama.Framework.Aspects.IAdviser{Metalama.Framework.Code.INamedType},Metalama.Framework.Code.IType,string,System.Action{Metalama.Framework.Code.DeclarationBuilders.IExtensionBlockBuilder})"/>
[InternalImplement]
public interface IExtensionBlockBuilder : INamedTypeBuilder, IExtensionBlock
{
    /// <summary>
    /// Gets the receiver parameter builder. Use this to configure the receiver type and name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// To create a <b>static extension</b> (members appear as static members of the extended type),
    /// set <see cref="IParameterBuilder.Name"/> to <c>null</c> or empty string.
    /// </para>
    /// <para>
    /// To create an <b>instance extension</b> (members appear as instance members of the extended type),
    /// set <see cref="IParameterBuilder.Name"/> to a non-empty string (e.g., "self", "value").
    /// </para>
    /// <para>
    /// The <see cref="IParameterBuilder.Type"/> property specifies the type being extended.
    /// </para>
    /// </remarks>
    new IParameterBuilder ReceiverParameter { get; }
}