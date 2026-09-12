// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;

namespace Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;

/// <summary>
/// The immutable data of an introduced delegate.
/// </summary>
internal sealed class DelegateBuilderData : NamedTypeBuilderData
{
    /// <summary>
    /// Gets the <c>Invoke</c> method of the delegate, which carries its signature.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It is stored here so that the facet can resolve it in any compilation that knows the delegate.
    /// <see cref="INamedType.Methods"/> is empty in a compilation to which the transformation that registers the
    /// method has not been applied, and an aspect that types an event by a delegate it has just introduced reads
    /// the facet in exactly such a compilation.
    /// </para>
    /// </remarks>
    public IFullRef<IMethod> InvokeMethod { get; }

    public DelegateBuilderData( DelegateBuilder builder, IFullRef<IDeclaration> containingDeclaration ) : base( builder, containingDeclaration )
    {
        this.InvokeMethod = builder.InvokeMethodBuilder.BuilderData.ToRef();
    }
}
