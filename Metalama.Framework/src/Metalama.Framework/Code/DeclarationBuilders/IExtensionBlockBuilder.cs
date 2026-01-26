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
///   <item>Cannot have a base type (always throws on <see cref="INamedTypeBuilder.BaseType"/> setter)</item>
///   <item>Cannot have a name (identified by receiver type, <see cref="IMemberOrNamedTypeBuilder.Name"/> setter throws)</item>
///   <item>Cannot set accessibility (extension blocks don't have access modifiers)</item>
///   <item>Cannot be abstract, sealed, or partial</item>
///   <item>Cannot contain fields (will fail at advice execution time)</item>
///   <item>Cannot contain auto-properties (will fail at advice execution time)</item>
///   <item>Cannot contain nested types (will fail at advice execution time)</item>
///   <item>Cannot contain constructors (will fail at advice execution time)</item>
/// </list>
/// </remarks>
/// <seealso cref="IExtensionBlock"/>
/// <seealso cref="INamedTypeBuilder"/>
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