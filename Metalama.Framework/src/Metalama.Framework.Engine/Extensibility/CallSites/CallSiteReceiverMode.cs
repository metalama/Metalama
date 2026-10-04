// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Framework.Engine.Extensibility.CallSites;

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
    /// <remarks>
    /// In a conditional access, <c>a?.M(x)</c>, the receiver exists only inside the conditional access, so the call cannot be written in the static
    /// form. The call is written <c>a?.F(x)</c>, where <c>F</c> is a forwarder: an extension method that the linker generates in an internal static
    /// class of the global namespace, and that calls the target with its fully qualified name. The receiver must convert to the first parameter of
    /// the target by an identity, reference or boxing conversion, and the target must be accessible from a top-level class of the project.
    /// </remarks>
    FirstArgument,

    /// <summary>
    /// The receiver is passed with <c>ref</c> as the first argument.
    /// </summary>
    FirstArgumentByRef,

    /// <summary>
    /// The receiver is passed with <c>in</c> as the first argument, for a <c>ref readonly</c> or <c>in</c> receiver parameter.
    /// </summary>
    FirstArgumentByIn
}
