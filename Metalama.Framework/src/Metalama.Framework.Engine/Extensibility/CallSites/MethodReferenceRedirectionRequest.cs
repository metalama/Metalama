// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Extensibility.CallSites;

/// <summary>
/// Describes a request to replace a source method group, converted to a delegate or to a function pointer, by a method group of another method.
/// </summary>
[PublicAPI]
public sealed class MethodReferenceRedirectionRequest
{
    /// <param name="methodReference">The method-group expression: a simple name, a generic name or a member access whose operation is an
    /// <c>IMethodReferenceOperation</c> converted to a delegate or to a function pointer.</param>
    /// <param name="target">The method that replaces the method group. It must be static.</param>
    /// <param name="receiverMode">The receiver mode. Only <see cref="CallSiteReceiverMode.Drop"/> is supported: the source method must be static,
    /// and the method group becomes <c>X.I</c>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="receiverMode"/> is <see cref="CallSiteReceiverMode.None"/>.</exception>
    public MethodReferenceRedirectionRequest( ExpressionSyntax methodReference, CallSiteRedirectionTarget target, CallSiteReceiverMode receiverMode )
    {
        if ( receiverMode == CallSiteReceiverMode.None )
        {
            throw new ArgumentOutOfRangeException( nameof(receiverMode), $"The receiver mode {nameof(CallSiteReceiverMode.None)} is not valid." );
        }

        this.MethodReference = methodReference ?? throw new ArgumentNullException( nameof(methodReference) );
        this.Target = target ?? throw new ArgumentNullException( nameof(target) );
        this.ReceiverMode = receiverMode;
    }

    /// <summary>
    /// Gets the method-group expression.
    /// </summary>
    public ExpressionSyntax MethodReference { get; }

    /// <summary>
    /// Gets the method that replaces the method group.
    /// </summary>
    public CallSiteRedirectionTarget Target { get; }

    /// <summary>
    /// Gets the receiver mode.
    /// </summary>
    public CallSiteReceiverMode ReceiverMode { get; }

    /// <summary>
    /// Gets the explicit type arguments of the target method, or the default value to let the compiler infer them.
    /// </summary>
    public ImmutableArray<IType> TypeArguments { get; init; }

    /// <summary>
    /// Gets a description of the request, used in the diagnostics of the linker.
    /// </summary>
    public string? Description { get; init; }
}
