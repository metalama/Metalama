// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.ExtensionPoints;

/// <summary>
/// The options of the <c>TestRedirectCalls</c> verbs of <see cref="TestExtensionPointsExtensions"/>.
/// </summary>
/// <remarks>
/// The options are strings because this assembly does not reference the engine, which defines the types of the request.
/// </remarks>
[CompileTime]
public sealed class TestRedirectionOptions
{
    /// <summary>
    /// Gets or sets the name of a member of the <c>CallSiteReceiverMode</c> enumeration of the engine. The default is <c>Drop</c>.
    /// </summary>
    public string ReceiverMode { get; set; } = "Drop";

    /// <summary>
    /// Gets or sets a value indicating whether the method groups are redirected instead of the calls.
    /// </summary>
    public bool MethodReferences { get; set; }

    /// <summary>
    /// Gets or sets the argument list of the new call, or <c>null</c> to keep the arguments of the source call site. The items are separated by
    /// semicolons. An item is <c>receiver</c>, <c>argument:N</c> for the argument of the parameter <c>N</c> of the source method,
    /// <c>value:E</c> for the C# expression <c>E</c>, or <c>parameter:N</c> for the parameter <c>N</c> of the member that contains the call site,
    /// passed as an <c>IParameter</c> of the code model. An item can be prefixed by <c>name=</c> to give the parameter name explicitly, and it can be
    /// followed by <c> as T</c> to cast the argument to the type whose reflection name is <c>T</c>.
    /// </summary>
    public string? Arguments { get; set; }

    /// <summary>
    /// Gets or sets the extra named arguments of the new call, as <c>name=E</c> items separated by semicolons, where <c>E</c> is a C# expression.
    /// </summary>
    public string? ExtraArguments { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the new call is cast to the return type of the source method.
    /// </summary>
    public bool CastResult { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the type arguments of the source method are written explicitly in the new call.
    /// </summary>
    public bool ExplicitTypeArguments { get; set; }
}
