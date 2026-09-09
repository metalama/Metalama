// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Code.Types;

/// <summary>
/// Represents the structure of a delegate: its <c>Invoke</c> method, which carries its signature.
/// </summary>
/// <remarks>
/// <para>
/// The facet is reached through <see cref="ITypeFacetCollection.Delegate"/>, as in
/// <c>type.Facets.Delegate?.ReturnType</c>. It is <c>null</c> for a type that
/// <see cref="INamedType.IsDelegate"/> reports as not being a delegate, which includes
/// <see cref="System.Delegate"/> and <see cref="System.MulticastDelegate"/> themselves.
/// </para>
/// <para>
/// The <c>BeginInvoke</c> and <c>EndInvoke</c> methods are not exposed. They exist only for a delegate compiled for
/// .NET Framework, and they are the asynchronous pattern that preceded <c>async</c>.
/// </para>
/// </remarks>
/// <seealso cref="INamedType.IsDelegate"/>
/// <seealso cref="INamedType.Facets"/>
[CompileTime]
[InternalImplement]
public interface IDelegateFacet : ITypeFacet
{
    /// <summary>
    /// Gets the <c>Invoke</c> method, which carries the signature of the delegate.
    /// </summary>
    IMethod InvokeMethod { get; }

    /// <summary>
    /// Gets the return type of the delegate, which is the return type of <see cref="InvokeMethod"/>.
    /// </summary>
    IType ReturnType { get; }

    /// <summary>
    /// Gets the parameters of the delegate, which are the parameters of <see cref="InvokeMethod"/>.
    /// </summary>
    IParameterList Parameters { get; }
}
