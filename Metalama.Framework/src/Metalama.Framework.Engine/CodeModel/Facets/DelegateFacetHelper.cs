// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using System;

namespace Metalama.Framework.Engine.CodeModel.Facets;

/// <summary>
/// Reads the delegate facet of a type at the sites that require the type to be a well-formed delegate.
/// </summary>
/// <remarks>
/// <para>
/// The sites that call this class require the facet instead of testing it, because they run on a declaration that
/// the compiler has accepted, where the type of an event or of a handler is a delegate. The sites that test the
/// facet read <see cref="ITypeFacetCollection.Delegate"/> directly.
/// </para>
/// <para>
/// The exception below is an <see cref="InvalidOperationException"/> and not an assertion, for two reasons. It
/// preserves the exception that these sites raised when they resolved the method by its identifier, through
/// <c>Single</c>. And <see cref="IEvent.Signature"/> is public, so a user aspect reaches it on whatever type the
/// compilation contains, which an assertion would not report in a release build.
/// </para>
/// </remarks>
internal static class DelegateFacetHelper
{
    /// <summary>
    /// Gets the <c>Invoke</c> method of a delegate type, and throws an <see cref="InvalidOperationException"/> when
    /// the type is not a well-formed delegate.
    /// </summary>
    public static IMethod GetInvokeMethod( INamedType delegateType )
        => delegateType.Facets.Delegate?.InvokeMethod
           ?? throw new InvalidOperationException( $"'{delegateType}' is not a well-formed delegate type: it declares no Invoke method." );

    /// <summary>
    /// Gets the <c>Invoke</c> method of the type of an event, which is the implementation of
    /// <see cref="IEvent.Signature"/> that every implementation of <see cref="IEvent"/> calls.
    /// </summary>
    public static IMethod GetSignature( IEvent declaredEvent ) => GetInvokeMethod( declaredEvent.Type );
}
