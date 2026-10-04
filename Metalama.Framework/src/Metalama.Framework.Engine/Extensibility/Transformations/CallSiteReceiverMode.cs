// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Framework.Engine.Extensibility.Transformations;

/// <summary>
/// Describes how the receiver of the source invocation is passed to the new target.
/// </summary>
[PublicAPI]
public enum CallSiteReceiverMode
{
    /// <summary>
    /// The receiver is not passed as the first argument. Valid when the source method is static, which includes a classic extension method called in
    /// its static form, and when the request passes the receiver at another position with <see cref="RedirectedArgument.SourceReceiver"/>.
    /// </summary>
    Drop,

    /// <summary>
    /// The receiver is passed by value as the first argument. An implicit receiver is written <c>this</c>, and <c>p-&gt;M()</c> gives <c>*p</c>.
    /// </summary>
    FirstArgument,

    /// <summary>
    /// The receiver is passed with <c>ref</c> as the first argument.
    /// </summary>
    FirstArgumentByRef,

    /// <summary>
    /// The receiver is passed with <c>in</c> as the first argument, for a <c>ref readonly</c> or <c>in</c> receiver parameter.
    /// </summary>
    FirstArgumentByIn,

    /// <summary>
    /// The receiver syntax is kept, and the target is invoked as an extension method: <c>r.M(x)</c> becomes <c>r.I(x)</c>, <c>a?.M(x)</c> becomes
    /// <c>a?.I(x)</c>, and an implicit receiver <c>M(x)</c> becomes <c>this.I(x)</c>. This is the mode that keeps the short-circuit of a conditional
    /// access.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The code generator of Metalama writes a call of an extension method in its static form and lets the simplifier reduce it, so that the
    /// generated code does not depend on the namespaces imported at the call site. The other modes follow this rule. This mode is the exception,
    /// because the static form cannot be written in a conditional access: in <c>a?.M(x)</c>, the receiver exists only inside the conditional
    /// access, so <c>I(a, x)</c> would require a second evaluation of <c>a</c> or a temporary variable. Only the extension form <c>a?.I(x)</c>
    /// keeps the evaluation and the short-circuit of the source call.
    /// </para>
    /// <para>
    /// The target is written with its name only, so it must be accessible as an extension method at the call site. The factory binds the rewritten
    /// call at the position of the call site and refuses the request when it does not bind to the target, for instance because the namespace of
    /// the target is not imported.
    /// </para>
    /// </remarks>
    ExtensionReceiver
}
