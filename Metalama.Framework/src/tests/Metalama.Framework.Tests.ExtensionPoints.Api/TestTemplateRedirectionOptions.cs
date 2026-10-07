// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.ExtensionPoints;

/// <summary>
/// The options of the <c>TestRedirectCallsToTemplate</c> verb of <see cref="TestExtensionPointsExtensions"/>.
/// </summary>
/// <remarks>
/// The verb declares one method for each source method, whose body is generated from a template of the aspect, and redirects every call site of
/// the source method to it. The declared method has the parameters of the source method, preceded by a parameter named <c>receiver</c> when the
/// source method is an instance method, and <c>meta.Proceed()</c> invokes the source method.
/// </remarks>
[CompileTime]
public sealed class TestTemplateRedirectionOptions
{
    /// <summary>
    /// Gets or sets the placement of the declared method: <c>StaticClass</c> for a static class declared by the extension, <c>Caller</c> for the
    /// type that contains the first call site, or <c>Type:N</c> for the type whose full name is <c>N</c>. The default is <c>StaticClass</c>.
    /// </summary>
    public string Placement { get; set; } = "StaticClass";

    /// <summary>
    /// Gets or sets the name of the static class of the <c>StaticClass</c> placement. A name that contains a dot is the full name of a class in an
    /// existing namespace. The default is <c>MetalamaInterceptors</c>.
    /// </summary>
    public string StaticClassName { get; set; } = "MetalamaInterceptors";

    /// <summary>
    /// Gets or sets a value indicating whether the declared method is an instance method, which is called on <c>this</c> and receives the receiver
    /// of the source call as its first parameter.
    /// </summary>
    public bool IsInstance { get; set; }

    /// <summary>
    /// Gets or sets the name of the declared method, or <c>null</c> for the name of the source method followed by <c>_Interceptor</c>.
    /// </summary>
    public string? NameHint { get; set; }

    /// <summary>
    /// Gets or sets the accessibility of the declared method, as the name of a member of the <c>Accessibility</c> enumeration. The default is
    /// <c>Internal</c>.
    /// </summary>
    public string Accessibility { get; set; } = "Internal";

    /// <summary>
    /// Gets or sets a value indicating whether a separate method is declared for each call site, instead of one method for each source method.
    /// </summary>
    public bool OneMethodPerSite { get; set; }

    /// <summary>
    /// Gets or sets the value of the compile-time template parameter <c>label</c>, or <c>null</c> when the template has no such parameter.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the declared method exposes a <c>TestMetaExtension</c> to the template, whose <c>Description</c>
    /// describes the call site that the method was declared for.
    /// </summary>
    public bool WithMetaExtension { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the signature is set on a builder created with <c>CreateMethodBuilder</c>, with restrictions that
    /// lock the receiver, the parameters of the source method and the return type, and then changed according to <see cref="RenameParameter"/>
    /// and <see cref="ChangeLockedRefKind"/>.
    /// </summary>
    public bool PrebuiltBuilder { get; set; }

    /// <summary>
    /// Gets or sets a rename of a parameter of the pre-built builder, as <c>old=new</c>, or <c>null</c>.
    /// </summary>
    public string? RenameParameter { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the reference kind of the first parameter of the pre-built builder is changed, which the
    /// restrictions refuse.
    /// </summary>
    public bool ChangeLockedRefKind { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the declared method has the type parameters of the source method, and whether the call passes the
    /// type arguments of the call site.
    /// </summary>
    public bool Generic { get; set; }
}
