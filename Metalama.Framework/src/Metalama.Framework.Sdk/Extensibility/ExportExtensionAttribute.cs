// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Engine.Extensibility;

/// <summary>
/// An assembly-level attribute that exports a Metalama extension type. Extensions are loaded by the Metalama
/// pipeline and can provide additional functionality such as pipeline extensions, design-time features,
/// or project services.
/// </summary>
/// <example>
/// <code>
/// [assembly: ExportExtension(typeof(MyServiceFactory), ExtensionKinds.ServiceFactory)]
/// </code>
/// </example>
/// <seealso cref="ExtensionKinds"/>
/// <seealso cref="Services.IProjectServiceFactory"/>
[AttributeUsage( AttributeTargets.Assembly, AllowMultiple = true )]
public sealed class ExportExtensionAttribute : Attribute
{
    /// <summary>
    /// Gets the type of the extension being exported. The type must have a public parameterless constructor
    /// and must implement the interface or base class required by the specified <see cref="ExtensionKinds"/>.
    /// </summary>
    public Type ExtensionType { get; }

    /// <summary>
    /// Gets the kinds of extension represented by <see cref="ExtensionType"/>. This is a flags enum,
    /// so a single type can serve multiple purposes (e.g., both <see cref="Extensibility.ExtensionKinds.Default"/>
    /// and <see cref="Extensibility.ExtensionKinds.ServiceFactory"/>).
    /// </summary>
    public ExtensionKinds ExtensionKinds { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportExtensionAttribute"/> class.
    /// </summary>
    /// <param name="extensionType">The type of the extension being exported.</param>
    /// <param name="extensionKinds">The kinds of extension represented by <paramref name="extensionType"/>.</param>
    public ExportExtensionAttribute( Type extensionType, ExtensionKinds extensionKinds )
    {
        this.ExtensionType = extensionType;
        this.ExtensionKinds = extensionKinds;
    }
}