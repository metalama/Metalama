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
/// Describes a request to replace a source invocation by an invocation of another method.
/// </summary>
[PublicAPI]
public sealed class InvocationRedirectionRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvocationRedirectionRequest"/> class.
    /// </summary>
    /// <param name="callSite">The invocation, which must be a node of a syntax tree of the source compilation.</param>
    /// <param name="target">The method that replaces the call site.</param>
    /// <param name="receiverMode">The way the receiver of the source invocation is passed to the target.</param>
    /// <exception cref="ArgumentNullException"><paramref name="callSite"/> or <paramref name="target"/> is <c>null</c>.</exception>
    public InvocationRedirectionRequest( InvocationExpressionSyntax callSite, CallSiteRedirectionTarget target, CallSiteReceiverMode receiverMode )
    {
        this.CallSite = callSite ?? throw new ArgumentNullException( nameof(callSite) );
        this.Target = target ?? throw new ArgumentNullException( nameof(target) );
        this.ReceiverMode = receiverMode;
    }

    /// <summary>
    /// Gets the invocation, which must be a node of a syntax tree of the source compilation.
    /// </summary>
    public InvocationExpressionSyntax CallSite { get; }

    /// <summary>
    /// Gets the method that replaces the call site.
    /// </summary>
    public CallSiteRedirectionTarget Target { get; }

    /// <summary>
    /// Gets the way the receiver of the source invocation is passed to the target.
    /// </summary>
    public CallSiteReceiverMode ReceiverMode { get; }

    /// <summary>
    /// Gets the explicit type arguments of the target method, or the default value to let the compiler infer them.
    /// </summary>
    public ImmutableArray<IType> TypeArguments { get; init; }

    /// <summary>
    /// Gets arguments appended to the rewritten call as named arguments.
    /// </summary>
    public ImmutableArray<CallSiteExtraArgument> ExtraArguments { get; init; }

    /// <summary>
    /// Gets the complete argument list of the new call, after the receiver when <see cref="ReceiverMode"/> passes it, or the default value when the
    /// new call passes the arguments of the source call site unchanged. <see cref="ExtraArguments"/> are appended in both cases.
    /// </summary>
    public ImmutableArray<RedirectedArgument> Arguments { get; init; }

    /// <summary>
    /// Gets a type to which the new call is cast, when its return type differs from the original one. The caller sets it only when the value of the
    /// call is used. It is not allowed inside a conditional access.
    /// </summary>
    public IType? ResultCast { get; init; }

    /// <summary>
    /// Gets a description of the request, used in the diagnostics of the linker.
    /// </summary>
    public string? Description { get; init; }
}
