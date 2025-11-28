// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace Metalama.Framework.Introspection;

/// <summary>
/// Provides detailed information about a specific occurrence of a reference between declarations, including
/// the exact source location and the kind of reference.
/// </summary>
/// <remarks>
/// <para>
/// A single <see cref="IIntrospectionReference"/> can have multiple details when the same origin declaration
/// references the same destination declaration in multiple locations or in different ways. Access the details
/// through the <see cref="IIntrospectionReference.Details"/> property.
/// </para>
/// <para>
/// To navigate to reference details from a declaration, use the extension methods
/// <c>Metalama.Framework.Workspaces.DeclarationExtensions.GetInboundReferences</c> or <c>Metalama.Framework.Workspaces.DeclarationExtensions.GetOutboundReferences</c>
/// to obtain <see cref="IIntrospectionReference"/> instances, then access their <see cref="IIntrospectionReference.Details"/> property.
/// </para>
/// </remarks>
/// <seealso cref="IIntrospectionReference"/>
/// <seealso cref="ReferenceKinds"/>
/// <seealso href="@introspection-api"/>
public readonly struct IntrospectionReferenceDetail : IDiagnosticLocation
{
    /// <summary>
    /// Gets the reference.
    /// </summary>
    public IIntrospectionReference Reference { get; }

    /// <summary>
    /// Gets the kinds of reference.
    /// </summary>
    public ReferenceKinds Kinds { get; }

    /// <summary>
    /// Gets the source location of the reference.
    /// </summary>
    public SourceReference Source { get; }

    internal IntrospectionReferenceDetail( IIntrospectionReference reference, ReferenceKinds kinds, SourceReference source )
    {
        this.Reference = reference;
        this.Kinds = kinds;
        this.Source = source;
    }
}