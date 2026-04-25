// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;
using System;

namespace Metalama.Framework.Engine.Extensibility;

/// <summary>
/// Specifies the kinds of extensions exported by an assembly using <see cref="ExportExtensionAttribute"/>.
/// This is a flags enum, allowing an extension type to serve multiple purposes.
/// </summary>
/// <seealso cref="ExportExtensionAttribute"/>
/// <seealso cref="Services.IProjectServiceFactory"/>
[Flags]
public enum ExtensionKinds
{
    /// <summary>
    /// No extension kind specified.
    /// </summary>
    [PublicAPI]
    None = 0,

    /// <summary>
    /// A compile-time pipeline extension. The extension type must derive from <c>PipelineExtension</c>.
    /// For internal use only.
    /// </summary>
    Default = 1,

    /// <summary>
    /// A design-time extension. The extension type must implement <c>IDesignTimeExtension</c>.
    /// For internal use only.
    /// </summary>
    DesignTime = 2,

    /// <summary>
    /// A project service factory. The extension type must implement <see cref="IProjectServiceFactory"/>,
    /// which creates <see cref="IProjectService"/> instances that are added to the project's service provider.
    /// Use this to provide services that can be consumed by aspects at compile time.
    /// </summary>
    ServiceFactory = 4
}