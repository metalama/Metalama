// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;

namespace Metalama.Framework.Introspection;

/// <summary>
/// Represents a reference from one declaration to another.
/// </summary>
/// <remarks>
/// <para>
/// To obtain instances of <see cref="IIntrospectionReference"/> from a declaration, use the extension methods
/// <c>Metalama.Framework.Workspaces.DeclarationExtensions.GetInboundReferences</c> and <c>Metalama.Framework.Workspaces.DeclarationExtensions.GetOutboundReferences</c>.
/// </para>
/// </remarks>
/// <seealso cref="IntrospectionReferenceDetail"/>
/// <seealso cref="IProjectIntrospectionService"/>
/// <seealso href="@introspection-api"/>
[PublicAPI]
public interface IIntrospectionReference
{
    /// <summary>
    /// Gets the destination declaration of the reference.
    /// </summary>
    IDeclaration DestinationDeclaration { get; }

    /// <summary>
    /// Gets the origin declaration of the reference.
    /// </summary>
    IDeclaration OriginDeclaration { get; }

    /// <summary>
    /// Gets the kinds of references.
    /// </summary>
    ReferenceKinds Kinds { get; }

    /// <summary>
    /// Gets the details of the reference.
    /// </summary>
    IReadOnlyList<IntrospectionReferenceDetail> Details { get; }
}